using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Podz.EditorTools
{
    /// <summary>Single resource-writing path shared by slime and the next monster group.</summary>
    internal static class SavedModelResources
    {
        public static void SaveMeshes(GameObject root, string folder)
        {
            var owned = new HashSet<Mesh>();
            foreach(var owner in root.GetComponentsInChildren<ArchitectureMeshOwner>(true))
                if(owner.Mesh!=null && !EditorUtility.IsPersistent(owner.Mesh))owned.Add(owner.Mesh);
            var saved = new Dictionary<Mesh, Mesh>(); int index = 0;
            Func<Mesh, Mesh> save = source =>
            {
                if (source == null) throw new InvalidOperationException("У детали нет Mesh.");
                if (EditorUtility.IsPersistent(source)) return source; // built-in/persistent shared meshes are valid references, not private duplicates
                if (!saved.TryGetValue(source, out var mesh))
                {
                    mesh = Object.Instantiate(source); mesh.name = source.name;
                    AssetDatabase.CreateAsset(mesh, folder + "/Mesh_" + index++ + ".asset"); saved.Add(source, mesh);
                }
                return mesh;
            };
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true)) filter.sharedMesh = save(filter.sharedMesh);
            foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true)) collider.sharedMesh = save(collider.sharedMesh);
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) throw new InvalidOperationException("Missing Script before mesh bake.");
                var serialized = new SerializedObject(component); var property = serialized.GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is Mesh mesh)
                        property.objectReferenceValue = saved.TryGetValue(mesh, out var persistent) ? persistent :
                            EditorUtility.IsPersistent(mesh) ? mesh : save(mesh);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var owner in root.GetComponentsInChildren<ArchitectureMeshOwner>(true)) owner.MarkSavedResource();
            var motion = root.GetComponent<DragonReferenceMotion>();
            if (motion != null) motion.MarkSavedNeck();
            foreach(var source in owned)if(source!=null)Object.DestroyImmediate(source);
        }

        static Color MaterialColor(Material material) => material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") :
            material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;

        public static void SaveMaterials(GameObject root, string folder, Func<MeshRenderer, Material> replacement)
        {
            var saved = new Dictionary<(Material, Color, Color), Material>();
            var textures = new Dictionary<Texture, Texture>();
            var block = new MaterialPropertyBlock();
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var supplied = replacement != null ? replacement(renderer) : null;
                if (supplied != null && EditorUtility.IsPersistent(supplied)) {
                    renderer.sharedMaterial = supplied; renderer.SetPropertyBlock(null); continue;
                }
                var sources = renderer.sharedMaterials;
                if (sources.Length != 1) throw new InvalidOperationException("Ожидался исходный однослотовый MeshRenderer: " + renderer.name);
                var source = supplied != null ? supplied : sources[0];
                if (source == null) throw new InvalidOperationException("Нет материала у " + renderer.name);
                block.Clear(); renderer.GetPropertyBlock(block);
                Color color = supplied != null ? MaterialColor(source) : block.HasColor("_BaseColor") ? block.GetColor("_BaseColor") :
                    block.HasColor("_Color") ? block.GetColor("_Color") : MaterialColor(source);
                Color emission = supplied == null && block.HasColor("_EmissionColor") ? block.GetColor("_EmissionColor") :
                    source.HasProperty("_EmissionColor") ? source.GetColor("_EmissionColor") : Color.black;
                var key = (source, color, emission);
                if (!saved.TryGetValue(key, out var material))
                {
                    material = new Material(source); material.EnableKeyword("_EMISSION");
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                    if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                    if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", emission);
                    // Mandrake bark uses a procedural Texture2D. Cloning only its Material is not enough.
                    foreach (string property in material.GetTexturePropertyNames())
                    {
                        var texture = material.GetTexture(property);
                        if (texture == null || EditorUtility.IsPersistent(texture)) continue;
                        if (!textures.TryGetValue(texture, out var persistent))
                        {
                            persistent = SavedCommonResources.SaveTexture(texture);
                            textures.Add(texture, persistent);
                        }
                        material.SetTexture(property, persistent);
                    }
                    material = SavedCommonResources.SaveMaterial(material); saved.Add(key, material);
                }
                renderer.sharedMaterial = material; renderer.SetPropertyBlock(null);
            }
        }

        public static void Validate(GameObject root)
        {
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                    throw new InvalidOperationException("Missing Script: " + transform.name);
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true)) RequirePersistent(filter.sharedMesh, filter.name);
            foreach (var collider in root.GetComponentsInChildren<MeshCollider>(true)) RequirePersistent(collider.sharedMesh, collider.name);
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                foreach (var material in renderer.sharedMaterials)
                {
                    RequirePersistent(material, renderer.name);
                    if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                        throw new InvalidOperationException("Нет рабочего шейдера: " + renderer.name);
                    foreach (var property in material.GetTexturePropertyNames())
                    {
                        var texture = material.GetTexture(property);
                        if (texture != null) RequirePersistent(texture, renderer.name + ":" + property);
                    }
                }
            foreach (var dependency in EditorUtility.CollectDependencies(new Object[] { root }))
                if (dependency is Mesh || dependency is Material || dependency is Texture || dependency is AudioClip)
                    RequirePersistent(dependency, dependency.name);
        }
        static void RequirePersistent(Object value, string context)
        {
            if (value == null || !EditorUtility.IsPersistent(value))
                throw new InvalidOperationException("Временный/потерянный ресурс: " + context);
        }
    }
}
