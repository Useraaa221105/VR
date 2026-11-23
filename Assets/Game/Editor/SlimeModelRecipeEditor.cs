using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Podz.EditorTools
{
    /// <summary>Один слайм из UpperCaveCreatures. Не открывает Main и не вызывает Monster.Init в редакторе.</summary>
    [CustomEditor(typeof(SlimeModelRecipe))]
    public class SlimeModelRecipeEditor : Editor
    {
        const string SlimeFolder = "Assets/Game/Dungeon/Floor01_UpperCaves/Monsters/Slime";

        [MenuItem("Podz/Префабы/Слайм — создать параметры")]
        static void CreateRecipe()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string path = EditorUtility.SaveFilePanelInProject("Параметры слайма", "SlimeModel", "asset",
                "Сохрани параметры. Затем выбери asset в Project и используй Inspector.", SlimeFolder);
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            { EditorUtility.DisplayDialog("Слайм", "Этот asset уже существует. Выбери другое имя.", "OK"); return; }
            var recipe = CreateInstance<SlimeModelRecipe>();
            AssetDatabase.CreateAsset(recipe, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = recipe;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var recipe = (SlimeModelRecipe)target;
            EditorGUILayout.HelpBox("Только существующий желейный купол: размеры, материалы, прежняя анимация или без неё. " +
                "Каждая генерация сохраняет новый вариант рядом с параметрами. Старый префаб и ручные правки не перезаписываются.", MessageType.Info);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Последний сохранённый", recipe.LastPrefab, typeof(GameObject), false);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Создать модель и сохранить НОВЫЙ префаб"))
                {
                    try { Generate(recipe); }
                    catch (Exception ex) { Debug.LogException(ex); EditorUtility.DisplayDialog("Слайм: ошибка", ex.Message, "OK"); }
                }
                using (new EditorGUI.DisabledScope(recipe.LastPrefab == null))
                {
                    if (GUILayout.Button("Открыть последний префаб")) AssetDatabase.OpenAsset(recipe.LastPrefab);
                    if (GUILayout.Button("Проверить сохранённые ссылки"))
                    {
                        try { Validate(AssetDatabase.GetAssetPath(recipe.LastPrefab)); }
                        catch (Exception ex) { Debug.LogException(ex); EditorUtility.DisplayDialog("Слайм: проверка", ex.Message, "OK"); }
                    }
                }
            }
        }

        static void CheckSize(Vector3 size, string field)
        {
            // Ограничение только инструмента авторинга, не новая игровая характеристика.
            foreach (float value in new[] { size.x, size.y, size.z })
                if (float.IsNaN(value) || float.IsInfinity(value) || value < .05f || value > 3f)
                    throw new InvalidOperationException(field + ": каждое измерение должно быть от 0.05 до 3 метров.");
        }

        internal static void Generate(SlimeModelRecipe recipe)
        {
            CheckSize(recipe.VisualSize, "Visual Size"); CheckSize(recipe.HitboxSize, "Hitbox Size");
            string recipePath = AssetDatabase.GetAssetPath(recipe);
            if (string.IsNullOrEmpty(recipePath) || !recipePath.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidOperationException("Сначала сохрани параметры как asset внутри Assets.");
            foreach (var asset in new Object[] { recipe.JellyMaterial, recipe.DetailMaterial, recipe.StunSound })
                if (asset != null && !EditorUtility.IsPersistent(asset))
                    throw new InvalidOperationException("Материалы и звук должны быть сохранёнными ресурсами проекта.");
            foreach (var material in new[] { recipe.JellyMaterial, recipe.DetailMaterial })
                if (material != null && (!material.HasProperty("_BaseColor") || !material.HasProperty("_EmissionColor")))
                    throw new InvalidOperationException("Для существующих сигналов состояния нужен материал с _BaseColor и _EmissionColor (например URP/Lit).");
            if (recipe.LastPrefab != null && !EditorUtility.DisplayDialog("Новый вариант слайма",
                "Будет создана новая папка с моделью, мешами и материалами. Ручные изменения старого префаба НЕ переносятся. " +
                "Старый вариант и его ссылки останутся нетронутыми.", "Создать новый вариант", "Отмена")) return;

            string parent = Path.GetDirectoryName(recipePath).Replace('\\', '/');
            string folder = AssetDatabase.GenerateUniqueAssetPath(parent + "/" + Path.GetFileNameWithoutExtension(recipePath) + "_Generated");
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(folder))))
                throw new IOException("Не удалось создать папку ресурсов: " + folder);
            var previewScene = EditorSceneManager.NewPreviewScene();
            GameObject root = null;
            GameObject previousPrefab = recipe.LastPrefab;
            try
            {
                // PreviewScene не трогает активную сцену/Prefab Mode пользователя.
                root = new GameObject("Slime");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, previewScene);
                var slime = root.AddComponent<Slime>();
                var body = Prim.Cube("Body", root.transform, Vector3.up * recipe.HitboxSize.y * .5f,
                    recipe.HitboxSize, new Color(.3f, .9f, .35f));
                var hitbox = body.GetComponent<BoxCollider>();
                HitZone.Add(body, slime, Zone.Body);
                var rigidbody = root.AddComponent<Rigidbody>(); rigidbody.isKinematic = true; rigidbody.useGravity = false;
                var animation = root.AddComponent<MonsterAnim>(); animation.Owner = slime;
                // Тот же построитель первого этажа: купол, внутренние пузырьки, лицо, блики.
                UpperCaveCreatures.Slime(slime, animation);
                animation.Squash.localScale = new Vector3(recipe.VisualSize.x / .58f, recipe.VisualSize.y / .42f, recipe.VisualSize.z / .58f);
                animation.enabled = recipe.Animation == SlimeModelRecipe.Motion.ExistingSquash;
                foreach (var renderer in animation.Hidden) if (renderer != null) renderer.enabled = false;
                var audio = root.AddComponent<AudioSource>();
                audio.playOnAwake = false; audio.loop = false; audio.spatialBlend = 1f;
                audio.minDistance = 1.5f; audio.maxDistance = 25f;
                var serialized = new SerializedObject(slime);
                serialized.FindProperty("_savedBody").objectReferenceValue = hitbox;
                serialized.FindProperty("_savedVisual").objectReferenceValue = animation.Squash;
                serialized.FindProperty("_stunAudio").objectReferenceValue = audio;
                serialized.FindProperty("_stunSound").objectReferenceValue = recipe.StunSound;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                SaveMeshes(root, folder);
                SaveMaterials(root, recipe, folder);
                string prefabPath = folder + "/Slime.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
                if (!success || prefab == null) throw new IOException("Unity не сохранила префаб: " + prefabPath);
                AssetDatabase.SaveAssets();
                Validate(prefabPath);
                Undo.RecordObject(recipe, "Последний префаб слайма");
                recipe.LastPrefab = prefab;
                EditorUtility.SetDirty(recipe); AssetDatabase.SaveAssets();
                EditorGUIUtility.PingObject(prefab);
                Debug.Log("[SlimePrefab] Сохранён НОВЫЙ вариант: " + prefabPath +
                    ". Проверка ссылок не является компиляцией/VR-проверкой. Закрой и заново открой Unity, затем повтори проверку.", prefab);
            }
            catch
            {
                // Только свежая папка этого вызова. Никаких Delete старых ресурсов/сцены/чужого результата.
                recipe.LastPrefab = previousPrefab;
                EditorUtility.SetDirty(recipe);
                AssetDatabase.DeleteAsset(folder);
                throw;
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
        }

        static void SaveMeshes(GameObject root, string folder) => SavedModelResources.SaveMeshes(root, folder);

        static void SaveMaterials(GameObject root, SlimeModelRecipe recipe, string folder)
            => SavedModelResources.SaveMaterials(root, folder,
                renderer => renderer.name == "TranslucentGroundedSlime" ? recipe.JellyMaterial : recipe.DetailMaterial);

        static void Validate(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Сначала создай префаб.");
            GameObject loaded = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var slime = loaded.GetComponent<Slime>();
                if (slime == null || !slime.HasSavedModel || loaded.GetComponent<Rigidbody>() == null)
                    throw new InvalidOperationException("Не сохранены поведение, модель, коллайдер или Rigidbody.");
                var zones = loaded.GetComponentsInChildren<HitZone>(true);
                if (zones.Length != 1 || zones[0].Owner != slime || zones[0].Zone != Zone.Body || zones[0].GetComponent<Collider>() == null)
                    throw new InvalidOperationException("Неверно сохранена зона Body/Owner.");
                var animation = loaded.GetComponent<MonsterAnim>();
                if (animation == null || animation.Owner != slime || animation.Squash == null || animation.Hidden.Count != 1)
                    throw new InvalidOperationException("Не сохранены ссылки существующей анимации.");
                foreach (var transform in loaded.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                        throw new InvalidOperationException("Missing Script: " + transform.name);
                int meshes = 0, materials = 0;
                foreach (var filter in loaded.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (filter.sharedMesh == null || !EditorUtility.IsPersistent(filter.sharedMesh))
                        throw new InvalidOperationException("Временный/потерянный Mesh: " + filter.name);
                    meshes++;
                }
                foreach (var renderer in loaded.GetComponentsInChildren<MeshRenderer>(true))
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material == null || !EditorUtility.IsPersistent(material))
                            throw new InvalidOperationException("Временный/потерянный Material: " + renderer.name);
                        if (material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                            throw new InvalidOperationException("Отсутствует рабочий шейдер: " + renderer.name);
                        materials++;
                    }
                foreach (var dependency in EditorUtility.CollectDependencies(new Object[] { loaded }))
                    if ((dependency is Mesh || dependency is Material || dependency is Texture || dependency is AudioClip) &&
                        !EditorUtility.IsPersistent(dependency))
                        throw new InvalidOperationException("Временная зависимость: " + dependency.name);
                Debug.Log($"[SlimePrefab] Ссылки сохранённого prefab прочитаны: {path}; MeshFilter={meshes}, Material slots={materials}. " +
                    "Это проверка ресурсов в Editor, не прохождение охоты и не проверка шлема.");
            }
            finally { PrefabUtility.UnloadPrefabContents(loaded); }
        }
    }
}
