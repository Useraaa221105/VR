using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Инструмент. Удар считается не по касанию, а по скорости кончика (буфер ~0,1 с), направлению и нормали лезвия.
    /// SphereCast от прошлой позиции кончика к текущей — быстрый инструмент не «пролетает» сквозь цель.
    /// </summary>
    public class Tool : MonoBehaviour, IGrabbable
    {
        [field: SerializeField] public ToolType Type { get; private set; }
        [field: SerializeField] public Transform Tip { get; private set; }
        public Transform Home { get; private set; }
        public HandController Holder { get; private set; }
        public bool IsHome => Holder == null && transform.parent == Home;
        public bool Broken => Time.time < _brokenUntil;
        public Vector3 TipVelocity { get; private set; }
        public Vector3 BladeNormal => transform.TransformDirection(_normalLocal);
        public static HitInfo LastHit;
        public static float LastHitTime = -10;

        static readonly List<Tool> _worldTools = new List<Tool>();
        static readonly List<Tool> _all = new List<Tool>();

        /// <summary>Сразу забыть цели (зоны убитого монстра) во всех инструментах.</summary>
        public static void ForgetTargets(List<int> ids)
        {
            foreach (var t in _all)
            {
                if (t == null) continue;
                foreach (var id in ids) { t._cooldown.Remove(id); t._contactPrev.Remove(id); t._contactNow.Remove(id); }
            }
        }
        public static int CooldownEntries { get { int n = 0; foreach (var t in _all) if (t != null) n += t._cooldown.Count; return n; } }
        static readonly RaycastHit[] _castBuf = new RaycastHit[16];
        static readonly Collider[] _overBuf = new Collider[16];

        [SerializeField] Vector3 _normalLocal = Vector3.up;
        [SerializeField] float _castR = 0.06f;
        bool _canHit = true;
        float _brokenUntil, _freeAt = -1;
        [SerializeField] Renderer[] _rends;
        [SerializeField] Color[] _baseColors;
        bool _greyed;

        readonly Vector3[] _bufPos = new Vector3[10];
        readonly float[] _bufT = new float[10];
        int _bi;
        Vector3 _lastTip;
        bool _hasLast;
        HashSet<int> _contactPrev = new HashSet<int>(), _contactNow = new HashSet<int>();
        readonly Dictionary<int, float> _cooldown = new Dictionary<int, float>();
        float _pruneAt;

        public static Tool FindWorldTool(ToolType t)
        {
            foreach (var w in _worldTools) if (w != null && w.Type == t && w.gameObject.activeInHierarchy) return w;
            return null;
        }

        public static Tool Create(ToolType t, Transform home, bool worldTool = false)
        {
            var prefab = SavedDungeonAssets.Active?.FindTool(t);
            if (prefab != null && prefab.Tip != null)
            {
                var saved = Instantiate(prefab, home, false);
                saved.Home = home;
                saved.ReturnHome();
                if (worldTool) _worldTools.Add(saved);
                _all.Add(saved);
                return saved;
            }
            var go = new GameObject("Tool_" + t);
            var tool = go.AddComponent<Tool>();
            tool.Type = t;
            tool.Home = home;
            tool.Build();
            tool.ReturnHome();
            if (worldTool) _worldTools.Add(tool);
            _all.Add(tool);
            return tool;
        }

        void OnDestroy()
        {
            _worldTools.Remove(this);
            _all.Remove(this);
            if (Holder != null) Holder.ClearIfHolding(this);
        }

        // ---------- внешний вид: у каждого инструмента своя форма, свой цвет и лёгкое свечение ----------
        readonly List<Renderer> _partR = new List<Renderer>();
        readonly List<Color> _partC = new List<Color>();
        [SerializeField] List<Color> _partE = new List<Color>();

        /// <summary>Деталь инструмента (без коллайдера). glow — сила свечения цветом детали.</summary>
        GameObject Part(PrimitiveType pt, string n, Vector3 pos, Vector3 size, Color c, float glow = 0f, Vector3? euler = null)
        {
            var go = Prim.Shape(pt, n, transform, pos, size, c, false);
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            var r = go.GetComponent<Renderer>();
            var e = glow > 0 ? c * glow * 1.7f : Color.black; // лёгкое свечение своим цветом — видно и в тёмном подземелье
            Prim.SetColor(r, c, e);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _partR.Add(r); _partC.Add(c); _partE.Add(e);
            return go;
        }
        GameObject Box(string n, Vector3 p, Vector3 s, Color c, float g = 0f, Vector3? e = null) => Part(PrimitiveType.Cube, n, p, s, c, g, e);
        // цилиндр Unity: высота 2 по оси Y → длина len вдоль оси Z инструмента
        GameObject RodZ(string n, Vector3 p, float d, float len, Color c, float g = 0f) => Part(PrimitiveType.Cylinder, n, p, new Vector3(d, len * 0.5f, d), c, g, new Vector3(90, 0, 0));

        void Build()
        {
            var steel = new Color(0.78f, 0.8f, 0.84f);
            var wood = new Color(0.5f, 0.31f, 0.16f);
            var col = Tools.ColorOf(Type);
            var tr = transform;
            Tip = new GameObject("Tip").transform;
            Tip.SetParent(tr, false);
            Vector3 boxC, boxS;
            switch (Type)
            {
                case ToolType.Spatula: // широкая плоская лопатка с прорезями, зелёная
                    RodZ("Handle", new Vector3(0, 0, 0.13f), 0.028f, 0.26f, wood);
                    RodZ("Grip", new Vector3(0, 0, 0.06f), 0.034f, 0.08f, col, 0.35f);
                    RodZ("Neck", new Vector3(0, 0, 0.27f), 0.018f, 0.05f, steel);
                    Box("Blade", new Vector3(0, 0, 0.34f), new Vector3(0.14f, 0.012f, 0.13f), col, 0.45f);
                    Part(PrimitiveType.Cylinder, "BladeTip", new Vector3(0, 0, 0.405f), new Vector3(0.14f, 0.006f, 0.07f), col, 0.45f);
                    for (int i = -1; i <= 1; i++) Box("Slot", new Vector3(i * 0.035f, 0.007f, 0.345f), new Vector3(0.012f, 0.003f, 0.08f), col * 0.35f);
                    Tip.localPosition = new Vector3(0, 0, 0.34f); _normalLocal = Vector3.up; _castR = 0.07f;
                    boxC = new Vector3(0, 0, 0.2f); boxS = new Vector3(0.13f, 0.05f, 0.42f); break;
                case ToolType.Net: // длинная ручка, круглый голубой обруч, сетка-мешок
                    RodZ("Handle", new Vector3(0, 0, 0.2f), 0.026f, 0.4f, wood);
                    RodZ("Grip", new Vector3(0, 0, 0.05f), 0.032f, 0.09f, col, 0.35f);
                    for (int i = 0; i < 14; i++)
                    {
                        float a = i / 14f * Mathf.PI * 2f, a2 = (i + 0.5f) / 14f * Mathf.PI * 2f;
                        var p = new Vector3(Mathf.Sin(a2) * 0.17f, 0, 0.57f + Mathf.Cos(a2) * 0.17f);
                        Box("Hoop", p, new Vector3(0.022f, 0.022f, 0.08f), col, 0.6f, new Vector3(0, a2 * Mathf.Rad2Deg + 90f, 0));
                    }
                    Part(PrimitiveType.Sphere, "Mesh", new Vector3(0, -0.07f, 0.57f), new Vector3(0.31f, 0.16f, 0.31f), new Color(0.75f, 0.92f, 1f, 0.5f), 0.15f);
                    Tip.localPosition = new Vector3(0, 0, 0.57f); _normalLocal = Vector3.up; _castR = 0.2f;
                    boxC = new Vector3(0, 0, 0.38f); boxS = new Vector3(0.38f, 0.1f, 0.76f); break;
                case ToolType.Cleaver: // широкий прямоугольный клинок с оранжевым светящимся лезвием и дыркой
                    RodZ("Handle", new Vector3(0, 0, 0.07f), 0.032f, 0.14f, new Color(0.3f, 0.18f, 0.1f));
                    Box("Guard", new Vector3(0, -0.02f, 0.145f), new Vector3(0.03f, 0.07f, 0.015f), col, 0.5f);
                    Box("Blade", new Vector3(0, -0.035f, 0.26f), new Vector3(0.012f, 0.14f, 0.22f), steel);
                    Box("Edge", new Vector3(0, -0.1f, 0.26f), new Vector3(0.014f, 0.022f, 0.222f), col, 0.9f);
                    Box("Spine", new Vector3(0, 0.032f, 0.26f), new Vector3(0.016f, 0.012f, 0.222f), col * 0.8f, 0.3f);
                    Part(PrimitiveType.Cylinder, "Hole", new Vector3(0, 0.0f, 0.34f), new Vector3(0.03f, 0.008f, 0.03f), new Color(0.1f, 0.08f, 0.06f), 0f, new Vector3(0, 0, 90));
                    Tip.localPosition = new Vector3(0, -0.06f, 0.27f); _normalLocal = Vector3.right; _castR = 0.08f;
                    boxC = new Vector3(0, -0.02f, 0.18f); boxS = new Vector3(0.05f, 0.15f, 0.36f); break;
                case ToolType.Hammer: // киянка: поперечная фиолетовая боёк-бочка
                    RodZ("Handle", new Vector3(0, 0, 0.15f), 0.032f, 0.3f, wood);
                    RodZ("Grip", new Vector3(0, 0, 0.05f), 0.038f, 0.09f, col, 0.35f);
                    Part(PrimitiveType.Cylinder, "Head", new Vector3(0, 0, 0.32f), new Vector3(0.1f, 0.075f, 0.1f), col, 0.5f, new Vector3(0, 0, 90));
                    Part(PrimitiveType.Cylinder, "CapL", new Vector3(-0.08f, 0, 0.32f), new Vector3(0.112f, 0.008f, 0.112f), steel, 0f, new Vector3(0, 0, 90));
                    Part(PrimitiveType.Cylinder, "CapR", new Vector3(0.08f, 0, 0.32f), new Vector3(0.112f, 0.008f, 0.112f), steel, 0f, new Vector3(0, 0, 90));
                    Tip.localPosition = new Vector3(0, 0, 0.32f); _normalLocal = Vector3.up; _castR = 0.07f;
                    boxC = new Vector3(0, 0, 0.18f); boxS = new Vector3(0.1f, 0.1f, 0.38f); break;
                case ToolType.Shield: // котёл-щит: круглый золотой котёл дном к врагу, обод и ручки
                    Part(PrimitiveType.Cylinder, "Shield", new Vector3(0, 0, 0.06f), new Vector3(0.5f, 0.04f, 0.5f), col * 0.9f, 0.55f, new Vector3(90, 0, 0));
                    Part(PrimitiveType.Cylinder, "Rim", new Vector3(0, 0, 0.085f), new Vector3(0.54f, 0.015f, 0.54f), new Color(0.55f, 0.35f, 0.15f), 0f, new Vector3(90, 0, 0));
                    Part(PrimitiveType.Sphere, "Boss", new Vector3(0, 0, 0.02f), new Vector3(0.14f, 0.14f, 0.06f), col, 0.6f);
                    Box("HandleL", new Vector3(-0.29f, 0, 0.08f), new Vector3(0.05f, 0.1f, 0.03f), new Color(0.3f, 0.2f, 0.1f));
                    Box("HandleR", new Vector3(0.29f, 0, 0.08f), new Vector3(0.05f, 0.1f, 0.03f), new Color(0.3f, 0.2f, 0.1f));
                    Tip.localPosition = new Vector3(0, 0, 0.1f); _canHit = false;
                    boxC = new Vector3(0, 0, 0.06f); boxS = new Vector3(0.5f, 0.5f, 0.1f); break;
                case ToolType.Knife: // кухонный нож (кухню не меняем — прежняя форма)
                    Box("Handle", new Vector3(0, 0, 0.05f), new Vector3(0.022f, 0.022f, 0.1f), wood);
                    Box("Blade", new Vector3(0, -0.01f, 0.18f), new Vector3(0.008f, 0.045f, 0.17f), steel);
                    Tip.localPosition = new Vector3(0, -0.02f, 0.2f); _normalLocal = Vector3.right; _castR = 0.05f;
                    boxC = new Vector3(0, 0, 0.13f); boxS = new Vector3(0.05f, 0.06f, 0.27f); break;
                default: // Ladle — кухонный половник (прежняя форма)
                    Box("Handle", new Vector3(0, 0, 0.17f), new Vector3(0.02f, 0.02f, 0.34f), wood);
                    Box("Bowl", new Vector3(0, -0.02f, 0.37f), new Vector3(0.09f, 0.05f, 0.09f), steel);
                    Tip.localPosition = new Vector3(0, -0.02f, 0.37f); _normalLocal = Vector3.up; _castR = 0.06f;
                    boxC = new Vector3(0, 0, 0.2f); boxS = new Vector3(0.09f, 0.07f, 0.42f); break;
            }
            var bc = gameObject.AddComponent<BoxCollider>();
            bc.isTrigger = true; bc.center = boxC; bc.size = boxS;
            _rends = _partR.ToArray();
            _baseColors = _partC.ToArray();
        }

        // ---------- IGrabbable ----------
        public bool CanGrab(HandController hand) => Holder == null && ToolRules.Allowed(Type);

        public void Grab(HandController hand)
        {
            Holder = hand;
            transform.SetParent(hand.transform, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            _hasLast = false;
            _contactPrev.Clear();
        }

        public void Release(HandController hand, Vector3 v)
        {
            Holder = null;
            if (PlayerRig.I != null && !PlayerRig.I.IsXR) { ReturnHome(); return; }
            if (Home != null && Vector3.Distance(transform.position, Home.position) < .4f) { ReturnHome(); InteractionFeedback.Emit(Home.position, hand); return; }
            var root = Home != null ? Home.root : null;
            if (PlayerRig.I != null && root == PlayerRig.I.transform) root = null; // не таскать брошенный инструмент вместе с игроком
            transform.SetParent(root, true);
            _freeAt = Time.time; // через 3 с сам вернётся на место
        }

        public void ReturnHome()
        {
            if (Holder != null) { var h = Holder; Holder = null; h.ClearIfHolding(this); }
            if (Home == null) return;
            transform.SetParent(Home, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            _freeAt = -1;
        }

        public void Break(float seconds)
        {
            _brokenUntil = Time.time + seconds;
            Hints.Show($"{Tools.Name(Type)} сломан на {seconds:0} с!", HintPri.Error);
        }

        void Update()
        {
            if (_freeAt > 0 && Holder == null && Time.time - _freeAt > 3f) ReturnHome();
            bool grey = Broken;
            if (grey != _greyed)
            {
                _greyed = grey;
                for (int i = 0; i < _rends.Length; i++) Prim.SetColor(_rends[i], grey ? new Color(0.25f, 0.25f, 0.25f) : _baseColors[i], grey ? Color.black : _partE[i]);
            }
        }

        void LateUpdate()
        {
            if (Holder == null || !_canHit) { _hasLast = false; return; }
            var tip = Tip.position;
            _bi = (_bi + 1) % _bufPos.Length;
            _bufPos[_bi] = tip; _bufT[_bi] = Time.time;
            int oldest = (_bi + 1) % _bufPos.Length;
            // окно ~0,1 с
            for (int k = 1; k < _bufPos.Length; k++)
            {
                int idx = (_bi - k + _bufPos.Length) % _bufPos.Length;
                if (Time.time - _bufT[idx] >= 0.1f) { oldest = idx; break; }
            }
            float dt = _bufT[_bi] - _bufT[oldest];
            TipVelocity = dt > 0.0001f ? (_bufPos[_bi] - _bufPos[oldest]) / dt : Vector3.zero;

            if (!_hasLast) { _lastTip = tip; _hasLast = true; return; }
            if (Broken) { _lastTip = tip; return; }

            _contactNow.Clear();
            var d = tip - _lastTip;
            float dist = d.magnitude;
            if (dist > 0.001f)
            {
                int n = Physics.SphereCastNonAlloc(_lastTip, _castR, d / dist, _castBuf, dist, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < n; i++) Consider(_castBuf[i].collider, _castBuf[i].point == Vector3.zero ? tip : _castBuf[i].point);
            }
            int m = Physics.OverlapSphereNonAlloc(tip, _castR, _overBuf, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < m; i++) Consider(_overBuf[i], tip);

            var tmp = _contactPrev; _contactPrev = _contactNow; _contactNow = tmp;
            _lastTip = tip;

            if (Time.time > _pruneAt) PruneCooldowns();
        }

        void Consider(Collider col, Vector3 point)
        {
            if (col == null) return;
            var t = col.transform;
            if (t.IsChildOf(transform)) return;
            if (PlayerRig.I != null && t.IsChildOf(PlayerRig.I.transform)) return;
            var target = col.GetComponentInParent<IHittable>();
            if (target == null) return;
            int id = ((Component)target).GetHashCode();
            if (_contactNow.Contains(id)) return;
            _contactNow.Add(id);
            if (_contactPrev.Contains(id)) return; // касание продолжается — это не новый удар
            if (_cooldown.TryGetValue(id, out var last) && Time.time - last < 0.25f) return;
            _cooldown[id] = Time.time;

            var h = new HitInfo
            {
                Tool = this, Type = Type, Velocity = TipVelocity, Speed = TipVelocity.magnitude,
                Point = point, BladeNormal = BladeNormal, Collider = col
            };
            LastHit = h; LastHitTime = Time.time;
            if (Holder != null && Tools.InDungeon && Rays.Blocked(Holder.transform.position, point, transform)) return;
            InteractionFeedback.Emit(point,Holder);
            target.OnToolHit(h);
        }

        /// <summary>Чистим кеш кулдаунов — иначе в нём копились бы id уже удалённых монстров.</summary>
        void PruneCooldowns()
        {
            _pruneAt = Time.time + 2f;
            if (_cooldown.Count == 0) return;
            var dead = new List<int>();
            foreach (var kv in _cooldown) if (Time.time - kv.Value > 1f) dead.Add(kv.Key);
            foreach (var k in dead) _cooldown.Remove(k);
        }
    }
}
