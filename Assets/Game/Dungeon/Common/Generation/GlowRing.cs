using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    public enum RingKind { Monster, Loot, Heal, Trap }

    /// <summary>
    /// Исправление 5: светящееся кольцо на полу — «где что»: монстр (цвет нужного инструмента), добыча/сундук (золото),
    /// лечение (зелёное, пульсирует), ловушка (красно-жёлтые полосы). Один общий меш на все кольца — дёшево для Quest.
    /// </summary>
    public class GlowRing : MonoBehaviour
    {
        public static readonly Color Gold = new Color(1f, 0.8f, 0.25f);
        public static readonly Color HealGreen = new Color(0.35f, 1f, 0.45f);
        public static readonly Color TrapRed = new Color(1f, 0.2f, 0.1f), TrapYellow = new Color(1f, 0.85f, 0.1f);
        public static readonly List<GlowRing> All = new List<GlowRing>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        [field: SerializeField] public RingKind Kind { get; private set; }
        [field: SerializeField] public Color Color { get; private set; }
        [SerializeField] Renderer[] _r; [SerializeField] Color[] _c; [SerializeField] float _seed, _speed, _min, _max;

        static Mesh _full, _even, _odd;

        /// <summary>Кольцо (внутр. радиус 0.82 от внешнего 1), part: 0 — целое, 1 — чётные сектора, 2 — нечётные.</summary>
        static Mesh RingMesh(int part)
        {
            const int seg = 24; const float inner = 0.82f;
            var v = new List<Vector3>(); var tri = new List<int>(); var n = new List<Vector3>();
            for (int i = 0; i < seg; i++)
            {
                if (part == 1 && i % 2 == 1) continue;
                if (part == 2 && i % 2 == 0) continue;
                float a0 = i * Mathf.PI * 2f / seg, a1 = (i + 1) * Mathf.PI * 2f / seg;
                int b = v.Count;
                v.Add(new Vector3(Mathf.Cos(a0) * inner, 0, Mathf.Sin(a0) * inner));
                v.Add(new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)));
                v.Add(new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)));
                v.Add(new Vector3(Mathf.Cos(a1) * inner, 0, Mathf.Sin(a1) * inner));
                for (int k = 0; k < 4; k++) n.Add(Vector3.up);
                tri.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2, b, b + 1, b + 2, b, b + 2, b + 3 }); // обе стороны
            }
            var m = new Mesh { name = "PodzRing" + part };
            m.SetVertices(v); m.SetNormals(n); m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            return m;
        }

        static Mesh Get(int part)
        {
            if (part == 1) return _even != null ? _even : (_even = RingMesh(1));
            if (part == 2) return _odd != null ? _odd : (_odd = RingMesh(2));
            return _full != null ? _full : (_full = RingMesh(0));
        }

        static Renderer Piece(Transform parent, Mesh mesh, Color c)
        {
            var go = new GameObject("RingPart");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = GameAssets.I.lit;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            Prim.SetColor(r, c, c);
            return r;
        }

        /// <summary>Кольцо радиусом radius (в метрах, масштаб родителя компенсируется) на высоте 2–3 см над полом.</summary>
        public static GlowRing Create(Transform parent, Vector3 worldPos, float radius, Color c, RingKind kind)
        {
            var go = new GameObject("GlowRing_" + kind);
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos + Vector3.up * 0.045f;
            go.transform.rotation = Quaternion.identity;
            var ls = parent != null ? parent.lossyScale : Vector3.one;
            go.transform.localScale = new Vector3(radius / Mathf.Max(1e-4f, Mathf.Abs(ls.x)), 1f / Mathf.Max(1e-4f, Mathf.Abs(ls.y)), radius / Mathf.Max(1e-4f, Mathf.Abs(ls.z)));
            var g = go.AddComponent<GlowRing>();
            g.Kind = kind; g.Color = c; g._seed = Random.value * 6f;
            if (kind == RingKind.Trap)
            {
                g._r = new[] { Piece(go.transform, Get(1), TrapRed), Piece(go.transform, Get(2), TrapYellow) };
                g._c = new[] { TrapRed, TrapYellow };
            }
            else { g._r = new[] { Piece(go.transform, Get(0), c) }; g._c = new[] { c }; }
            switch (kind)
            {
                case RingKind.Heal: g._speed = 2.2f; g._min = 0.5f; g._max = 1.6f; break;
                case RingKind.Trap: g._speed = 1.5f; g._min = 0.5f; g._max = 1.1f; break;
                case RingKind.Loot: g._speed = 1.2f; g._min = 0.45f; g._max = 1.0f; break;
                default: g._speed = 1.0f; g._min = 0.45f; g._max = 0.8f; break;
            }
            if(kind==RingKind.Monster) foreach(var r in g._r) if(r!=null) Prim.SetColor(r,c*.6f,c*.12f);
            return g;
        }

        internal void RandomizePhase() => _seed = Random.value * 6f;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public static int Count(RingKind k) { int n = 0; foreach (var g in All) if (g != null && g.Kind == k) n++; return n; }

        void Update()
        {
            bool visible = !Tools.InDungeon || HintsToggle.On;
            foreach (var r in _r) if (r != null) r.enabled = visible;
            // держим кольцо ровно на полу, даже если родитель (монстр) наклоняется
            if (transform.parent != null) transform.rotation = Quaternion.identity;
            float k = Mathf.Lerp(_min, _max, 0.5f + 0.5f * Mathf.Sin((Time.time + _seed) * _speed * Mathf.PI));
            for (int i = 0; i < _r.Length; i++) if (_r[i] != null) Prim.SetColor(_r[i], _c[i] * 0.6f, _c[i] * k * (Kind==RingKind.Monster?.2f:1f));
        }
    }

    /// <summary>Светящиеся грибы и кристаллы (только эмиссия, без источников света — дёшево).</summary>
    public static class GlowDecor
    {
        public static void Mushrooms(Transform parent, Vector3 worldPos, Color cap, int count)
        {
            var t = new GameObject("GlowMushrooms").transform;
            t.SetParent(parent, true); t.position = worldPos;
            for (int i = 0; i < count; i++)
            {
                var p = new Vector3(Random.Range(-0.35f, 0.35f), 0, Random.Range(-0.35f, 0.35f));
                float h = Random.Range(0.12f, 0.32f);
                Prim.Cyl("Stem", t, p + Vector3.up * h * 0.5f, new Vector3(0.04f, h, 0.04f), new Color(0.85f, 0.85f, 0.75f));
                Prim.Glow(Prim.Sphere("Cap", t, p + Vector3.up * h, new Vector3(0.16f, 0.07f, 0.16f) * Random.Range(0.8f, 1.4f), cap), cap * 1.6f);
            }
        }

        public static void Crystals(Transform parent, Vector3 worldPos, Color c, int count)
        {
            var t = new GameObject("GlowCrystals").transform;
            t.SetParent(parent, true); t.position = worldPos;
            for (int i = 0; i < count; i++)
            {
                var p = new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f));
                float h = Random.Range(0.25f, 0.6f);
                Prim.Glow(Prim.Part(PrimitiveType.Cube, "Crystal", t, p + Vector3.up * h * 0.45f, new Vector3(0.1f, h, 0.1f), c, new Vector3(Random.Range(-25f, 25f), Random.Range(0, 90f), Random.Range(-25f, 25f))), c * 1.8f);
            }
        }

        /// <summary>Тёплый факел на стене: держатель + пламя + мерцающий свет.</summary>
        public static Light Torch(Transform parent, Vector3 localPos, Vector3 wallNormal)
        {
            var t = new GameObject("WarmTorch").transform;
            t.SetParent(parent, false); t.localPosition = localPos;
            t.localRotation = Quaternion.LookRotation(-wallNormal, Vector3.up);
            Prim.Part(PrimitiveType.Cube, "Plate", t, new Vector3(0, -0.05f, 0.01f), new Vector3(0.16f, 0.3f, 0.03f), new Color(0.22f, 0.2f, 0.18f));
            Prim.Part(PrimitiveType.Cube, "Bracket", t, new Vector3(0, -0.12f, 0.08f), new Vector3(0.05f, 0.05f, 0.14f), new Color(0.22f, 0.2f, 0.18f));
            Prim.Part(PrimitiveType.Cylinder, "Stick", t, new Vector3(0, -0.02f, 0.14f), new Vector3(0.06f, 0.36f, 0.06f), new Color(0.42f, 0.26f, 0.12f));
            Prim.Part(PrimitiveType.Cylinder, "Bowl", t, new Vector3(0, 0.17f, 0.14f), new Vector3(0.12f, 0.06f, 0.12f), new Color(0.3f, 0.26f, 0.22f));
            var flame = Prim.Glow(Prim.Sphere("TorchFlame", t, new Vector3(0, 0.27f, 0.14f), new Vector3(0.12f, 0.2f, 0.12f), new Color(1f, 0.7f, 0.3f)), new Color(1f, 0.55f, 0.15f) * 3.2f);
            Prim.Glow(Prim.Sphere("FlameCore", t, new Vector3(0, 0.25f, 0.14f), new Vector3(0.06f, 0.1f, 0.06f), new Color(1f, 0.95f, 0.7f)), new Color(1f, 0.9f, 0.6f) * 3f);
            var l = Prim.PointLight(flame.transform, Vector3.zero, new Color(1f, 0.68f, 0.38f), 7f, 1.6f);
            l.transform.position = flame.transform.position + t.forward * 0.3f;
            flame.AddComponent<Flicker>().Init(l);
            return l;
        }
    }
}
