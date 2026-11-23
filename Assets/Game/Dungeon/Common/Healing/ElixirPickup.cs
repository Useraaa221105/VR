using UnityEngine;

namespace Podz
{
    public class ElixirPickup : MonoBehaviour
    {
        public static readonly System.Collections.Generic.List<ElixirPickup> All = new System.Collections.Generic.List<ElixirPickup>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        [SerializeField] Transform _bottle;
        [SerializeField] float _phase; float _fullHintAt;
        public const float Radius = 1f;

        public static ElixirPickup Create(Transform parent, Vector3 worldPos)
        {
            var dm = DungeonManager.I;
            var index = dm != null && dm.InDungeon && dm.Level <= 2 ? dm.Level - 1 : 2;
            var template = SavedDungeonAssets.FindTemplate<ElixirPickup>(
                SavedDungeonAssets.Variant(SavedDungeonAssets.Active?.Elixirs, index));
            if (template != null && template._bottle != null && template.GetComponentInChildren<GlowRing>() != null)
            {
                worldPos.y = parent != null ? parent.position.y : worldPos.y;
                var saved = Instantiate(template, worldPos, Quaternion.identity, parent);
                saved.name = "Elixir";
                saved.GetComponentInChildren<GlowRing>().RandomizePhase();
                saved._phase = Random.value * 6f;
                SavedDungeonAssets.RestoreText(saved.gameObject);
                return saved;
            }
            var go = new GameObject("Elixir");
            go.transform.SetParent(parent, true);
            worldPos.y = parent != null ? parent.position.y : worldPos.y;
            go.transform.position = worldPos;
            var e = go.AddComponent<ElixirPickup>();
            e._bottle = new GameObject("Bottle").transform;
            e._bottle.SetParent(go.transform, false);
            e._bottle.localPosition = new Vector3(0, 0.55f, 0);
            BuildBottle(e._bottle, 1f);
            Prim.PointLight(go.transform, new Vector3(0, 0.7f, 0), Elixirs.Col, 3.5f, 1.4f);
            // тонкий светящийся столб — видно из другого конца комнаты
            var beam = Prim.Cyl("Beam", go.transform, new Vector3(0, 1.6f, 0), new Vector3(0.04f, 1.6f, 0.04f), Elixirs.Col);
            Prim.Glow(beam, Elixirs.Col * 2f);
            if (DungeonManager.I != null && DungeonManager.I.InDungeon && DungeonManager.I.Level == 1) {
                beam.GetComponent<Renderer>().enabled=false;
                foreach(var light in go.GetComponentsInChildren<Light>()){light.range=2f;light.intensity=.35f;}
            }
            if (DungeonManager.I != null && DungeonManager.I.InDungeon && DungeonManager.I.Level == 2) {
                beam.GetComponent<Renderer>().enabled=false;
                foreach(var light in go.GetComponentsInChildren<Light>()){light.range=2f;light.intensity=.35f;}
            }
            var label = Prim.Text(go.transform, new Vector3(0, 1.05f, 0), "Эликсир +20 HP", 0.06f, new Color(1f, 0.75f, 0.85f));
            label.gameObject.AddComponent<FaceViewer>();
            GlowRing.Create(go.transform, worldPos, 0.45f, Elixirs.Col, RingKind.Heal);
            e._phase = Random.value * 6f;
            return e;
        }

        /// <summary>Бутылочка из примитивов: колба, горлышко, пробка; жидкость светится.</summary>
        public static void BuildBottle(Transform t, float s)
        {
            Prim.Glow(Prim.Sphere("Flask", t, Vector3.zero, Vector3.one * 0.16f * s, Elixirs.Col), Elixirs.Col * 1.6f);
            Prim.Cyl("Neck", t, new Vector3(0, 0.1f * s, 0), new Vector3(0.05f, 0.05f, 0.05f) * s, new Color(0.85f, 0.9f, 0.95f));
            Prim.Cyl("Cork", t, new Vector3(0, 0.15f * s, 0), new Vector3(0.055f, 0.02f, 0.055f) * s, new Color(0.6f, 0.42f, 0.22f));
        }

        void Update()
        {
            if (_bottle != null)
            {
                _bottle.localRotation = Quaternion.Euler(0, Time.time * 70f, 0);
                _bottle.localPosition = new Vector3(0, 0.55f + 0.06f * Mathf.Sin(Time.time * 2.2f + _phase), 0);
            }
            var P = PlayerRig.I;
            if (P == null) return;
            var d = P.Feet - transform.position; d.y = 0;
            if (d.magnitude >= Radius) return;
            if (Elixirs.Count >= Elixirs.Max)
            {
                if (Time.time > _fullHintAt) { _fullHintAt = Time.time + 3f; Elixirs.Add(); }
                return;
            }
            if (Elixirs.Add()) { FX.Sparkle(transform.position + Vector3.up * 0.6f, Elixirs.Col); Destroy(gameObject); }
        }
    }
}
