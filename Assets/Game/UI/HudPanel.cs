using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Экранный HUD для ПК (камера): общая разметка экрана — чтобы надписи никогда не налезали друг на друга.
    ///   верх по центру — строка ЗАДАЧА/ОБУЧЕНИЕ (одна строка, Guide);
    ///   слева сверху — крупная полоска HP с цифрами (зелёная → жёлтая → красная), под ней монеты и маленькой строкой место (этаж/комната);
    ///   справа сверху — панель инструментов (Hotbar, вертикально);
    ///   середина — плашки добычи (Banner) и подсказка (Hints) чуть ниже центра; низ экрана свободен.
    /// Все тексты — на тёмной подложке. Размеры считаются от поля зрения и пропорций окна (ноутбук 16:9, 16:10, 4:3).
    /// </summary>
    public class HudPanel : MonoBehaviour
    {
        public static HudPanel I;
        public const float Z = 1.2f;          // плоскость HUD перед камерой
        public const float Margin = 0.03f;
        public const float TopStrip = 0.085f; // высота строки задачи сверху
        public const float PanelW = 0.62f;
        public const float HpFont = 0.046f;
        /// <summary>Открыт большой экран (карточка монстра, витрина ледника) — второстепенный HUD прячется.</summary>
        public static bool ModalOpen => BestiaryCard.Open || FridgeScreen.IsOpen || HuntIntro.Open || DungeonSettings.Open || BackpackView.Open;

        /// <summary>Половина высоты/ширины видимой области на расстоянии Z (м).</summary>
        public static float HalfH => Mathf.Tan((PlayerRig.I != null && PlayerRig.I.Cam != null ? PlayerRig.I.Cam.fieldOfView : 60f) * 0.5f * Mathf.Deg2Rad) * Z;
        public static float HalfW => HalfH * (PlayerRig.I != null && PlayerRig.I.Cam != null ? PlayerRig.I.Cam.aspect : 16f / 9f);

        // для автотеста
        public string HpText => _hp != null ? _hp.text : "";
        public string CoinsText => _coins != null ? _coins.text : "";
        public string PlaceText => _place != null ? _place.text : "";
        public Color BarColor { get; private set; }
        public float BarFrac { get; private set; }
        /// <summary>Прямоугольник панели HP в координатах HUD (x, y — левый верхний угол; w, h).</summary>
        public Rect Area { get; private set; }

        [SerializeField] Transform _root;
        [SerializeField] Renderer _bg, _barBg, _bar, _coin, _coinIn;
        [SerializeField] TextMesh _hpLabel, _hp, _coins, _place;

        public static HudPanel Create(Transform camera)
        {
            var template = Application.isPlaying ? SavedDungeonAssets.Active?.Hud : null;
            var panel = template != null ? Instantiate(template) :
                SavedUi.CreateRoot("HudPanel").AddComponent<HudPanel>();
            panel.AttachTo(camera);
            return panel;
        }
        float _next, _lastHp = -1f;

        PlayerRig P => PlayerRig.I;

        public void AttachTo(Transform cam)
        {
            I = this;
            transform.SetParent(cam, false);
            transform.localPosition = new Vector3(0, 0, Z);
            transform.localRotation = Quaternion.identity;
            if (_root == null) Build();
            else SavedDungeonAssets.RestoreText(gameObject, true);
            Layout();
        }

        void Build()
        {
            _root = new GameObject("HudLeft").transform;
            _root.SetParent(transform, false);
            _bg = Q("Bg", new Vector3(PanelW * 0.5f, -0.125f, 0.004f), new Vector2(PanelW, 0.25f), new Color(0.02f, 0.015f, 0.01f, 1f), 1); // непрозрачная: при 0.97 надписи мира (доска рецептов) ещё слегка просвечивали
            _hpLabel = T(new Vector3(0.02f, -0.052f, 0), "HP", 0.05f, new Color(1f, 0.85f, 0.8f), TextAnchor.MiddleLeft);
            _hpLabel.fontStyle = FontStyle.Bold;
            _barBg = Q("BarBg", new Vector3(0.1f + 0.25f, -0.052f, 0.003f), new Vector2(0.5f, 0.062f), new Color(0.15f, 0.05f, 0.05f, 0.95f), 2);
            _bar = Q("Bar", new Vector3(0.1f + 0.25f, -0.052f, 0.002f), new Vector2(0.49f, 0.052f), Color.green, 3);
            _hp = T(new Vector3(0.35f, -0.052f, 0), "", HpFont, Color.white, TextAnchor.MiddleCenter);
            _hp.fontStyle = FontStyle.Bold;
            _coin = Disc(new Vector3(0.045f, -0.125f, 0.002f), 0.05f, new Color(0.75f, 0.5f, 0.1f, 1f), 3);
            _coinIn = Disc(new Vector3(0.045f, -0.125f, 0.001f), 0.036f, new Color(1f, 0.82f, 0.25f, 1f), 4);
            _coins = T(new Vector3(0.085f, -0.125f, 0), "", HpFont, new Color(1f, 0.88f, 0.4f), TextAnchor.MiddleLeft);
            _coins.fontStyle = FontStyle.Bold;
            _place = T(new Vector3(0.02f, -0.19f, 0), "", 0.032f, new Color(0.85f, 0.93f, 1f), TextAnchor.MiddleLeft);
        }

        Renderer Q(string n, Vector3 p, Vector2 s, Color c, int order)
        {
            var r = Prim.Quad(n, _root, p, s, c);
            r.sortingOrder = order; Prim.Hud(r); Prim.SetOverlayColor(r, c);
            return r;
        }
        Renderer Disc(Vector3 p, float d, Color c, int order)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = "Coin";
            go.transform.SetParent(_root, false);
            go.transform.localPosition = p;
            go.transform.localRotation = Quaternion.Euler(90, 0, 0);
            go.transform.localScale = new Vector3(d, 0.0005f, d);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Prim.HudMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.sortingOrder = order;
            Prim.SetOverlayColor(r, c);
            return r;
        }
        TextMesh T(Vector3 p, string s, float h, Color c, TextAnchor a)
        {
            var t = Prim.Text(_root, p, s, h, c, a);
            t.GetComponent<MeshRenderer>().sortingOrder = 6;
            return t;
        }

        void Layout()
        {
            float x = -HalfW + Margin, y = HalfH - Margin - TopStrip - 0.01f;
            _root.localPosition = new Vector3(x, y, 0);
            Area = new Rect(x, y, PanelW, 0.25f);
        }

        float _nextSort;
        static readonly List<Renderer> _rs = new List<Renderer>();
        /// <summary>Порядок сортировки в Unity важнее очереди шейдера: таблички в мире с sortingOrder 5 рисовались поверх подложек HUD.
        /// Всё, что висит на камере (HUD, плашки, вспышка урона, предмет в руке), поднимается на +1000 — относительный порядок внутри HUD сохраняется.</summary>
        public static void LiftHudSorting(Transform cam)
        {
            if (cam == null) return;
            cam.GetComponentsInChildren(true, _rs);
            foreach (var r in _rs)
                if (r != null && r.sortingOrder < 1000 && r.name != "Overlay") r.sortingOrder += 1000; // «Overlay» — вспышка урона/статуса: не поверх карточек
            _rs.Clear();
        }

        /// <summary>Ширина текста TextMesh в метрах (по метрикам шрифта) — чтобы подогнать подложку и не налезать на соседей.</summary>
        public static float TextWidth(TextMesh t, string s = null)
        {
            s = s ?? t.text;
            if (string.IsNullOrEmpty(s) || t.font == null) return 0f;
            int size = t.fontSize > 0 ? t.fontSize : 64;
            t.font.RequestCharactersInTexture(s, size, t.fontStyle);
            float w = 0, best = 0;
            foreach (var ch in s)
            {
                if (ch == '\n') { best = Mathf.Max(best, w); w = 0; continue; }
                if (t.font.GetCharacterInfo(ch, out var ci, size, t.fontStyle)) w += ci.advance;
                else w += size * 0.5f;
            }
            best = Mathf.Max(best, w);
            return best * t.characterSize * 0.1f;
        }

        void Update()
        {
            if (P == null || _root == null) return;
            bool hide = (BestiaryCard.Open && BestiaryCard.HidesHud) || FridgeScreen.IsOpen; // большой экран ледника — HUD не налезает на него
            if (_root.gameObject.activeSelf == hide) _root.gameObject.SetActive(!hide);
            if (Time.unscaledTime < _next && Mathf.Approximately(_lastHp, P.Health.HP)) return;
            _lastHp = P.Health.HP;
            _next = Time.unscaledTime + 0.1f;
            if (Time.unscaledTime >= _nextSort) { _nextSort = Time.unscaledTime + 0.5f; LiftHudSorting(P.Cam != null ? P.Cam.transform : transform.parent); } // 10 раз в секунду, без строк в каждом кадре
            Layout();
            float hp = P.Health.HP, max = PlayerHealth.Max;
            float f = Mathf.Clamp01(hp / Mathf.Max(1f, max));
            BarFrac = f;
            BarColor = f > 0.6f ? new Color(0.25f, 0.85f, 0.3f) : f > 0.3f ? new Color(1f, 0.82f, 0.15f) : new Color(1f, 0.25f, 0.2f);
            _bar.transform.localScale = new Vector3(0.49f * Mathf.Max(0.001f, f), 0.052f, 1f);
            _bar.transform.localPosition = new Vector3(0.1f + 0.005f + 0.49f * f * 0.5f, -0.052f, 0.002f);
            Prim.SetOverlayColor(_bar, BarColor);
            string hpS = $"{hp:0} / {max:0}";
            if (_hp.text != hpS) _hp.text = hpS;
            string c = $"{Wallet.Coins} монет";
            if (_coins.text != c) _coins.text = c;
            var dm = DungeonManager.I;
            string place;
            if (dm != null && dm.InDungeon) place = $"{DungeonManager.GetBiome(dm.Level).Name} · {dm.RoomNameAt(P.Feet)} · рюкзак {Inventory.Backpack.Total}/{Inventory.Backpack.Capacity}";
            else place = Kitchen.I != null && Kitchen.I.InKitchen(P.Feet) ? "Таверна · кухня" : "Таверна «Подземелье вкусностей»";
            if (Guide.God) place += " · [F8] бессмертие";
            // место не шире панели: длинное — уменьшаем шрифт, потом обрезаем
            if (_place.text != place)
            {
                _place.characterSize = 0.032f * 10f / 64f;
                _place.text = place;
                float w = TextWidth(_place);
                if (w > PanelW - 0.04f) _place.characterSize *= Mathf.Max(0.75f, (PanelW - 0.04f) / w);
                while (TextWidth(_place) > PanelW - 0.04f && _place.text.Length > 8) _place.text = _place.text.Substring(0, _place.text.Length - 2) + "…";
            }
        }

        void OnDestroy() { if (I == this) I = null; }
    }
}
