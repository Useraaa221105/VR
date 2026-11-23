using System;
using UnityEngine;
namespace Podz
{
    public class Hazard : MonoBehaviour
    {
        [SerializeField] Renderer _r;
        [SerializeField] Color _c;
        [SerializeField] float _radius, _dmg, _t, _period;
        [SerializeField] string _name;
        Status? _status;
        [SerializeField, HideInInspector] bool _savedState, _hasStatus;
        [SerializeField, HideInInspector] Status _savedStatus;
#if UNITY_EDITOR
        public void PrepareSavedState() { _savedState=true;_hasStatus=_status.HasValue;if(_hasStatus)_savedStatus=_status.Value; }
#endif
        void Start() { if(!_savedState)return;_status=_hasStatus?(Status?)_savedStatus:null;_period=UnityEngine.Random.Range(4f,6f);_t=UnityEngine.Random.Range(0f,_period); }
        bool _struck;
        [SerializeField] Transform _vis; Vector3 _visRest;
        public static readonly System.Collections.Generic.List<Hazard> All = new System.Collections.Generic.List<Hazard>();
        public static int Strikes, PlayerHits;
        public static string LastHitName;
        public string Name => _name;
        public float Radius => _radius;
        internal bool UsesSavedVisual => _usesSavedVisual;
        public bool Warning => _t >= _period - 1.2f && _t < _period;
        /// <summary>Автотест: сразу перейти к предупреждению (через 1,2 с — удар).</summary>
        public void DebugTrigger() { _t = _period - 1.2f; _forced = true; }
        /// <summary>Автотест: остановить все ловушки, чтобы не мешали проверке монстров.</summary>
        public static bool Paused;
        bool _forced, _usesSavedVisual;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); Strikes = 0; PlayerHits = 0; LastHitName = null; Paused = false; }

        /// <summary>Внешний вид ловушки по названию: сталактит, споры, лозы, камни, маятник, дротики, решётка, кислота.</summary>
        void BuildVisual()
        {
            var t = transform;
            if (_name.Contains("сталактит") || _name.Contains("камнепад"))
            {
                var g = new GameObject("Drop"); g.transform.SetParent(t, false); g.transform.localPosition = Vector3.up * 3.2f;
                if (_name.Contains("сталактит"))
                    for (int i = 0; i < 3; i++) Prim.Part(PrimitiveType.Cylinder, "Stalactite", g.transform, new Vector3((i - 1) * 0.3f, -0.3f - (i % 2) * 0.15f, (i % 2) * 0.2f), new Vector3(0.18f - i * 0.03f, 0.7f, 0.18f), new Color(0.45f, 0.55f, 0.6f));
                else
                    for (int i = 0; i < 4; i++)
                    {
                        SampleRock(out var position, out var scale);
                        Prim.Sphere("Rock", g.transform, position, scale, new Color(0.45f, 0.42f, 0.35f));
                    }
                _vis = g.transform;
            }
            else if (_name.Contains("спор"))
            {
                Prim.Cyl("Stem", t, new Vector3(0, 0.3f, 0), new Vector3(0.15f, 0.6f, 0.15f), new Color(0.9f, 0.85f, 0.6f));
                var cap = Prim.Sphere("Cap", t, new Vector3(0, 0.62f, 0), new Vector3(0.6f, 0.3f, 0.6f), new Color(0.95f, 0.8f, 0.2f));
                Prim.Glow(cap, new Color(0.6f, 0.5f, 0.05f));
                _vis = cap.transform;
            }
            else if (_name.Contains("лоз"))
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 60f * Mathf.Deg2Rad;
                    Prim.Cyl("Vine", t, new Vector3(Mathf.Cos(a) * _radius * 0.6f, 0.05f, Mathf.Sin(a) * _radius * 0.6f), new Vector3(0.08f, _radius * 1.1f, 0.08f), new Color(0.2f, 0.5f, 0.15f), new Vector3(90, i * 60f + 90f, 0));
                }
            }
            else if (_name.Contains("маятник"))
            {
                var pivot = new GameObject("Pendulum"); pivot.transform.SetParent(t, false); pivot.transform.localPosition = Vector3.up * 3.4f;
                Prim.Cyl("Rod", pivot.transform, new Vector3(0, -1.3f, 0), new Vector3(0.06f, 2.6f, 0.06f), new Color(0.4f, 0.4f, 0.42f));
                Prim.Part(PrimitiveType.Cylinder, "Blade", pivot.transform, new Vector3(0, -2.7f, 0), new Vector3(1.1f, 0.06f, 0.4f), new Color(0.8f, 0.8f, 0.85f), new Vector3(0, 0, 0));
                _vis = pivot.transform;
            }
            else if (_name.Contains("дротик"))
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    var wall = Prim.Part(PrimitiveType.Cube, "DartBox", t, new Vector3(s * (_radius + 0.2f), 1.2f, 0), new Vector3(0.2f, 0.4f, 0.6f), new Color(0.3f, 0.28f, 0.26f));
                    for (int i = 0; i < 3; i++) Prim.Glow(Prim.Sphere("Hole", wall.transform.parent, new Vector3(s * (_radius + 0.09f), 1.2f, -0.18f + i * 0.18f), Vector3.one * 0.06f, Color.black), new Color(0.6f, 0.1f, 0f));
                }
                Prim.Part(PrimitiveType.Cube, "Plate", t, new Vector3(0, 0.02f, 0), new Vector3(0.6f, 0.03f, 0.6f), new Color(0.45f, 0.4f, 0.35f));
            }
            else if (_name.Contains("решётк"))
            {
                for (int i = -2; i <= 2; i++)
                {
                    Prim.Part(PrimitiveType.Cube, "Bar", t, new Vector3(i * _radius * 0.35f, 0.03f, 0), new Vector3(0.06f, 0.05f, _radius * 1.6f), new Color(0.2f, 0.18f, 0.17f));
                    Prim.Part(PrimitiveType.Cube, "Bar", t, new Vector3(0, 0.03f, i * _radius * 0.35f), new Vector3(_radius * 1.6f, 0.05f, 0.06f), new Color(0.2f, 0.18f, 0.17f));
                }
            }
            else if (_name.Contains("кислот"))
            {
                var g = new GameObject("Drip"); g.transform.SetParent(t, false); g.transform.localPosition = Vector3.up * 3.3f;
                Prim.Glow(Prim.Sphere("Drop", g.transform, Vector3.zero, Vector3.one * 0.18f, new Color(0.4f, 1f, 0.6f)), new Color(0.2f, 0.8f, 0.3f));
                _vis = g.transform;
            }
            if (_vis != null) _visRest = _vis.localPosition;
            var label = Prim.Text(t, Vector3.up * 0.25f, "! " + _name + " !", 0.06f, new Color(1f, 0.85f, 0.4f, 0.9f));
            label.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }

        /// <summary>Оформление сталактита только −1; цикл, предупреждение и урон неизменны.</summary>
        public void UpperCaveStoneVisual(float ceilingHeight) {
            if(_vis==null || !_name.Contains("сталактит"))return;
            if (_usesSavedVisual)
            {
                _vis.localPosition = Vector3.up * ceilingHeight;
                _visRest = _vis.localPosition;
                return;
            }
            foreach(var r in _vis.GetComponentsInChildren<Renderer>())r.enabled=false;
            _vis.localPosition=Vector3.up*ceilingHeight;_visRest=_vis.localPosition;
            CavePrototype.Stalactite(_vis,Vector3.zero,.8f,81);
        }
        /// <summary>Только визуальная замена ловушек руин −2, без изменения урона/таймингов.</summary>
        public void RuinsVisual(float ceilingHeight) {
            if(_name.Contains("камнепад") && _vis!=null){
                _vis.localPosition=Vector3.up*(ceilingHeight+.05f);_visRest=_vis.localPosition;
                foreach(var r in _vis.GetComponentsInChildren<Renderer>()){r.sharedMaterial=OvergrownRuins.Stone;r.SetPropertyBlock(null);}
            }
            if(_name.Contains("лоз") && !_usesSavedVisual){
                foreach(var r in GetComponentsInChildren<Renderer>())if(r.name=="Vine")r.enabled=false;
                OvergrownRuins.ThreatVines(transform);
            }
            if(_name.Contains("дротик"))foreach(var r in GetComponentsInChildren<Renderer>())if(r.name=="DartBox" || r.name=="Hole")r.enabled=false;
        }
        void AnimateVisual()
        {
            if (_vis == null) return;
            float warnAt = _period - 1.2f;
            if (_name.Contains("маятник"))
            {
                float a = Mathf.Sin(_t / _period * Mathf.PI * 2f) * 25f + (_t >= warnAt ? Mathf.Sin((_t - warnAt) / 1.5f * Mathf.PI) * 45f : 0);
                _vis.localRotation = Quaternion.Euler(0, 0, a);
            }
            else if (_name.Contains("спор"))
                _vis.localScale = Vector3.one * (_t >= warnAt ? 1f + Mathf.PingPong(_t * 4f, 0.25f) : 1f);
            else
            {
                // падает к полу в момент удара и возвращается
                float k = _t >= warnAt && _t < _period ? (_t - warnAt) / 1.2f : _t >= _period ? 1f : 0f;
                var p = _visRest;
                if (_t >= warnAt) p += Vector3.right * Mathf.Sin(_t * 40f) * 0.03f;           // дрожит
                if (_t >= _period - 0.25f) p.y = Mathf.Lerp(_visRest.y, 0.2f, Mathf.Clamp01((_t - (_period - 0.25f)) / 0.25f));
                _vis.localPosition = p;
                _vis.gameObject.SetActive(!(k >= 1f && _t > _period + 0.2f));
            }
        }

        static void SampleRock(out Vector3 position, out Vector3 scale)
        {
            position = new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), -0.2f, UnityEngine.Random.Range(-0.5f, 0.5f));
            scale = Vector3.one * UnityEngine.Random.Range(0.25f, 0.4f);
        }

        public static Hazard Create(Transform parent, Vector3 worldPos, string name, Color c, float radius, float dmg, Status? status = null)
        {
            var assets = SavedDungeonAssets.Active;
            var recipe = name switch
            {
                "споры" => assets?.CaveSpores,
                "споровый гриб" => assets?.RuinsSpores,
                "сталактит" when DungeonManager.I != null && DungeonManager.I.Level == 1 => assets?.CaveStalactite,
                "лозы" => assets?.RuinsVines,
                "камнепад" => assets?.RuinsRockfall,
                "дротики" when DungeonManager.I != null && DungeonManager.I.Level == 2 => assets?.RuinsDarts,
                "кислотная капля" => assets?.CaveAcid,
                "дротики" when DungeonManager.I != null && DungeonManager.I.Level == 3 => assets?.FortressDarts,
                "маятник" => assets?.FortressPendulum,
                "огненная решётка" => assets?.FortressFireGrate,
                _ => null
            };
            var prefab = recipe != null ? recipe.LastPrefab : null;
            var template = prefab != null ? prefab.GetComponentInChildren<Hazard>() : null;
            if (template != null && Mathf.Approximately(template._radius, radius) && template._dmg == dmg &&
                template._hasStatus == status.HasValue && (!status.HasValue || template._savedStatus == status.Value))
            {
                var root = Instantiate(prefab, worldPos, Quaternion.identity, parent);
                var saved = root.GetComponentInChildren<Hazard>();
                saved._name = name;
                saved._c = c;
                saved._status = status;
                saved._savedState = false;
                saved._usesSavedVisual = true;
                SavedDungeonAssets.RestoreText(saved.gameObject);
                saved._period = UnityEngine.Random.Range(4f, 6f);
                saved._t = UnityEngine.Random.Range(0f, saved._period);
                if (saved._vis != null)
                {
                    if (name == "камнепад")
                    {
                        saved._vis.localPosition = Vector3.up * 3.2f;
                        var rocks = Array.FindAll(saved._vis.GetComponentsInChildren<Transform>(true),
                            part => part.parent == saved._vis && part.name == "Rock");
                        for (int i = 0; i < 4; i++)
                        {
                            SampleRock(out var position, out var scale);
                            if (i >= rocks.Length) continue;
                            rocks[i].localPosition = position;
                            rocks[i].localScale = scale;
                        }
                    }
                    saved._visRest = saved._vis.localPosition;
                }
                var ring = saved.GetComponentInChildren<GlowRing>();
                if (ring != null) ring.RandomizePhase();
                return saved;
            }
            var go = new GameObject("Hazard_" + name);
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            var h = go.AddComponent<Hazard>();
            h._name = name; h._c = c; h._radius = radius; h._dmg = dmg; h._status = status;
            h._r = FX.Disc(go.transform, Vector3.up * 0.015f, radius, c * 0.5f).GetComponent<Renderer>();
            h._period = UnityEngine.Random.Range(4f, 6f);
            h._t = UnityEngine.Random.Range(0f, h._period);
            h.BuildVisual();
            GlowRing.Create(go.transform, worldPos, radius + 0.06f, c, RingKind.Trap); // исправление 5: красно-жёлтая «опасная» кайма
            return h;
        }

        void Update()
        {
            float dt = DTime.Delta;
            if (dt <= 0 || PlayerRig.I == null) return;
            if ((PlayerRig.I.Feet - transform.position).sqrMagnitude > 400f) return; // далеко — спит
            if (Paused && !_forced) return;
            _t += dt;
            float warnAt = _period - 1.2f;
            if (_t < warnAt) { Prim.SetColor(_r, _c * 0.5f); _struck = false; }
            else if (_t < _period)
            {
                if (_t - dt < warnAt) Sfx.Play(SfxId.Warn, transform.position, 0.5f);
                Prim.SetColor(_r, _c, _c * (Mathf.PingPong(_t * 6f, 1f) * 2f)); // предупреждение
            }
            else if (!_struck)
            {
                _struck = true;
                Strikes++;
                Sfx.Play(SfxId.Strike, transform.position);
                Prim.SetColor(_r, Color.white, _c * 4f);
                var d = PlayerRig.I.Feet - transform.position; d.y = 0;
                if (d.magnitude < _radius)
                {
                    PlayerHits++; LastHitName = _name;
                    if (_dmg > 0) PlayerRig.I.Health.Damage(_dmg, _name);
                    if (_status.HasValue) PlayerRig.I.Health.Apply(_status.Value, 3f);
                }
            }
            AnimateVisual();
            if (_t > _period + 0.3f) { _t = 0; _forced = false; if (_vis != null) { _vis.localPosition = _visRest; _vis.gameObject.SetActive(true); } }
        }
    }
}
