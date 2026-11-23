using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    public enum SoupState { Empty, Water, Cooking, Ready, Ruined, Burnt }

    /// <summary>Очаг: один на все котлы. Жар 0–3, сам падает на 1 каждые 25 с, полено +1 (этап 2+).</summary>
    public class Hearth
    {
        public int Heat = 2;
        float _t;
        public void Tick(float dt, bool enabled)
        {
            if (!enabled) { Heat = 2; return; }
            _t += dt;
            if (_t >= 25f) { _t = 0; Heat = Mathf.Max(0, Heat - 1); if (Heat <= 1) Hints.Show($"Жар падает ({Heat}) — подкинь полено", HintPri.Action); }
        }
        public void AddLog() { Heat = Mathf.Min(3, Heat + 1); _t = 0; }
        // Снимок лишь для временной проверки этапов; обычные сроки/правила очага не меняются.
        public Hearth Copy() => new Hearth { Heat = Heat, _t = _t };
        public void Restore(Hearth snapshot) { if (snapshot != null) { Heat = snapshot.Heat; _t = snapshot._t; } }
        public static float Mul(int h) => h <= 0 ? 0f : h == 1 ? 0.5f : h == 2 ? 1f : 1.5f;
    }

    /// <summary>
    /// Котёл: Empty → Water → Cooking → Ready (большой 3 порции, маленький 1) / Ruined / Burnt. Продукт бросают в горло (триггер).
    /// Этап 2+: жар и помешивание (каждые 8 с, окно 5 с, 2 круга половником; ПК — E с половником = 1 круг).
    /// Этап 3: специи. Качество: 0 ошибок ★★★, 1 — ★★, 2+ — ★.
    /// </summary>
    public class Cauldron : MonoBehaviour
    {
        public static readonly List<Cauldron> All = new List<Cauldron>();
        public SoupState State { get; private set; }
        public Recipe Recipe { get; private set; }
        public int Portions { get; private set; }
        public bool Large { get; private set; }
        [SerializeField, Min(1)] int largePortions = 3, smallPortions = 1;
        [SerializeField] Pressable _waterButton, _fireButton, _serveButton, _drainButton;
        public int BatchPortions => Large ? largePortions : smallPortions;
        public Vector3 MouthPosition => transform.TransformPoint(new Vector3(0,1.2f,.05f));
        public float WorldRimY => transform.TransformPoint(new Vector3(0,RimY,0)).y;
        public int Stars => _errors == 0 ? 3 : _errors == 1 ? 2 : 1;

        readonly List<ItemId> _contents = new List<ItemId>();
        public IReadOnlyList<ItemId> Contents => _contents;
        public float Progress01 => Recipe != null && Recipe.CookTime > 0 ? Mathf.Clamp01(_progress / Recipe.CookTime) : 0f;
        readonly Dictionary<Spice, int> _spices = new Dictionary<Spice, int>();
        float _progress, _stirAt, _stirWindowUntil, _angleAcc, _lastAngle;
        int _stirs, _errors;
        bool _heatWarned, _oversalted, _unwashed, _hasLastAngle;
        [SerializeField] Renderer _soup;
        [SerializeField] TextMesh _label;
        Transform _serveSpot;
        [SerializeField] List<GameObject> _spiceButtons = new List<GameObject>();
        int _stage;

        public static Cauldron Create(Transform parent, Vector3 localPos, float yaw, Transform serveSpot, bool large = true)
        {
            var assets = SavedDungeonAssets.Active;
            var prefab = large ? assets?.LargeCauldron : assets?.SmallCauldron;
            if (prefab != null && prefab._waterButton != null && prefab._soup != null)
            {
                var saved = Instantiate(prefab, parent, false);
                saved.transform.SetLocalPositionAndRotation(localPos, Quaternion.Euler(0, yaw, 0));
                saved._serveSpot = serveSpot;
                saved.Large = large;
                saved.BindSavedButtons();
                saved.Reset(SoupState.Empty);
                All.Add(saved);
                return saved;
            }
            var go = new GameObject("Cauldron");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var c = go.AddComponent<Cauldron>();
            c._serveSpot = serveSpot; c.Large = large; go.name = large ? "Большой казан" : "Маленький котёл";
            c.Build();
            go.transform.localScale = large ? new Vector3(1.3f,1.05f,1.3f) : new Vector3(.82f,1f,.82f);
            All.Add(c);
            return c;
        }

        void BindSavedButtons()
        {
            _waterButton.OnPress = AddWater;
            _fireButton.OnPress = AddFire;
            _serveButton.OnPress = Serve;
            _drainButton.OnPress = Drain;
            Spice[] spices = { Spice.Salt, Spice.Pepper, Spice.Herbs, Spice.Fire };
            for (int i = 0; i < spices.Length; i++)
            {
                var spice = spices[i];
                _spiceButtons[i].GetComponent<Pressable>().OnPress = () => AddSpice(spice);
            }
            foreach (var label in GetComponentsInChildren<TextMesh>(true))
                label.GetComponent<Renderer>().sharedMaterial = Prim.DepthTextMaterial;
        }

        void OnDestroy() { All.Remove(this); }

        // ---------- внешний вид (баг-лист, пункт 3): каменная печь с открытым огнём + чугунный казан на ножках ----------
        [SerializeField] Transform _flames, _floaters, _steam, _bubbles, _icons;
        [SerializeField] Light _fireLight;
        [SerializeField] TextMesh _heatText;
        [SerializeField] List<Transform> _flameParts = new List<Transform>();
        readonly List<ItemId> _shownFloaters = new List<ItemId>();
        string _iconsFor = "";
        bool _starterFloaters;
        public static readonly Color WaterColor = new Color(0.2f, 0.55f, 1f);
        /// <summary>Цвет содержимого сейчас (для автотеста: вода синяя, суп меняет цвет).</summary>
        public Color ContentColor { get; private set; }
        public int FlameLevel { get; private set; }
        public int FloaterCount => _floaters != null ? _floaters.childCount : 0;
        public bool SteamOn => _steam != null && _steam.gameObject.activeSelf;
        public bool BubblesOn => _bubbles != null && _bubbles.gameObject.activeSelf;
        public bool LabelShown => _label != null && _label.gameObject.activeSelf;
        public string LabelNow => _label != null ? _label.text : "";
        public string HeatTextNow => _heatText != null ? _heatText.text : "";
        public int IconsOk { get; private set; }
        public int IconsMissing { get; private set; }
        public const float RimY = 1.14f;
        public bool StirNow => _stirWindowUntil > 0;
        /// <summary>К3: сколько кругов половником нужно за одно окно «Мешай!» и сколько длится окно.</summary>
        public const int StirsNeeded = 2;
        public const float StirWindow = 5f, StirNagAfter = 2.5f;
        public int Stirs => _stirs;
        /// <summary>Автотест: открыть окно «Мешай!» прямо сейчас (не ждать 8 с).</summary>
        public void DebugStirSoon() { if (State == SoupState.Cooking) _stirAt = DTime.Now; }
        /// <summary>Нужно ли по рецепту мешать (на этапе 1 — нет).</summary>
        public bool NeedsStir => _stage >= 2;
        /// <summary>Сколько секунд уже открыто окно «Мешай!» (для тренера «Помешай суп — …»).</summary>
        public float StirOpenFor => StirNow ? StirWindow - (_stirWindowUntil - DTime.Now) : 0f;
        public bool StirBarShown => _stirRoot != null && _stirRoot.gameObject.activeSelf;
        public string StirTextNow => _stirText != null ? _stirText.text : "";
        public string StirBarTextNow => _stirBarText != null ? _stirBarText.text : "";
        public float StirBarFill => _stirFill != null ? _stirFill.localScale.x / StirBarW : 0f;
        [SerializeField] Transform _stirRoot, _stirFill;
        [SerializeField] TextMesh _stirText, _stirBarText;
        const float StirBarW = 0.56f;
        /// <summary>Какую клавишу жать, чтобы мешать: с половником в руке — E, иначе сначала взять половник.</summary>
        public static string StirKey()
        {
            var P = PlayerRig.I;
            if (P != null && P.IsXR) return "крути половником в котле";
            bool ladle = P != null && P.Right != null && P.Right.HeldTool != null && P.Right.HeldTool.Type == ToolType.Ladle;
            return ladle ? "E" : $"{Tools.KitchenKey(ToolType.Ladle)}, потом E";
        }

        void Build()
        {
            var t = transform;
            var stone = new Color(0.46f, 0.43f, 0.4f);
            var iron = new Color(0.13f, 0.13f, 0.14f);
            // каменная печь: блок из камней + камни-кольцо очага сверху + дрова
            Prim.Cube("Base", t, new Vector3(0, 0.25f, 0.05f), new Vector3(0.95f, 0.5f, 0.95f), stone);
            for (int i = 0; i < 6; i++) // «кладка» — тёмные швы
                Prim.Cube("Seam", t, new Vector3(0, 0.08f + i * 0.08f, -0.427f), new Vector3(0.96f, 0.012f, 0.01f), stone * 0.6f, false);
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f;
                Prim.Shape(PrimitiveType.Sphere, "HearthStone", t, new Vector3(Mathf.Sin(a) * 0.4f, 0.53f, 0.05f + Mathf.Cos(a) * 0.4f), new Vector3(0.17f, 0.1f, 0.15f), stone * (0.85f + 0.1f * (i % 3)), false);
            }
            for (int i = 0; i < 3; i++)
            {
                var log = Prim.Shape(PrimitiveType.Cylinder, "Log", t, new Vector3(0, 0.55f, 0.05f), new Vector3(0.08f, 0.24f, 0.08f), new Color(0.35f, 0.2f, 0.1f), false);
                log.transform.localRotation = Quaternion.Euler(90, i * 60f, 0);
            }
            // открытый огонь между печью и казаном: языки пламени, размер = сила огня (1 маленький, 2 средний, 3 большой)
            _flames = new GameObject("Flames").transform; _flames.SetParent(t, false); _flames.localPosition = new Vector3(0, 0.56f, 0.05f);
            for (int i = 0; i < 7; i++)
            {
                float a = i * Mathf.PI * 2f / 6f;
                var pos = i == 6 ? Vector3.zero : new Vector3(Mathf.Sin(a) * 0.16f, 0, Mathf.Cos(a) * 0.16f);
                var col = i % 2 == 0 ? new Color(1f, 0.45f, 0.08f) : new Color(1f, 0.8f, 0.2f);
                var f = Prim.Shape(PrimitiveType.Sphere, "Flame", _flames, pos, new Vector3(0.1f, 0.22f, 0.1f), col, false);
                Prim.SetEmission(f.GetComponent<Renderer>(), col * 2.2f);
                _flameParts.Add(f.transform);
            }
            _fireLight = Prim.PointLight(t, new Vector3(0, 0.75f, -0.3f), new Color(1f, 0.55f, 0.2f), 3f, 1.2f);
            // ножки-тренога казана
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f + 0.5f;
                var leg = Prim.Shape(PrimitiveType.Cylinder, "PotLeg", t, new Vector3(Mathf.Sin(a) * 0.27f, 0.64f, 0.05f + Mathf.Cos(a) * 0.27f), new Vector3(0.04f, 0.12f, 0.04f), iron, false);
                leg.transform.localRotation = Quaternion.Euler(Mathf.Cos(a) * 12f, 0, -Mathf.Sin(a) * 12f);
            }
            // круглый чугунный казан: шарообразное дно + стенки + толстый обод + две «ушки» + дужка-ручка
            Prim.Shape(PrimitiveType.Sphere, "PotBottom", t, new Vector3(0, 0.86f, 0.05f), new Vector3(0.7f, 0.34f, 0.7f), iron);
            Prim.Shape(PrimitiveType.Cylinder, "PotWall", t, new Vector3(0, 0.96f, 0.05f), new Vector3(0.7f, 0.12f, 0.7f), iron, false);
            for (int i = 0; i < 20; i++)
            {
                float a = i * Mathf.PI * 2f / 20f;
                var seg = Prim.Cube("PotRim", t, new Vector3(Mathf.Sin(a) * 0.35f, RimY - 0.03f, 0.05f + Mathf.Cos(a) * 0.35f), new Vector3(0.12f, 0.06f, 0.05f), iron * 1.4f, false);
                seg.transform.localRotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0);
            }
            for (int sd = -1; sd <= 1; sd += 2)
                Prim.Cube("PotEar", t, new Vector3(sd * 0.39f, 1.07f, 0.05f), new Vector3(0.06f, 0.06f, 0.1f), iron * 1.4f, false);
            for (int i = 0; i <= 10; i++)
            {
                float a = Mathf.PI * i / 10f;
                var h = Prim.Cube("PotHandle", t, new Vector3(Mathf.Cos(a) * 0.39f, 1.07f + Mathf.Sin(a) * 0.3f, 0.05f), new Vector3(0.025f, 0.08f, 0.025f), iron * 1.6f, false);
                h.transform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg);
            }
            // содержимое: диск жидкости сверху (видно сверху), плавающие фигурки продуктов, пар, пузыри
            _soup = SoupVisual.Disc(t, new Vector3(0, 1.085f, .05f), .32f, new Color(.08f,.08f,.08f));
            _floaters = new GameObject("Floaters").transform; _floaters.SetParent(t, false); _floaters.localPosition = new Vector3(0, 1.1f, 0.05f);
            _steam = new GameObject("Steam").transform; _steam.SetParent(t, false); _steam.localPosition = new Vector3(0, 1.15f, 0.05f);
            for (int i = 0; i < 5; i++)
            {
                var puff=Prim.Shape(PrimitiveType.Sphere,"Puff",_steam,new Vector3(Mathf.Sin(i*1.3f)*.12f,i*.12f,Mathf.Cos(i*1.7f)*.12f),Vector3.one*.06f,Color.white,false);
                var renderer=puff.GetComponent<Renderer>();renderer.sharedMaterial=GameAssets.I.overlay;
                Prim.SetOverlayColor(renderer,new Color(.9f,.93f,.96f,.13f));
            }
            _bubbles = new GameObject("Bubbles").transform; _bubbles.SetParent(t, false); _bubbles.localPosition = new Vector3(0, 1.09f, 0.05f);
            for (int i = 0; i < 6; i++) Prim.Shape(PrimitiveType.Sphere, "Bubble", _bubbles, new Vector3(Mathf.Sin(i * 2.4f) * 0.2f, 0, Mathf.Cos(i * 2.4f) * 0.2f), Vector3.one * 0.04f, new Color(0.85f, 0.9f, 1f), false);
            var mouth = new GameObject("Mouth");
            mouth.transform.SetParent(t, false);
            mouth.transform.localPosition = new Vector3(0, 1.2f, 0.05f);
            var bc = mouth.AddComponent<BoxCollider>();
            bc.isTrigger = true; bc.size = new Vector3(0.62f, 0.3f, 0.62f);
            mouth.AddComponent<CauldronMouth>().Owner = this;
            // ОДНА табличка над котлом (только у котла, к которому подошёл игрок) + значки продуктов ✓/✗
            _label = Prim.Text(t, new Vector3(0, 1.62f, -0.1f), "", 0.075f, Color.white);
            _label.fontStyle = FontStyle.Bold;
            _icons = new GameObject("NeedIcons").transform; _icons.SetParent(t, false); _icons.localPosition = new Vector3(0, 1.5f, -0.1f);
            // К3: крупно «Мешать — E» и полоска «Помешано 2/3» (видна, пока открыто окно «Мешай!»)
            _stirRoot = new GameObject("StirPanel").transform; _stirRoot.SetParent(t, false); _stirRoot.localPosition = new Vector3(0, 1.36f, -0.42f);
            Prim.Cube("StirBg", _stirRoot, new Vector3(0, 0.02f, 0.012f), new Vector3(0.9f, 0.26f, 0.01f), new Color(0.12f, 0.08f, 0.03f), false);
            _stirText = Prim.Text(_stirRoot, new Vector3(0, 0.075f, -0.004f), "", 0.085f, new Color(1f, 0.85f, 0.2f)); _stirText.fontStyle = FontStyle.Bold;
            Prim.Cube("StirBarBg", _stirRoot, new Vector3(0, -0.035f, 0.004f), new Vector3(StirBarW, 0.06f, 0.01f), new Color(0.3f, 0.27f, 0.24f), false);
            var fill = Prim.Cube("StirBarFill", _stirRoot, new Vector3(-StirBarW * 0.5f, -0.035f, -0.002f), new Vector3(0.001f, 0.06f, 0.01f), new Color(0.3f, 0.9f, 0.35f), false);
            Prim.SetEmission(fill.GetComponent<Renderer>(), new Color(0.15f, 0.5f, 0.15f));
            _stirFill = fill.transform;
            _stirBarText = Prim.Text(_stirRoot, new Vector3(0, -0.035f, -0.012f), "", 0.045f, Color.white);
            foreach (var col in _stirRoot.GetComponentsInChildren<Collider>()) Destroy(col);
            _stirRoot.gameObject.SetActive(false);
            // крупные кнопки с иконками перед печью, на наклонной полке (к игроку = −Z локально)
            var panel = new GameObject("Panel").transform;
            panel.SetParent(t, false);
            panel.localPosition = new Vector3(0, 0, -0.52f);
            Prim.Cube("ButtonShelf", panel, new Vector3(0, 0.6f, 0.04f), new Vector3(0.98f, 0.04f, 0.16f), new Color(0.4f, 0.27f, 0.15f), false);
            var bsz = new Vector3(0.3f, 0.13f, 0.04f);
            var water = _waterButton = Pressable.Create("Вода", panel, new Vector3(-0.33f, 0.7f, 0), new Color(0.2f, 0.45f, 0.95f), AddWater, bsz, 0.045f);
            var fireB = _fireButton = Pressable.Create("Огонь", panel, new Vector3(0f, 0.7f, 0), new Color(0.9f, 0.4f, 0.1f), AddFire, bsz, 0.045f);
            var serve = _serveButton = Pressable.Create("Разлить", panel, new Vector3(0.33f, 0.7f, 0), new Color(0.25f, 0.65f, 0.3f), Serve, bsz, 0.045f);
            BtnIcon(water, 0); BtnIcon(fireB, 1); BtnIcon(serve, 2);
            _heatText = Prim.Text(panel, new Vector3(0f, 0.81f, -0.035f), "", 0.04f, new Color(1f, 0.8f, 0.4f));
            _drainButton = Pressable.Create("Слить", panel, new Vector3(0.33f, 0.47f, -0.02f), new Color(0.5f, 0.3f, 0.3f), Drain, new Vector3(0.2f, 0.08f, 0.03f), 0.03f);
            Spice[] sp = { Spice.Salt, Spice.Pepper, Spice.Herbs, Spice.Fire };
            for (int i = 0; i < sp.Length; i++)
            {
                var s = sp[i];
                var b = Pressable.Create(Recipes.SpiceName(s), panel, new Vector3(-0.42f + i * 0.17f, 0.33f, -0.02f), new Color(0.55f, 0.45f, 0.25f), () => AddSpice(s), new Vector3(0.16f, 0.07f, 0.03f), 0.022f);
                _spiceButtons.Add(b.gameObject);
            }
            Reset(SoupState.Empty);
        }

        /// <summary>Иконка на кнопке слева: 0 — капля воды, 1 — пламя, 2 — миска.</summary>
        static void BtnIcon(Pressable p, int kind)
        {
            var root = p.transform.parent;
            var lp = p.transform.localPosition + new Vector3(-0.1f, 0, -0.03f);
            if (p.Label != null) { p.Label.transform.localPosition += new Vector3(0.035f, 0, 0); }
            if (kind == 0)
            {
                var d = Prim.Shape(PrimitiveType.Sphere, "IcoDrop", root, lp + new Vector3(0, -0.01f, 0), new Vector3(0.06f, 0.06f, 0.02f), new Color(0.6f, 0.85f, 1f), false);
                Prim.SetEmission(d.GetComponent<Renderer>(), new Color(0.2f, 0.5f, 1f));
                var tip = Prim.Cube("IcoDropTip", root, lp + new Vector3(0, 0.025f, 0), new Vector3(0.03f, 0.03f, 0.02f), new Color(0.6f, 0.85f, 1f), false);
                tip.transform.localRotation = Quaternion.Euler(0, 0, 45);
            }
            else if (kind == 1)
            {
                var f = Prim.Shape(PrimitiveType.Sphere, "IcoFlame", root, lp, new Vector3(0.05f, 0.08f, 0.02f), new Color(1f, 0.85f, 0.2f), false);
                Prim.SetEmission(f.GetComponent<Renderer>(), new Color(1f, 0.6f, 0.1f));
                Prim.Shape(PrimitiveType.Sphere, "IcoFlame2", root, lp + new Vector3(0, -0.012f, -0.004f), new Vector3(0.03f, 0.045f, 0.02f), new Color(1f, 1f, 0.6f), false);
            }
            else
            {
                Prim.Shape(PrimitiveType.Sphere, "IcoBowl", root, lp + new Vector3(0, -0.005f, 0), new Vector3(0.08f, 0.045f, 0.02f), new Color(0.9f, 0.85f, 0.75f), false);
                Prim.Cube("IcoBowlSoup", root, lp + new Vector3(0, 0.012f, -0.004f), new Vector3(0.07f, 0.012f, 0.02f), new Color(0.9f, 0.6f, 0.2f), false);
            }
        }

        /// <summary>Кнопка «Огонь»: +1 к жару очага (этап 2+). На этапе 1 огонь держится сам (жар 2/3).</summary>
        public static int FireButtonPresses;
        void AddFire()
        {
            FireButtonPresses++;
            var k = Kitchen.I;
            if (k == null) return;
            if (_stage < 2) { Hints.Good("Огонь горит: жар 2/3 — на 1-м этапе держится сам"); return; }
            k.Hearth.AddLog();
            Hints.Good($"Огонь сильнее! Жар {k.Hearth.Heat}/3");
        }

        public void SetStage(int stage)
        {
            _stage = stage;
            foreach (var b in _spiceButtons) b.SetActive(stage >= 3);
        }

        void Reset(SoupState s)
        {
            State = s; Recipe = null; Portions = 0; _progress = 0; _errors = 0; _stirs = 0;
            _contents.Clear(); _spices.Clear(); _oversalted = false; _unwashed = false; _heatWarned = false;
            _stirWindowUntil = 0;
            ContentColor = s == SoupState.Water ? WaterColor : new Color(0.08f, 0.08f, 0.08f);
            Prim.SetColor(_soup, ContentColor, Color.black);
        }

        void SetContent(Color c, float glow)
        {
            ContentColor = c;
            Prim.SetColor(_soup, c, Color.black);
        }

        void AddWater()
        {
            if (State == SoupState.Empty) { Reset(SoupState.Water); Sfx.Play(SfxId.Pour, transform.position, 0.6f); Hints.Good("Вода налита!"); }
            else { Hints.Show("Вода уже есть — клади продукты (или «Слить»)", HintPri.Error); KitchenCoach.Forbid("Вода уже налита — клади продукты в котёл"); }
        }

        void Drain()
        {
            if (State == SoupState.Empty) return;
            if (State == SoupState.Cooking || State == SoupState.Water) Hints.Show("Котёл слит, продукты потеряны", HintPri.Action);
            Reset(SoupState.Empty);
        }

        void AddSpice(Spice s)
        {
            if (State != SoupState.Cooking && State != SoupState.Water) { Hints.Show("Специи — во время варки", HintPri.Error); return; }
            _spices.TryGetValue(s, out var n);
            _spices[s] = n + 1;
            Hints.Show($"+ {Recipes.SpiceName(s)} ({n + 1})", HintPri.Action, 1.5f);
        }

        public void OnIngredient(Ingredient ing)
        {
            if (ing == null || ing.IsHeld || ing.Despawning) return;
            if (ing.Spoiled) { Hints.Show($"{Items.Name(ing.Id)}: {(ing.Dropped ? "упал на пол" : "сгорел")} — в котёл нельзя! {TrashBin.Tip}", HintPri.Error, 4f); KitchenCoach.Forbid("В котёл нельзя: продукт упал/испорчен — в урну"); Bounce(ing); return; }
            if (State == SoupState.Empty) { Hints.Show("Сначала налей воду (кнопка «Вода»)", HintPri.Error); KitchenCoach.Forbid("Сначала налей воду — кнопка «Вода»"); Bounce(ing); return; }
            if (State != SoupState.Water) { Hints.Show("Котёл занят — слей или дождись готовности", HintPri.Error); Bounce(ing); return; }
            var id = ing.Id;
            bool ok = ing.Prepared;
            if (id == ItemId.MandrakeRoot && !ing.Washed) _unwashed = true;
            ing.Despawn(false); // продукт «утонул» и удалён — ссылка больше нигде не хранится
            if (!ok) { Ruin($"{Items.Name(id)} не подготовлен(о)"); return; }
            _contents.Add(id);
            bool dragon = GameFlow.I != null && GameFlow.I.Save.dragonDefeated;
            var r = Recipes.Match(_contents, _stage, dragon);
            if (r != null)
            {
                Recipe = r; State = SoupState.Cooking; _progress = 0;
                _stirAt = DTime.Now + 8f;
                SetContent(SoupVisual.IsStarter(r) ? SoupVisual.CookingColor(r, 0) : Color.Lerp(WaterColor, r.SoupColor, 0.35f), 0.4f);
                Hints.Good($"{ShortName(id)} добавлен{Ending(id)}! Варится: {r.Name}", 2.2f);
                if (_stage >= 2) Hints.Show($"Варится: {r.Name}" + (_stage >= 3 ? $" · специи: {r.SpiceText}" : "") + $" · жар: {Recipes.HeatName(r.Heat)}", HintPri.Tutorial, 4f);
            }
            else if (!Recipes.CanBecome(_contents, _stage, dragon)) Ruin("лишний ингредиент — такого рецепта нет");
            else { SetContent(Color.Lerp(WaterColor, ItemTint(id), 0.25f * _contents.Count), 0.5f); Hints.Good($"{ShortName(id)} добавлен{Ending(id)}!"); }
        }

        static readonly Dictionary<ItemId, (string name, string end)> Names = new Dictionary<ItemId, (string, string)>
        {
            { ItemId.SlimeJelly, ("Желе", "о") }, { ItemId.MushroomLegs, ("Ножки гриба", "ы") }, { ItemId.MandrakeRoot, ("Мандрагора", "а") },
            { ItemId.BasiliskMeat, ("Мясо василиска", "о") }, { ItemId.ShellfishMeat, ("Моллюски", "ы") }, { ItemId.MimicMeat, ("Мясо мимика", "о") },
            { ItemId.DragonMeat, ("Мясо дракона", "о") }, { ItemId.Cabbage, ("Капуста", "а") }, { ItemId.Potato, ("Картофель", "") },
            { ItemId.Carrot, ("Морковь", "а") }, { ItemId.Onion, ("Лук", "") },
        };
        public static string ShortName(ItemId id) => Names.TryGetValue(id, out var n) ? n.name : Items.Name(id);
        public static string Ending(ItemId id) => Names.TryGetValue(id, out var n) ? n.end : "";
        /// <summary>Каким цветом продукт подкрашивает воду (суп меняет цвет с каждым продуктом).</summary>
        public static Color ItemTint(ItemId id)
        {
            switch (id)
            {
                case ItemId.SlimeJelly: return new Color(0.55f, 0.95f, 0.6f);
                case ItemId.MushroomLegs: return new Color(0.85f, 0.7f, 0.5f);
                case ItemId.MandrakeRoot: return new Color(0.8f, 0.65f, 0.4f);
                case ItemId.Cabbage: return new Color(0.5f, 0.85f, 0.35f);
                case ItemId.Potato: return new Color(0.95f, 0.85f, 0.55f);
                case ItemId.Carrot: return new Color(1f, 0.55f, 0.15f);
                case ItemId.Onion: return new Color(0.95f, 0.9f, 0.75f);
                default: return new Color(0.75f, 0.4f, 0.3f);
            }
        }

        void Bounce(Ingredient ing)
        {
            if (ing == null) return; // вернуть на стол раздачи рядом (не на пол — иначе продукт «упадёт» и испортится)
            var p = _serveSpot != null ? _serveSpot.position + new Vector3(Random.Range(-0.1f, 0.1f), 0.05f, Random.Range(-0.1f, 0.1f)) : transform.position + transform.rotation * new Vector3(0.6f, 1.0f, -0.6f);
            ing.PlaceAt(p);
        }

        void Ruin(string why)
        {
            State = SoupState.Ruined;
            SetContent(new Color(0.35f, 0.38f, 0.25f), 0.1f);
            Hints.Show($"Суп испорчен: {why}. Нажми «Слить»", HintPri.Error, 4f);
        }

        /// <summary>Один круг половником (ПК: E по котлу с половником в руке).</summary>
        public void StirOnce()
        {
            if (State != SoupState.Cooking) return;
            if (_stage < 2) { Hints.Show("В этом рецепте мешать не нужно — просто жди", HintPri.Action, 1.5f); return; }
            if (DTime.Now < _stirWindowUntil) { _stirs++; if (_stirs >= StirsNeeded) { _stirWindowUntil = 0; _stirAt = DTime.Now + 8f; Hints.Good($"Помешано {_stirs}/{StirsNeeded} — отлично!"); } else Hints.Show($"Помешано {_stirs}/{StirsNeeded}", HintPri.Action, 1f); }
            else Hints.Show("Сейчас мешать не нужно — жди сигнала «Мешать»", HintPri.Action, 1.5f);
        }

        void Update()
        {
            if (DungeonManager.I != null && DungeonManager.I.InDungeon) return;
            float dt = DTime.Delta;
            var kitchen = Kitchen.I;
            int heat = kitchen != null && _stage >= 2 ? kitchen.Hearth.Heat : 2;
            if (dt > 0 && State == SoupState.Cooking && Recipe != null)
            {
                _progress += dt * (_stage >= 2 ? Hearth.Mul(heat) : 1f);
                if (_stage >= 2)
                {
                    if (!_heatWarned && heat > 0 && heat != Recipe.Heat && _progress > 2f) { _heatWarned = true; _errors++; Hints.Show($"Жар не тот: нужен {Recipes.HeatName(Recipe.Heat)} (−★)", HintPri.Error); }
                    StirLogic(heat);
                }
                if (State == SoupState.Cooking && _progress >= Recipe.CookTime) Finish();
            }
            Visuals(heat);
        }

        /// <summary>Котёл, у которого сейчас показывается табличка: ближайший активный к игроку (табличка ОДНА на кухне).</summary>
        public static Cauldron LabelOwner()
        {
            var P = PlayerRig.I;
            if (P == null || Kitchen.I == null || !Kitchen.I.InKitchen(P.Feet)) return null;
            Cauldron best = null; float bd = 4.5f;
            foreach (var c in All)
            {
                if (c == null || !c.gameObject.activeInHierarchy) continue;
                float d = (c.transform.position - P.Feet).magnitude;
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        void Visuals(int heat)
        {
            float t = Time.time;
            // огонь: размер языков = сила огня
            FlameLevel = heat;
            float size = heat <= 0 ? 0f : heat == 1 ? 0.45f : heat == 2 ? 0.75f : 1.1f;
            if (_flames.gameObject.activeSelf != heat > 0) _flames.gameObject.SetActive(heat > 0);
            for (int i = 0; i < _flameParts.Count; i++)
            {
                float fl = 1f + 0.18f * Mathf.Sin(t * 9f + i * 1.7f);
                _flameParts[i].localScale = new Vector3(0.1f * size, 0.24f * size * fl, 0.1f * size);
                var lp = _flameParts[i].localPosition; lp.y = 0.1f * size * fl; _flameParts[i].localPosition = lp;
            }
            if (_fireLight != null) _fireLight.intensity = heat * 0.6f * (1f + 0.1f * Mathf.Sin(t * 13f));
            if (_heatText != null) _heatText.text = $"Жар {heat}/3 · {(heat <= 0 ? "погас" : heat == 1 ? "маленький" : heat == 2 ? "средний" : "большой")}";
            // вода/суп: цвет меняется по мере варки
            if (State == SoupState.Cooking && Recipe != null) SetContent(SoupVisual.IsStarter(Recipe) ? SoupVisual.CookingColor(Recipe, Progress01) : Color.Lerp(Color.Lerp(WaterColor, Recipe.SoupColor, 0.35f), Recipe.SoupColor, Progress01), 0.35f + 0.1f * Mathf.Sin(t * 3f));
            else if (State == SoupState.Water) Prim.SetColor(_soup, ContentColor, Color.black);
            // плавающие продукты — видно, что уже в котле
            bool hasLiquid = State != SoupState.Empty;
            int want = hasLiquid ? _contents.Count : 0;
            _soup.enabled = hasLiquid;
            bool starter = SoupVisual.IsStarter(Recipe);
            bool same = want == _shownFloaters.Count && starter == _starterFloaters;
            for (int i = 0; same && i < want; i++) if (_shownFloaters[i] != _contents[i]) same = false;
            if (!same)
            {
                for (int k = _floaters.childCount - 1; k >= 0; k--) { var ch = _floaters.GetChild(k); ch.SetParent(null); Destroy(ch.gameObject); }
                _shownFloaters.Clear();
                _starterFloaters = starter;
                if (starter)
                {
                    SoupVisual.Chunks(_floaters, Recipe.Items, Vector3.zero, .29f);
                    _shownFloaters.AddRange(_contents);
                }
                else for (int i = 0; i < want; i++)
                {
                    var m = FoodModels.BuildPieces(_contents[i], _floaters, 0.13f, 3);
                    foreach (var col in m.GetComponentsInChildren<Collider>()) Destroy(col);
                    float a = i * 2.2f;
                    m.localPosition = new Vector3(Mathf.Cos(a) * 0.15f, 0, Mathf.Sin(a) * 0.15f);
                    _shownFloaters.Add(_contents[i]);
                }
            }
            for (int i = 0; i < _floaters.childCount; i++)
            {
                var ch = _floaters.GetChild(i);
                var lp = ch.localPosition; lp.y = (_starterFloaters ? .003f : .015f) * Mathf.Sin(t * 2.5f + i); ch.localPosition = lp;
                ch.Rotate(0, (_starterFloaters ? 4f : 20f) * Time.deltaTime, 0, Space.Self);
            }
            // пар и пузыри: при воде на огне и при варке
            bool hot = hasLiquid && heat > 0 && State != SoupState.Ruined;
            if (_steam.gameObject.activeSelf != hot) _steam.gameObject.SetActive(hot);
            bool boil = hot && (State == SoupState.Cooking || State == SoupState.Water || State == SoupState.Ready);
            if (_bubbles.gameObject.activeSelf != boil) _bubbles.gameObject.SetActive(boil);
            if (hot)
                for (int i = 0; i < _steam.childCount; i++)
                {
                    var ch = _steam.GetChild(i);
                    float ph = Mathf.Repeat(t * 0.35f + i / 5f, 1f);
                    ch.localPosition = new Vector3(Mathf.Sin(i * 1.3f + ph * 2f) * 0.13f, ph * 0.6f, Mathf.Cos(i * 1.7f) * 0.12f);
                    ch.localScale = new Vector3(1f,1.6f,1f) * (.04f + ph * .08f) * (1f - ph * .6f);
                    Prim.SetOverlayColor(ch.GetComponent<Renderer>(),new Color(.9f,.93f,.96f,.13f*(1f-ph)));
                }
            if (boil)
                for (int i = 0; i < _bubbles.childCount; i++)
                {
                    var ch = _bubbles.GetChild(i);
                    float ph = Mathf.Repeat(t * (0.8f + heat * 0.3f) + i * 0.37f, 1f);
                    ch.localScale = Vector3.one * (0.015f + 0.05f * ph);
                }
            // одна табличка + значки «что есть / чего не хватает» — только у котла рядом с игроком
            var P = PlayerRig.I;
            bool mine = LabelOwner() == this && !(DungeonManager.I != null && DungeonManager.I.InDungeon);
            if (_label.gameObject.activeSelf != mine) _label.gameObject.SetActive(mine);
            UpdateStirPanel(P, mine);
            if (_icons.gameObject.activeSelf != mine) _icons.gameObject.SetActive(mine);
            if (!mine || P == null) return;
            Prim.FaceTo(_label.transform, P.HeadPos);
            Prim.FaceTo(_icons, P.HeadPos);
            _label.text = LabelText(heat);
            _label.characterSize = (State == SoupState.Ready ? .045f : .075f) * 10f / 64f;
            UpdateIcons();
        }

        /// <summary>К3: «Мешать — E» крупно и полоска «Помешано N/2» над котлом, пока открыто окно «Мешай!» (не отключается клавишей H — это часть котла).</summary>
        void UpdateStirPanel(PlayerRig P, bool mine)
        {
            bool on = mine && StirNow && State == SoupState.Cooking && P != null;
            if (_stirRoot.gameObject.activeSelf != on) _stirRoot.gameObject.SetActive(on);
            if (!on) return;
            Prim.FaceTo(_stirRoot, P.HeadPos);
            string t = "Мешать — " + StirKey();
            if (_stirText.text != t) _stirText.text = t;
            string b = $"Помешано {_stirs}/{StirsNeeded} · {Mathf.Max(0f, _stirWindowUntil - DTime.Now):0} с";
            if (_stirBarText.text != b) _stirBarText.text = b;
            float w = Mathf.Max(0.001f, StirBarW * _stirs / StirsNeeded);
            _stirFill.localScale = new Vector3(w, 0.06f, 0.01f);
            _stirFill.localPosition = new Vector3(-StirBarW * 0.5f + w * 0.5f, -0.035f, -0.002f);
        }

        /// <summary>Значки продуктов рецепта над котлом: ✓ — уже в котле, ✗ — ещё нужно.</summary>
        void UpdateIcons()
        {
            var r = Guide.CookableRecipe(this);
            var left = new List<ItemId>(_contents);
            bool full = State == SoupState.Cooking || State == SoupState.Ready;
            var sb = new System.Text.StringBuilder(); sb.Append(r.Index);
            var have = new List<bool>();
            foreach (var id in r.Items) { bool h = left.Remove(id) || full; have.Add(h); sb.Append(h ? '+' : '-'); }
            var key = sb.ToString();
            if (key == _iconsFor) return;
            _iconsFor = key;
            for (int k = _icons.childCount - 1; k >= 0; k--) { var ch = _icons.GetChild(k); ch.SetParent(null); Destroy(ch.gameObject); }
            IconsOk = 0; IconsMissing = 0;
            int n = r.Items.Length; float step = 0.2f, x0 = -(n - 1) * step * 0.5f;
            for (int i = 0; i < n; i++)
            {
                var cell = new GameObject("Need_" + r.Items[i]).transform; cell.SetParent(_icons, false); cell.localPosition = new Vector3(x0 + i * step, 0, 0);
                var bg = Prim.Cube("NeedBg", cell, new Vector3(0, 0, 0.03f), new Vector3(0.17f, 0.17f, 0.01f), have[i] ? new Color(0.15f, 0.4f, 0.18f) : new Color(0.35f, 0.12f, 0.1f), false);
                var m = FoodModels.Build(r.Items[i], cell, 0.11f);
                foreach (var col in m.GetComponentsInChildren<Collider>()) Destroy(col);
                m.localPosition = new Vector3(0, -0.02f, 0); m.localRotation = Quaternion.Euler(-20, 30, 0);
                if (have[i]) { KIcons.Check(cell, new Vector3(0.055f, 0.055f, -0.03f), 0.07f); IconsOk++; }
                else { KIcons.Cross(cell, new Vector3(0.055f, 0.055f, -0.03f), 0.06f); IconsMissing++; }
            }
        }

        void StirLogic(int heat)
        {
            if (_stirWindowUntil == 0 && DTime.Now >= _stirAt) { _stirWindowUntil = DTime.Now + StirWindow; _stirs = 0; _angleAcc = 0; _hasLastAngle = false; Hints.Show($"Помешай! ({StirsNeeded} круга половником, ПК: {Tools.KitchenKey(ToolType.Ladle)} → E по котлу)", HintPri.Action); }
            if (_stirWindowUntil > 0)
            {
                // XR: кончик половника внутри котла, считаем угол обхода центра
                var ladle = Tool.FindWorldTool(ToolType.Ladle);
                if (ladle != null && ladle.Holder != null)
                {
                    var local = transform.InverseTransformPoint(ladle.Tip.position);
                    if (Mathf.Abs(local.x) < 0.3f && Mathf.Abs(local.z) < 0.3f && local.y > .96f && local.y < 1.35f)
                    {
                        float a = Mathf.Atan2(local.z, local.x) * Mathf.Rad2Deg;
                        if (_hasLastAngle) _angleAcc += Mathf.Abs(Mathf.DeltaAngle(_lastAngle, a));
                        _lastAngle = a; _hasLastAngle = true;
                        if (_angleAcc >= 360f) { _angleAcc -= 360f; StirOnce(); } // один круг —360°, остаток не теряем
                    }
                    else _hasLastAngle = false;
                }
                if (_stirWindowUntil > 0 && DTime.Now > _stirWindowUntil)
                {
                    _stirWindowUntil = 0; _stirAt = DTime.Now + 8f;
                    if (heat >= 3) { State = SoupState.Burnt; SetContent(new Color(0.05f, 0.04f, 0.03f), 0f); Hints.Show("Пригорело! Сильный жар без помешивания. «Слить»", HintPri.Error, 4f); return; }
                    _errors++;
                    Hints.Show("Не помешал (−★)", HintPri.Error);
                }
            }
        }

        void Finish()
        {
            if (_stage >= 3)
            {
                foreach (var kv in Recipe.Spices)
                {
                    _spices.TryGetValue(kv.Key, out var have);
                    if (have < kv.Value) { _errors++; Hints.Show($"Мало специи: {Recipes.SpiceName(kv.Key)} (−★)", HintPri.Error); }
                    else if (have >= kv.Value + 2) _oversalted = true;
                }
                foreach (var kv in _spices) if (!Recipe.Spices.ContainsKey(kv.Key) && kv.Value >= 2) _oversalted = true;
            }
            State = SoupState.Ready;
            Portions = BatchPortions; // объём котла, независимо от заказов: 3 / 1
            Sfx.Play(SfxId.Ding, transform.position, .6f);
            SetContent(Recipe.SoupColor, 0f);
            Hints.Show($"Готово: {Recipe.Name} {new string('★', Stars)}{(_oversalted ? " (пересолено)" : "")} — «Разлить»", HintPri.Window, 4f);
        }

        void Serve()
        {
            if (State != SoupState.Ready || Portions <= 0) { Hints.Show(State == SoupState.Cooking ? "Ещё варится" : "Нечего разливать", HintPri.Error); KitchenCoach.Forbid(State == SoupState.Cooking ? "Рано разливать — суп ещё варится" : "Нечего разливать — сначала свари суп"); return; }
            var pos = _serveSpot != null ? _serveSpot.position : transform.position + Vector3.up;
            // Стол разлива всего 0.5 × 0.5 м: второй ряд прежней сетки был уже за краем.
            // Не тратим порцию и не создаём новую миску поверх оставленной.
            foreach (var existing in Bowl.All)
                if (existing != null && Vector3.Distance(existing.transform.position, pos) < .3f)
                { Hints.Show("Забери миску со стола разлива — затем разлей следующую порцию", HintPri.Action); return; }
            var bowl = Bowl.Spawn(Recipe, Stars, _oversalted || _unwashed, pos);
            if (bowl == null) return;
            Hints.Good("Суп разлит! Миску — на окно раздачи");
            Portions--;
            if (Portions <= 0) Reset(SoupState.Empty);
        }

        public void ServePortion() => Serve();

        string LabelText(int heat)
        {
            string fire = $"  · жар {heat}/3";
            switch (State)
            {
                case SoupState.Empty: return "Котёл пуст → «Вода»" + fire;
                case SoupState.Water: return $"Вода · в котле {_contents.Count}" + fire;
                case SoupState.Cooking:
                    string stir = !NeedsStir ? "\nМешать не нужно — жди" : _stirWindowUntil > 0 ? $"\nМЕШАЙ! {_stirs}/{StirsNeeded} ({_stirWindowUntil - DTime.Now:0} с)" : "\nМешать — по сигналу";
                    string sp = _stage >= 3 ? $"\nСпеции: {SpicesNow()}" : "";
                    return $"{Recipe.Name}: {Mathf.Clamp01(_progress / Recipe.CookTime) * 100:0}%{fire}{stir}{sp}";
                case SoupState.Ready: return $"Готово: {Recipe.Name} {new string('★', Stars)} ×{Portions}";
                case SoupState.Ruined: return "Испорчено — «Слить»";
                default: return "Пригорело — «Слить»";
            }
        }

        string SpicesNow()
        {
            if (_spices.Count == 0) return "—";
            var parts = new List<string>();
            foreach (var kv in _spices) parts.Add($"{Recipes.SpiceName(kv.Key)} {kv.Value}");
            return string.Join(", ", parts);
        }
    }
}
