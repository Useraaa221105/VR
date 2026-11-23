using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Плоские значки из примитивов для рецептов-картинок (баг-лист, пункт 5): котёл, вода, нож, тесак, молоток, доска, мойка,
    /// гриль, ледник, кладовая, таймер, половник, миска, окно раздачи, колокольчик, огонь. Смотрят в −Z (как текст на табличках).
    /// </summary>
    public static class PicIcons
    {
        static GameObject Q(Transform p, Vector3 pos, Vector3 size, Color c, float rotZ = 0, float glow = 0.35f)
        {
            var g = Prim.Cube("Pic", p, pos, size, c, false);
            if (rotZ != 0) g.transform.localRotation = Quaternion.Euler(0, 0, rotZ);
            Prim.SetEmission(g.GetComponent<Renderer>(), c * glow);
            return g;
        }
        static GameObject Disc(Transform p, Vector3 pos, float d, Color c, float glow = 0.35f)
        {
            var g = Prim.Shape(PrimitiveType.Cylinder, "PicDisc", p, pos, new Vector3(d, 0.004f, d), c, false);
            g.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Prim.SetEmission(g.GetComponent<Renderer>(), c * glow);
            return g;
        }
        static GameObject Blob(Transform p, Vector3 pos, Vector3 size, Color c, float glow = 0.35f)
        {
            var g = Prim.Shape(PrimitiveType.Sphere, "PicBlob", p, pos, size, c, false);
            Prim.SetEmission(g.GetComponent<Renderer>(), c * glow);
            return g;
        }
        static Transform Root(Transform parent, string name, Vector3 pos)
        {
            var r = new GameObject("Ico_" + name).transform; r.SetParent(parent, false); r.localPosition = pos; return r;
        }
        static readonly Color Iron = new Color(0.22f, 0.22f, 0.24f), Wood = new Color(0.62f, 0.4f, 0.2f), Steel = new Color(0.8f, 0.83f, 0.88f);

        public static Transform Cauldron(Transform parent, Vector3 pos, float s, Color? liquid = null)
        {
            var r = Root(parent, "Cauldron", pos);
            Blob(r, new Vector3(0, -0.08f * s, 0), new Vector3(s, 0.8f * s, 0.05f * s), Iron, 0.6f);
            Q(r, new Vector3(0, 0.3f * s, -0.01f), new Vector3(1.08f * s, 0.12f * s, 0.01f), Iron * 1.6f, 0, 0.6f);
            Q(r, new Vector3(0, 0.22f * s, -0.012f), new Vector3(0.84f * s, 0.07f * s, 0.01f), liquid ?? Cauldron_Water, 0, 0.8f);
            Q(r, new Vector3(-0.3f * s, -0.5f * s, 0), new Vector3(0.08f * s, 0.2f * s, 0.01f), Iron, 15);
            Q(r, new Vector3(0.3f * s, -0.5f * s, 0), new Vector3(0.08f * s, 0.2f * s, 0.01f), Iron, -15);
            for (int i = 0; i < 5; i++) { float a = Mathf.PI * i / 4f; Q(r, new Vector3(Mathf.Cos(a) * 0.45f * s, 0.33f * s + Mathf.Sin(a) * 0.3f * s, 0.005f), new Vector3(0.05f * s, 0.18f * s, 0.008f), Iron * 1.8f, a * Mathf.Rad2Deg); }
            return r;
        }
        public static readonly Color Cauldron_Water = new Color(0.25f, 0.6f, 1f);

        public static Transform Drop(Transform parent, Vector3 pos, float s)
        {
            var r = Root(parent, "Drop", pos);
            var c = new Color(0.35f, 0.7f, 1f);
            Blob(r, new Vector3(0, -0.1f * s, 0), new Vector3(0.62f * s, 0.62f * s, 0.06f * s), c, 0.8f);
            Q(r, new Vector3(0, 0.17f * s, 0), new Vector3(0.36f * s, 0.36f * s, 0.01f), c, 45, 0.8f);
            Blob(r, new Vector3(-0.12f * s, -0.05f * s, -0.02f * s), new Vector3(0.12f * s, 0.18f * s, 0.02f * s), Color.white, 0.8f);
            return r;
        }

        public static Transform Tool(Transform parent, ToolType t, Vector3 pos, float s)
        {
            var r = Root(parent, t.ToString(), pos);
            switch (t)
            {
                case ToolType.Knife:
                    Q(r, new Vector3(-0.3f * s, 0, 0), new Vector3(0.36f * s, 0.14f * s, 0.01f), Wood);
                    Q(r, new Vector3(0.15f * s, 0.02f * s, 0), new Vector3(0.58f * s, 0.18f * s, 0.01f), Steel, 0, 0.5f);
                    Q(r, new Vector3(0.44f * s, 0.06f * s, 0), new Vector3(0.13f * s, 0.13f * s, 0.01f), Steel, 45, 0.5f);
                    r.localRotation = Quaternion.Euler(0, 0, 30); break;
                case ToolType.Cleaver:
                    Q(r, new Vector3(-0.32f * s, -0.12f * s, 0), new Vector3(0.34f * s, 0.12f * s, 0.01f), Wood);
                    Q(r, new Vector3(0.12f * s, 0.02f * s, 0), new Vector3(0.56f * s, 0.42f * s, 0.01f), Steel, 0, 0.5f);
                    Q(r, new Vector3(0.12f * s, -0.18f * s, -0.002f), new Vector3(0.56f * s, 0.05f * s, 0.01f), Color.white, 0, 0.6f); break;
                case ToolType.Hammer:
                    Q(r, new Vector3(0, -0.15f * s, 0), new Vector3(0.12f * s, 0.6f * s, 0.01f), Wood);
                    Q(r, new Vector3(0, 0.2f * s, 0), new Vector3(0.6f * s, 0.26f * s, 0.01f), new Color(0.55f, 0.55f, 0.6f), 0, 0.5f);
                    r.localRotation = Quaternion.Euler(0, 0, -25); break;
                case ToolType.Ladle:
                    Q(r, new Vector3(0.1f * s, 0.15f * s, 0), new Vector3(0.08f * s, 0.6f * s, 0.01f), Wood, -20);
                    Blob(r, new Vector3(-0.1f * s, -0.22f * s, 0), new Vector3(0.36f * s, 0.24f * s, 0.04f * s), Steel, 0.5f); break;
                default:
                    Q(r, Vector3.zero, new Vector3(0.5f * s, 0.12f * s, 0.01f), Tools.ColorOf(t)); break;
            }
            return r;
        }

        public static Transform Board(Transform parent, Vector3 pos, float s)
        {
            var r = Root(parent, "Board", pos);
            Q(r, Vector3.zero, new Vector3(0.9f * s, 0.55f * s, 0.01f), new Color(0.85f, 0.68f, 0.45f), 0, 0.5f);
            Q(r, new Vector3(0, -0.24f * s, -0.002f), new Vector3(0.9f * s, 0.06f * s, 0.01f), new Color(0.6f, 0.45f, 0.28f));
            Disc(r, new Vector3(0.33f * s, 0.15f * s, -0.004f), 0.09f * s, new Color(0.35f, 0.25f, 0.15f));
            return r;
        }
        public static Transform Sink(Transform parent, Vector3 pos, float s)
        {
            var r = Root(parent, "Sink", pos);
            Q(r, new Vector3(0, -0.1f * s, 0), new Vector3(0.9f * s, 0.45f * s, 0.01f), new Color(0.7f, 0.72f, 0.75f));
            Q(r, new Vector3(0, -0.1f * s, -0.002f), new Vector3(0.72f * s, 0.3f * s, 0.01f), new Color(0.3f, 0.6f, 1f), 0, 0.8f);
            Q(r, new Vector3(0.2f * s, 0.25f * s, 0), new Vector3(0.08f * s, 0.3f * s, 0.01f), Steel);
            Q(r, new Vector3(0.1f * s, 0.38f * s, 0), new Vector3(0.25f * s, 0.07f * s, 0.01f), Steel);
            return r;
        }
        public static Transform Grill(Transform parent, Vector3 pos, float s)
        {
            var r = Root(parent, "Grill", pos);
            Q(r, Vector3.zero, new Vector3(0.9f * s, 0.55f * s, 0.01f), new Color(0.15f, 0.12f, 0.1f));
            for (int i = 0; i < 4; i++) Q(r, new Vector3(-0.3f * s + i * 0.2f * s, 0, -0.003f), new Vector3(0.06f * s, 0.45f * s, 0.01f), new Color(1f, 0.35f, 0.05f), 0, 1.2f);
            return r;
        }
        public static Transform Fridge(Transform parent, Vector3 pos, float s)
        {
            var r = Root(parent, "Fridge", pos);
            Q(r, Vector3.zero, new Vector3(0.62f * s, 0.95f * s, 0.01f), new Color(0.55f, 0.75f, 0.9f), 0, 0.5f);
            Q(r, new Vector3(0, 0.12f * s, -0.002f), new Vector3(0.62f * s, 0.03f * s, 0.01f), new Color(0.25f, 0.35f, 0.45f));
            Q(r, new Vector3(0.22f * s, -0.1f * s, -0.003f), new Vector3(0.05f * s, 0.2f * s, 0.01f), new Color(0.25f, 0.3f, 0.35f));
            for (int i = 0; i < 3; i++) Q(r, new Vector3(-0.05f * s, 0.3f * s, -0.004f), new Vector3(0.3f * s, 0.04f * s, 0.01f), Color.white, i * 60f, 0.9f); // снежинка
            return r;
        }
        public static Transform Pantry(Transform parent, Vector3 pos, float s)
        {
            var r = Root(parent, "Pantry", pos);
            Q(r, Vector3.zero, new Vector3(0.8f * s, 0.6f * s, 0.01f), new Color(0.6f, 0.42f, 0.22f));
            Q(r, new Vector3(0, 0, -0.002f), new Vector3(0.95f * s, 0.06f * s, 0.01f), new Color(0.4f, 0.26f, 0.12f), 34);
            Q(r, new Vector3(0, 0, -0.002f), new Vector3(0.95f * s, 0.06f * s, 0.01f), new Color(0.4f, 0.26f, 0.12f), -34);
            return r;
        }
        public static Transform Timer(Transform parent, Vector3 pos, float s, float progress = -1f)
        {
            var r = Root(parent, "Timer", pos);
            Disc(r, Vector3.zero, 0.85f * s, new Color(0.25f, 0.2f, 0.15f));
            Disc(r, new Vector3(0, 0, -0.002f), 0.72f * s, new Color(0.96f, 0.94f, 0.88f), 0.6f);
            Q(r, new Vector3(0, 0.46f * s, 0), new Vector3(0.16f * s, 0.1f * s, 0.01f), new Color(0.9f, 0.3f, 0.2f));
            Q(r, new Vector3(0, 0.13f * s, -0.005f), new Vector3(0.05f * s, 0.26f * s, 0.01f), Color.black, 0, 0f);
            float a = progress >= 0 ? -progress * 360f : -110f;
            var hand = new GameObject("Hand").transform; hand.SetParent(r, false); hand.localPosition = new Vector3(0, 0, -0.006f); hand.localRotation = Quaternion.Euler(0, 0, a);
            Q(hand, new Vector3(0, 0.15f * s, 0), new Vector3(0.04f * s, 0.3f * s, 0.01f), new Color(0.8f, 0.15f, 0.1f), 0, 0.5f);
            return r;
        }
        public static Transform Bowl(Transform parent, Vector3 pos, float s, Color? soup = null)
        {
            var r = Root(parent, "Bowl", pos);
            Blob(r, new Vector3(0, -0.08f * s, 0), new Vector3(0.95f * s, 0.55f * s, 0.05f * s), new Color(0.9f, 0.85f, 0.72f), 0.5f);
            Q(r, new Vector3(0, 0.13f * s, -0.01f), new Vector3(0.85f * s, 0.1f * s, 0.01f), soup ?? new Color(0.95f, 0.65f, 0.25f), 0, 0.7f);
            for (int i = 0; i < 3; i++) Q(r, new Vector3((-0.2f + i * 0.2f) * s, 0.36f * s, 0), new Vector3(0.05f * s, 0.2f * s, 0.01f), new Color(0.95f, 0.95f, 0.95f), 10f - i * 10f, 0.4f); // пар
            return r;
        }
        public static Transform Window(Transform parent, Vector3 pos, float s)
        {
            var r = Root(parent, "Window", pos);
            Q(r, Vector3.zero, new Vector3(0.8f * s, 0.7f * s, 0.01f), new Color(1f, 0.9f, 0.6f), 0, 0.8f);
            var fr = new Color(0.45f, 0.3f, 0.15f);
            Q(r, new Vector3(0, 0.35f * s, -0.002f), new Vector3(0.9f * s, 0.08f * s, 0.01f), fr);
            Q(r, new Vector3(0, -0.35f * s, -0.002f), new Vector3(0.9f * s, 0.12f * s, 0.01f), fr);
            Q(r, new Vector3(-0.42f * s, 0, -0.002f), new Vector3(0.08f * s, 0.75f * s, 0.01f), fr);
            Q(r, new Vector3(0.42f * s, 0, -0.002f), new Vector3(0.08f * s, 0.75f * s, 0.01f), fr);
            return r;
        }
        public static Transform Bell(Transform parent, Vector3 pos, float s)
        {
            var r = Root(parent, "Bell", pos);
            var gold = new Color(0.95f, 0.78f, 0.25f);
            Blob(r, new Vector3(0, 0, 0), new Vector3(0.6f * s, 0.55f * s, 0.05f * s), gold, 0.7f);
            Q(r, new Vector3(0, -0.24f * s, 0), new Vector3(0.8f * s, 0.08f * s, 0.01f), gold * 0.8f);
            Disc(r, new Vector3(0, 0.3f * s, 0), 0.12f * s, gold);
            return r;
        }
        public static Transform Flame(Transform parent, Vector3 pos, float s)
        {
            var r = Root(parent, "Flame", pos);
            Blob(r, Vector3.zero, new Vector3(0.45f * s, 0.75f * s, 0.05f * s), new Color(1f, 0.5f, 0.1f), 1f);
            Blob(r, new Vector3(0, -0.1f * s, -0.01f), new Vector3(0.25f * s, 0.4f * s, 0.03f * s), new Color(1f, 0.95f, 0.5f), 1f);
            return r;
        }
        public static Transform ArrowR(Transform parent, Vector3 pos, float s, Color? c = null) => KIcons.Arrow(parent, pos, s, c ?? new Color(1f, 0.9f, 0.6f));

        /// <summary>Значок места/инструмента подготовки продукта.</summary>
        public static void Prep(Transform parent, PrepKind prep, Vector3 pos, float s)
        {
            switch (prep)
            {
                case PrepKind.Knife: Board(parent, pos + new Vector3(-0.28f * s, 0, 0), s * 0.7f); Tool(parent, ToolType.Knife, pos + new Vector3(0.3f * s, 0, -0.01f), s * 0.8f); break;
                case PrepKind.Cleaver: Board(parent, pos + new Vector3(-0.28f * s, 0, 0), s * 0.7f); Tool(parent, ToolType.Cleaver, pos + new Vector3(0.3f * s, 0, -0.01f), s * 0.8f); break;
                case PrepKind.Hammer: Board(parent, pos + new Vector3(-0.28f * s, 0, 0), s * 0.7f); Tool(parent, ToolType.Hammer, pos + new Vector3(0.3f * s, 0, -0.01f), s * 0.8f); break;
                case PrepKind.WashOnly: Sink(parent, pos, s * 0.8f); break;
                case PrepKind.WashThenKnife: Sink(parent, pos + new Vector3(-0.36f * s, 0, 0), s * 0.6f); Board(parent, pos + new Vector3(0.05f * s, 0, 0), s * 0.55f); Tool(parent, ToolType.Knife, pos + new Vector3(0.4f * s, 0, -0.01f), s * 0.6f); break;
                case PrepKind.Grill: Grill(parent, pos, s * 0.8f); break;
            }
        }
    }

    /// <summary>Один шаг рецепта-картинки.</summary>
    public struct PicStep
    {
        public int Kind; // 0 — вода, 1 — продукт, 2 — варка, 3 — разлить и подать
        public ItemId Item; public bool Done, Current, Missing, FromFridge; public float Progress; public bool Stir;
    }

    /// <summary>
    /// Рецепт КАРТИНКАМИ (баг-лист, пункт 5): плитки шагов с номером в кружке и значками продукта, места и инструмента.
    /// Текущий шаг — жёлтая светящаяся рамка, сделанные — зелёные с большой галочкой. Используется табличкой над котлом,
    /// крупной карточкой по R и книгой рецептов на стене.
    /// </summary>
    public static class RecipePicture
    {
        public const float TileW = 0.37f, TileH = 0.4f;
        static readonly Color Done = new Color(0.12f, 0.38f, 0.16f), Cur = new Color(0.75f, 0.52f, 0.08f), Todo = new Color(0.2f, 0.18f, 0.16f), Bad = new Color(0.45f, 0.12f, 0.1f);

        public static List<PicStep> Steps(Cauldron c, Recipe r, int stage)
        {
            var list = new List<PicStep>();
            var steps = Guide.RecipeSteps(c, r);
            int cur = Guide.CurrentStep(c, r, steps);
            var need = new Dictionary<ItemId, int>();
            for (int i = 0; i < steps.Count; i++)
            {
                var p = new PicStep { Done = steps[i].done, Current = i == cur };
                if (i == 0) p.Kind = 0;
                else if (i <= r.Items.Length)
                {
                    p.Kind = 1; p.Item = r.Items[i - 1];
                    p.FromFridge = !Fridge.Infinite(p.Item);
                    if (!p.Done)
                    {
                        need.TryGetValue(p.Item, out var k); need[p.Item] = k + 1;
                        int onKitchen = 0; foreach (var g in Ingredient.All) if (g != null && g.Id == p.Item && !g.Spoiled) onKitchen++;
                        p.Missing = k >= onKitchen + Fridge.Count(p.Item);
                    }
                }
                else if (i == r.Items.Length + 1) { p.Kind = 2; p.Progress = c != null && c.State == SoupState.Cooking ? c.Progress01 : p.Done ? 1f : 0f; p.Stir = stage >= 2; }
                else p.Kind = 3;
                list.Add(p);
            }
            return list;
        }

        /// <summary>Построить плитку шага с центром в pos.</summary>
        public static Transform Tile(Transform parent, Vector3 pos, int number, PicStep s, Recipe r, float scale = 1f)
        {
            var t = new GameObject("Tile" + number).transform; t.SetParent(parent, false); t.localPosition = pos; t.localScale = Vector3.one * scale;
            var bgc = s.Done ? Done : s.Current ? Cur : s.Missing ? Bad : Todo;
            var bg = Prim.Cube("TileBg", t, new Vector3(0, 0, 0.004f), new Vector3(TileW - 0.02f, TileH - 0.02f, 0.006f), bgc, false);
            Prim.SetEmission(bg.GetComponent<Renderer>(), bgc * (s.Current ? 0.9f : 0.4f));
            if (s.Current)
            {   // жёлтая светящаяся рамка вокруг текущего шага
                var y = new Color(1f, 0.85f, 0.2f);
                foreach (var (p, sz) in new[] { (new Vector3(0, TileH * 0.5f - 0.01f, 0), new Vector3(TileW, 0.02f, 0.008f)), (new Vector3(0, -TileH * 0.5f + 0.01f, 0), new Vector3(TileW, 0.02f, 0.008f)),
                    (new Vector3(-TileW * 0.5f + 0.01f, 0, 0), new Vector3(0.02f, TileH, 0.008f)), (new Vector3(TileW * 0.5f - 0.01f, 0, 0), new Vector3(0.02f, TileH, 0.008f)) })
                { var f = Prim.Cube("CurFrame", t, p, sz, y, false); Prim.SetEmission(f.GetComponent<Renderer>(), y * 1.5f); }
            }
            // номер шага в кружке
            var disc = Prim.Shape(PrimitiveType.Cylinder, "NumDisc", t, new Vector3(-0.135f, 0.15f, -0.004f), new Vector3(0.075f, 0.003f, 0.075f), s.Done ? new Color(0.4f, 0.95f, 0.5f) : s.Current ? new Color(1f, 0.9f, 0.3f) : new Color(0.75f, 0.72f, 0.66f), false);
            disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var num = Prim.Text(t, new Vector3(-0.135f, 0.15f, -0.01f), number.ToString(), 0.06f, Color.black); num.fontStyle = FontStyle.Bold;
            const float z = -0.012f;
            switch (s.Kind)
            {
                case 0: // котёл + вода
                    PicIcons.Cauldron(t, new Vector3(-0.05f, -0.03f, z), 0.17f);
                    PicIcons.Drop(t, new Vector3(0.11f, 0.06f, z), 0.12f);
                    break;
                case 1:
                {
                    var info = Items.Get(s.Item);
                    var m = FoodModels.Build(s.Item, t, 0.13f);
                    foreach (var col in m.GetComponentsInChildren<Collider>()) Object.Destroy(col);
                    m.localPosition = new Vector3(-0.06f, 0.06f, -0.06f); m.localRotation = Quaternion.Euler(-25, 30, 0);
                    // откуда: ледник (добыча) или кладовая
                    if (s.FromFridge) PicIcons.Fridge(t, new Vector3(0.12f, 0.12f, z), 0.1f); else PicIcons.Pantry(t, new Vector3(0.12f, 0.12f, z), 0.09f);
                    // как подготовить → в котёл
                    if (info.Prep != PrepKind.None) PicIcons.Prep(t, info.Prep, new Vector3(-0.06f, -0.12f, z), 0.13f);
                    PicIcons.ArrowR(t, new Vector3(0.065f, -0.12f, z), 0.06f);
                    PicIcons.Cauldron(t, new Vector3(0.125f, -0.12f, z), 0.08f);
                    if (s.Missing) KIcons.Cross(t, new Vector3(0.0f, 0.03f, z * 2f), 0.22f);
                    break;
                }
                case 2: // таймер (+ половник на этапе 2+)
                    PicIcons.Timer(t, new Vector3(-0.03f, 0.0f, z), 0.2f, s.Progress);
                    if (s.Stir) PicIcons.Tool(t, ToolType.Ladle, new Vector3(0.12f, -0.08f, z), 0.12f);
                    else PicIcons.Flame(t, new Vector3(0.12f, -0.09f, z), 0.1f);
                    if (s.Progress > 0f && s.Progress < 1f)
                    {
                        Prim.Cube("Bar", t, new Vector3(0, -0.165f, z), new Vector3(0.3f, 0.03f, 0.004f), new Color(0.1f, 0.1f, 0.1f), false);
                        var fill = Prim.Cube("Fill", t, new Vector3(-0.15f + 0.15f * s.Progress, -0.165f, z * 1.4f), new Vector3(0.3f * s.Progress, 0.03f, 0.004f), new Color(0.3f, 0.95f, 0.4f), false);
                        Prim.SetEmission(fill.GetComponent<Renderer>(), new Color(0.2f, 0.7f, 0.3f));
                    }
                    break;
                default: // миска → окно (+ колокольчик)
                    PicIcons.Bowl(t, new Vector3(-0.1f, 0.0f, z), 0.12f, r.SoupColor);
                    PicIcons.ArrowR(t, new Vector3(0.0f, 0.0f, z), 0.06f);
                    PicIcons.Window(t, new Vector3(0.1f, 0.03f, z), 0.12f);
                    PicIcons.Bell(t, new Vector3(0.1f, -0.12f, z), 0.08f);
                    break;
            }
            if (s.Done) KIcons.Check(t, new Vector3(0.11f, 0.12f, z * 2.5f), 0.16f);
            return t;
        }

        /// <summary>Сетка плиток: до 4 в ряд, со стрелками между ними. Возвращает число плиток.</summary>
        public static int Grid(Transform parent, List<PicStep> steps, Recipe r, float topY, float scale = 1f)
        {
            int perRow = 4;
            float gap = 0.03f * scale, w = TileW * scale, h = TileH * scale;
            for (int i = 0; i < steps.Count; i++)
            {
                int row = i / perRow, col = i % perRow;
                int inRow = Mathf.Min(perRow, steps.Count - row * perRow);
                float x0 = -(inRow - 1) * (w + gap) * 0.5f;
                var pos = new Vector3(x0 + col * (w + gap), topY - h * 0.5f - row * (h + gap * 2f), 0);
                Tile(parent, pos, i + 1, steps[i], r, scale);
                if (col < inRow - 1) PicIcons.ArrowR(parent, pos + new Vector3((w + gap) * 0.5f, 0, -0.012f), 0.07f * scale, new Color(1f, 0.85f, 0.4f));
            }
            return steps.Count;
        }
    }

    /// <summary>Крупная карточка рецепта по центру экрана по клавише R (повторно R — закрыть). Те же картинки, что над котлом.</summary>
    public class RecipeCardHud : MonoBehaviour
    {
        public static RecipeCardHud I;
        public static bool Open => I != null && I._root != null && I._root.gameObject.activeSelf;
        public static int Opened;
        public int Tiles { get; private set; }
        public string Title { get; private set; } = "";
        Transform _root, _content;
        string _key = "";
        float _next;

        public static void Ensure()
        {
            if (I != null || PlayerRig.I == null || PlayerRig.I.Cam == null) return;
            var go = new GameObject("RecipeCardHud");
            go.transform.SetParent(PlayerRig.I.Cam.transform, false);
            go.transform.localPosition = new Vector3(0, 0.02f, 1.3f);
            I = go.AddComponent<RecipeCardHud>();
            I._root = go.transform;
            go.SetActive(false);
        }

        public static void Toggle()
        {
            Ensure();
            if (I == null) return;
            bool on = !Open;
            if (on && (DungeonManager.I != null && DungeonManager.I.InDungeon)) { Hints.Show("Рецепт (R) — на кухне", HintPri.Action); return; }
            I._root.gameObject.SetActive(on);
            if (on) { Opened++; I._key = ""; I.Rebuild(); }
        }
        public static void Close() { if (Open) I._root.gameObject.SetActive(false); }

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.25f;
            if (DungeonManager.I != null && DungeonManager.I.InDungeon) { Close(); return; }
            Rebuild();
        }

        void Rebuild()
        {
            var c = Cauldron.LabelOwner() ?? Guide.ActiveCauldron() ?? Cauldron.All.Find(x => x != null && x.gameObject.activeInHierarchy);
            var r = Guide.CookableRecipe(c);
            int stage = GameFlow.I != null ? GameFlow.I.TavernStage : 1;
            var steps = RecipePicture.Steps(c, r, stage);
            var sb = new System.Text.StringBuilder(); sb.Append(stage).Append('|').Append(r.Index);
            foreach (var s in steps) sb.Append(s.Done ? 'd' : s.Current ? 'c' : s.Missing ? 'm' : 't').Append((int)(s.Progress * 10));
            var key = sb.ToString();
            if (key == _key) return;
            _key = key;
            if (_content != null) { _content.SetParent(null); Destroy(_content.gameObject); }
            _content = new GameObject("Content").transform; _content.SetParent(_root, false);
            // масштаб: карточка 1.62 × 1.1 м на 1.3 м от глаз — по центру, крупно
            float sc = Mathf.Min(1f, 2f * HudPanel.HalfW * (1.3f / HudPanel.Z) * 0.92f / 1.62f);
            _content.localScale = Vector3.one * sc * 0.82f;
            Prim.Cube("Frame", _content, new Vector3(0, 0, 0.03f), new Vector3(1.64f, 1.12f, 0.02f), new Color(0.75f, 0.55f, 0.2f), false);
            Prim.Cube("Panel", _content, new Vector3(0, 0, 0.018f), new Vector3(1.6f, 1.08f, 0.02f), new Color(0.1f, 0.075f, 0.05f), false);
            KIcons.Dish(_content, r, new Vector3(-0.68f, 0.44f, -0.05f), 0.13f);
            Title = r.Name;
            var tt = Prim.Text(_content, new Vector3(-0.58f, 0.45f, -0.01f), r.Name, 0.085f, new Color(1f, 0.85f, 0.35f), TextAnchor.MiddleLeft); tt.fontStyle = FontStyle.Bold;
            Prim.Text(_content, new Vector3(0.76f, 0.45f, -0.01f), "R — закрыть", 0.04f, new Color(0.8f, 0.75f, 0.65f), TextAnchor.MiddleRight);
            Tiles = RecipePicture.Grid(_content, steps, r, 0.36f, 1f);
            Prim.OnTop(_root);
        }
    }
}
