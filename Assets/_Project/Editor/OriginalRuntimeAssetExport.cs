using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Podz.EditorTools
{
    public static class OriginalRuntimeAssetExport
    {
        const string DirectoryPath = "Assets/_Project/Prefabs/Original";
        const string ConfigPath = "Assets/_Project/Configs/Original/RuntimeAssets.asset";
        const string OriginalConfigs = "Assets/_Project/Configs/Original/";

        [MenuItem("Podz/Перенос/Подключить сохранённые объекты")]
        public static void Export()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Leave Play Mode before export.");
            var main = SceneManager.GetActiveScene();
            if (main.path != "Assets/Game/Scenes/Main.unity" || main.isDirty)
                throw new InvalidOperationException("Open and save the original Main scene first.");
            DungeonPrefabCatalogEditor.Folder(DirectoryPath);
            var config = AssetDatabase.LoadAssetAtPath<SavedDungeonAssets>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<SavedDungeonAssets>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }
            config.Caves = Floor(1);
            config.Ruins = Floor(2);
            config.Lair = Floor(4);
            ExportFortress(config);
            config.CaveAcid = Existing("AcidDrop_F1");
            config.FortressDarts = Existing("DartPlate_F3");
            config.Doors = Array.ConvertAll(new[] { 1, 2, 3 }, floor => Existing($"DoorWithHandles_F{floor}"));
            config.Elixirs = Array.ConvertAll(new[] { 1, 2, 3 }, floor => Existing($"Elixir_F{floor}"));
            config.CavePuddle = Existing("Puddle_F1");
            config.FortressChest = Existing("Chest_F3");
            ExportRemainingViews(config);
            config.RuinsVines = Existing("Vines_F2");
            config.RuinsDarts = Existing("DartPlate_F2");
            config.RuinsRockfall = Existing("Rockfall_F2");
            config.Hud = Bake("HudPanel", host =>
            {
                host.gameObject.AddComponent<Camera>().enabled = false;
                return HudPanel.Create(host).gameObject;
            }).GetComponent<HudPanel>();
            config.ToolBar = Bake("Hotbar", host =>
            {
                host.gameObject.AddComponent<Camera>().enabled = false;
                return Hotbar.Create(host).gameObject;
            }).GetComponent<Hotbar>();
            config.Orders = Bake("OrderBoard", host => OrderBoard.Create(host).gameObject).GetComponent<OrderBoard>();
            config.RecipeDisplay = Bake("RecipeScreen", host =>
                RecipeScreen.Create(host, null, Vector3.zero, 0).gameObject).GetComponent<RecipeScreen>();
            config.Settings = Bake("DungeonSettings", _ => DungeonSettings.BuildVisual().gameObject)
                .GetComponent<DungeonSettings>();
            config.FridgeDisplay = Bake("FridgeScreen", _ => FridgeScreen.BuildVisual().gameObject)
                .GetComponent<FridgeScreen>();
            config.ReturnPortal = Existing("ReturnPortalVisual_F1");
            config.DescentPortal = Existing("PortalVisual_F1");
            config.CaveSpores = Existing("Spores_F1");
            config.RuinsSpores = Existing("Spores_F2");
            config.CaveStalactite = Existing("Stalactite_F1");
            config.FortressPendulum = Existing("Pendulum_F3");
            config.FortressFireGrate = Existing("FireGrate_F3");
            config.Loot = Array.ConvertAll(new[] { ItemId.SlimeJelly, ItemId.MushroomLegs, ItemId.MandrakeRoot,
                ItemId.BasiliskMeat, ItemId.ShellfishMeat, ItemId.MimicMeat, ItemId.DragonMeat }, item => Existing("Loot_" + item));
            config.Tools = Array.ConvertAll((ToolType[])Enum.GetValues(typeof(ToolType)), type =>
                Bake("Tool_" + type, host => Tool.Create(type, host).gameObject).GetComponent<Tool>());
            config.LargeCauldron = Bake("LargeCauldron",
                host => Cauldron.Create(host, Vector3.zero, 0, null).gameObject).GetComponent<Cauldron>();
            config.SmallCauldron = Bake("SmallCauldron",
                host => Cauldron.Create(host, Vector3.zero, 0, null, false).gameObject).GetComponent<Cauldron>();
            config.Preparation = Bake("KitchenPreparation", host =>
            {
                var root = new GameObject("Preparation surfaces");
                root.transform.SetParent(host, false);
                root.AddComponent<KitchenPreparation>();
                Kitchen.BuildPreparationGeometry(root.transform);
                return root;
            });

            config.Fridge = Bake("Fridge", host =>
                Fridge.Create(host, Vector3.zero, 0, Vector3.zero).gameObject).GetComponent<Fridge>();
            config.Shop = Bake("TavernShop", host => TavernShop.Build(host).gameObject).GetComponent<TavernShop>();
            config.Toby = Bake("Toby", host => Toby.BuildForEditor().gameObject).GetComponent<Toby>();
            config.Guests = Array.ConvertAll((GuestType[])Enum.GetValues(typeof(GuestType)), type =>
                Bake("Guest_" + type, host => Guest.BuildForEditor(type).gameObject).GetComponent<Guest>());
            config.Ingredients = Array.ConvertAll((ItemId[])Enum.GetValues(typeof(ItemId)), item =>
                Bake("Ingredient_" + item, host => Ingredient.Spawn(item, Vector3.zero).gameObject).GetComponent<Ingredient>());
            config.Bowls = Recipes.All.ConvertAll(recipe =>
                Bake("Bowl_" + recipe.Index, host => Bowl.Spawn(recipe, 3, false, Vector3.zero).gameObject).GetComponent<Bowl>()).ToArray();

            if (config.RecipeParameters == null || config.RecipeParameters.Length == 0)
                config.RecipeParameters = Recipes.ExportTuning();

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            var bootstrap = Array.Find(main.GetRootGameObjects(), g => g.GetComponent<GameBootstrap>() != null);
            if (bootstrap == null) throw new InvalidOperationException("Cannot find Main bootstrap.");
            var serialized = new SerializedObject(bootstrap.GetComponent<GameBootstrap>());
            serialized.FindProperty("savedWorld").objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!EditorSceneManager.SaveScene(main)) throw new IOException("Main was not saved.");
            Debug.Log("[OriginalRuntimeAssetExport] Linked matching original assets to Main.");
        }

        static void ExportRemainingViews(SavedDungeonAssets config)
        {
            var sizes = new[] { new Vector3(8, 4.8f, 8), new Vector3(12, 4.8f, 8), new Vector3(8, 4.8f, 11) };
            var decor = new System.Collections.Generic.List<GameObject>();
            foreach (var size in sizes)
                for (int type = 0; type < 8; type++)
                {
                    string name = $"FortressDecor_{size.x}x{size.z}_{type}";
                    decor.Add(Bake(name, host =>
                    {
                        var root = new GameObject(name);
                        root.transform.SetParent(host, false);
                        FortressReference.Build(root.transform, type, size);
                        DungeonArchitecture.SurfaceAll(root.transform, 3);
                        return root;
                    }));
                }
            config.FortressDecor = decor.ToArray();
            config.GuideCanvas = Bake("GuideCanvas", host =>
            {
                host.gameObject.AddComponent<Camera>().enabled = false;
                return Guide.Create(host).gameObject;
            });
            config.KitchenCoachCanvas = Bake("KitchenCoachCanvas", host =>
            {
                host.gameObject.AddComponent<Camera>().enabled = false;
                return KitchenCoach.Create(host).gameObject;
            });
            config.HuntCoachCanvas = Bake("HuntCoachCanvas", host =>
            {
                host.gameObject.AddComponent<Camera>().enabled = false;
                return HuntCoach.Create(host).gameObject;
            });
            config.BestiaryCanvas = Bake("BestiaryCanvas", _ => BestiaryCard.BuildCanvas());
            config.HuntIntroPc = Bake("HuntIntroPc", _ => HuntIntro.BuildVisual(false).gameObject);
            config.HuntIntroVr = Bake("HuntIntroVr", _ => HuntIntro.BuildVisual(true).gameObject);
        }

        static void ExportFortress(SavedDungeonAssets config)
        {
            var sizes = new[] { new Vector2(8, 8), new Vector2(12, 8), new Vector2(8, 11) };
            var rooms = new System.Collections.Generic.List<FortressGeometryBlock>();
            foreach (var size in sizes)
                for (int openings = 1; openings < 16; openings++)
                {
                    string name = $"FortressRoom_{size.x}x{size.y}_{openings:X}";
                    rooms.Add(Bake(name, host =>
                    {
                        var root = new GameObject(name);
                        root.transform.SetParent(host, false);
                        FloorGenerator.BuildFortressShellForEditor(root.transform, size, openings);
                        var block = root.AddComponent<FortressGeometryBlock>();
                        block.RoomSize = size;
                        block.Openings = openings;
                        return root;
                    }).GetComponent<FortressGeometryBlock>());
                }
            config.FortressRooms = rooms.ToArray();

            config.FortressPassages = Array.ConvertAll(new[] { 5.4f, 3.4f, 1.4f, 3.9f, 2.4f }, length =>
            {
                string name = $"FortressPassage_{Mathf.RoundToInt(length * 10)}dm";
                return Bake(name, host =>
                {
                    var root = new GameObject(name);
                    root.transform.SetParent(host, false);
                    FloorGenerator.BuildFortressPassageForEditor(root.transform, length);
                    root.AddComponent<FortressGeometryBlock>().PassageLength = length;
                    return root;
                }).GetComponent<FortressGeometryBlock>();
            });
        }

        static DungeonPrefabCatalog Floor(int floor) =>
            AssetDatabase.LoadAssetAtPath<DungeonPrefabCatalog>(OriginalConfigs + $"Floor_{floor}.asset")
            ?? throw new InvalidOperationException($"Missing floor {floor} catalog.");

        static DungeonObjectRecipe Existing(string key)
        {
            var recipe = AssetDatabase.LoadAssetAtPath<DungeonObjectRecipe>(OriginalConfigs + key + ".asset");
            if (recipe == null || recipe.LastPrefab == null) throw new InvalidOperationException($"Missing original prefab: {key}");
            return recipe;
        }

        static GameObject Bake(string name, Func<Transform, GameObject> build)
        {
            string folder = DirectoryPath + "/" + name;
            string path = folder + "/" + name + ".prefab";
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (saved != null) return saved;
            if (AssetDatabase.IsValidFolder(folder))
                throw new IOException("An incomplete export folder already exists: " + folder);
            DungeonPrefabCatalogEditor.Folder(folder);
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var host = new GameObject("Original authoring host");
                SceneManager.MoveGameObjectToScene(host, preview);
                var root = build(host.transform);
                if (root.transform.parent == null)
                    SceneManager.MoveGameObjectToScene(root, preview);
                SavedModelResources.SaveMeshes(root, folder);
                foreach (var label in root.GetComponentsInChildren<TextMesh>(true))
                    label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
                SavedModelResources.SaveMaterials(root, folder, renderer =>
                    renderer.TryGetComponent<TextMesh>(out var label) ? label.font.material : null);
                if (root.TryGetComponent<Cauldron>(out _))
                    foreach (Transform puff in root.transform.Find("Steam"))
                    {
                        var material = new Material(puff.GetComponent<Renderer>().sharedMaterial) { color = new Color(.9f,.93f,.96f,.13f) };
                        puff.GetComponent<Renderer>().sharedMaterial = SavedCommonResources.SaveMaterial(material);
                    }
                saved = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                if (!success || saved == null) throw new IOException(path);
                var contents = PrefabUtility.LoadPrefabContents(path);
                try { SavedModelResources.Validate(contents); }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
                return saved;
            }
            catch
            {
                AssetDatabase.DeleteAsset(folder);
                throw;
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
    }
}
