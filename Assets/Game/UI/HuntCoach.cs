using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Пункт 3: живая подсказка охоты крупно по центру экрана — «Шаг 1/3: возьми молоток (4)» → «Шаг 2/3: бей ЛКМ 3 раза (1/3)» →
    /// «Шаг 3/3: ЖМИ E — хватай за ногу! 4…». Меняется сама по состоянию ближнего монстра (Monster.Hunt()).
    /// Когда можно бить/хватать — цель (горло, шея, листья…) обводится ярким пульсирующим контуром.
    /// </summary>
    public class HuntCoach : MonoBehaviour
    {
        public static HuntCoach I;
        [SerializeField] TextMesh _text;
        [SerializeField] Renderer _bg;
        [SerializeField] Transform _outline;
        [SerializeField] Renderer[] _edges = new Renderer[12];
        string _shown = "";
        Transform _target;

        /// <summary>Что сейчас написано (для автотеста).</summary>
        public static string Shown => I != null && I.Visible_ ? I._shown : "";
        public static bool Visible => I != null && I.Visible_;
        public static Monster For { get; private set; }
        public static HuntStep Step { get; private set; }
        public static bool ToolStep { get; private set; }
        /// <summary>Что обведено контуром (null — контура нет).</summary>
        public static Transform Outlined => I != null && I._outline != null && I._outline.gameObject.activeSelf ? I._target : null;
        public static Rect Area => !Visible ? Rect.zero :
            new Rect(-I._bg.transform.localScale.x * 0.5f, I.transform.localPosition.y + I._bg.transform.localScale.y * 0.5f, I._bg.transform.localScale.x, I._bg.transform.localScale.y);
        bool Visible_ => _bg != null && _bg.gameObject.activeSelf;

        public const float Y = -0.17f;
        /// <summary>Только для снимков «было» в автотесте: выключить подсказку и контур.</summary>
        public static bool Off;

        public static HuntCoach Create(Transform camera)
        {
            var template = Application.isPlaying ? SavedDungeonAssets.Active?.HuntCoachCanvas : null;
            var panel = template != null ? Instantiate(template).GetComponent<HuntCoach>() :
                SavedUi.CreateRoot("HuntCoach").AddComponent<HuntCoach>();
            panel.AttachTo(camera);
            return panel;
        }

        public void AttachTo(Transform cam)
        {
            I = this;
            transform.SetParent(cam, false);
            transform.localPosition = new Vector3(0, Y, 1.2f); // по центру, над строкой обычных подсказок (−0,36)
            transform.localRotation = Quaternion.identity;
            if (_text == null) BuildVisual();
            else SavedDungeonAssets.RestoreText(gameObject, true);
            if (Application.isPlaying) _outline.SetParent(null, false);
        }

        void BuildVisual()
        {
            _bg = Prim.Quad("HuntBg", transform, new Vector3(0, 0, 0.01f), new Vector2(1f, 0.1f), new Color(0, 0, 0, 0.8f));
            _bg.sortingOrder = 2; Prim.Hud(_bg); Prim.SetOverlayColor(_bg, new Color(0.05f, 0.03f, 0.02f, 0.88f));
            _text = Prim.Text(transform, Vector3.zero, "", 0.062f, Color.white);
            _text.GetComponent<MeshRenderer>().sortingOrder = 7;
            _outline = new GameObject("StrikeOutline").transform;
            _outline.SetParent(transform, false);
            for (int i = 0; i < 12; i++)
            {
                var e = Prim.Cube("Edge" + i, _outline, Vector3.zero, Vector3.one * 0.02f, new Color(1f, 0.9f, 0.3f), false);
                _edges[i] = e.GetComponent<Renderer>();
            }
            _outline.gameObject.SetActive(false);
            Show(false);
        }

        void Show(bool v)
        {
            if (_bg.gameObject.activeSelf != v) { _bg.gameObject.SetActive(v); _text.gameObject.SetActive(v); }
        }

        static Monster Pick()
        {
            var P = PlayerRig.I;
            Monster best = null; float bd = 999f;
            foreach (var m in MonsterRegistry.Alive)
            {
                if (m == null || m.Dead) continue;
                float range = m.Kind == MonsterKind.Dragon ? 25f : 8f;
                float d = (m.transform.position - P.Feet).magnitude;
                if (d > range || d >= bd) continue;
                if (m.Hunt().Total <= 0) continue; // мимик в маскировке — не выдаём
                bd = d; best = m;
            }
            return best;
        }

        void LateUpdate()
        {
            var P = PlayerRig.I;
            var dm = DungeonManager.I;
            Monster m = null;
            if (!Off && HintsToggle.On && P != null && dm != null && dm.InDungeon && dm.Level != 1 && dm.Level != 2 && !HudPanel.ModalOpen && P.Health.HP > 0 && !(Fader.I != null && Fader.I.Busy)) m = Pick();
            For = m;
            if (m == null) { Show(false); _outline.gameObject.SetActive(false); _target = null; return; }

            var s = m.Hunt();
            Step = s;
            var need = Guide.RequiredTool(m.Kind);
            var held = P.Right.HeldTool != null ? P.Right.HeldTool.Type : (ToolType?)null;
            bool hasTool = held == need;
            ToolStep = !hasTool && !s.NoTool;
            string msg; Color col;
            string tool = Tools.Name(need).ToLower();
            string key = P.IsXR ? Tools.VrSlot(need) : Tools.Key(need);
            if (ToolStep)
            {
                bool wrong = held.HasValue && held.Value != ToolType.Shield;
                msg = wrong ? $"Шаг 1/{s.Total}: не тот инструмент! Нужно: {tool} — {(P.IsXR ? key : "жми " + key)} (в руке {Tools.Name(held.Value).ToLower()})"
                            : $"Шаг 1/{s.Total}: возьми {(tool.EndsWith("а") ? tool.Substring(0, tool.Length - 1) + "у" : tool)} ({key})";
                col = wrong ? new Color(1f, 0.55f, 0.5f) : new Color(1f, 0.95f, 0.85f);
            }
            else
            {
                msg = $"Шаг {s.Step}/{s.Total}: {s.Text}";
                if (s.Strike && m.WindowBarVisible && m.WindowLeft > 0)
                {   // обратный отсчёт: «4…3…2…» — текущая секунда и следующая
                    int sec = Mathf.CeilToInt(m.WindowLeft);
                    msg += sec > 1 ? $"  {sec}…{sec - 1}…" : $"  {sec}…";
                }
                col = s.Strike ? new Color(1f, 0.82f, 0.2f) : new Color(1f, 0.95f, 0.85f);
            }
            if (msg != _shown)
            {
                _shown = msg;
                string text = msg;
                if (msg.Length > 50)
                {   // длинная — в две строки по пробелу ближе к середине
                    int mid = msg.Length / 2, a = msg.LastIndexOf(' ', mid), b = msg.IndexOf(' ', mid);
                    int cut = b > 0 && (a < 0 || b - mid < mid - a) ? b : a;
                    if (cut > 0) text = msg.Substring(0, cut) + "\n" + msg.Substring(cut + 1);
                }
                _text.text = text;
                int lines = text.Split('\n').Length;
                _bg.transform.localScale = new Vector3(Mathf.Clamp(HudPanel.TextWidth(_text) + 0.07f, 0.4f, 2f * HudPanel.HalfW - 0.06f), 0.072f * lines + 0.035f, 1f);
            }
            _text.color = col;
            Show(true);

            // контур цели: только когда сейчас можно бить/хватать и в руке правильный инструмент (или шаг — рукой)
            var t = s.Strike && s.Target != null && s.Target.gameObject.activeInHierarchy && (hasTool || s.NoTool) ? s.Target : null;
            _target = t;
            if (t == null) { if (_outline.gameObject.activeSelf) _outline.gameObject.SetActive(false); return; }
            if (!_outline.gameObject.activeSelf) _outline.gameObject.SetActive(true);
            _outline.SetPositionAndRotation(t.position, t.rotation);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 8f);
            var size = t.lossyScale + Vector3.one * (0.05f + 0.02f * pulse);
            float th = 0.03f + 0.015f * pulse;
            int i = 0;
            for (int ax = 0; ax < 3; ax++)
                for (int sa = -1; sa <= 1; sa += 2)
                    for (int sb = -1; sb <= 1; sb += 2)
                    {
                        int a1 = (ax + 1) % 3, a2 = (ax + 2) % 3;
                        var p = Vector3.zero; p[a1] = sa * size[a1] * 0.5f; p[a2] = sb * size[a2] * 0.5f;
                        var sc = Vector3.one * th; sc[ax] = size[ax] + th;
                        var e = _edges[i++].transform;
                        e.localPosition = p; e.localScale = sc;
                    }
            var c = Color.Lerp(new Color(1f, 0.75f, 0.1f), new Color(1f, 1f, 0.6f), pulse);
            foreach (var r in _edges) Prim.SetColor(r, c, c * (2f + pulse));
        }

        void OnDestroy()
        {
            if (_outline != null && _outline.parent == null) Destroy(_outline.gameObject);
            if (I == this) { I = null; For = null; }
        }
    }
}
