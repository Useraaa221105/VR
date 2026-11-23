using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Podz
{
    /// <summary>Значок шага на карточке монстра.</summary>
    public enum CardIcon { Yellow, Tool, Lmb, Rmb, KeyE, KeyC, KeyF, Shield }

    public struct CardStep
    {
        public CardIcon Icon; public string Text; public ToolType Tool;
        public static CardStep Of(CardIcon i, string t, ToolType tool = ToolType.Spatula) => new CardStep { Icon = i, Text = t, Tool = tool };
    }

    /// <summary>Короткое «как убить» с картинками для карточки первой встречи (пункт 8).</summary>
    public struct CardData
    {
        public string Who, YellowWhen, Dont2, Window;
        public CardStep[] Steps;
    }

    /// <summary>
    /// Пункт 8: карточка первой встречи — картинка монстра и название, кто это и чем опасен, КАК УБИТЬ по шагам с картинками
    /// («1. Дождись жёлтого света → 2. [значок] Лопатка (1) → 3. Бей [ЛКМ]»), НЕЛЬЗЯ, окно удара, светофор, кнопка «Понятно (Enter)».
    /// Подробный текст (все шаги, уворот, ингредиент) — вторая страница (Tab).
    /// </summary>
    public static class MonsterCard
    {
        public const string DontGreen = "Не бей, пока зелёный — лут испортится";

        public static string WindowText(MonsterKind k) => k == MonsterKind.Basilisk ? "5 секунд" : k == MonsterKind.Dragon ? "5 секунд (перегрев) / 4 секунды (хвост)" : "3 секунды";

        public static CardData Get(MonsterKind k)
        {
            string key(ToolType t) => Tools.DungeonKey(t);
            switch (k)
            {
                case MonsterKind.Slime: return new CardData {
                    Who = "Живое желе. Прыгает на лицо и душит (−2 HP в секунду). Слабый.",
                    YellowWhen = "оглушён (после удара лопаткой или промаха)", Dont2 = "Не бей сачком, тесаком, молотком — раздавишь",
                    Steps = new[] { CardStep.Of(CardIcon.Yellow, "Дождись жёлтого света"), CardStep.Of(CardIcon.Tool, $"Возьми лопатку ({key(ToolType.Spatula)})", ToolType.Spatula), CardStep.Of(CardIcon.Lmb, "Бей ЛКМ") } };
                case MonsterKind.Mushroom: return new CardData {
                    Who = "Ходячий гриб. Убегает, вблизи пускает споры (−3 HP). Слабый.",
                    YellowWhen = "устал после бега или загнан в угол", Dont2 = "Не бей лопаткой, тесаком, молотком — растечётся",
                    Steps = new[] { CardStep.Of(CardIcon.Yellow, "Дождись жёлтого: устал"), CardStep.Of(CardIcon.Tool, $"Возьми сачок ({key(ToolType.Net)})", ToolType.Net), CardStep.Of(CardIcon.Lmb, "Накрой сверху ЛКМ") } };
                case MonsterKind.Mandrake: return new CardData {
                    Who = "Корень с листьями. Кричит, если разбудить или выдернуть (−8…−15 HP).",
                    YellowWhen = "листья схвачены рукой", Dont2 = "Не выдёргивай, не срезав листья — крик и порча",
                    Steps = new[] { CardStep.Of(CardIcon.KeyC, $"Подкрадись присев ({Tools.CrouchKey})"), CardStep.Of(CardIcon.KeyE, "Схвати листья (E) — станет жёлтой"), CardStep.Of(CardIcon.Tool, $"Тесак ({key(ToolType.Cleaver)})", ToolType.Cleaver), CardStep.Of(CardIcon.Rmb, "Срежь ПКМ, потом E") } };
                case MonsterKind.Basilisk: return new CardData {
                    Who = "Петух-змей. Клюёт (−12 HP), взгляд окаменяет, хвост кусает сзади.",
                    YellowWhen = "клюв застрял в полу / оглушён", Dont2 = "Не смотри в глаза, не заходи сзади",
                    Steps = new[] { CardStep.Of(CardIcon.Yellow, "Дождись жёлтого: клюв застрял"), CardStep.Of(CardIcon.Tool, $"Возьми тесак ({key(ToolType.Cleaver)})", ToolType.Cleaver), CardStep.Of(CardIcon.Lmb, "Бей по шее ЛКМ ×2"), CardStep.Of(CardIcon.KeyE, "Хватай голову (E)") } };
                case MonsterKind.Armor: return new CardData {
                    Who = "Пустые доспехи с мечом (−12 HP). Внутри живут моллюски.",
                    YellowWhen = "меч застрял / доспех на колене", Dont2 = "Не бей спереди и по щиту — отскок",
                    Steps = new[] { CardStep.Of(CardIcon.Yellow, "Дождись жёлтого: меч застрял"), CardStep.Of(CardIcon.Tool, $"Возьми молоток ({key(ToolType.Hammer)})", ToolType.Hammer), CardStep.Of(CardIcon.Lmb, "Бей ЛКМ ×3"), CardStep.Of(CardIcon.KeyE, "Сорви шлем (E)") } };
                case MonsterKind.Mimic: return new CardData {
                    Who = "Сундук-ловушка. Засада (−18 HP) и клешни (−15 HP).",
                    YellowWhen = "панцирь треснул", Dont2 = "Не открывай подозрительный сундук рукой",
                    Steps = new[] { CardStep.Of(CardIcon.KeyF, "Простучи сундук (F)"), CardStep.Of(CardIcon.Tool, $"Возьми молоток ({key(ToolType.Hammer)})", ToolType.Hammer), CardStep.Of(CardIcon.Lmb, "Бей по крышке ЛКМ ×3"), CardStep.Of(CardIcon.KeyE, "Жёлтый: тяни за ногу (E)") } };
                default: return new CardData {
                    Who = "Босс логова. Огонь, лапа, хвост и огненные круги (до −25 HP).",
                    YellowWhen = "перегрев (голова внизу) / хвост пролетел", Dont2 = "Не стой в огне и в красных кругах",
                    Steps = new[] { CardStep.Of(CardIcon.Shield, $"Щит ({key(ToolType.Shield)}) — лови огонь", ToolType.Shield), CardStep.Of(CardIcon.Yellow, "Дождись жёлтого: перегрев"), CardStep.Of(CardIcon.Tool, $"Возьми тесак ({key(ToolType.Cleaver)})", ToolType.Cleaver), CardStep.Of(CardIcon.Lmb, "Руби горло ЛКМ ×3") } };
            }
        }

        // ---------- портрет ----------
        static readonly Dictionary<MonsterKind, List<CardPart>> _portraits = new Dictionary<MonsterKind, List<CardPart>>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _portraits.Clear();
        public static bool HasPortrait(MonsterKind k) => _portraits.ContainsKey(k);

        public static void Remember(Monster m)
        {
            if (m == null || _portraits.ContainsKey(m.Kind)) return;
            var l = new List<CardPart>(); m.PortraitParts(l);
            if (l.Count > 0) _portraits[m.Kind] = l;
        }

        /// <summary>Нарисовать монстра в рамке (size — сторона квадрата). Нет портрета — силуэт цвета инструмента с буквой.</summary>
        public static bool Portrait(Transform root, MonsterKind k, Monster m, Vector3 center, float size)
        {
            if (m != null) Remember(m);
            if (!_portraits.ContainsKey(k)) foreach (var a in MonsterRegistry.Alive) if (a != null && !a.Dead && a.Kind == k) { Remember(a); break; }
            if (!_portraits.TryGetValue(k, out var parts))
            {
                var t = Prim.Text(root, center, Bestiary.Get(k).Name.Substring(0, 1), size * 0.6f, Tools.ColorOf(Bestiary.Get(k).Tool));
                t.GetComponent<MeshRenderer>().sortingOrder = 5;
                return false;
            }
            var b = new Bounds(); bool first = true;
            foreach (var p in parts)
            {
                var mb = p.Mesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var w = p.Mat.MultiplyPoint3x4(c);
                    if (first) { b = new Bounds(w, Vector3.zero); first = false; } else b.Encapsulate(w);
                }
            }
            float scale = size * 0.68f / Mathf.Max(0.05f, Mathf.Max(b.size.x, b.size.y, b.size.z));
            var holder = new GameObject("Portrait").transform;
            holder.SetParent(root, false);
            holder.localPosition = center;
            holder.localRotation = Quaternion.Euler(12f, 150f, 0f); // монстр лицом к игроку, вполоборота
            holder.localScale = Vector3.one * scale;
            var pivot = new GameObject("Pivot").transform; pivot.SetParent(holder, false); pivot.localPosition = -b.center;
            foreach (var p in parts)
            {
                var go = new GameObject("Part");
                go.transform.SetParent(pivot, false);
                go.transform.localPosition = p.Mat.GetColumn(3);
                go.transform.localRotation = p.Mat.rotation;
                go.transform.localScale = p.Mat.lossyScale;
                go.AddComponent<MeshFilter>().sharedMesh = p.Mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = p.Material; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Prim.SetColor(r, p.Col);
            }
            return true;
        }

        // ---------- значки ----------
        static Renderer Q(Transform p, Vector3 pos, Vector2 size, Color c, int order = 4)
        {
            var q = Prim.Quad("Ico", p, pos, size, c); q.sortingOrder = order; return q;
        }
        static TextMesh Tx(Transform p, Vector3 pos, string s, float h, Color c, TextAnchor a = TextAnchor.MiddleCenter)
        {
            var t = Prim.Text(p, pos, s, h, c, a); t.GetComponent<MeshRenderer>().sortingOrder = 6; return t;
        }
        /// <summary>Круг «светофора» (цилиндр, повернутый к игроку).</summary>
        public static void Lamp(Transform p, Vector3 pos, float d, Color c)
        {
            var g = Prim.Shape(PrimitiveType.Cylinder, "Lamp", p, pos, new Vector3(d, 0.004f, d), c, false);
            g.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Prim.SetEmission(g.GetComponent<Renderer>(), c);
        }

        public static void Icon(Transform p, Vector3 pos, CardStep s)
        {
            var dark = new Color(0.08f, 0.06f, 0.05f);
            switch (s.Icon)
            {
                case CardIcon.Yellow:
                    Lamp(p, pos + new Vector3(0, 0, -0.004f), 0.085f, new Color(1f, 0.85f, 0.1f));
                    break;
                case CardIcon.Tool:
                case CardIcon.Shield:
                    Q(p, pos, new Vector2(0.09f, 0.09f), Tools.ColorOf(s.Tool));
                    var k = Tx(p, pos + new Vector3(0, 0, -0.003f), Tools.DungeonKey(s.Tool), 0.06f, Color.black); k.fontStyle = FontStyle.Bold;
                    break;
                case CardIcon.Lmb:
                case CardIcon.Rmb:
                    Q(p, pos, new Vector2(0.065f, 0.095f), new Color(0.85f, 0.85f, 0.85f));
                    Q(p, pos + new Vector3(s.Icon == CardIcon.Lmb ? -0.0165f : 0.0165f, 0.024f, -0.002f), new Vector2(0.03f, 0.045f), new Color(1f, 0.8f, 0.1f), 5);
                    Q(p, pos + new Vector3(0, 0.024f, -0.003f), new Vector2(0.003f, 0.045f), dark, 5);
                    Q(p, pos + new Vector3(0, 0.001f, -0.003f), new Vector2(0.065f, 0.003f), dark, 5);
                    break;
                default:
                    string letter = s.Icon == CardIcon.KeyE ? "E" : s.Icon == CardIcon.KeyF ? "F" : Tools.CrouchKey.Split(' ')[0];
                    Q(p, pos, new Vector2(0.09f, 0.09f), new Color(0.75f, 0.72f, 0.68f));
                    Q(p, pos + new Vector3(0, 0.004f, -0.002f), new Vector2(0.075f, 0.072f), new Color(0.95f, 0.93f, 0.9f), 5);
                    var t = Tx(p, pos + new Vector3(0, 0.004f, -0.004f), letter, 0.055f, Color.black); t.fontStyle = FontStyle.Bold;
                    break;
            }
        }

        /// <summary>Шеврон «→» между плитками.</summary>
        static void Chevron(Transform p, Vector3 pos, Color c)
        {
            var a = Q(p, pos + new Vector3(0, 0.011f, 0), new Vector2(0.035f, 0.009f), c); a.transform.localRotation = Quaternion.Euler(0, 0, -40f);
            var b = Q(p, pos + new Vector3(0, -0.011f, 0), new Vector2(0.035f, 0.009f), c); b.transform.localRotation = Quaternion.Euler(0, 0, 40f);
        }

        /// <summary>Лицевая страница карточки. Возвращает её текст (для автотеста).</summary>
        public static string BuildFace(Transform root, MonsterKind kind, Monster m, bool reopen, int index, int count)
        {
            var info = Bestiary.Get(kind);
            var d = Get(kind);
            var sb = new StringBuilder();
            var gold = new Color(1f, 0.82f, 0.35f);
            string head = reopen ? $"БЕСТИАРИЙ ({index}/{count}): {info.Name}" : $"НОВЫЙ МОНСТР: {info.Name}";
            Tx(root, new Vector3(0, 0.47f, 0), head, 0.065f, gold, TextAnchor.UpperCenter);
            sb.AppendLine(head);
            // портрет и имя
            var pc = new Vector3(-0.56f, 0.17f, 0);
            Q(root, pc + new Vector3(0, 0, 0.006f), new Vector2(0.44f, 0.44f), gold * 0.7f, 2);
            Q(root, pc + new Vector3(0, 0, 0.005f), new Vector2(0.42f, 0.42f), new Color(0.2f, 0.17f, 0.15f), 3);
            bool pic = Portrait(root, kind, m, pc + new Vector3(0, 0, -0.07f), 0.4f);
            sb.AppendLine("Картинка: " + (pic ? "портрет монстра" : "силуэт"));
            Tx(root, new Vector3(-0.56f, -0.085f, 0), info.Name, 0.055f, Color.white);
            string dz = info.Danger >= 3 ? "высокая" : info.Danger == 2 ? "средняя" : "низкая";
            Tx(root, new Vector3(-0.56f, -0.14f, 0), $"Этаж {info.Floor} · опасность {dz}", 0.03f, new Color(1, 1, 1, 0.75f));
            // кто это
            float L = -0.27f, W = 1.03f;
            var who = Tx(root, new Vector3(L, 0.4f, 0), d.Who, 0.034f, Color.white, TextAnchor.UpperLeft);
            Prim.FitWidth(who, W);
            sb.AppendLine("КТО ЭТО: " + d.Who);
            // как убить
            Tx(root, new Vector3(L, 0.32f, 0), "КАК УБИТЬ:", 0.042f, new Color(0.45f, 1f, 0.5f), TextAnchor.UpperLeft);
            sb.Append("КАК УБИТЬ: ");
            int n = d.Steps.Length; float tw = 0.205f, gap = (W - n * tw) / Mathf.Max(1, n - 1);
            for (int i = 0; i < n; i++)
            {
                var s = d.Steps[i];
                float cx = L + tw * 0.5f + i * (tw + gap);
                var tc = new Vector3(cx, 0.17f, 0);
                Q(root, tc + new Vector3(0, 0, 0.004f), new Vector2(tw, 0.17f), new Color(0.24f, 0.19f, 0.13f), 3);
                Tx(root, tc + new Vector3(-tw * 0.5f + 0.02f, 0.065f, 0), (i + 1).ToString(), 0.04f, gold);
                Icon(root, tc + new Vector3(0, 0.005f, -0.004f), s);
                var cap = Tx(root, new Vector3(cx, 0.075f, 0), Wrap(s.Text, 16), 0.027f, new Color(0.9f, 1f, 0.9f), TextAnchor.UpperCenter);
                Prim.FitWidth(cap, tw + gap * 0.8f);
                if (i < n - 1) Chevron(root, new Vector3(cx + tw * 0.5f + gap * 0.5f, 0.17f, 0), gold);
                sb.Append($"{i + 1}. {s.Text}{(i < n - 1 ? " → " : "")}");
            }
            sb.AppendLine();
            // нельзя и окно
            Tx(root, new Vector3(L, -0.035f, 0), "НЕЛЬЗЯ: " + DontGreen, 0.034f, new Color(1f, 0.5f, 0.45f), TextAnchor.UpperLeft);
            var d2 = Tx(root, new Vector3(L + 0.155f, -0.08f, 0), d.Dont2, 0.029f, new Color(1f, 0.8f, 0.78f), TextAnchor.UpperLeft);
            Prim.FitWidth(d2, W - 0.155f);
            sb.AppendLine("НЕЛЬЗЯ: " + DontGreen + "; " + d.Dont2);
            string win = WindowText(kind);
            Tx(root, new Vector3(L, -0.135f, 0), "ОКНО УДАРА: " + win, 0.044f, new Color(1f, 0.85f, 0.25f), TextAnchor.UpperLeft);
            sb.AppendLine("ОКНО УДАРА: " + win);
            // светофор
            Q(root, new Vector3(0, -0.245f, 0.007f), new Vector2(1.5f, 0.004f), new Color(0.5f, 0.4f, 0.3f));
            Lamp(root, new Vector3(-0.72f, -0.3f, -0.004f), 0.05f, new Color(0.15f, 0.9f, 0.3f));
            var g = Tx(root, new Vector3(-0.68f, -0.3f, 0), "ЗЕЛЁНЫЙ свет — бить НЕЛЬЗЯ, лут испортится", 0.029f, new Color(0.55f, 1f, 0.6f), TextAnchor.MiddleLeft);
            Prim.FitWidth(g, 0.66f);
            Lamp(root, new Vector3(0.03f, -0.3f, -0.004f), 0.05f, new Color(1f, 0.85f, 0.1f));
            var y = Tx(root, new Vector3(0.07f, -0.3f, 0), "ЖЁЛТЫЙ — можно бить: " + d.YellowWhen, 0.029f, new Color(1f, 0.9f, 0.45f), TextAnchor.MiddleLeft);
            Prim.FitWidth(y, 0.7f);
            sb.AppendLine("ЗЕЛЁНЫЙ — бить НЕЛЬЗЯ, лут испортится; ЖЁЛТЫЙ — можно бить: " + d.YellowWhen);
            // кнопка
            Q(root, new Vector3(0, -0.4f, 0.004f), new Vector2(0.5f, 0.085f), new Color(0.2f, 0.55f, 0.25f), 3);
            Tx(root, new Vector3(0, -0.4f, 0), "Понятно (Enter)", 0.048f, Color.white);
            string foot = (reopen ? "← → листать · " : "") + "Tab — подробнее · B — бестиарий (перечитать) · VR: A/X — понятно, Y — бестиарий";
            Tx(root, new Vector3(0, -0.465f, 0), foot, 0.024f, new Color(1, 1, 1, 0.6f));
            sb.AppendLine("Понятно (Enter)");
            return sb.ToString();
        }

        static string Wrap(string s, int width)
        {
            var words = s.Split(' '); var sb = new StringBuilder(); int line = 0;
            foreach (var w in words)
            {
                if (line > 0 && line + w.Length > width) { sb.Append('\n'); line = 0; }
                sb.Append(w).Append(' '); line += w.Length + 1;
            }
            return sb.ToString().TrimEnd();
        }
    }
}
