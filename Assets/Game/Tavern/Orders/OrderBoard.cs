using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>Значки из простых фигур (галочка, крестик, стрелка) — для экранов кухни.</summary>
    public static class KIcons
    {
        static GameObject Bar(Transform p, Vector3 pos, float len, float th, float rot, Color c)
        {
            var g = Prim.Cube("IcoBar", p, pos, new Vector3(len, th, 0.008f), c, false);
            g.transform.localRotation = Quaternion.Euler(0, 0, rot);
            Prim.SetEmission(g.GetComponent<Renderer>(), c * 0.6f);
            return g;
        }
        public static Transform Check(Transform parent, Vector3 pos, float s, Color? col = null)
        {
            var root = new GameObject("IcoCheck").transform; root.SetParent(parent, false); root.localPosition = pos;
            var c = col ?? new Color(0.25f, 0.9f, 0.35f);
            Bar(root, new Vector3(-0.22f * s, -0.08f * s, 0), 0.45f * s, 0.14f * s, -45f, c);
            Bar(root, new Vector3(0.12f * s, 0.06f * s, 0), 0.8f * s, 0.14f * s, 50f, c);
            return root;
        }
        public static Transform Cross(Transform parent, Vector3 pos, float s)
        {
            var root = new GameObject("IcoCross").transform; root.SetParent(parent, false); root.localPosition = pos;
            var c = new Color(0.95f, 0.25f, 0.2f);
            Bar(root, Vector3.zero, 0.8f * s, 0.14f * s, 45f, c);
            Bar(root, Vector3.zero, 0.8f * s, 0.14f * s, -45f, c);
            return root;
        }
        public static Transform Arrow(Transform parent, Vector3 pos, float s, Color c)
        {
            var root = new GameObject("IcoArrow").transform; root.SetParent(parent, false); root.localPosition = pos;
            Bar(root, new Vector3(-0.1f * s, 0.16f * s, 0), 0.5f * s, 0.14f * s, -35f, c);
            Bar(root, new Vector3(-0.1f * s, -0.16f * s, 0), 0.5f * s, 0.14f * s, 35f, c);
            return root;
        }
        public static Transform Dot(Transform parent, Vector3 pos, float s, Color c)
        {
            var root = new GameObject("IcoDot").transform; root.SetParent(parent, false); root.localPosition = pos;
            Bar(root, Vector3.zero, 0.3f * s, 0.3f * s, 45f, c);
            return root;
        }

        /// <summary>Картинка блюда: миска с супом цвета рецепта и фигурки главных продуктов сверху (наклонена к зрителю).</summary>
        public static Transform Dish(Transform parent, Recipe r, Vector3 pos, float size)
        {
            var root = new GameObject("Dish_" + r.Index).transform; root.SetParent(parent, false); root.localPosition = pos;
            root.localRotation = Quaternion.Euler(-55f, 0, 0);
            var bowl = Prim.Shape(PrimitiveType.Cylinder, "Bowl", root, Vector3.zero, new Vector3(size, size * 0.18f, size), new Color(0.55f, 0.33f, 0.18f), false);
            var soup = Prim.Shape(PrimitiveType.Cylinder, "Soup", root, new Vector3(0, size * 0.1f, 0), new Vector3(size * 0.86f, size * 0.03f, size * 0.86f), r.SoupColor, false);
            Prim.SetEmission(soup.GetComponent<Renderer>(), r.SoupColor * 0.35f);
            var seen = new List<ItemId>();
            int k = 0;
            foreach (var id in r.Items)
            {
                if (seen.Contains(id)) continue; seen.Add(id);
                if (k >= 3) break;
                float a = k * 2.1f + 0.4f;
                var m = FoodModels.Build(id, root, size * 0.36f);
                m.localPosition = new Vector3(Mathf.Cos(a) * size * 0.22f, size * 0.12f, Mathf.Sin(a) * size * 0.22f);
                k++;
            }
            return root;
        }
    }

    /// <summary>
    /// Карточки заказов над окном раздачи (со стороны кухни): номер стола, картинка блюда, название, гость
    /// и кольцо терпения (зелёное → жёлтое → красное, сегменты гаснут по мере ожидания).
    /// </summary>
    public class OrderBoard : MonoBehaviour
    {
        public static OrderBoard I;
        public const int MaxCards = 4, RingSeg = 16;

        [System.Serializable]
        public class Card
        {
            public Transform Root, Pic; public TextMesh Title, Dish, Who; public Renderer Bg;
            public List<Renderer> Ring = new List<Renderer>();
            [System.NonSerialized] public Guest G; [System.NonSerialized] public Recipe R; public int RingOn = -1; public string Status = ""; public Color RingColor;
            /// <summary>К5: гость скоро уйдёт — карточка мигает красным.</summary>
            public bool Blinking; public bool BlinkLit; public int Blinks;
            public bool Visible => Root != null && Root.gameObject.activeSelf;
        }
        public List<Card> Cards = new List<Card>();
        public int VisibleCount { get; private set; }
        [SerializeField] TextMesh _empty;
        [SerializeField] GameObject _emptyBg;
        float _next;

        public static OrderBoard Create(Transform board)
        {
            var template = Application.isPlaying ? SavedDungeonAssets.Active?.Orders : null;
            if (template != null && template._empty != null && template.Cards.Count == MaxCards)
            {
                var saved = Instantiate(template, board, false);
                SavedDungeonAssets.RestoreText(saved.gameObject);
                I = saved;
                return saved;
            }
            var go = SavedUi.CreateRoot("OrderBoard");
            go.transform.SetParent(board, false);
            var b = go.AddComponent<OrderBoard>();
            I = b;
            b.Build();
            return b;
        }
        void OnDestroy() { if (I == this) I = null; }

        void Build()
        {
            Prim.Text(transform, new Vector3(0, 0.48f, -0.012f), "ЗАКАЗЫ", 0.09f, new Color(1f, 0.88f, 0.55f)).fontStyle = FontStyle.Bold;
            Prim.Cube("HeadStrip", transform, new Vector3(0, 0.48f, -0.004f), new Vector3(0.7f, 0.12f, 0.006f), new Color(0.14f, 0.1f, 0.07f), false);
            _emptyBg = Prim.Cube("EmptyBg", transform, new Vector3(0, 0, -0.004f), new Vector3(1.1f, 0.16f, 0.006f), new Color(0.14f, 0.1f, 0.07f), false);
            _empty = Prim.Text(transform, new Vector3(0, 0, -0.012f), "", 0.08f, new Color(1f, 0.92f, 0.75f));
            for (int i = 0; i < MaxCards; i++)
            {
                var c = new Card();
                c.Root = new GameObject("OrderCard" + i).transform;
                c.Root.SetParent(transform, false);
                c.Root.localPosition = new Vector3(-0.99f + i * 0.66f, -0.06f, -0.015f);
                c.Bg = Prim.Cube("CardBg", c.Root, new Vector3(0, 0, 0.004f), new Vector3(0.62f, 0.86f, 0.006f), new Color(0.14f, 0.1f, 0.07f), false).GetComponent<Renderer>();
                c.Title = Prim.Text(c.Root, new Vector3(0, 0.36f, -0.006f), "", 0.085f, new Color(1f, 0.85f, 0.3f)); c.Title.fontStyle = FontStyle.Bold;
                for (int s = 0; s < RingSeg; s++)
                {
                    float a = s * Mathf.PI * 2f / RingSeg;
                    var seg = Prim.Cube("Ring", c.Root, new Vector3(Mathf.Sin(a) * 0.19f, 0.09f + Mathf.Cos(a) * 0.19f, -0.004f), new Vector3(0.07f, 0.03f, 0.006f), Color.green, false);
                    seg.transform.localRotation = Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg);
                    c.Ring.Add(seg.GetComponent<Renderer>());
                }
                c.Dish = Prim.Text(c.Root, new Vector3(0, -0.21f, -0.006f), "", 0.055f, new Color(1f, 0.95f, 0.85f));
                c.Who = Prim.Text(c.Root, new Vector3(0, -0.355f, -0.006f), "", 0.045f, new Color(0.75f, 0.85f, 0.75f));
                c.Root.gameObject.SetActive(false);
                Cards.Add(c);
            }
        }

        static string Wrap2(string s, int max)
        {
            if (s.Length <= max) return s;
            int cut = s.LastIndexOf(' ', Mathf.Min(max, s.Length - 1));
            return cut > 3 ? s.Substring(0, cut) + "\n" + s.Substring(cut + 1) : s;
        }

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.15f;
            var gm = GuestManager.I;
            var list = new List<Guest>();
            if (gm != null) foreach (var g in gm.Guests) if (g != null && g.HasOrdered) list.Add(g);
            list.Sort((a, b) => a.Table.CompareTo(b.Table));
            VisibleCount = Mathf.Min(MaxCards, list.Count);
            string emptyText = list.Count > 0 ? "" : GameFlow.I != null && GameFlow.I.State == DayState.Open ? "ждём гостей…" : "таверна закрыта";
            if (_empty.text != emptyText) _empty.text = emptyText;
            if (_emptyBg.activeSelf != (emptyText.Length > 0)) _emptyBg.SetActive(emptyText.Length > 0);
            for (int i = 0; i < MaxCards; i++)
            {
                var c = Cards[i];
                var g = i < list.Count ? list[i] : null;
                if (c.Root.gameObject.activeSelf != (g != null)) c.Root.gameObject.SetActive(g != null);
                c.G = g;
                if (g == null) continue;
                if (c.R != g.Order)
                {
                    c.R = g.Order;
                    if (c.Pic != null) Destroy(c.Pic.gameObject);
                    c.Pic = KIcons.Dish(c.Root, g.Order, new Vector3(0, 0.09f, -0.03f), 0.24f);
                    c.Dish.text = Wrap2(g.Order.Name, 16);
                }
                string title = $"Стол {g.Table + 1}";
                if (c.Title.text != title) c.Title.text = title;
                c.Status = g.Assigned ? gm.DeliveryStatus(g) : g.Eating ? "ест" : g.Hurry ? "уходит!" : "ждёт";
                string missing = g.Waiting && !g.Assigned ? Recipes.MissingIngredients(g.Order) : "";
                string who = $"{Guest.TypeName(g.Type)} · {c.Status}";
                if (missing.Length > 0) who += "\nНе хватает: " + missing;
                if (c.Who.text != who) c.Who.text = who;
                float f = g.DishInTransit || g.Eating ? 1f : g.PatienceFrac;
                int on = Mathf.CeilToInt(f * RingSeg);
                if (on != c.RingOn)
                {
                    c.RingOn = on;
                    var col = f > 0.5f ? new Color(0.2f, 0.85f, 0.3f) : f > 0.2f ? new Color(1f, 0.8f, 0.1f) : new Color(1f, 0.2f, 0.15f);
                    c.RingColor = col;
                    for (int s = 0; s < RingSeg; s++)
                    {
                        bool lit = s < on;
                        Prim.SetColor(c.Ring[s], lit ? col : new Color(0.3f, 0.27f, 0.24f), lit ? col * 0.5f : Color.black);
                    }
                    if (!c.Blinking) Prim.SetColor(c.Bg, f > 0.2f ? new Color(0.14f, 0.1f, 0.07f) : new Color(0.35f, 0.08f, 0.06f));
                }
                // К5: скоро уйдёт — фон карточки мигает ярко-красным (2 раза в секунду), заголовок красный
                bool hurry = g.Hurry;
                bool blinkOn = hurry && Mathf.Repeat(Time.unscaledTime * 2f, 1f) < 0.5f;
                if (hurry != c.Blinking || blinkOn != c.BlinkLit)
                {
                    if (blinkOn && !c.BlinkLit) c.Blinks++;
                    c.Blinking = hurry; c.BlinkLit = blinkOn;
                    Prim.SetColor(c.Bg, blinkOn ? new Color(0.9f, 0.1f, 0.08f) : hurry ? new Color(0.3f, 0.05f, 0.04f) : f > 0.2f ? new Color(0.14f, 0.1f, 0.07f) : new Color(0.35f, 0.08f, 0.06f), blinkOn ? new Color(0.6f, 0.05f, 0.03f) : Color.black);
                    c.Title.color = hurry ? new Color(1f, 0.35f, 0.3f) : new Color(1f, 0.85f, 0.3f);
                }
            }
        }
    }
}
