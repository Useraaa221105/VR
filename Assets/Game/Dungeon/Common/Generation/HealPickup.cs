using System;
using UnityEngine;
namespace Podz
{
    public class HealPickup : MonoBehaviour
    {
        [SerializeField] float _amount;
        [SerializeField] int _uses = 2;
        [field: SerializeField] public HealKind Kind { get; private set; }
        public static readonly System.Collections.Generic.List<HealPickup> All = new System.Collections.Generic.List<HealPickup>();
        public static int Used;
        public static string LastUsed;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); Used = 0; LastUsed = null; }

        public static string KindName(HealKind k) => "целебный колодец";

        public static HealPickup Create(Transform parent, Vector3 worldPos, float amount) => Create(parent, worldPos, amount, HealKind.Well);

        public static HealPickup Create(Transform parent, Vector3 worldPos, float amount, HealKind kind)
        {
            var go = new GameObject("Heal_Well");
            go.transform.SetParent(parent, true);
            go.transform.position = worldPos;
            var t = go.transform;
            Prim.Cyl("WellRing", t, new Vector3(0, 0.35f, 0), new Vector3(1.0f, 0.7f, 1.0f), new Color(0.5f, 0.48f, 0.42f));
            Prim.Glow(Prim.Cyl("Water", t, new Vector3(0, 0.71f, 0), new Vector3(0.8f, 0.02f, 0.8f), new Color(0.3f, 0.7f, 1f)), new Color(0.2f, 0.5f, 0.9f) * 1.2f);
            for (int s = -1; s <= 1; s += 2) Prim.Part(PrimitiveType.Cube, "Post", t, new Vector3(s * 0.45f, 1.0f, 0), new Vector3(0.08f, 0.9f, 0.08f), new Color(0.4f, 0.28f, 0.15f));
            Prim.Part(PrimitiveType.Cube, "Roof", t, new Vector3(0, 1.5f, 0), new Vector3(1.1f, 0.08f, 0.6f), new Color(0.5f, 0.2f, 0.12f));
            var label = Prim.Text(t, new Vector3(0, 1.75f, 0), $"+{amount:0} HP · {KindName(kind)}", 0.06f, new Color(0.6f, 1f, 0.6f));
            label.gameObject.AddComponent<FaceViewer>();
            var h = go.AddComponent<HealPickup>();
            h._amount = amount; h.Kind = HealKind.Well; h._uses = 2;
            GlowRing.Create(go.transform, worldPos, 0.9f, GlowRing.HealGreen, RingKind.Heal); // исправление 5: зелёное пульсирующее кольцо
            return h;
        }

        void Update()
        {
            var P = PlayerRig.I;
            if (P == null) return;
            var d = P.Feet - transform.position; d.y = 0;
            if (d.magnitude < 1.1f && P.Health.HP < PlayerHealth.Max)
            {
                P.Health.Heal(_amount, PlayerHealth.HealSource.Well);
                Used++; LastUsed = KindName(Kind);
                _uses--;
                Hints.Show($"+{_amount:0} HP ({KindName(Kind)}){(_uses > 0 ? $" — осталось {_uses}" : "")}", HintPri.Error);
                if (_uses <= 0) Destroy(gameObject);
            }
        }
    }
}
