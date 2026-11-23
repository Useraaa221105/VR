using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Панель инструментов СПРАВА СВЕРХУ (ПК), вертикальным столбиком: иконка каждого инструмента его цветом, цифра-клавиша,
    /// выбранный — крупнее и в белой рамке. В подземелье — только охотничьи (1–5) и эликсир (6, ×N), на кухне — 1 нож, 2 половник, 3 тесак, 4 молоток (пункт 7: номера по месту, с 1).
    /// Недоступные (не куплен / сломан) — тусклые с короткой пометкой слева на тёмной подложке. Иконки нарисованы плоскими фигурами.
    /// </summary>
    public class Hotbar : MonoBehaviour
    {
        public static Hotbar I;
        public const float NumFont = 0.052f, NameFont = 0.029f;
        public static readonly ToolType[] Order = { ToolType.Spatula, ToolType.Net, ToolType.Cleaver, ToolType.Hammer, ToolType.Shield, ToolType.Knife, ToolType.Ladle };
        public static readonly ToolType[] DungeonSet = { ToolType.Spatula, ToolType.Net, ToolType.Cleaver, ToolType.Hammer, ToolType.Shield };
        public static readonly ToolType[] KitchenSet = { ToolType.Knife, ToolType.Ladle, ToolType.Cleaver, ToolType.Hammer };
        /// <summary>Для автотеста: какой слот подсвечен и сколько слотов доступно.</summary>
        public ToolType? Selected { get; private set; }
        public int AvailableCount { get; private set; }
        public string StateText { get; private set; } = "";
        /// <summary>Какие инструменты сейчас показаны на панели (для автотеста).</summary>
        public string ShownText { get; private set; } = "";
        public bool IsShown(ToolType t) { foreach (var s in _slots) if (!s.Elixir && s.T == t) return s.Visible; return false; }
        static int KeyOf(Slot s) { int.TryParse(s.Elixir ? ElixirKey : Tools.Key(s.T), out var n); return n; }
        /// <summary>Пункт 5: клавиша и видимость слота эликсира, подпись «×N».</summary>
        public const string ElixirKey = "6";
        public bool ElixirShown { get { foreach (var s in _slots) if (s.Elixir) return s.Visible; return false; } }
        public string ElixirText { get { foreach (var s in _slots) if (s.Elixir) return s.Name.text; return ""; } }
        /// <summary>Левая граница панели в координатах HUD (чтобы строка задачи и карточки не налезали).</summary>
        public float LeftEdge { get; private set; }
        /// <summary>Прямоугольник столбика инструментов (x, верх, ширина, высота) в координатах HUD.</summary>
        public Rect Area { get; private set; }

        [System.Serializable]
        class Slot
        {
            public ToolType T; public Transform Root; public Renderer Bg, Frame, NoteBg; public List<Renderer> Icon = new List<Renderer>(); public List<Color> IconCol = new List<Color>();
            public TextMesh Num, Name, Note; public bool? LastOn; public bool LastSel; public bool Visible = true; public bool Elixir; public int LastCount = -1;
        }
        [SerializeField] List<Slot> _slots = new List<Slot>();
        Transform _root;
        [SerializeField] Renderer _back;
        bool _shown = true;
        bool? _dungeon;
        const float Size = 0.13f, Step = 0.15f;

        PlayerRig P => PlayerRig.I;

        public static Hotbar Create(Transform camera)
        {
            var template = Application.isPlaying ? SavedDungeonAssets.Active?.ToolBar : null;
            var panel = template != null && template._slots.Count == Order.Length + 1
                ? Instantiate(template) : SavedUi.CreateRoot("Hotbar").AddComponent<Hotbar>();
            panel.AttachTo(camera);
            return panel;
        }

        public void AttachTo(Transform cam)
        {
            I = this;
            transform.SetParent(cam, false);
            transform.localPosition = new Vector3(0, 0, HudPanel.Z);
            transform.localRotation = Quaternion.identity;
            _root = transform;
            if (_slots.Count > 0)
            {
                SavedDungeonAssets.RestoreText(gameObject, true);
                return;
            }
            _back = Prim.Quad("HotbarBack", _root, new Vector3(0, 0, 0.004f), new Vector2(Size + 0.04f, Step * 5 + 0.03f), new Color(0, 0, 0, 0.85f));
            _back.sortingOrder = 8; Prim.Hud(_back); Prim.SetOverlayColor(_back, new Color(0, 0, 0, 0.85f));
            for (int i = 0; i < Order.Length; i++) MakeSlot(Order[i], false);
            MakeSlot(ToolType.Spatula, true); // пункт 5: слот «6 — эликсир ×N» (только в подземелье)
        }

        void MakeSlot(ToolType t, bool elixir)
        {
            {
                var s = new Slot { T = t, Elixir = elixir };
                s.Root = new GameObject(elixir ? "Slot_Elixir" : "Slot_" + t).transform;
                s.Root.SetParent(_root, false);
                s.Frame = Prim.Quad("Frame", s.Root, new Vector3(0, 0, 0.003f), new Vector2(Size + 0.014f, Size + 0.014f), Color.white); s.Frame.sortingOrder = 9; Prim.Hud(s.Frame);
                s.Bg = Prim.Quad("Bg", s.Root, new Vector3(0, 0, 0.002f), new Vector2(Size, Size), new Color(0.08f, 0.07f, 0.06f, 1f)); s.Bg.sortingOrder = 10; Prim.Hud(s.Bg);
                if (elixir) DrawElixir(s); else DrawIcon(s, t, Tools.ColorOf(t));
                s.Num = Prim.Text(s.Root, new Vector3(-Size * 0.5f + 0.01f, Size * 0.5f - 0.004f, 0), elixir ? ElixirKey : Tools.Key(t), NumFont, Color.white, TextAnchor.UpperLeft);
                s.Num.fontStyle = FontStyle.Bold;
                s.Name = Prim.Text(s.Root, new Vector3(0, -Size * 0.5f + 0.004f, 0), elixir ? "×0" : Tools.Name(t), NameFont, Color.white, TextAnchor.LowerCenter);
                if (!elixir && Tools.Name(t).Length > 7) s.Name.characterSize *= 0.8f;
                s.NoteBg = Prim.Quad("NoteBg", s.Root, new Vector3(-Size * 0.5f - 0.075f, 0, 0.002f), new Vector2(0.14f, 0.045f), new Color(0, 0, 0, 0.7f)); s.NoteBg.sortingOrder = 10; Prim.Hud(s.NoteBg); Prim.SetOverlayColor(s.NoteBg, new Color(0, 0, 0, 0.7f));
                s.Note = Prim.Text(s.Root, new Vector3(-Size * 0.5f - 0.012f, 0, 0), "", 0.028f, new Color(1f, 0.6f, 0.5f), TextAnchor.MiddleRight);
                foreach (var tm in new[] { s.Num, s.Name, s.Note }) tm.GetComponent<MeshRenderer>().sortingOrder = 12;
                _slots.Add(s);
            }
        }

        /// <summary>Разложить видимые слоты столбиком у правого края под строкой задачи.</summary>
        void Layout(bool dungeon)
        {
            var set = dungeon ? DungeonSet : KitchenSet;
            float x = HudPanel.HalfW - HudPanel.Margin - (Size + 0.04f) * 0.5f;
            float top = HudPanel.HalfH - HudPanel.Margin - HudPanel.TopStrip - 0.01f;
            int k = 0, n = 0;
            foreach (var s0 in _slots) if (s0.Elixir ? dungeon : System.Array.IndexOf(set, s0.T) >= 0) n++;
            // 6 слотов (с эликсиром) — чуть компактнее, чтобы столбик не опускался к низу экрана
            float step = n > 5 ? 0.125f : Step, sc = step / Step;
            var names = new System.Text.StringBuilder();
            // пункт 7: цифры — по текущему месту, столбик сверху вниз по возрастанию номера
            var sorted = new List<Slot>(_slots);
            sorted.Sort((a, b) => KeyOf(a).CompareTo(KeyOf(b)));
            foreach (var s in sorted)
            {
                s.Visible = s.Elixir ? dungeon : System.Array.IndexOf(set, s.T) >= 0;
                s.Root.gameObject.SetActive(s.Visible && _shown);
                if (!s.Visible) continue;
                s.Num.text = s.Elixir ? ElixirKey : Tools.Key(s.T);
                s.Root.localScale = Vector3.one * sc;
                s.Root.localPosition = new Vector3(x, top - 0.015f * sc - Size * sc * 0.5f - k * step, 0);
                names.Append(s.Elixir ? ElixirKey : Tools.Key(s.T)).Append(' ');
                k++;
            }
            _back.transform.localScale = new Vector3(Size + 0.04f, step * k + 0.012f, 1f);
            _back.transform.localPosition = new Vector3(x, top - (step * k + 0.012f) * 0.5f, 0.004f);
            LeftEdge = x - (Size + 0.04f) * 0.5f;
            Area = new Rect(LeftEdge, top, Size + 0.04f, step * k + 0.012f);
            ShownText = names.ToString().TrimEnd();
        }

        // ---------- иконки из плоских фигур ----------
        void Q(Slot s, float x, float y, float w, float h, float rot, Color c)
        {
            var r = Prim.Quad("Ico", s.Root, new Vector3(x, y + 0.014f, 0.001f), new Vector2(w, h), c);
            r.transform.localRotation = Quaternion.Euler(0, 0, rot);
            r.sortingOrder = 11; Prim.Hud(r); s.Icon.Add(r); s.IconCol.Add(c);
        }
        void D(Slot s, float x, float y, float d, Color c, int order = 11)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = "IcoDisc";
            go.transform.SetParent(s.Root, false);
            go.transform.localPosition = new Vector3(x, y + 0.014f, 0.001f);
            go.transform.localRotation = Quaternion.Euler(90, 0, 0);
            go.transform.localScale = new Vector3(d, 0.0005f, d);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Prim.HudMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.sortingOrder = order;
            Prim.SetOverlayColor(r, c);
            s.Icon.Add(r); s.IconCol.Add(c);
        }

        void DrawIcon(Slot s, ToolType t, Color c)
        {
            var wood = new Color(0.65f, 0.42f, 0.22f);
            var dark = new Color(0.08f, 0.07f, 0.06f, 1f);
            switch (t)
            {
                case ToolType.Spatula: Q(s, -0.018f, -0.018f, 0.012f, 0.06f, 45, wood); Q(s, 0.014f, 0.014f, 0.042f, 0.036f, 45, c); break;
                case ToolType.Net: Q(s, -0.022f, -0.022f, 0.01f, 0.06f, 45, wood); D(s, 0.012f, 0.012f, 0.05f, c); D(s, 0.012f, 0.012f, 0.034f, new Color(0.5f, 0.7f, 0.85f, 1f), 12); break;
                case ToolType.Cleaver: Q(s, -0.026f, -0.012f, 0.03f, 0.012f, 0, wood); Q(s, 0.012f, 0f, 0.048f, 0.036f, 0, new Color(0.8f, 0.82f, 0.86f)); Q(s, 0.012f, -0.02f, 0.048f, 0.008f, 0, c); break;
                case ToolType.Hammer: Q(s, 0, -0.015f, 0.012f, 0.05f, 0, wood); Q(s, 0, 0.018f, 0.06f, 0.026f, 0, c); break;
                case ToolType.Shield: D(s, 0, 0.002f, 0.064f, new Color(0.55f, 0.35f, 0.15f)); D(s, 0, 0.002f, 0.052f, c, 12); break;
                case ToolType.Knife: Q(s, -0.02f, -0.02f, 0.01f, 0.03f, 45, wood); Q(s, 0.01f, 0.01f, 0.012f, 0.05f, 45, c); break;
                default: Q(s, -0.012f, 0.008f, 0.01f, 0.055f, 30, wood); D(s, 0.012f, -0.018f, 0.032f, c); break;
            }
        }

        /// <summary>Можно ли сейчас взять инструмент и почему нет (короткая пометка над слотом).</summary>
        public static bool Available(ToolType t, out string note)
        {
            note = "";
            var p = PlayerRig.I;
            if (p == null) return false;
            if (!ToolRules.Allowed(t, out var why, out note)) return false;
            if (t == ToolType.Knife || t == ToolType.Ladle) { if (Tool.FindWorldTool(t) == null) { note = "кухня"; return false; } return true; }
            if (!p.Tools.TryGetValue(t, out var tool)) { note = "не куплен"; return false; }
            if (tool.Broken) { note = "сломан"; return false; }
            return true;
        }

        void Update()
        {
            if (P == null || _root == null) return;
            ToolRules.Tick(P);
            bool show = !P.IsXR && !HudPanel.ModalOpen;
            bool dungeon = DungeonManager.I != null && DungeonManager.I.InDungeon;
            if (_shown != show) { _shown = show; _back.gameObject.SetActive(show); _dungeon = null; }
            if (_dungeon != dungeon || Time.frameCount % 30 == 0) { _dungeon = dungeon; Layout(dungeon); }
            ToolType? sel = P.Right.HeldTool != null ? P.Right.HeldTool.Type : (ToolType?)null;
            bool shieldUp = P.Left.HeldTool != null && P.Left.HeldTool.Type == ToolType.Shield;
            Selected = sel;
            int avail = 0;
            var sb = new System.Text.StringBuilder();
            foreach (var s in _slots)
            {
                if (s.Elixir) { UpdateElixir(s); continue; }
                bool on = Available(s.T, out var note);
                if (on && s.Visible) avail++;
                bool isSel = sel == s.T || (s.T == ToolType.Shield && shieldUp);
                sb.Append(Tools.Key(s.T)).Append(isSel ? "*" : on ? "+" : "-").Append(' ');
                if (s.LastOn == on && s.LastSel == isSel && s.Note.text == note) continue;
                s.LastOn = on; s.LastSel = isSel;
                var col = Tools.ColorOf(s.T);
                float a = on ? 1f : 0.3f;
                Prim.SetOverlayColor(s.Frame, isSel ? new Color(1f, 1f, 1f, 1f) : new Color(col.r, col.g, col.b, on ? 0.55f : 0.15f));
                Prim.SetOverlayColor(s.Bg, isSel ? new Color(col.r * 0.45f, col.g * 0.45f, col.b * 0.45f, 1f) : new Color(0.08f, 0.07f, 0.06f, 1f));
                for (int i = 0; i < s.Icon.Count; i++) { var c = s.IconCol[i]; c.a *= a; Prim.SetOverlayColor(s.Icon[i], c); }
                s.Num.color = isSel ? new Color(1f, 0.95f, 0.5f) : new Color(1, 1, 1, on ? 0.95f : 0.45f);
                s.Name.color = new Color(1, 1, 1, isSel ? 1f : on ? 0.8f : 0.4f);
                s.Note.text = note;
                s.NoteBg.gameObject.SetActive(!string.IsNullOrEmpty(note));
                if (!string.IsNullOrEmpty(note))
                {
                    float w = HudPanel.TextWidth(s.Note) + 0.02f;
                    s.NoteBg.transform.localScale = new Vector3(w, 0.045f, 1f);
                    s.NoteBg.transform.localPosition = new Vector3(-Size * 0.5f - 0.012f - w * 0.5f + 0.01f, 0, 0.002f);
                }
                // выбранный — крупнее (рамка белая) и чуть выдвинут влево, к центру экрана
                s.Root.localScale = Vector3.one * (isSel ? 1.18f : 1f);
            }
            foreach (var s in _slots) if (s.Visible) { var p = s.Root.localPosition; float bx = HudPanel.HalfW - HudPanel.Margin - (Size + 0.04f) * 0.5f; s.Root.localPosition = new Vector3(bx - (s.LastSel ? 0.012f : 0f), p.y, 0); }
            AvailableCount = avail;
            StateText = sb.ToString().TrimEnd();
        }

        void DrawElixir(Slot s)
        {
            var c = Elixirs.Col;
            D(s, 0, -0.006f, 0.05f, c);
            Q(s, 0, 0.026f, 0.016f, 0.02f, 0, new Color(0.85f, 0.9f, 0.95f));
            Q(s, 0, 0.04f, 0.02f, 0.008f, 0, new Color(0.6f, 0.42f, 0.22f));
        }

        void UpdateElixir(Slot s)
        {
            bool on = Elixirs.Count > 0;
            bool isSel = P.Right.ElixirOut;
            if (s.LastOn == on && s.LastSel == isSel && s.LastCount == Elixirs.Count) return;
            s.LastOn = on; s.LastSel = isSel; s.LastCount = Elixirs.Count;
            var col = Elixirs.Col;
            Prim.SetOverlayColor(s.Frame, isSel ? Color.white : new Color(col.r, col.g, col.b, on ? 0.55f : 0.15f));
            Prim.SetOverlayColor(s.Bg, isSel ? new Color(col.r * 0.45f, col.g * 0.45f, col.b * 0.45f, 1f) : new Color(0.08f, 0.07f, 0.06f, 1f));
            for (int i = 0; i < s.Icon.Count; i++) { var c = s.IconCol[i]; c.a *= on ? 1f : 0.3f; Prim.SetOverlayColor(s.Icon[i], c); }
            s.Num.color = isSel ? new Color(1f, 0.95f, 0.5f) : new Color(1, 1, 1, on ? 0.95f : 0.45f);
            s.Name.text = "×" + Elixirs.Count;
            s.Name.color = on ? new Color(1f, 0.8f, 0.9f) : new Color(1, 1, 1, 0.4f);
            s.Note.text = ""; s.NoteBg.gameObject.SetActive(false);
            s.Root.localScale = Vector3.one * (isSel ? 1.18f : 1f);
        }

        void OnDestroy() { if (I == this) I = null; }
    }
}
