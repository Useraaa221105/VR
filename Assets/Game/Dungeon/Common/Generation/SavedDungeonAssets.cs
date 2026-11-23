using System;
using UnityEngine;

namespace Podz
{
    [CreateAssetMenu(menuName = "Podz/Saved original assets")]
    public sealed class SavedDungeonAssets : ScriptableObject
    {
        public DungeonPrefabCatalog Caves, Ruins, Lair;
        public FortressGeometryBlock[] FortressRooms, FortressPassages;

        internal FortressGeometryBlock FindFortressRoom(Vector2 size, int openings) =>
            Array.Find(FortressRooms ?? Array.Empty<FortressGeometryBlock>(),
                block => block != null && block.RoomSize == size && block.Openings == openings);

        internal FortressGeometryBlock FindFortressPassage(float length) =>
            Array.Find(FortressPassages ?? Array.Empty<FortressGeometryBlock>(),
                block => block != null && Mathf.Abs(block.PassageLength - length) < .001f);
        public DungeonObjectRecipe ReturnPortal, DescentPortal, CaveSpores, RuinsSpores;
        public DungeonObjectRecipe[] Loot;
        public DungeonObjectRecipe[] Doors, Elixirs;
        public DungeonObjectRecipe CavePuddle, FortressChest;
        public GameObject[] FortressDecor;

        public GameObject FindFortressDecor(int type, Vector3 size)
        {
            int sizeIndex = size.x == 12f ? 1 : size.z == 11f ? 2 : 0;
            int index = sizeIndex * 8 + type;
            return FortressDecor != null && index >= 0 && index < FortressDecor.Length ? FortressDecor[index] : null;
        }
        public GameObject GuideCanvas;
        public GameObject KitchenCoachCanvas;
        public GameObject HuntCoachCanvas;
        public GameObject BestiaryCanvas, HuntIntroPc, HuntIntroVr;

        public static T FindTemplate<T>(DungeonObjectRecipe recipe) where T : Component =>
            recipe != null && recipe.LastPrefab != null ? recipe.LastPrefab.GetComponentInChildren<T>(true) : null;

        public static DungeonObjectRecipe Variant(DungeonObjectRecipe[] recipes, int index) =>
            recipes != null && index >= 0 && index < recipes.Length ? recipes[index] : null;

        public DungeonObjectRecipe CaveAcid, FortressDarts, RuinsVines, RuinsDarts, RuinsRockfall;
        public HudPanel Hud;
        public Hotbar ToolBar;
        public OrderBoard Orders;
        public RecipeScreen RecipeDisplay;
        public DungeonSettings Settings;
        public FridgeScreen FridgeDisplay;
        public Tool[] Tools;
        public Cauldron LargeCauldron, SmallCauldron;
        public GameObject Preparation;
        public Fridge Fridge;
        public TavernShop Shop;
        public DungeonObjectRecipe CaveStalactite, FortressPendulum, FortressFireGrate;
        public Toby Toby;
        public Guest[] Guests;
        public Ingredient[] Ingredients;
        public Bowl[] Bowls;
        [Serializable]
        public struct RecipeTuning
        {
            public string Name;
            [Min(.1f)] public float CookSeconds;
            [Min(0)] public int Price;
            [Range(1,3)] public int Heat;
        }
        public RecipeTuning[] RecipeParameters;
        [Min(1)] public int CleaverPrice = 150, HammerPrice = 300, ShieldPrice = 400;
        [Min(1)] public float DaySeconds = 300;
        [Min(0)] public int FirstGoal = 120, SecondGoal = 350, ThirdGoal = 700;

        public static SavedDungeonAssets Active { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearActiveReference() => Active = null;

        public static bool TryBuildFixed(DungeonPrefabCatalog catalog, int floor, Transform root,
            FloorLayout layout, int[,] edges)
        {
            if (catalog == null || catalog.Floor != floor) return false;
            int rooms = layout.Rooms.Count, passages = edges.GetLength(0);
            var roomAssets = new DungeonBlock[rooms];
            var passageAssets = new DungeonBlock[passages];
            foreach (var prefab in catalog.Blocks ?? Array.Empty<GameObject>())
            {
                if (prefab == null || !prefab.TryGetComponent<DungeonBlock>(out var block) ||
                    block.Floor != floor || string.IsNullOrEmpty(block.SourceKey)) continue;
                string[] key = block.SourceKey.Split('_');
                var slots = block.Corridor ? passageAssets : roomAssets;
                if (key.Length < 4 || !int.TryParse(block.Corridor ? key[key.Length - 1] : key[2], out int index) ||
                    index < 0 || index >= slots.Length || slots[index] != null) continue;
                slots[index] = block;
            }
            for (int i = 0; i < rooms; i++)
            {
                var block = roomAssets[i];
                if (block == null || !Mathf.Approximately(block.Footprint.size.x, layout.Rooms[i].size.x) ||
                    !Mathf.Approximately(block.Footprint.size.z, layout.Rooms[i].size.z)) return false;
                int expectedPorts = 0;
                for (int j = 0; j < passages; j++)
                {
                    int a = edges[j, 0], b = edges[j, 1];
                    if (a != i && b != i) continue;
                    expectedPorts++;
                    Vector3 direction = (layout.Rooms[a == i ? b : a].center - layout.Rooms[i].center).normalized;
                    if (!Array.Exists(block.Connections ?? Array.Empty<DungeonBlock.Port>(),
                        port => Vector3.Dot(port.Forward, direction) > .99f)) return false;
                }
                if (block.Connections == null || block.Connections.Length != expectedPorts) return false;
            }
            var positions = new Vector3[passages];
            var rotations = new Quaternion[passages];
            for (int i = 0; i < passages; i++)
            {
                var a = layout.Rooms[edges[i, 0]];
                var b = layout.Rooms[edges[i, 1]];
                var direction = (b.center - a.center).normalized;
                float extent = Mathf.Abs(direction.x) > .5f ? a.extents.x + b.extents.x : a.extents.z + b.extents.z;
                if (passageAssets[i] == null ||
                    !Mathf.Approximately(passageAssets[i].Footprint.size.z, Vector3.Distance(a.center, b.center) - extent))
                    return false;
                float ah = Mathf.Abs(direction.x) > .5f ? a.extents.x : a.extents.z;
                float bh = Mathf.Abs(direction.x) > .5f ? b.extents.x : b.extents.z;
                positions[i] = (a.center + direction * ah + b.center - direction * bh) * .5f;
                positions[i].y = layout.Origin.y;
                rotations[i] = Quaternion.LookRotation(direction);
            }
            for (int i = 0; i < rooms; i++)
            {
                Vector3 position = layout.Rooms[i].center;
                position.y = layout.Origin.y;
                Instantiate(roomAssets[i].gameObject, position, Quaternion.identity, root);
            }
            for (int i = 0; i < passages; i++)
                Instantiate(passageAssets[i].gameObject, positions[i], rotations[i], root);
            Physics.SyncTransforms();
            return true;
        }

        public static bool TryBuildLair(Transform root)
        {
            var catalog = Active?.Lair;
            if (catalog == null || catalog.Floor != 4 || catalog.Blocks == null || catalog.Blocks.Length != 1 ||
                catalog.Blocks[0] == null || !catalog.Blocks[0].TryGetComponent<DungeonBlock>(out var block) || block.Floor != 4)
                return false;
            Instantiate(block.gameObject, root.position, root.rotation, root);
            Physics.SyncTransforms();
            return true;
        }

        public Guest FindGuest(GuestType type) => Array.Find(Guests ?? Array.Empty<Guest>(),
            guest => guest != null && guest.Type == type);

        public Ingredient FindIngredient(ItemId item) => Array.Find(Ingredients ?? Array.Empty<Ingredient>(),
            ingredient => ingredient != null && ingredient.Id == item);

        public Bowl FindBowl(int index) => index < 0 || Bowls == null || index >= Bowls.Length ? null : Bowls[index];

        internal static void RestoreText(GameObject root, bool hud = false)
        {
            foreach (var label in root.GetComponentsInChildren<TextMesh>(true))
                label.GetComponent<Renderer>().sharedMaterial = hud ? Prim.HudTextMaterial : Prim.DepthTextMaterial;
        }

        public Tool FindTool(ToolType type) => Array.Find(Tools ?? Array.Empty<Tool>(),
            tool => tool != null && tool.Type == type);

        public DungeonObjectRecipe FindLoot(ItemId id) => Array.Find(Loot ?? Array.Empty<DungeonObjectRecipe>(),
            recipe => recipe != null && recipe.Loot == id);
    }
}
