using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Podz
{
    /// <summary>
    /// Кухня, часть 1: ЛЕДНИК — шкаф со стеклянными дверцами. Вся добыча лежит на полках узнаваемыми фигурками
    /// (сколько штук — столько фигурок, до 4) с подписью «Желе слайма ×3». Внизу — кладовая с овощами (∞).
    /// Взять продукт: смотреть на полку и E (VR — grip рукой у полки). Кнопка «Витрина» — экран со всеми продуктами.
    /// </summary>
    public class Fridge : MonoBehaviour
    {
        public static Fridge I;
        public static readonly ItemId[] Order =
        {
            ItemId.SlimeJelly, ItemId.MushroomLegs, ItemId.MandrakeRoot, ItemId.BasiliskMeat,
            ItemId.ShellfishMeat, ItemId.MimicMeat, ItemId.DragonMeat,
            ItemId.Cabbage, ItemId.Potato, ItemId.Carrot, ItemId.Onion
        };
        public readonly Dictionary<ItemId, FridgeSlot> Slots = new Dictionary<ItemId, FridgeSlot>();
        public Vector3 TakeSpot;               // куда кладётся продукт, если руки заняты
        public static int Taken;               // для автотеста
        public static ItemId LastTaken;
        readonly HashSet<ItemId> _new = new HashSet<ItemId>();
        [SerializeField] Transform _doorL, _doorR;
        [SerializeField] Pressable _showcase;
        float _open;
        public float DoorsOpen01 => _open;
        public Vector3 Front => transform.position + transform.forward * 1.3f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { I = null; Taken = 0; }

        public static bool Infinite(ItemId id) => !Items.Get(id).Monster;
        public static int Count(ItemId id) => Infinite(id) ? 99 : Inventory.Fridge.Count(id);

        /// <summary>Пометить продукты как «новые» (после возвращения из подземелья) — мерцают на полке.</summary>
        public void MarkNew(IEnumerable<ItemId> ids) { foreach (var i in ids) _new.Add(i); }
        public bool IsNew(ItemId id) => _new.Contains(id);

        public static Fridge Create(Transform parent, Vector3 localPos, float yaw, Vector3 takeSpot)
        {
            var prefab = SavedDungeonAssets.Active?.Fridge;
            if (prefab != null && prefab._showcase != null)
            {
                var saved = Instantiate(prefab, parent, false);
                saved.transform.SetLocalPositionAndRotation(localPos, Quaternion.Euler(0, yaw, 0));
                saved.TakeSpot = takeSpot;
                saved._showcase.OnPress = FridgeScreen.Toggle;
                foreach (var slot in saved.GetComponentsInChildren<FridgeSlot>(true))
                {
                    slot.Bind(saved);
                    saved.Slots.Add(slot.Id, slot);
                }
                SavedDungeonAssets.RestoreText(saved.gameObject);
                I = saved;
                return saved;
            }
            var go = new GameObject("Fridge");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var f = go.AddComponent<Fridge>();
            I = f;
            f.TakeSpot = takeSpot;
            f.Build();
            return f;
        }

        void OnDestroy() { if (I == this) I = null; }

        // локальные оси: +Z — к игроку (лицевая сторона)
        void Build()
        {
            var t = transform;
            var ice = new Color(0.62f, 0.8f, 0.9f);
            var wood = new Color(0.42f, 0.28f, 0.17f);
            const float W = 1.5f, H = 2.05f, D = 0.55f;
            // корпус: задняя стенка, бока, верх, низ (лицо открыто)
            Prim.Cube("Back", t, new Vector3(0, H / 2, -D / 2), new Vector3(W, H, 0.05f), ice * 0.55f);
            Prim.Cube("SideL", t, new Vector3(-W / 2, H / 2, 0), new Vector3(0.06f, H, D), wood);
            Prim.Cube("SideR", t, new Vector3(W / 2, H / 2, 0), new Vector3(0.06f, H, D), wood);
            Prim.Cube("Top", t, new Vector3(0, H, 0), new Vector3(W + 0.08f, 0.08f, D + 0.06f), wood * 1.1f);
            Prim.Cube("Plinth", t, new Vector3(0, 0.06f, 0), new Vector3(W, 0.12f, D), wood * 0.8f);
            // вывеска и иней
            var sign = Prim.Text(t, new Vector3(0, H + 0.17f, D / 2 - 0.05f), "ЛЕДНИК", 0.12f, new Color(0.75f, 0.95f, 1f));
            sign.fontStyle = FontStyle.Bold;
            sign.transform.localRotation = Quaternion.Euler(0, 180, 0);
            for (int i = 0; i < 7; i++)
                Prim.Glow(Prim.Part(PrimitiveType.Cube, "Frost", t, new Vector3(-0.65f + i * 0.21f, H + 0.06f, D / 2 - 0.02f), new Vector3(0.05f, 0.08f + (i % 3) * 0.04f, 0.05f), new Color(0.85f, 0.97f, 1f), new Vector3(0, 45, 45)), new Color(0.4f, 0.7f, 0.9f));
            // холодный свет внутри
            Prim.PointLight(t, new Vector3(0, H - 0.3f, 0.1f), new Color(0.6f, 0.85f, 1f), 2.2f, 1.2f);
            // полки: 2 ряда добычи + ряд кладовой
            float[] ys = { 1.42f, 0.9f, 0.32f };
            for (int r = 0; r < 3; r++)
            {
                Prim.Cube("Shelf", t, new Vector3(0, ys[r] - 0.03f, 0), new Vector3(W - 0.1f, 0.04f, D - 0.08f), r == 2 ? wood * 1.2f : new Color(0.8f, 0.92f, 1f), false);
                Prim.Glow(Prim.Cube("ShelfEdge", t, new Vector3(0, ys[r] - 0.03f, D / 2 - 0.05f), new Vector3(W - 0.1f, 0.05f, 0.015f), new Color(0.5f, 0.85f, 1f), false), r == 2 ? Color.black : new Color(0.2f, 0.45f, 0.6f));
            }
            var shelfTitle = Prim.Text(t, new Vector3(0, 0.62f, D / 2 - 0.02f), "КЛАДОВАЯ — овощи без счёта", 0.035f, new Color(1f, 0.9f, 0.65f));
            shelfTitle.transform.localRotation = Quaternion.Euler(0, 180, 0);
            for (int i = 0; i < Order.Length; i++)
            {
                int row = i < 4 ? 0 : i < 7 ? 1 : 2;
                int col = i < 4 ? i : i < 7 ? i - 4 : i - 7;
                int n = row == 1 ? 3 : 4;
                float x = (col - (n - 1) * 0.5f) * (row == 1 ? 0.46f : 0.35f);
                Slots[Order[i]] = FridgeSlot.Create(this, Order[i], new Vector3(x, ys[row], 0), row == 1 ? 0.44f : 0.33f);
            }
            // дверцы — стекло с инеем по краю, распахиваются, когда подходишь
            _doorL = Door("DoorL", -1, W, H, D);
            _doorR = Door("DoorR", 1, W, H, D);
            // кнопка «Витрина»
            var b = _showcase = Pressable.Create("Витрина (E)", t, new Vector3(0.52f, H - 0.18f, D / 2 + 0.03f), new Color(0.25f, 0.5f, 0.7f), () => FridgeScreen.Toggle(), new Vector3(0.3f, 0.09f, 0.03f), 0.026f);
            b.transform.localRotation = Quaternion.Euler(0, 180, 0);
            b.Label.transform.localPosition = b.transform.localPosition + new Vector3(0, 0, 0.035f); // подпись — на лицевой стороне (+Z)
            b.Label.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }

        Transform Door(string n, int side, float W, float H, float D)
        {
            var hinge = new GameObject(n).transform;
            hinge.SetParent(transform, false);
            hinge.localPosition = new Vector3(side * W / 2, 0, D / 2 + 0.01f);
            var glass = Prim.Quad("Glass", hinge, new Vector3(-side * W / 4, H / 2 + 0.03f, 0), new Vector2(W / 2 - 0.02f, H - 0.2f), new Color(0.75f, 0.92f, 1f, 0.16f));
            glass.transform.localRotation = Quaternion.Euler(0, 180, 0);
            Prim.Cube("FrameTop", hinge, new Vector3(-side * W / 4, H - 0.08f, 0), new Vector3(W / 2, 0.05f, 0.03f), new Color(0.5f, 0.35f, 0.2f), false);
            Prim.Cube("FrameBottom", hinge, new Vector3(-side * W / 4, 0.14f, 0), new Vector3(W / 2, 0.05f, 0.03f), new Color(0.5f, 0.35f, 0.2f), false);
            Prim.Cube("FrameOuter", hinge, new Vector3(0, H / 2 + 0.03f, 0), new Vector3(0.04f, H - 0.2f, 0.03f), new Color(0.5f, 0.35f, 0.2f), false);
            Prim.Cube("Handle", hinge, new Vector3(-side * (W / 2 - 0.06f), 1.1f, 0.03f), new Vector3(0.025f, 0.22f, 0.03f), new Color(0.85f, 0.75f, 0.4f), false);
            return hinge;
        }

        void Update()
        {
            var P = PlayerRig.I;
            if (P == null) return;
            bool near = (P.Feet - transform.position).sqrMagnitude < 2.6f * 2.6f || FridgeScreen.IsOpen;
            _open = Mathf.MoveTowards(_open, near ? 1f : 0f, Time.deltaTime * 2.5f);
            float a = Mathf.SmoothStep(0, 1, _open) * 105f;
            if (_doorL != null) _doorL.localRotation = Quaternion.Euler(0, a, 0);
            if (_doorR != null) _doorR.localRotation = Quaternion.Euler(0, -a, 0);
            if (near && _new.Count > 0 && _open >= 1f && Time.frameCount % 30 == 0 && (P.Feet - transform.position).sqrMagnitude < 2.2f * 2.2f) _newSeenFor += 0.5f;
            if (_newSeenFor > 6f) { _new.Clear(); _newSeenFor = 0; }
        }
        float _newSeenFor;

        /// <summary>Взять один продукт: в свободную руку (ПК — правая, если в ней нет инструмента), иначе — на стол у доски.</summary>
        public Ingredient Take(ItemId id, HandController hand = null)
        {
            if (!Infinite(id) && Inventory.Fridge.Count(id) <= 0)
            {
                var src = FoodModels.Source(id);
                Hints.Show($"{Items.Name(id)}: в леднике пусто — добудь: {FoodModels.SourceText(id)}", HintPri.Error);
                Sfx.Play(SfxId.Warn, transform.position, 0.5f);
                return null;
            }
            var P = PlayerRig.I;
            if (hand == null && P != null) hand = P.IsXR ? null : P.Right;
            bool toHand = hand != null && hand.Held == null;
            var pos = toHand ? hand.transform.position : TakeSpot + Random.insideUnitSphere * 0.05f;
            var ing = Ingredient.Spawn(id, pos);
            if (ing == null) return null;
            if (!Infinite(id)) Inventory.Fridge.Take(id);
            _new.Remove(id);
            Taken++; LastTaken = id;
            if (toHand) hand.Grab(ing);
            Sfx.Play(SfxId.Pickup, transform.position, 0.6f);
            Hints.Show(toHand ? $"Взял: {Items.Name(id)} — {(id == ItemId.MushroomLegs ? "очистить ножом плашмя15см, затем2реза (ПК: ПКМ → ЛКМ×2)" : Items.Get(id).PrepText)}" : $"{Items.Name(id)} — на столе у доски ({(id == ItemId.MushroomLegs ? "очистить ножом плашмя15см, затем2реза (ПК: ПКМ → ЛКМ×2)" : Items.Get(id).PrepText)})", HintPri.Tutorial);
            return ing;
        }
    }
}
