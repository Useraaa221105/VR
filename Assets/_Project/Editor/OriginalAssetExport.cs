using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Podz.EditorTools
{
    // Uses the original builders in Edit Mode. The generated catalog is for review
    // until the live DungeonManager explicitly binds placements and interactions.
    public static class OriginalAssetExport
    {
        const string Configs = "Assets/_Project/Configs/Original";
        const string Scenes = "Assets/_Project/Scenes/Original";

        [MenuItem("Podz/Перенос/Сохранить оригинальные модели и блоки")]
        public static void Export()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Выйдите из Play Mode перед экспортом.");
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                throw new InvalidOperationException("Команда предназначена для проекта переноса.");
            DungeonPrefabCatalogEditor.Folder(Configs);
            var failures = new List<Exception>();
            Try(() =>
            {
                var slime = GetConfig<SlimeModelRecipe>("Slime");
                if (slime.LastPrefab == null) SlimeModelRecipeEditor.Generate(slime);
            }, failures);
            foreach (MonsterModelRecipe.Species species in Enum.GetValues(typeof(MonsterModelRecipe.Species)))
                Try(() =>
                {
                    var recipe = GetConfig<MonsterModelRecipe>(species.ToString());
                    recipe.Monster = species;
                    if (recipe.LastPrefab == null) MonsterModelRecipeEditor.Generate(recipe);
                }, failures);
            for (int floor = 1; floor <= 4; floor++)
            {
                int number = floor;
                Try(() => ExportFloor(number, failures), failures);
            }
            foreach (var (kind, floor) in Variants)
                Try(() =>
                {
                    var recipe = GetConfig<DungeonObjectRecipe>($"{kind}_F{floor}");
                    recipe.Kind = kind;
                    recipe.Floor = floor;
                    if (recipe.LastPrefab == null) DungeonObjectRecipeEditor.Generate(recipe);
                }, failures);
            foreach (var drop in new[] { ItemId.SlimeJelly, ItemId.MushroomLegs, ItemId.MandrakeRoot,
                ItemId.BasiliskMeat, ItemId.ShellfishMeat, ItemId.MimicMeat, ItemId.DragonMeat })
                Try(() =>
                {
                    var recipe = GetConfig<DungeonObjectRecipe>($"Loot_{drop}");
                    recipe.Kind = DungeonObjectRecipe.ObjectKind.PhysicalLoot;
                    recipe.Loot = drop;
                    if (recipe.LastPrefab == null) DungeonObjectRecipeEditor.Generate(recipe);
                }, failures);

            AssetDatabase.SaveAssets();
            if (failures.Count > 0) throw new AggregateException("Часть исходных ресурсов не удалось сохранить.", failures);
        }

        static void ExportFloor(int floor, List<Exception> failures)
        {
            var config = GetConfig<DungeonPrefabCatalog>($"Floor_{floor}");
            config.Floor = floor;
            var plan = DungeonSourcePlan.Read(floor, config.Seed, config.FortressRooms);
            ExportBlocks(config, plan, false, plan.Rooms.Count, failures);
            ExportBlocks(config, plan, true, plan.Edges.Count, failures);
            EditorUtility.SetDirty(config);

            if (!Enumerable.Range(0, plan.Rooms.Count).All(i => Find(config, plan.Key(false, i)) != null) ||
                !Enumerable.Range(0, plan.Edges.Count).All(i => Find(config, plan.Key(true, i)) != null))
                return;

            DungeonPrefabCatalogEditor.Folder(Scenes);
            string path = $"{Scenes}/Floor_{floor}_Review.unity";
            if (File.Exists(path)) return;
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                Debug.LogWarning("Блоки сохранены. Откройте сохранённую сцену, чтобы создать обзорную раскладку.");
                return;
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var root = new GameObject($"Original Floor {floor} - Geometry Review");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                for (int i = 0; i < plan.Rooms.Count; i++)
                {
                    var position = plan.Rooms[i].center;
                    position.y = 0;
                    Place(Find(config, plan.Key(false, i)), root.transform, position, Quaternion.identity);
                }
                for (int i = 0; i < plan.Edges.Count; i++)
                {
                    var edge = plan.Edges[i];
                    var direction = plan.Rooms[edge.y].center - plan.Rooms[edge.x].center;
                    Place(Find(config, plan.Key(true, i)), root.transform, plan.PassageCenter(i), Quaternion.LookRotation(direction));
                }
                var lamp = new GameObject("Review Light").AddComponent<Light>();
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lamp.gameObject, scene);
                lamp.type = LightType.Directional;
                lamp.transform.rotation = Quaternion.Euler(50, -30, 0);
                if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException(path);
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        static void ExportBlocks(DungeonPrefabCatalog config, DungeonSourcePlan plan, bool corridor,
            int count, List<Exception> failures)
        {
            for (int i = 0; i < count; i++)
            {
                if (Find(config, plan.Key(corridor, i)) != null) continue;
                config.SourceIndex = i;
                config.Corridor = corridor;
                Try(() => DungeonPrefabCatalogEditor.Generate(config), failures);
            }
        }

        static GameObject Find(DungeonPrefabCatalog config, string key) =>
            (config.Blocks ?? Array.Empty<GameObject>()).FirstOrDefault(prefab =>
                prefab != null && prefab.TryGetComponent<DungeonBlock>(out var block) && block.SourceKey == key);

        static void Place(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.SetLocalPositionAndRotation(position, rotation);
        }

        static T GetConfig<T>(string name) where T : ScriptableObject
        {
            string path = $"{Configs}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            if (File.Exists(path)) throw new IOException($"Файл уже существует: {path}");
            var result = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(result, path);
            return result;
        }

        static void Try(Action operation, List<Exception> failures)
        {
            try { operation(); }
            catch (Exception error) { failures.Add(error); Debug.LogException(error); }
        }

        static readonly (DungeonObjectRecipe.ObjectKind kind, int floor)[] Variants =
            {
            (DungeonObjectRecipe.ObjectKind.Stalactite, 1), (DungeonObjectRecipe.ObjectKind.Spores, 1),
            (DungeonObjectRecipe.ObjectKind.Spores, 2), (DungeonObjectRecipe.ObjectKind.AcidDrop, 1),
            (DungeonObjectRecipe.ObjectKind.Puddle, 1), (DungeonObjectRecipe.ObjectKind.Rockfall, 2),
            (DungeonObjectRecipe.ObjectKind.Vines, 2), (DungeonObjectRecipe.ObjectKind.DartPlate, 2),
            (DungeonObjectRecipe.ObjectKind.DartPlate, 3), (DungeonObjectRecipe.ObjectKind.Pendulum, 3),
            (DungeonObjectRecipe.ObjectKind.FireGrate, 3), (DungeonObjectRecipe.ObjectKind.Well, 1),
            (DungeonObjectRecipe.ObjectKind.Well, 2), (DungeonObjectRecipe.ObjectKind.Elixir, 1),
            (DungeonObjectRecipe.ObjectKind.Elixir, 2), (DungeonObjectRecipe.ObjectKind.Elixir, 3),
            (DungeonObjectRecipe.ObjectKind.DoorWithHandles, 1), (DungeonObjectRecipe.ObjectKind.DoorWithHandles, 2),
            (DungeonObjectRecipe.ObjectKind.DoorWithHandles, 3), (DungeonObjectRecipe.ObjectKind.StepsAndPlatform, 1),
            (DungeonObjectRecipe.ObjectKind.PortalVisual, 1), (DungeonObjectRecipe.ObjectKind.ReturnPortalVisual, 1),
            (DungeonObjectRecipe.ObjectKind.FireCover, 4), (DungeonObjectRecipe.ObjectKind.FakeArmor, 3),
            (DungeonObjectRecipe.ObjectKind.Chest, 3)
        };
    }
}
