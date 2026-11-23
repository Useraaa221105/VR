using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace Podz
{
    /// <summary>Подробная карточка монстра: инструмент (цвет, клавиша), КАК МОЖНО по шагам, КАК НЕЛЬЗЯ, чем опасен, как увернуться, окно, ингредиент.</summary>
    public struct MonsterInfo
    {
        public string Name, Floor, FloorName;
        public int Danger;              // 1..3 (дракон — 3, особо опасен)
        public ToolType Tool;
        public bool NeedsHand;          // нужна ещё и рука (E / хват в VR)
        public ToolType? Extra;         // второй инструмент (дракон — щит)
        public string[] Steps, Dont;
        public string Threat, Dodge, Window;
        public ItemId Loot;
    }

    public static class Bestiary
    {
        static readonly HashSet<MonsterKind> _seen = new HashSet<MonsterKind>();
        static string C => Tools.CrouchKey;

        public static readonly Dictionary<MonsterKind, MonsterInfo> Info = new Dictionary<MonsterKind, MonsterInfo>
        {
            { MonsterKind.Slime, new MonsterInfo { Name = "Слайм", Floor = "−1", FloorName = "Пещеры слизи", Danger = 1, Tool = ToolType.Spatula, Loot = ItemId.SlimeJelly,
                Steps = new[] {
                    "Возьми лопатку и подойди. Слайм сожмётся и покраснеет — сейчас прыгнет.",
                    "Ударь его лопаткой (ЛКМ или ПКМ) — или шагни вбок (A/D), чтобы он промахнулся. Слайм оглушён: 3 с светится золотым.",
                    "Пока светится золотым — ЛЮБОЙ удар лопаткой (ЛКМ или ПКМ) = добыт!" },
                Dont = new[] {
                    "Бить сачком, тесаком, молотком — от него ничего не останется.",
                    "Бить в прыжке или пока он на лице — не попадёшь." },
                Threat = "Прыгает на лицо и душит: −2 HP в секунду (слабый).",
                Dodge = "Увидел красное сжатие — шаг вбок. Прилип к лицу — сорви рукой (E 3 раза).",
                Window = "3 с, пока оглушён (после удара лопаткой или промаха)" } },
            { MonsterKind.Mushroom, new MonsterInfo { Name = "Ходячий гриб", Floor = "−1", FloorName = "Пещеры слизи", Danger = 1, Tool = ToolType.Net, Loot = ItemId.MushroomLegs,
                Steps = new[] {
                    "Присядь ({C}) — так гриб заметит тебя только с 2 м.",
                    "Подкрадись или гони его в угол: побегав, он устаёт.",
                    "Гриб «Устал» (3 с; таймер — в добровольной помощи только в добровольной помощи) или «Загнан в угол» — накрой сачком СВЕРХУ (ЛКМ)." },
                Dont = new[] {
                    "Лопатка, тесак, молоток — гриб растечётся жижей.",
                    "Махать сачком, пока он бодрый, — выскользнет.",
                    "Стоять вплотную дольше 3 с — надышишься спорами." },
                Threat = "Облако спор вблизи: −3 HP и жёлтая пелена.",
                Dodge = "Не стой рядом: подошёл — сразу лови или отойди.",
                Window = "3 с усталости после бега (таймер — в добровольной помощи) или пока загнан в угол" } },
            { MonsterKind.Mandrake, new MonsterInfo { Name = "Мандрагора", Floor = "−2", FloorName = "Заросшие руины", Danger = 2, Tool = ToolType.Cleaver, NeedsHand = true, Loot = ItemId.MandrakeRoot,
                Steps = new[] {
                    "Подходи тихо, присев ({C}): громкие шаги её будят.",
                    "Схвати листья рукой (E). Теперь у тебя 3 секунды (таймер только в добровольной помощи).",
                    "Срежь листья тесаком ГОРИЗОНТАЛЬНО, лезвием у земли (ПКМ).",
                    "Сразу вытяни корень рукой (E)." },
                Dont = new[] {
                    "Выдёргивать, не срезав листья, — крик (−15 HP), корень испорчен.",
                    "Рубить её прямо в земле — испорчена.",
                    "Лопатка, сачок, молоток — одни щепки.",
                    "Ждать после среза дольше 10 с — корень завянет." },
                Threat = "Крик бьёт в радиусе 4 м: −8 HP (выдернул без среза — −15 HP).",
                Dodge = "Проснулась и шевелит листьями — отойди дальше 4 м и подожди.",
                Window = "3 с после захвата листьев, 10 с на корень после среза" } },
            { MonsterKind.Basilisk, new MonsterInfo { Name = "Василиск", Floor = "−2", FloorName = "Заросшие руины", Danger = 3, Tool = ToolType.Cleaver, NeedsHand = true, Loot = ItemId.BasiliskMeat,
                Steps = new[] {
                    "Не смотри ему в глаза — держи взгляд на лапах.",
                    "Подпусти близко: голова поднимется и покраснеет — это клевок. Шагни вбок (A/D).",
                    "Клюв застрял в полу на 5 с (таймер только в добровольной помощи) — ударь тесаком по ШЕЕ 2 раза (ЛКМ). После 1-го удара окно не пропадает.",
                    "Он оглушён на 5 с — схвати за голову рукой (E)." },
                Dont = new[] {
                    "Смотреть в глаза 2 с — окаменеешь на 3 с.",
                    "Заходить сзади — кусает хвост-змея (−8 HP).",
                    "Лопатка, сачок, молоток — мясо каменеет и рассыпается." },
                Threat = "Клевок −12 HP, окаменяющий взгляд, хвост-змея сзади −8 HP.",
                Dodge = "Красная поднятая голова — шаг вбок, не назад. Держись спереди-сбоку.",
                Window = "5 с, пока клюв застрял в полу (+ оглушение 5 с на захват)" } },
            { MonsterKind.Armor, new MonsterInfo { Name = "Живые доспехи", Floor = "−3", FloorName = "Подземный город", Danger = 2, Tool = ToolType.Hammer, NeedsHand = true, Loot = ItemId.ShellfishMeat,
                Steps = new[] {
                    "Зайди ему за спину — или дождись удара мечом и отступи.",
                    "Меч застрял в полу (3 с; таймер — в добровольной помощи) или доспех к тебе спиной — бей молотком СИЛЬНО (ЛКМ), 3 раза.",
                    "Доспех упал на колено на 3 с — сорви шлем рукой (E). Внутри — моллюски." },
                Dont = new[] {
                    "Бить спереди и по щиту — отскок.",
                    "Лопатка, сачок, тесак ломаются о броню на 10 с.",
                    "Пустой (ложный) доспех без слизи в щелях шлема — лута нет." },
                Threat = "Меч перед собой: −12 HP.",
                Dodge = "Меч поднят и светится красным — 2 шага назад или вбок.",
                Window = "3 с, пока меч застрял; спина открыта всегда; 3 с на колене" } },
            { MonsterKind.Mimic, new MonsterInfo { Name = "Мимик", Floor = "−3", FloorName = "Подземный город", Danger = 2, Tool = ToolType.Hammer, NeedsHand = true, Loot = ItemId.MimicMeat,
                Steps = new[] {
                    "На сундуке нет пыли? Подозрительно. Простучи его тихо (F — тычок): звонко = мимик.",
                    "Бей молотком по крышке СИЛЬНО 3 раза (ЛКМ).",
                    "Панцирь треснул — 3 с (таймер — в добровольной помощи): вытяни мимика за ногу (E)." },
                Dont = new[] {
                    "Открывать подозрительный сундук рукой — засада (−18 HP).",
                    "Тесак разрубит панцирь вместе с мясом.",
                    "Лопатка и сачок — отскакивают." },
                Threat = "Засада −18 HP, клешни −15 HP вблизи спереди.",
                Dodge = "Стой сбоку от сундука, не прямо перед крышкой.",
                Window = "3 с, пока панцирь треснул" } },
            { MonsterKind.Dragon, new MonsterInfo { Name = "Красный дракон", Floor = "Логово", FloorName = "Логово дракона", Danger = 3, Tool = ToolType.Cleaver, Extra = ToolType.Shield, Loot = ItemId.DragonMeat,
                Steps = new[] {
                    "Дракон набирает воздух — подними котёл-щит ({K5}) и лови огонь.",
                    "Поймал почти весь огонь (≥ 90%) — перегрев: голова опускается на 5 с (таймер — в добровольной помощи). Пригнулся под хвостом — на 4 с.",
                    "Руби тесаком ({K3}, ЛКМ) обратную чешую на горле — 3 удара. Так 3 фазы." },
                Dont = new[] {
                    "Стоять в огне без щита или колонны.",
                    "Стоять в красных кругах на полу.",
                    $"Стоять в полный рост, когда метёт хвост." },
                Threat = "Лапа и хвост −25 HP (максимум), огненные круги −20, огонь без щита −10 в секунду.",
                Dodge = "Хвост — присядь ({C}); круги — выйди; огонь — щит или колонна.",
                Window = "5 с перегрева / 4 с после удара хвостом" } },
        };

        public static MonsterInfo Get(MonsterKind k) => Info[k];
        public static string Fill(string s) => s.Replace("{C}", C).Replace("{K5}", Tools.DungeonKey(ToolType.Shield)).Replace("{K3}", Tools.DungeonKey(ToolType.Cleaver));
        public static bool Seen(MonsterKind k) => _seen.Contains(k);
        public static void MarkSeen(MonsterKind k) => _seen.Add(k);
        public static IEnumerable<MonsterKind> AllSeen => _seen;
        /// <summary>Увиденные монстры по порядку этажей (для листания бестиария).</summary>
        public static List<MonsterKind> SeenOrdered()
        {
            var l = new List<MonsterKind>();
            foreach (MonsterKind k in System.Enum.GetValues(typeof(MonsterKind))) if (_seen.Contains(k)) l.Add(k);
            return l;
        }
        public static void Load(List<string> names)
        {
            _seen.Clear();
            foreach (var n in names) if (System.Enum.TryParse(n, out MonsterKind k)) _seen.Add(k);
        }
        public static List<string> ToList() { var l = new List<string>(); foreach (var k in _seen) l.Add(k.ToString()); return l; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _seen.Clear();

        /// <summary>Кнопка «Бестиарий»: ПК — B, VR — Y (левый контроллер). Вызывается каждый кадр из Guide.</summary>
        static Edge _y;
        public static void PollButton()
        {
            _y.Update(XRIn.Secondary(XRNode.LeftHand));
            if (DIn.Down(Key.B) || _y.Down) ToggleBook();
        }

        /// <summary>Открыть/закрыть бестиарий (карточка ближайшего знакомого монстра или последнего встреченного).</summary>
        public static void ToggleBook()
        {
            if (BestiaryCard.Open) { BestiaryCard.ForceClose(); return; }
            if (DungeonSettings.Open) DungeonSettings.ClosePanel();
            BackpackView.ClosePanel();
            foreach (MonsterKind kind in System.Enum.GetValues(typeof(MonsterKind))) MarkSeen(kind);
            var list = SeenOrdered();
            if (list.Count == 0) { Bestiary.MarkSeen(MonsterKind.Slime); list = SeenOrdered(); }
            var near = Guide.NearestMonster(12f);
            var k = near != null && _seen.Contains(near.Kind) ? near.Kind : list[list.Count - 1];
            BestiaryCard.ShowKind(k, null, true);
        }
    }

    /// <summary>
    /// Карточка монстра. Открывается только вручную (время подземелья стоит).
    /// Открыть снова: B (ПК) / Y (VR). Листать: ← → (ПК) / стик (VR). Закрыть: Enter/Пробел/B, A/X; сама — через 25 с.
    /// </summary>
    public class BestiaryCard : MonoBehaviour
    {
        static BestiaryCard _i;
        Monster _target;
        float _openedAt;
        bool _reopen;
        MonsterKind _kind;
        Edge _a, _x, _sl, _sr;

        public static bool Open => _i != null;
        public static int Shown, Reopened;
        public static MonsterKind LastKind;
        public static string LastText = "";
        /// <summary>Текст лицевой страницы (картинки, шаги, светофор) — для автотеста.</summary>
        public static string LastFace = "";
        /// <summary>Сколько раз нажали «Понятно» (Enter/Пробел/A/X).</summary>
        public static int Understood;
        public static bool DetailsOpen => _i != null && _i._details != null && _i._details.activeSelf;
        GameObject _face, _details;
        const float AutoClose = 90f;
        public static bool HidesHud { get; private set; }

        public static void Show(Monster m)
        {
            if (_i != null || m == null || PlayerRig.I == null) return;
            ShowKind(m.Kind, m, false);
        }

        public static void ShowKind(MonsterKind kind, Monster m, bool reopen)
        {
            if (PlayerRig.I == null) return;
            bool wasPaused = _i != null;
            if (_i != null) _i.CloseQuiet();
            Bestiary.MarkSeen(kind);
            if (reopen) Reopened++; else Shown++;
            LastKind = kind;
            var cam = PlayerRig.I.Cam.transform;
            var template = SavedDungeonAssets.Active?.BestiaryCanvas;
            var go = template != null ? Instantiate(template) : BuildCanvas();
            // карточка ровно перед глазами (даже если смотришь вниз на слайма) — целиком в кадре
            var fwd = cam.forward;
            if (Mathf.Abs(fwd.y) > 0.6f) { fwd.y = Mathf.Sign(fwd.y) * 0.6f; }
            fwd.Normalize();
            // сдвиг вправо, чтобы карточка не налезала на панель HP слева (в координатах HUD на расстоянии HudPanel.Z)
            float halfHud = 0.81f * 0.88f * HudPanel.Z / 1.4f;
            float need = -HudPanel.HalfW + HudPanel.Margin + HudPanel.PanelW + 0.03f + halfHud;
            float maxO = HudPanel.HalfW - 0.01f - halfHud;
            float o = Mathf.Clamp(need, 0f, Mathf.Max(0f, maxO));
            HidesHud = need > maxO + 0.001f; // экран слишком узкий — на время карточки панель HP прячется
            var right = Vector3.Cross(Vector3.up, fwd).normalized;
            go.transform.position = cam.position + fwd * 1.4f + Vector3.up * 0.1f + right * (o * 1.4f / HudPanel.Z);
            go.transform.localScale = Vector3.one * 0.88f;
            go.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            _i = go.AddComponent<BestiaryCard>();
            _i._target = m;
            _i._kind = kind;
            _i._reopen = reopen;
            _i._openedAt = Time.unscaledTime;
            // пункт 8: лицевая страница с картинками (портрет, шаги со значками, светофор, «Понятно (Enter)») + подробная (Tab)
            var face = go.transform.Find("Face");
            var details = go.transform.Find("Details");
            var info0 = Bestiary.Get(kind);
            var dc = info0.Danger >= 3 ? new Color(1f, 0.3f, 0.2f) : info0.Danger == 2 ? new Color(1f, 0.7f, 0.2f) : new Color(0.5f, 0.95f, 0.45f);
            Prim.SetColor(face.Find("Frame").GetComponent<Renderer>(), dc * 0.8f);
            Prim.SetColor(details.Find("Frame").GetComponent<Renderer>(), dc * 0.8f);
            var seen = Bestiary.SeenOrdered();
            string faceText = MonsterCard.BuildFace(face, kind, m, reopen, seen.IndexOf(kind) + 1, seen.Count);
            Build(details, kind, reopen);
            LastFace = faceText;
            LastText = faceText + "\n" + LastText;
            Prim.OnTop(go.transform); // пункт 6: карточка поверх стен, монстров и инструмента в руке
            details.gameObject.SetActive(false);
            _i._face = face.gameObject; _i._details = details.gameObject;
            if (!wasPaused) Sfx.Play(SfxId.Card);
            DTime.Paused = true;
            if (m != null) m.Highlight(true);
            GameEvents.MonsterDied += _i.OnMonsterDied;
        }

        public static GameObject BuildCanvas()
        {
            var root = SavedUi.CreateRoot("BestiaryCard");
            foreach (string name in new[] { "Face", "Details" })
            {
                var page = new GameObject(name).transform;
                page.SetParent(root.transform, false);
                Prim.Cube("Frame", page, new Vector3(0, 0, 0.03f), new Vector3(1.62f, 1.08f, 0.02f), Color.white, false);
                Prim.Cube("Panel", page, new Vector3(0, 0, 0.018f), new Vector3(1.58f, 1.04f, 0.02f), new Color(0.11f, 0.075f, 0.05f), false);
            }
            return root;
        }

        static TextMesh T(Transform p, float x, float y, string s, float h, Color c, TextAnchor a = TextAnchor.UpperLeft)
        {
            var t = Prim.Text(p, new Vector3(x, y, 0), s, h, c, a);
            t.GetComponent<MeshRenderer>().sortingOrder = 5;
            return t;
        }

        static void Build(Transform root, MonsterKind kind, bool reopen)
        {
            var info = Bestiary.Get(kind);
            var dangerCol = info.Danger >= 3 ? new Color(1f, 0.3f, 0.2f) : info.Danger == 2 ? new Color(1f, 0.7f, 0.2f) : new Color(0.5f, 0.95f, 0.45f);
            float L = -0.75f, R = 0.04f;
            var sb = new System.Text.StringBuilder();
            // заголовок
            var list = Bestiary.SeenOrdered();
            string head = reopen ? $"БЕСТИАРИЙ ({list.IndexOf(kind) + 1}/{list.Count}): {info.Name}" : $"НОВЫЙ МОНСТР: {info.Name}";
            T(root, 0, 0.5f, head, 0.07f, new Color(1f, 0.82f, 0.35f), TextAnchor.UpperCenter);
            sb.AppendLine(head);
            // этаж и опасность (квадратики вместо звёзд — встроенный шрифт не рисует ★)
            string floor = $"Этаж {info.Floor} «{info.FloorName}»   ·   Опасность:";
            T(root, L, 0.41f, floor, 0.036f, Color.white);
            for (int i = 0; i < 3; i++)
            {
                Swatch(root, new Vector3(0.02f + i * 0.05f, 0.392f, 0), 0.036f, i < info.Danger ? dangerCol : new Color(0.32f, 0.28f, 0.25f));
            }
            string dz = info.Danger >= 3 ? "высокая" : info.Danger == 2 ? "средняя" : "низкая";
            T(root, 0.17f, 0.41f, dz, 0.036f, dangerCol);
            sb.AppendLine(floor + " " + dz);
            // инструмент: цветная плашка + клавиша
            float ty = 0.335f;
            ToolRowMaxRight = -1f;
            // пункт 6: строка «Инструмент…» не наезжает: при двух инструментах каждая в своей половине, длинная уменьшается
            bool two = info.Extra.HasValue;
            ToolRow(root, L, ty, info.Tool, info.NeedsHand, sb, two ? (R + 0.06f) - (L + 0.065f) - 0.03f : 0.75f - (L + 0.065f));
            if (two) ToolRow(root, R + 0.06f, ty, info.Extra.Value, false, sb, 0.75f - (R + 0.06f + 0.065f));
            ToolRowLimit = two ? R + 0.06f : 0.8f;
            // две колонки: КАК МОЖНО / КАК НЕЛЬЗЯ
            float cy = 0.245f;
            T(root, L, cy, "КАК МОЖНО — по шагам:", 0.04f, new Color(0.45f, 1f, 0.5f));
            T(root, R, cy, "КАК НЕЛЬЗЯ:", 0.04f, new Color(1f, 0.45f, 0.4f));
            var steps = new System.Text.StringBuilder();
            for (int i = 0; i < info.Steps.Length; i++) steps.Append($"{i + 1}. ").Append(Wrap(Bestiary.Fill(info.Steps[i]), 46, "    ")).Append('\n');
            var dont = new System.Text.StringBuilder();
            foreach (var d in info.Dont) dont.Append("— ").Append(Wrap(Bestiary.Fill(d), 46, "    ")).Append('\n');
            var tSteps = T(root, L, cy - 0.06f, steps.ToString().TrimEnd(), 0.031f, new Color(0.85f, 1f, 0.85f));
            var tDont = T(root, R, cy - 0.06f, dont.ToString().TrimEnd(), 0.031f, new Color(1f, 0.85f, 0.82f));
            tSteps.lineSpacing = 1.05f; tDont.lineSpacing = 1.05f;
            Prim.FitWidth(tSteps, R - L - 0.03f); Prim.FitWidth(tDont, 0.75f - R);
            sb.AppendLine("КАК МОЖНО:\n" + steps).AppendLine("КАК НЕЛЬЗЯ:\n" + dont);
            // опасность и уворот
            float dy = -0.2f;
            Prim.Cube("Sep", root, new Vector3(0, dy + 0.025f, 0.008f), new Vector3(1.5f, 0.004f, 0.01f), new Color(0.5f, 0.4f, 0.3f), false);
            T(root, L, dy, "ЧЕМ ОПАСЕН:", 0.034f, new Color(1f, 0.6f, 0.3f));
            Prim.FitWidth(T(root, L, dy - 0.045f, Wrap(Bestiary.Fill(info.Threat), 48, ""), 0.03f, Color.white), R - L - 0.03f);
            T(root, R, dy, "КАК УВЕРНУТЬСЯ:", 0.034f, new Color(0.5f, 0.85f, 1f));
            Prim.FitWidth(T(root, R, dy - 0.045f, Wrap(Bestiary.Fill(info.Dodge), 48, ""), 0.03f, Color.white), 0.75f - R);
            sb.AppendLine("ЧЕМ ОПАСЕН: " + info.Threat).AppendLine("КАК УВЕРНУТЬСЯ: " + Bestiary.Fill(info.Dodge));
            // окно добычи и ингредиент
            float wy = -0.345f;
            Prim.FitWidth(T(root, L, wy, "ОКНО ДОБЫЧИ: " + info.Window, 0.032f, new Color(1f, 0.85f, 0.3f)), 1.5f);
            var item = Items.Get(info.Loot);
            Swatch(root, new Vector3(L + 0.015f, wy - 0.068f, 0), 0.032f, item.Color);
            string ing = $"ИНГРЕДИЕНТ: {item.Name} → рюкзак → ледник. На кухне: {item.PrepText}";
            Prim.FitWidth(T(root, L + 0.045f, wy - 0.05f, ing, 0.032f, new Color(0.7f, 0.95f, 1f)), 0.75f - (L + 0.045f));
            sb.AppendLine("ОКНО: " + info.Window).AppendLine(ing);
            string foot = (reopen ? "← → листать   ·   " : "") + "Закрыть: Enter / Пробел (VR: A/X)   ·   Открыть снова: B — бестиарий (VR: Y)";
            T(root, 0, -0.47f, foot, 0.028f, new Color(1, 1, 1, 0.65f), TextAnchor.UpperCenter);
            LastText = sb.ToString();
        }

        /// <summary>Автотест: правый край первой строки «Инструмент…» и где начинается вторая колонка.</summary>
        public static float ToolRowMaxRight, ToolRowLimit;
        static void Swatch(Transform root, Vector3 p, float size, Color c)
        {
            var q = Prim.Quad("Swatch", root, p + new Vector3(0, 0, 0.004f), new Vector2(size, size), c);
            q.sortingOrder = 4;
        }

        static void ToolRow(Transform root, float x, float y, ToolType t, bool hand, System.Text.StringBuilder sb, float maxW)
        {
            var col = Tools.ColorOf(t);
            Swatch(root, new Vector3(x + 0.025f, y - 0.022f, 0), 0.055f, col);
            // цифра клавиши на плашке
            var k = T(root, x + 0.025f, y - 0.022f, Tools.DungeonKey(t), 0.04f, Color.black, TextAnchor.MiddleCenter);
            k.transform.localPosition += new Vector3(0, 0, -0.002f);
            k.fontStyle = FontStyle.Bold;
            string s = $"ИНСТРУМЕНТ: {Tools.Name(t)} ({Tools.ColorName(t)}) — клавиша {Tools.DungeonKey(t)}{(hand ? " + рука (E)" : "")}";
            var main = T(root, x + 0.065f, y, s, 0.034f, col);
            Prim.FitWidth(main, maxW);
            float minSize = 0.034f * 10f / 64f * 0.8f;
            if (main.characterSize < minSize) { main.characterSize = minSize; main.text = Tools.Name(t) + " — клавиша " + Tools.DungeonKey(t) + (hand ? " + E" : ""); Prim.FitWidth(main, maxW); }
            if (ToolRowMaxRight < -0.5f) ToolRowMaxRight = x + 0.065f + HudPanel.TextWidth(main);
            var vr = T(root, x + 0.065f, y - 0.04f, $"в VR: {Tools.VrSlot(t)}", 0.026f, new Color(1, 1, 1, 0.6f));
            Prim.FitWidth(vr, maxW);
            sb.AppendLine(s + $" · VR: {Tools.VrSlot(t)}");
        }

        static string Wrap(string s, int width, string indent)
        {
            var words = s.Split(' ');
            var sb = new System.Text.StringBuilder();
            int line = 0;
            foreach (var w in words)
            {
                if (line > 0 && line + w.Length > width) { sb.Append('\n').Append(indent); line = indent.Length; }
                sb.Append(w).Append(' ');
                line += w.Length + 1;
            }
            return sb.ToString().TrimEnd();
        }

        void OnMonsterDied(Monster m, DeathReason r) { if (m == _target) _target = null; } // не держим ссылку на удалённого

        void Update()
        {
            _a.Update(XRIn.Primary(XRNode.RightHand));
            _x.Update(XRIn.Primary(XRNode.LeftHand));
            var st = XRIn.Stick(XRNode.RightHand) + XRIn.Stick(XRNode.LeftHand);
            _sl.Update(st.x < -0.7f); _sr.Update(st.x > 0.7f);
            if (Time.unscaledTime - _openedAt < 0.3f) return;
            if (DIn.Down(Key.RightArrow) || DIn.Down(Key.LeftArrow) || _sl.Down || _sr.Down)
            {
                var list = Bestiary.SeenOrdered();
                if (list.Count > 1)
                {
                    int dir = DIn.Down(Key.LeftArrow) || _sl.Down ? -1 : 1;
                    int i = (list.IndexOf(_kind) + dir + list.Count) % list.Count;
                    ShowKind(list[i], null, true);
                    return;
                }
            }
            if (DIn.Down(Key.Tab) && _face != null && _details != null) { bool det = !_details.activeSelf; _details.SetActive(det); _face.SetActive(!det); return; }
            bool ok = _a.Down || _x.Down || DIn.Down(Key.Enter) || DIn.Down(Key.NumpadEnter) || DIn.Down(Key.Space);
            if (ok) Understood++;
            if (ok || Time.unscaledTime - _openedAt > AutoClose) Close();
        }

        public static void ForceClose() { if (_i != null) _i.Close(); }

        /// <summary>Тихо закрыть перед открытием другой карточки (пауза не снимается).</summary>
        void CloseQuiet()
        {
            GameEvents.MonsterDied -= OnMonsterDied;
            if (_target != null) _target.Highlight(false);
            _target = null;
            if (_i == this) _i = null;
            Destroy(gameObject);
        }

        void Close()
        {
            CloseQuiet();
            DTime.Paused = false;
        }

        void OnDestroy()
        {
            GameEvents.MonsterDied -= OnMonsterDied;
            if (_i == this) { _i = null; DTime.Paused = false; }
        }
    }

    /// <summary>Короткоживущие эффекты: сами уничтожаются (Destroy с таймером), ничего не копится.</summary>
    public sealed class VanishEvidence { public int RenderersCreated; public bool RenderedByPlayerCamera; }
    public sealed class VanishRenderProbe : MonoBehaviour {
        [System.NonSerialized] public VanishEvidence Evidence;
        void OnWillRenderObject() {
            if(Evidence!=null && PlayerRig.I!=null && Camera.current==PlayerRig.I.Cam)Evidence.RenderedByPlayerCamera=true;
        }
    }
    public static class FX
    {
        /// <summary>Монстр исчезает: облачко из сфер разлетается и тает (объекты самоуничтожаются).</summary>
        public static VanishEvidence Vanish(Vector3 pos, float size, Color c)
        {
            var evidence = new VanishEvidence();
            var root = new GameObject("VanishFx");
            root.transform.position = pos;
            for (int i = 0; i < 10; i++)
            {
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) * 0.8f + 0.2f;
                var s = Prim.Shape(PrimitiveType.Sphere, "Puff", root.transform, d * size * 0.15f, Vector3.one * size * Random.Range(0.25f, 0.45f), Color.Lerp(c, Color.white, 0.6f), false);
                Prim.SetEmission(s.GetComponent<Renderer>(), Color.white * 0.4f);
                s.AddComponent<PuffFx>().Dir = d * Random.Range(0.8f, 1.6f);
                s.AddComponent<VanishRenderProbe>().Evidence = evidence;
                if(s.GetComponent<Renderer>().enabled) evidence.RenderersCreated++;
            }
            Object.Destroy(root, 1.3f);
            return evidence;
        }

        /// <summary>Зелёные искры вверх при удачной добыче (сами исчезают).</summary>
        public static void Sparkle(Vector3 pos, Color c)
        {
            var root = new GameObject("SparkleFx");
            root.transform.position = pos;
            for (int i = 0; i < 14; i++)
            {
                var d = Random.onUnitSphere; d.y = Mathf.Abs(d.y) + 0.6f;
                var s = Prim.Shape(PrimitiveType.Cube, "Spark", root.transform, Vector3.zero, Vector3.one * Random.Range(0.04f, 0.07f), c, false);
                Prim.SetEmission(s.GetComponent<Renderer>(), c * 2f);
                s.AddComponent<PuffFx>().Dir = d.normalized * Random.Range(1.2f, 2.2f);
            }
            Object.Destroy(root, 1.0f);
        }

        public static void Splat(Vector3 pos, Color c)
        {
            var go = Prim.Cube("Splat", null, pos + Vector3.up * 0.01f, new Vector3(0.6f, 0.02f, 0.6f), c * 0.7f, false);
            Object.Destroy(go, 1.5f);
        }

        public static void LootFly(Vector3 pos, Color c)
        {
            var go = Prim.Cube("Loot", null, pos, Vector3.one * 0.12f, c, false);
            go.AddComponent<LootFly>();
            Object.Destroy(go, 1.2f);
        }

        public static GameObject Disc(Transform parent, Vector3 pos, float radius, Color c)
        {
            var go = Prim.Shape(PrimitiveType.Cylinder, "Disc", parent, pos, new Vector3(radius * 2, 0.01f, radius * 2), c, false);
            return go;
        }
    }

    public class PuffFx : MonoBehaviour
    {
        public Vector3 Dir;
        void Update()
        {
            transform.position += Dir * Time.deltaTime;
            transform.localScale *= 1f - Mathf.Clamp01(Time.deltaTime * 1.6f);
        }
    }

    public class LootFly : MonoBehaviour
    {
        void Update()
        {
            if (PlayerRig.I == null) return;
            var target = PlayerRig.I.Body.position + Vector3.up * 1.1f - PlayerRig.I.Body.forward * 0.2f;
            transform.position = Vector3.MoveTowards(transform.position, target, 6f * Time.deltaTime);
            transform.localScale *= 0.985f;
        }
    }

    public static class Inventory
    {
        public static readonly ItemStore Backpack = new ItemStore(100, 100);
        public static readonly ItemStore Fridge = new ItemStore(int.MaxValue, int.MaxValue);

        public static List<ItemCount> TransferToFridge()
        {
            var moved = new List<ItemCount>();
            foreach(var kv in new List<KeyValuePair<ItemId,int>>(Backpack.All))
            {
                int accepted=Fridge.Add(kv.Key,kv.Value);
                if(accepted>0) { Backpack.Take(kv.Key,accepted);moved.Add(new ItemCount {id=kv.Key,n=accepted}); }
            }
            return moved;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Backpack.Clear(); Fridge.Clear(); }
    }
}
