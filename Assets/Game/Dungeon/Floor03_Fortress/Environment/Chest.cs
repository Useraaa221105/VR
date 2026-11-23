using UnityEngine;

namespace Podz
{
    public class Chest : MonoBehaviour, IHittable
    {
        [SerializeField] Pressable _btn;
        [SerializeField] Transform _lid;
        bool _opened;
        [SerializeField, HideInInspector] bool _savedBindings;
#if UNITY_EDITOR
        public void PrepareSavedBindings() { _savedBindings=true; }
#endif
        void Start() { if(_savedBindings && _btn!=null)_btn.OnPress=Open; }

        public static Chest Create(Transform parent, Vector3 pos, float yaw)
        {
            var template = SavedDungeonAssets.FindTemplate<Chest>(SavedDungeonAssets.Active?.FortressChest);
            if (template != null && template._lid != null && template._btn != null &&
                template.GetComponentInChildren<GlowRing>() != null)
            {
                var saved = Instantiate(template, parent, false);
                saved.name = "Chest";
                saved.transform.SetLocalPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
                var dust = System.Array.FindAll(saved._lid.GetComponentsInChildren<Transform>(true),
                    part => part.parent == saved._lid && part.name == "Dust");
                for (int i = 0; i < 7; i++)
                {
                    var position = SampleDust();
                    if (i < dust.Length) dust[i].localPosition = position;
                }
                saved.GetComponentInChildren<GlowRing>().RandomizePhase();
                saved._savedBindings = false;
                saved._btn.OnPress = saved.Open;
                SavedDungeonAssets.RestoreText(saved.gameObject);
                return saved;
            }
            var go = new GameObject("Chest");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var c = go.AddComponent<Chest>();
            var wood = new Color(0.45f, 0.3f, 0.16f);
            Prim.Cube("Box", go.transform, new Vector3(0, 0.22f, 0), new Vector3(0.7f, 0.44f, 0.45f), wood);
            c._lid = Prim.Cube("Lid", go.transform, new Vector3(0, 0.5f, 0), new Vector3(0.72f, 0.12f, 0.47f), wood * 1.1f).transform;
            Prim.Cube("Lock", go.transform, new Vector3(0, 0.42f, 0.235f), new Vector3(0.08f, 0.1f, 0.02f), new Color(0.8f, 0.7f, 0.2f), false);
            // пыль: серые «хлопья» на крышке — главный признак настоящего сундука
            for (int i = 0; i < 7; i++)
                Prim.Cube("Dust", c._lid, SampleDust(), new Vector3(0.12f, 0.15f, 0.12f), new Color(0.6f, 0.6f, 0.58f), false);
            GlowRing.Create(go.transform, go.transform.position, 0.75f, GlowRing.Gold, RingKind.Loot); // исправление 5: добыча — золотое кольцо
            c._btn = Pressable.Create("Открыть", go.transform, new Vector3(0, 0.3f, 0.26f), wood * 0.8f, c.Open, new Vector3(0.2f, 0.08f, 0.02f), 0.03f);
            return c;
        }

        static Vector3 SampleDust() => new Vector3(Random.Range(-0.4f, 0.4f), 0.55f, Random.Range(-0.4f, 0.4f));

        public static int OpenedTotal;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetOpened() { OpenedTotal = 0; }
        /// <summary>Автотест: открыть сундук как кнопкой.</summary>
        public void DebugOpen() => Open();
        void Open()
        {
            if (_opened) return;
            _opened = true;
            var ring = GetComponentInChildren<GlowRing>();
            if (ring != null) Destroy(ring.gameObject); // добыча забрана — кольцо гаснет
            int coins = Random.Range(10, 21);
            Wallet.Add(coins);
            bool elixir = Elixirs.Count < Elixirs.Max;
            if (elixir) { Elixirs.Count++; Elixirs.Picked++; }
            Hints.Show(elixir ? $"Сундук: +{coins} монет и {Elixirs.Name} ({Elixirs.Count}/{Elixirs.Max})" : $"Сундук: +{coins} монет ({Elixirs.FullText.ToLower()})", HintPri.Window);
            OpenedTotal++;
            _lid.localPosition += new Vector3(0, 0.15f, -0.2f);
            _lid.localRotation = Quaternion.Euler(-60, 0, 0);
            _btn.SetLabel("Пусто");
            _btn.SetInteractable(false);
        }

        public void OnToolHit(HitInfo h)
        {
            if (h.Speed < 0.5f) Hints.Show("Глухо — обычный сундук", HintPri.Action);
        }
    }
}
