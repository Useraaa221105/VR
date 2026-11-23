using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Podz
{
    /// <summary>
    /// Экран-витрина ледника: все продукты — фигурка, название, сколько есть, из какого монстра, в каких блюдах, как готовить.
    /// Кнопки «Взять (E)» у каждой строки. Закрыть: Enter / Пробел / кнопка «Закрыть» / отойти (VR: A/X).
    /// Время не останавливается (на кухне всё идёт своим чередом).
    /// </summary>
    public class FridgeScreen : MonoBehaviour
    {
        static FridgeScreen _i;
        public static bool IsOpen => _i != null;
        public static int Opened;
        public static string LastText = "";
        readonly Dictionary<ItemId, TextMesh> _counts = new Dictionary<ItemId, TextMesh>();
        readonly Dictionary<ItemId, Pressable> _btns = new Dictionary<ItemId, Pressable>();
        [SerializeField] List<Transform> _spin = new List<Transform>();
        [SerializeField] List<TextMesh> _countLabels = new List<TextMesh>();
        [SerializeField] List<Pressable> _buttons = new List<Pressable>();
        [SerializeField] List<Renderer> _rowBackgrounds = new List<Renderer>();
        [SerializeField] TextMesh _header;
        [SerializeField] Pressable _close;
        Vector3 _openedAt; float _openedTime;
        bool _a, _x;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _i = null; Opened = 0; LastText = ""; }

        public static void Toggle() { if (_i != null) Close(); else Show(); }

        public static void Close()
        {
            if (_i == null) return;
            Destroy(_i.gameObject);
            _i = null;
            Sfx.Play(SfxId.Card, null, 0.4f);
        }

        public static void Show()
        {
            var P = PlayerRig.I;
            if (P == null || _i != null) return;
            if (BestiaryCard.Open) return;
            var cam = P.Cam.transform;
            var template = SavedDungeonAssets.Active?.FridgeDisplay;
            bool saved = template != null && template._buttons.Count == Fridge.Order.Length;
            _i = saved ? Instantiate(template) : BuildVisual();
            var go = _i.gameObject;
            var fwd = cam.forward; fwd.y = Mathf.Clamp(fwd.y, -0.35f, 0.35f); fwd.Normalize();
            go.transform.position = cam.position + fwd * 0.8f; // ближе, чем полки ледника, — ничего не загораживает
            go.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            go.transform.localScale = Vector3.one * 0.5f;
            _i._openedAt = P.Feet; _i._openedTime = Time.unscaledTime;
            if (saved) SavedDungeonAssets.RestoreText(go);
            _i.Bind();
            Prim.OnTop(go.transform); // пункт 6: экран ледника поверх всего
            Opened++;
            Sfx.Play(SfxId.Card, null, 0.6f);
        }

        static TextMesh T(Transform p, float x, float y, string s, float h, Color c, TextAnchor a = TextAnchor.UpperLeft)
        {
            var t = Prim.Text(p, new Vector3(x, y, 0), s, h, c, a);
            t.GetComponent<MeshRenderer>().sortingOrder = 5;
            return t;
        }

        public static FridgeScreen BuildVisual()
        {
            var screen = SavedUi.CreateRoot("FridgeScreen").AddComponent<FridgeScreen>();
            screen.Build();
            return screen;
        }

        void Build()
        {
            var root = transform;
            Prim.Cube("Frame", root, new Vector3(0, 0, 0.03f), new Vector3(1.86f, 1.16f, 0.02f), new Color(0.45f, 0.75f, 0.9f), false);
            Prim.Cube("Panel", root, new Vector3(0, 0, 0.018f), new Vector3(1.82f, 1.12f, 0.02f), new Color(0.06f, 0.1f, 0.13f), false);
            _header = T(root, 0, 0.54f, "ЛЕДНИК — вся добыча", 0.065f, new Color(0.7f, 0.95f, 1f), TextAnchor.UpperCenter);
            _header.fontStyle = FontStyle.Bold;
            T(root, -0.86f, 0.455f, "ДОБЫЧА ИЗ ПОДЗЕМЕЛЬЯ", 0.032f, new Color(1f, 0.85f, 0.4f));
            T(root, 0.3f, 0.455f, "КЛАДОВАЯ (сколько угодно)", 0.032f, new Color(1f, 0.85f, 0.4f));
            for (int i = 0; i < Fridge.Order.Length; i++)
            {
                var id = Fridge.Order[i];
                bool veg = Fridge.Infinite(id);
                float x = veg ? 0.3f : -0.86f;
                int row = veg ? i - 7 : i;
                float y = 0.40f - row * (veg ? 0.13f : 0.128f);
                Row(root, id, x, y, veg ? 0.56f : 1.1f);
            }
            // подсказки
            T(root, 0.3f, -0.17f, "КАК ВЗЯТЬ:", 0.03f, new Color(0.5f, 1f, 0.6f));
            T(root, 0.3f, -0.21f, "Смотри на «Взять» и жми E —\nпродукт окажется в руке\n(если в руке инструмент — на столе у доски).\nVR: поднеси руку к полке и сожми grip.\nПустая ячейка подскажет, кого добыть.", 0.025f, Color.white);
            _close = Pressable.Create("Закрыть", root, new Vector3(0.6f, -0.49f, -0.01f), new Color(0.45f, 0.3f, 0.3f), Close, new Vector3(0.3f, 0.08f, 0.03f), 0.026f);
            _close.transform.localRotation = Quaternion.identity;
            T(root, 0, -0.52f, "Закрыть: Enter / Пробел / отойти (VR: A/X)", 0.024f, new Color(1, 1, 1, 0.6f), TextAnchor.UpperCenter);
        }

        void Row(Transform root, ItemId id, float x, float y, float w)
        {
            var info = Items.Get(id);
            bool veg = !info.Monster;
            _rowBackgrounds.Add(Prim.Cube("RowBg", root, new Vector3(x + w / 2, y - 0.05f, 0.012f),
                new Vector3(w, 0.115f, 0.005f), new Color(0.1f, 0.16f, 0.2f), false).GetComponent<Renderer>());
            var icon = FoodModels.Build(id, root, 0.07f);
            icon.localPosition = new Vector3(x + 0.06f, y - 0.085f, -0.03f);
            icon.localRotation = Quaternion.Euler(-15, 160, 0);
            _spin.Add(icon);
            T(root, x + 0.13f, y - 0.005f, info.Name, 0.034f, info.Color * 1.1f + new Color(0.1f, 0.1f, 0.1f)).fontStyle = FontStyle.Bold;
            var cnt = T(root, x + (veg ? 0.42f : 0.47f), y - 0.005f, veg ? "∞" : "×0", 0.04f, veg ? new Color(1f, 0.9f, 0.6f) : new Color(1f, 0.45f, 0.4f));
            cnt.fontStyle = FontStyle.Bold;
            _countLabels.Add(cnt);
            string src = veg ? "" : $"из: {FoodModels.SourceText(id)}";
            var dishes = FoodModels.Dishes(id);
            string dish = dishes.Count > 0 ? "блюда: " + string.Join(", ", dishes) : "";
            if (!veg)
            {
                T(root, x + 0.13f, y - 0.05f, src + "   ·   готовить: " + info.PrepText, 0.022f, new Color(0.75f, 0.9f, 1f));
                T(root, x + 0.13f, y - 0.08f, Clip(dish, 82), 0.021f, new Color(1f, 0.9f, 0.7f));
            }
            else T(root, x + 0.13f, y - 0.05f, info.PrepText + "\n" + Clip(dish, 40), 0.021f, new Color(0.75f, 0.9f, 1f));
            var b = Pressable.Create("Взять (E)", root, new Vector3(x + w - 0.09f, y - 0.075f, -0.01f), new Color(0.25f, 0.55f, 0.35f), null, new Vector3(0.15f, 0.055f, 0.03f), 0.022f);
            b.transform.localRotation = Quaternion.identity;
            b.name = "Btn_Взять_" + id;
            _buttons.Add(b);
        }

        void Bind()
        {
            _close.OnPress = Close;
            _header.text = $"ЛЕДНИК — вся добыча ({Inventory.Fridge.Total} шт.)";
            var text = new System.Text.StringBuilder().AppendLine(_header.text);
            for (int i = 0; i < Fridge.Order.Length; i++)
            {
                var id = Fridge.Order[i];
                var info = Items.Get(id);
                int n = Fridge.Count(id);
                bool veg = Fridge.Infinite(id);
                var button = _buttons[i];
                var count = _countLabels[i];
                _btns[id] = button;
                _counts[id] = count;
                button.OnPress = () => TakeRow(id);
                if (!veg)
                {
                    count.text = $"×{n}";
                    count.color = n > 0 ? new Color(0.55f, 1f, 0.6f) : new Color(1f, 0.45f, 0.4f);
                    button.SetLabel(n > 0 ? "Взять (E)" : "нет");
                    if (n <= 0)
                    {
                        Prim.SetColor(_rowBackgrounds[i], new Color(0.16f, 0.09f, 0.09f));
                        button.SetBaseColor(new Color(0.3f, 0.25f, 0.25f));
                    }
                    button.SetInteractable(n > 0);
                }
                var dishes = FoodModels.Dishes(id);
                string source = veg ? "" : $"из: {FoodModels.SourceText(id)}";
                string dish = dishes.Count > 0 ? "блюда: " + string.Join(", ", dishes) : "";
                text.AppendLine($"{info.Name} {(veg ? "∞" : "×" + n)} | {source} | {dish} | {info.PrepText}");
            }
            LastText = text.ToString();
        }

        static string Clip(string s, int n) => s.Length <= n ? s : s.Substring(0, n - 1) + "…";

        void TakeRow(ItemId id)
        {
            if (Fridge.I == null) return;
            Fridge.I.Take(id);
        }

        void Update()
        {
            foreach (var t in _spin) if (t != null) t.Rotate(0, 40f * Time.unscaledDeltaTime, 0, Space.Self);
            foreach (var kv in _counts)
            {
                if (Fridge.Infinite(kv.Key)) continue;
                int n = Fridge.Count(kv.Key);
                kv.Value.text = $"×{n}";
                kv.Value.color = n > 0 ? new Color(0.55f, 1f, 0.6f) : new Color(1f, 0.45f, 0.4f);
                _btns[kv.Key].SetLabel(n > 0 ? "Взять (E)" : "нет");
                _btns[kv.Key].SetInteractable(n > 0);
            }
            if (Time.unscaledTime - _openedTime < 0.3f) return;
            bool a = XRIn.Primary(XRNode.RightHand), x = XRIn.Primary(XRNode.LeftHand);
            bool vr = (a && !_a) || (x && !_x); _a = a; _x = x;
            var P = PlayerRig.I;
            bool away = P != null && (P.Feet - _openedAt).sqrMagnitude > 1.5f * 1.5f;
            if (DIn.Down(Key.Enter) || DIn.Down(Key.Space) || vr || away || BestiaryCard.Open) Close();
        }

        void OnDestroy() { if (_i == this) _i = null; }
    }
}
