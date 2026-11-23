using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Podz.EditorTools
{
    [CustomEditor(typeof(MonsterModelRecipe))]
    public class MonsterModelRecipeEditor : Editor
    {
        const string Root = "Assets/Game/Dungeon/";
        [MenuItem("Podz/Префабы/Гриб — создать параметры")]
        static void MushroomRecipe() => CreateRecipe(MonsterModelRecipe.Species.Mushroom);
        [MenuItem("Podz/Префабы/Мандрагора — создать параметры")]
        static void MandrakeRecipe() => CreateRecipe(MonsterModelRecipe.Species.Mandrake);
        [MenuItem("Podz/Префабы/Василиск — создать параметры")]
        static void BasiliskRecipe() => CreateRecipe(MonsterModelRecipe.Species.Basilisk);

        [MenuItem("Podz/Префабы/Доспехи — создать параметры")]
        static void ArmorRecipe() => CreateRecipe(MonsterModelRecipe.Species.LivingArmor);
        [MenuItem("Podz/Префабы/Мимик — создать параметры")]
        static void MimicRecipe() => CreateRecipe(MonsterModelRecipe.Species.Mimic);
        [MenuItem("Podz/Префабы/Дракон — создать параметры")]
        static void DragonRecipe() => CreateRecipe(MonsterModelRecipe.Species.Dragon);
        static bool FixedScale(MonsterModelRecipe.Species s) => s == MonsterModelRecipe.Species.Basilisk ||
            s == MonsterModelRecipe.Species.LivingArmor || s == MonsterModelRecipe.Species.Mimic || s == MonsterModelRecipe.Species.Dragon;

        static string MonsterFolder(MonsterModelRecipe.Species species) => Root +
            (species == MonsterModelRecipe.Species.Mushroom ? "Floor01_UpperCaves" :
                species == MonsterModelRecipe.Species.LivingArmor || species == MonsterModelRecipe.Species.Mimic ? "Floor03_Fortress" :
                species == MonsterModelRecipe.Species.Dragon ? "Floor04_DragonLair" : "Floor02_OvergrownRuins") + "/Monsters/" + species;

        static void CreateRecipe(MonsterModelRecipe.Species species)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string folder = MonsterFolder(species);
            string path = EditorUtility.SaveFilePanelInProject("Параметры " + species, species + "Model", "asset",
                "Выбери asset в Project и используй Inspector.", folder);
            if (string.IsNullOrEmpty(path)) return;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            { EditorUtility.DisplayDialog("Параметры", "Выбери новое имя: существующие assets не перезаписываются.", "OK"); return; }
            var recipe = CreateInstance<MonsterModelRecipe>(); recipe.Monster = species;
            AssetDatabase.CreateAsset(recipe, path); AssetDatabase.SaveAssets(); Selection.activeObject = recipe;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Monster"));
            if (!FixedScale((MonsterModelRecipe.Species)serializedObject.FindProperty("Monster").enumValueIndex))
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ModelScale"));
            else EditorGUILayout.HelpBox("Этот тип: только исходный Scale=1. Зоны, мировые пороги боя и точки логова не масштабируются произвольно.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Animation"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Materials"), true);
            if ((MonsterModelRecipe.Species)serializedObject.FindProperty("Monster").enumValueIndex == MonsterModelRecipe.Species.Mandrake)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LeafGatherStrength"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("LeafResponse"));
            }
            serializedObject.ApplyModifiedProperties();
            var recipe = (MonsterModelRecipe)target;
            EditorGUILayout.HelpBox("Существующая модель, без нового AI. Scale=1 и пустые Materials сохраняют исходные размеры/цвета. " +
                "Новая папка на каждый запуск; основной спавнер не переключается. Настоящий хват листьев не заменяется просмотром в Editor.", MessageType.Info);
            using (new EditorGUI.DisabledScope(true)) EditorGUILayout.ObjectField("Последний сохранённый", recipe.LastPrefab, typeof(GameObject), false);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Создать модель и сохранить НОВЫЙ префаб")) Run(() => Generate(recipe));
                using (new EditorGUI.DisabledScope(recipe.LastPrefab == null))
                {
                    if (GUILayout.Button("Открыть последний префаб")) AssetDatabase.OpenAsset(recipe.LastPrefab);
                    if (GUILayout.Button("Проверить сохранённые ссылки")) Run(() => Validate(AssetDatabase.GetAssetPath(recipe.LastPrefab)));
                }
            }
        }
        static void Run(Action action)
        {
            try { action(); }
            catch (Exception ex) { Debug.LogException(ex); EditorUtility.DisplayDialog("Префаб: ошибка", ex.Message, "OK"); }
        }

        internal static void Generate(MonsterModelRecipe recipe)
        {
            if (!Enum.IsDefined(typeof(MonsterModelRecipe.Species), recipe.Monster) ||
                float.IsNaN(recipe.ModelScale) || float.IsInfinity(recipe.ModelScale) || recipe.ModelScale < .75f || recipe.ModelScale > 1.25f)
                throw new InvalidOperationException("Недопустимый тип или Scale: диапазон 0.75–1.25, исходный размер = 1.");
            if (float.IsNaN(recipe.LeafGatherStrength) || float.IsInfinity(recipe.LeafGatherStrength) || recipe.LeafGatherStrength < 0 || recipe.LeafGatherStrength > 1 ||
                float.IsNaN(recipe.LeafResponse) || float.IsInfinity(recipe.LeafResponse) || recipe.LeafResponse < 1 || recipe.LeafResponse > 30)
                throw new InvalidOperationException("Leaf Gather: 0–1; Leaf Response: 1–30 в секунду.");
            if (FixedScale(recipe.Monster) && recipe.ModelScale != 1f)
                throw new InvalidOperationException("Для выбранного типа поддержан только ModelScale=1. Верни 1 до смены типа рецепта.");
            string recipePath = AssetDatabase.GetAssetPath(recipe);
            if (string.IsNullOrEmpty(recipePath) || !recipePath.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidOperationException("Сначала сохрани параметры в Assets.");
            var replacements = new Dictionary<string, Material>(StringComparer.Ordinal);
            foreach (var item in recipe.Materials ?? new MonsterModelRecipe.MaterialOverride[0])
            {
                if (item == null || string.IsNullOrWhiteSpace(item.RendererName) || item.Material == null || !EditorUtility.IsPersistent(item.Material))
                    throw new InvalidOperationException("В каждой замене нужны точное имя MeshRenderer и сохранённый Material.");
                if (!item.Material.HasProperty("_BaseColor") || !item.Material.HasProperty("_EmissionColor"))
                    throw new InvalidOperationException("Материал должен поддерживать _BaseColor/_EmissionColor, например URP/Lit.");
                if (replacements.ContainsKey(item.RendererName)) throw new InvalidOperationException("Повтор имени: " + item.RendererName);
                replacements.Add(item.RendererName, item.Material);
            }
            if (recipe.LastPrefab != null && !EditorUtility.DisplayDialog("Новый вариант",
                "Создать новый вариант? Старые ресурсы и ручные изменения сохранятся, но не будут скопированы в новую модель.", "Создать", "Отмена")) return;
            string parent = MonsterFolder(recipe.Monster);
            string folder = AssetDatabase.GenerateUniqueAssetPath(parent + "/" + Path.GetFileNameWithoutExtension(recipePath) + "_Generated");
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(folder)))) throw new IOException("Не создана папка: " + folder);
            var scene = EditorSceneManager.NewPreviewScene();
            var randomState = UnityEngine.Random.state;
            GameObject root = null; GameObject previous = recipe.LastPrefab;
            try
            {
                root = new GameObject(recipe.Monster.ToString());
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                Monster monster; MonsterKind kind;
                switch (recipe.Monster)
                {
                    case MonsterModelRecipe.Species.Mushroom: monster = root.AddComponent<Mushroom>(); kind = MonsterKind.Mushroom; break;
                    case MonsterModelRecipe.Species.Mandrake: monster = root.AddComponent<Mandrake>(); kind = MonsterKind.Mandrake; break;
                    case MonsterModelRecipe.Species.Basilisk: monster = root.AddComponent<Basilisk>(); kind = MonsterKind.Basilisk; break;
                    case MonsterModelRecipe.Species.LivingArmor: monster = root.AddComponent<LivingArmor>(); kind = MonsterKind.Armor; break;
                    case MonsterModelRecipe.Species.Mimic: monster = root.AddComponent<Mimic>(); kind = MonsterKind.Mimic; break;
                    case MonsterModelRecipe.Species.Dragon: monster = root.AddComponent<Dragon>(); kind = MonsterKind.Dragon; break;
                    default: throw new InvalidOperationException("Монстр ещё не поддержан.");
                }
                // Existing physical proxy Build plus the existing correct-floor visual builder.
                monster.BuildGeometryForEditor(kind);
                var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
                var animation = root.AddComponent<MonsterAnim>(); animation.Owner = monster;
                animation.Animate = recipe.Animation == MonsterModelRecipe.Motion.Existing;
                if (kind == MonsterKind.Mushroom) UpperCaveCreatures.Mushroom(monster, animation);
                else if (kind == MonsterKind.Mandrake) RuinsCreatures.Mandrake(monster, animation);
                else if (kind == MonsterKind.Basilisk) RuinsCreatures.Basilisk(monster, animation);
                else if (kind == MonsterKind.Armor) FortressCreatures.Armor(monster.transform, true, animation);
                else if (kind == MonsterKind.Mimic) FortressCreatures.Mimic(monster, animation);
                else DragonReference.Build(monster, animation);
                var dragonMotion = root.GetComponent<DragonReferenceMotion>();
                if (dragonMotion != null) dragonMotion.Animate = recipe.Animation == MonsterModelRecipe.Motion.Existing;
                foreach (var renderer in animation.Hidden) if (renderer != null) renderer.enabled = false;
                foreach (var renderer in animation.HiddenAlways) if (renderer != null) renderer.enabled = false;
                var pose = root.GetComponentInChildren<MandrakeLeafPose>(true);
                if (pose != null) { pose.GatherStrength = recipe.LeafGatherStrength; pose.Response = recipe.LeafResponse; }
                root.transform.localScale = Vector3.one * recipe.ModelScale;
                foreach (var name in replacements.Keys)
                {
                    bool found = false;
                    foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true)) if (renderer.name == name) { found = true; break; }
                    if (!found) throw new InvalidOperationException("Нет MeshRenderer с именем: " + name);
                }
                SavedModelResources.SaveMeshes(root, folder);
                SavedModelResources.SaveMaterials(root, folder, renderer => replacements.TryGetValue(renderer.name, out var material) ? material : null);
                string path = folder + "/" + recipe.Monster + ".prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                if (!success || prefab == null) throw new IOException("Unity не сохранила префаб: " + path);
                AssetDatabase.SaveAssets(); Validate(path);
                Undo.RecordObject(recipe, "Последний префаб монстра"); recipe.LastPrefab = prefab;
                EditorUtility.SetDirty(recipe); AssetDatabase.SaveAssets(); EditorGUIUtility.PingObject(prefab);
                Debug.Log("[MonsterPrefab] Сохранён " + path + ". Теперь закрой/открой Unity и повтори проверку ссылок. Охота/VR этим не проверены.", prefab);
            }
            catch
            {
                recipe.LastPrefab = previous; EditorUtility.SetDirty(recipe);
                AssetDatabase.DeleteAsset(folder); throw; // only this invocation's newly created folder
            }
            finally
            {
                if (root != null) Object.DestroyImmediate(root);
                EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Random.state = randomState;
            }
        }

        static void Validate(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Сначала создай префаб.");
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var monster = root.GetComponent<Monster>(); var body = root.GetComponent<Rigidbody>();
                if (monster == null || !monster.HasSavedGeometry || body == null || !body.isKinematic)
                    throw new InvalidOperationException("Не сохранены Monster/геометрия/Rigidbody.");
                string[] fields; int zoneCount;
                if (monster is Mushroom) { fields = new[] { "_cap" }; zoneCount = 2; }
                else if (monster is Mandrake) { fields = new[] { "_leaves", "_root", "_leavesGo", "_rootGo" }; zoneCount = 1; }
                else if (monster is Basilisk) { fields = new[] { "_neck", "_head", "_grab", "_eyes" }; zoneCount = 4; }
                else if (monster is LivingArmor) { fields = new[] { "_sword", "_helmet", "_torso", "_helm" }; zoneCount = 3; }
                else if (monster is Mimic) { fields = new[] { "_lid", "_clawL", "_clawR", "_leg", "_open" }; zoneCount = 2; }
                else if (monster is Dragon) { fields = new[] { "_head", "_flame", "_paw", "_tail", "_throatR" }; zoneCount = 5; }
                else throw new InvalidOperationException("Проверка этой группы не поддерживает данный тип.");
                var serialized = new SerializedObject(monster);
                foreach (var field in fields)
                {
                    var property = serialized.FindProperty(field); var reference = property != null ? property.objectReferenceValue : null;
                    Transform t = reference is GameObject go ? go.transform : reference is Component c ? c.transform : null;
                    if (t == null || !t.IsChildOf(root.transform)) throw new InvalidOperationException("Потеряна/внешняя ссылка " + field);
                }
                var zones = root.GetComponentsInChildren<HitZone>(true);
                if (zones.Length != zoneCount) throw new InvalidOperationException("Не совпадает число зон: " + zones.Length);
                foreach (var zone in zones)
                    if (zone.Owner != monster || zone.GetComponent<Collider>() == null) throw new InvalidOperationException("Потеряна зона/Owner: " + zone.name);
                foreach (var grab in root.GetComponentsInChildren<MonsterGrabPoint>(true))
                    if (grab.Owner != monster || grab.GetComponent<Collider>() == null) throw new InvalidOperationException("Потерян захват/Owner: " + grab.name);
                var expected = monster is Mushroom ? new[] { Zone.Body, Zone.Head } :
                    monster is Mandrake ? new[] { Zone.Leaves } :
                    monster is LivingArmor ? new[] { Zone.Body, Zone.Head, Zone.Shield } :
                    monster is Mimic ? new[] { Zone.Body, Zone.Lid } :
                    monster is Dragon ? new[] { Zone.Body, Zone.Head, Zone.Throat, Zone.Front, Zone.Tail } : new[] { Zone.Body, Zone.Neck, Zone.Head, Zone.Tail };
                foreach (var expectedZone in expected)
                {
                    int count = 0; foreach (var zone in zones) if (zone.Zone == expectedZone) count++;
                    if (count != 1) throw new InvalidOperationException("Неверная зона: " + expectedZone);
                }
                var animation = root.GetComponent<MonsterAnim>();
                if (animation == null || animation.Owner != monster) throw new InvalidOperationException("Потеряна анимация/Owner.");
                foreach (var renderer in animation.Hidden) if (renderer == null) throw new InvalidOperationException("Потерян Hidden renderer.");
                foreach (var renderer in animation.HiddenAlways) if (renderer == null) throw new InvalidOperationException("Потерян HiddenAlways renderer.");
                foreach (var t in animation.Legs) if (t == null) throw new InvalidOperationException("Потеряна нога.");
                foreach (var t in animation.Wings) if (t == null) throw new InvalidOperationException("Потеряно крыло.");
                if (monster is Mandrake)
                {
                    var pose = root.GetComponentInChildren<MandrakeLeafPose>(true);
                    if (pose == null || pose.Owner != monster || pose.GrabPoint == null || pose.GrabPoint.Owner != monster || pose.Foliage == null ||
                        pose.LeafMeshes == null || pose.VerticesPerLeaf == null || pose.RestDirections == null ||
                        pose.LeafMeshes.Length != 3 || pose.VerticesPerLeaf.Length != 3 || pose.RestDirections.Length != 9)
                        throw new InvalidOperationException("Потеряны ссылки визуального захвата листьев.");
                    for (int i = 0; i < 3; i++)
                        if (pose.LeafMeshes[i] == null || !pose.LeafMeshes[i].transform.IsChildOf(pose.Foliage) ||
                            pose.LeafMeshes[i].sharedMesh == null || !pose.LeafMeshes[i].sharedMesh.isReadable ||
                            pose.LeafMeshes[i].sharedMesh.vertexCount != 9 * pose.VerticesPerLeaf[i])
                            throw new InvalidOperationException("Изменена/потеряна топология листьев.");
                }
                if (monster is Basilisk)
                {
                    var links = root.GetComponent<RuinsNeckLinks>();
                    if (links == null || links.Neck == null || links.Head == null || links.Links.Count != 2 || links.Links[0] == null || links.Links[1] == null)
                        throw new InvalidOperationException("Потеряны соединители шеи.");
                }
                if (monster is Mimic mimic)
                {
                    var legs = new SerializedObject(mimic).FindProperty("_legs");
                    if (legs == null || legs.arraySize != 6) throw new InvalidOperationException("Мимик: не сохранены шесть ног.");
                    for (int i = 0; i < 6; i++) if (legs.GetArrayElementAtIndex(i).objectReferenceValue == null)
                        throw new InvalidOperationException("Мимик: потеряна нога.");
                }
                if (monster is Dragon)
                {
                    var motion = root.GetComponent<DragonReferenceMotion>();
                    if (motion == null || motion.Owner != monster || motion.HeadPivot == null || motion.Jaw == null || motion.Neck == null ||
                        motion.Neck.vertexCount != 62 || motion.Wings.Count != 2 || motion.Wings[0] == null || motion.Wings[1] == null)
                        throw new InvalidOperationException("Дракон: потеряны шея/челюсть/крылья.");
                }
                SavedModelResources.Validate(root);
                Debug.Log("[MonsterPrefab] Сохранённые ссылки прочитаны: " + path + "; HitZone=" + zones.Length +
                    ". Это Editor-проверка ресурсов, не охота/VR/компиляционный отчёт.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
