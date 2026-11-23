using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Podz
{
    /// <summary>
    /// Помощник игрока (ПК): строка «ЗАДАЧА», панель справа (комната, HP, инструмент, рюкзак), стрелка к цели,
    /// карточка рецепта с галочками шагов у котла, подсказка после 5 с бездействия, «тренер» (не тот инструмент / СЕЙЧАС!),
    /// обучение первого дня (9 шагов) и читы для показа (F7 набор демо, F8 бессмертие, F10 следующий этаж, F11 пропустить обучение).
    /// </summary>
    public class Guide : MonoBehaviour
    {
        public static Guide I;
        public static bool IdleEnabled = true;
        public static float IdleDelay = 5f;
        public int IdleHintsShown { get; private set; }
        public string LastIdleHint { get; private set; }
        public int CoachHintsShown { get; private set; }
        public string LastCoach { get; private set; }
        public string Objective { get; private set; } = "";
        public bool RecipeCardVisible => _card != null && _card.gameObject.activeSelf;
        public string RecipeCardText => _cardText != null ? _cardText.text : "";
        public static bool God;
        public readonly Tutorial Tutorial = new Tutorial();

        [SerializeField] TextMesh _objective, _cardText;
        [SerializeField] Renderer _cardBg;
        public const float ObjH = 0.045f; const float CardW = 0.7f;
        /// <summary>Строка задачи сейчас на экране (после подгонки под ширину) — для автотеста.</summary>
        public string ObjectiveShown => _objective != null ? _objective.text : "";
        public Rect ObjectiveArea { get; private set; }
        public Rect CardArea => RecipeCardVisible ? _cardRect : Rect.zero;
        Rect _cardRect;
        [SerializeField] Transform _card, _arrow;
        /// <summary>Стрелка «куда идти» к монстру в подземелье видна (пункт 10: выключается H).</summary>
        public bool ArrowShown => _arrow != null && _arrow.gameObject.activeSelf;
        [SerializeField] Renderer _objBg;
        float _idleT, _coachT;
        Vector3 _lastFeet; Quaternion _lastLook;
        readonly Dictionary<MonsterKind, float> _coachSeen = new Dictionary<MonsterKind, float>();
        string _lastWindowMonster;
        bool _hudHidden;

        PlayerRig P => PlayerRig.I;

        public static Guide Create(Transform camera)
        {
            var template = Application.isPlaying ? SavedDungeonAssets.Active?.GuideCanvas : null;
            var panel = template != null ? Instantiate(template).GetComponent<Guide>() :
                SavedUi.CreateRoot("Guide").AddComponent<Guide>();
            panel.AttachTo(camera);
            return panel;
        }

        public void AttachTo(Transform cam)
        {
            I = this;
            transform.SetParent(cam, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if (_objective == null) BuildVisual();
            else SavedDungeonAssets.RestoreText(gameObject, true);
        }

        void BuildVisual()
        {
            // ЗАДАЧА / ОБУЧЕНИЕ — сверху по центру, одной строкой, на тёмной подложке (размеры — по краям экрана, см. LayoutObjective)
            _objBg = Prim.Quad("ObjectiveBg", transform, new Vector3(0, 0.6f, HudPanel.Z + 0.01f), new Vector2(1.2f, HudPanel.TopStrip), new Color(0, 0, 0, 0.7f));
            _objBg.sortingOrder = 1; Prim.Hud(_objBg); Prim.SetOverlayColor(_objBg, new Color(0, 0, 0, 1f));
            _objective = Prim.Text(transform, new Vector3(0, 0.6f, HudPanel.Z), "", ObjH, new Color(1f, 0.9f, 0.55f), TextAnchor.MiddleCenter);
            _objective.GetComponent<MeshRenderer>().sortingOrder = 6;
            // карточка рецепта — слева, под панелью HP (низ экрана свободен, справа — инструменты)
            _card = new GameObject("RecipeCard").transform;
            _card.SetParent(transform, false);
            _card.localPosition = new Vector3(-0.9f, 0.25f, HudPanel.Z);
            _cardBg = Prim.Quad("CardBg", _card, new Vector3(CardW * 0.5f, -0.2f, 0.01f), new Vector2(CardW, 0.4f), new Color(0.1f, 0.07f, 0.04f, 0.85f));
            _cardBg.sortingOrder = 1; Prim.Hud(_cardBg); Prim.SetOverlayColor(_cardBg, new Color(0.1f, 0.07f, 0.04f, 1f));
            _cardText = Prim.Text(_card, new Vector3(0.02f, -0.015f, 0), "", 0.036f, Color.white, TextAnchor.UpperLeft);
            _cardText.GetComponent<MeshRenderer>().sortingOrder = 6;
            _card.gameObject.SetActive(false);
            // стрелка к цели (в подземелье — к ближайшему монстру / к порталу домой)
            _arrow = new GameObject("GuideArrow").transform;
            _arrow.SetParent(transform, false);
            _arrow.localPosition = new Vector3(0, 0.36f, 1.2f);
            var shaft = Prim.Cube("Shaft", _arrow, new Vector3(0, 0, -0.02f), new Vector3(0.012f, 0.004f, 0.05f), new Color(1f, 0.85f, 0.3f), false);
            var tip = Prim.Cube("Tip", _arrow, new Vector3(0, 0, 0.012f), new Vector3(0.03f, 0.004f, 0.03f), new Color(1f, 0.85f, 0.3f), false);
            tip.transform.localRotation = Quaternion.Euler(0, 45, 0);
            Prim.SetEmission(shaft.GetComponent<Renderer>(), new Color(1f, 0.7f, 0.2f));
            Prim.SetEmission(tip.GetComponent<Renderer>(), new Color(1f, 0.7f, 0.2f));
            _arrow.gameObject.SetActive(false);
        }

        void Start() { Tutorial.Begin(); } // после GameFlow.Init (сохранение уже загружено)

        void OnDestroy() { if (I == this) I = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { God = false; IdleEnabled = true; IdleDelay = 5f; }

        // ================= справочники =================
        public static ToolType RequiredTool(MonsterKind k)
        {
            switch (k)
            {
                case MonsterKind.Slime: return ToolType.Spatula;
                case MonsterKind.Mushroom: return ToolType.Net;
                case MonsterKind.Armor: case MonsterKind.Mimic: return ToolType.Hammer;
                default: return ToolType.Cleaver;
            }
        }

        /// <summary>Нижняя строка карточки рецепта (пункт 7: номера кухни).</summary>
        public static string RecipeCardLine() => $"{Tools.KitchenKey(ToolType.Knife)} — нож (ЛКМ по продукту) · {Tools.KitchenKey(ToolType.Ladle)} — половник · E — положить";
        public static string ToolKey(ToolType t) => Tools.Key(t); // пункт 7: один источник номеров

        static readonly Dictionary<MonsterKind, string> HowShort = new Dictionary<MonsterKind, string>
        {
            { MonsterKind.Slime, "ударь лопаткой — оглушён (золотой) — ударь ещё раз (ЛКМ или ПКМ)" },
            { MonsterKind.Mushroom, $"присядь ({Tools.CrouchKey}), подкрадись или загони в угол — ЛКМ сачком сверху" },
            { MonsterKind.Mandrake, "E — схватить листья, ПКМ тесаком — срезать, E — вытянуть корень" },
            { MonsterKind.Basilisk, "не смотри в глаза; жди клевка, шагни вбок, ЛКМ по шее ×2, затем E" },
            { MonsterKind.Armor, "зайди со спины: ЛКМ молотком ×3, затем E — сорвать шлем" },
            { MonsterKind.Mimic, "F — простучать сундук, ЛКМ молотком ×3, E — за ногу" },
            { MonsterKind.Dragon, $"{Tools.DungeonKey(ToolType.Shield)} — щит против огня; при перегреве ЛКМ тесаком по горлу ×3 в каждой фазе; {Tools.CrouchKey} — присесть под хвостом" },
        };
        public static string How(MonsterKind k) => HowShort[k];

        public static Monster NearestMonster(float maxDist = 999f)
        {
            if (PlayerRig.I == null) return null;
            Monster best = null; float bd = maxDist;
            foreach (var m in MonsterRegistry.Alive)
            {
                if (m == null || m.Dead) continue;
                float d = (m.transform.position - PlayerRig.I.Feet).magnitude;
                if (d < bd) { bd = d; best = m; }
            }
            return best;
        }

        public static Cauldron ActiveCauldron()
        {
            if (PlayerRig.I == null) return null;
            Cauldron best = null; float bd = 3.2f;
            foreach (var c in Cauldron.All)
            {
                if (c == null || !c.gameObject.activeInHierarchy) continue;
                float d = (c.transform.position - PlayerRig.I.Feet).magnitude;
                if (d < bd) { bd = d; best = c; }
            }
            return best;
        }

        /// <summary>Рецепт для карточки: то, что варится, иначе первый, на который хватает продуктов, иначе первый доступный.</summary>
        public static Recipe CookableRecipe(Cauldron c)
        {
            if (c != null && c.Recipe != null) return c.Recipe;
            int stage = GameFlow.I != null ? GameFlow.I.TavernStage : 1;
            bool dragon = GameFlow.I != null && GameFlow.I.Save.dragonDefeated;
            var list = Recipes.ForStage(stage, dragon);
            // сначала — заказ ждущего гостя (самого нетерпеливого), если на него хватает продуктов
            var order = OrderFor(list);
            if (order != null) return order;
            foreach (var r in list) if (Recipes.HasIngredients(r)) return r;
            return list.Count > 0 ? list[0] : Recipes.All[0];
        }

        static Recipe OrderFor(List<Recipe> list)
        {
            var gm = GuestManager.I;
            if (gm == null) return null;
            Recipe best = null; float bp = 2f;
            foreach (var g in gm.Guests)
                if (g != null && g.Waiting && !g.Assigned && g.Order != null && list.Contains(g.Order) && Recipes.HasIngredients(g.Order) && g.PatienceFrac < bp)
                { bp = g.PatienceFrac; best = g.Order; }
            return best;
        }

        /// <summary>Гость, который ждёт это блюдо (для строки «Заказ: стол N»), иначе null.</summary>
        public static Guest WaitingFor(Recipe r)
        {
            var gm = GuestManager.I;
            if (gm == null || r == null) return null;
            Guest best = null;
            foreach (var g in gm.Guests)
                if (g != null && g.Waiting && !g.Assigned && g.Order == r && (best == null || g.PatienceFrac < best.PatienceFrac)) best = g;
            return best;
        }

        /// <summary>
        /// К4: текущий шаг рецепта с учётом порядка «ледник → доска → «Вода» → котёл». Пришёл заказ, котёл пуст, первый продукт
        /// можно взять (в леднике/кладовой или уже на кухне), но он ещё не подготовлен — ведём к продукту (шаг 2),
        /// а «Вода» — когда продукт готов (шаг 1), сразу перед тем как бросить его в котёл.
        /// </summary>
        public static int CurrentStep(Cauldron c, Recipe r, List<(string text, bool done)> steps)
        {
            int cur = steps.FindIndex(s => !s.done);
            if (cur != 0 || steps.Count < 2 || c == null || r == null || c.State != SoupState.Empty) return cur;
            // только когда пришёл заказ на это блюдо (в обучении — по-старому: сначала «Вода»)
            if (WaitingFor(r) == null || (I != null && I.Tutorial != null && I.Tutorial.Active)) return cur;
            var item = RecipeScreen.StepItem(r, 1);
            if (!item.HasValue) return cur;
            var id = item.Value;
            bool lying = false, ready = false;
            foreach (var i in Ingredient.All)
                if (i != null && i.Id == id && !i.Spoiled) { lying = true; if (i.Prepared) ready = true; }
            if (ready) return 0;                         // продукт готов — теперь «Вода»
            if (lying || Fridge.Count(id) > 0) return 1; // сначала за продуктом
            return 0;
        }

        /// <summary>К4: «Вода» отложена — ведём сначала к продукту.</summary>
        public static bool WaterDeferred(Cauldron c, Recipe r, List<(string text, bool done)> steps) => steps.Count > 1 && !steps[0].done && CurrentStep(c, r, steps) == 1;

        /// <summary>Шаги рецепта с отметками (для карточки и автотеста).</summary>
        public static List<(string text, bool done)> RecipeSteps(Cauldron c, Recipe r)
        {
            var steps = new List<(string, bool)>();
            bool water = c != null && c.State != SoupState.Empty;
            steps.Add(("Нажми «Вода» на котле (E)", water));
            var left = new List<ItemId>(c != null ? c.Contents : new List<ItemId>());
            bool cookingOrMore = c != null && (c.State == SoupState.Cooking || c.State == SoupState.Ready);
            foreach (var id in r.Items)
            {
                bool inPot = left.Remove(id) || cookingOrMore;
                var info = Items.Get(id);
                string prep = id == ItemId.MushroomLegs ? " (очистить плашмя15см, потом2реза)" : info.Prep == PrepKind.None ? "" : $" ({info.PrepText})";
                steps.Add(($"{Items.Name(id)}{prep} → в котёл", inPot));
            }
            steps.Add(($"Варить {r.CookTime:0} с, мешать половником ({Tools.KitchenKey(ToolType.Ladle)}) по сигналу", c != null && c.State == SoupState.Ready));
            steps.Add(("«Разлить» → миску на окно → колокольчик", Bowl.All.Count > 0 && c != null && c.State != SoupState.Cooking && c.State != SoupState.Ready));
            return steps;
        }

        // ================= кадр =================
        void Update()
        {
            if (P == null) return;
            Cheats();
            Bestiary.PollButton(); // B (VR: Y) — открыть бестиарий снова
            // пока открыта карточка монстра — HUD прячется, чтобы не налезал на текст карточки
            bool card = BestiaryCard.Open;
            if (card != _hudHidden)
            {
                _hudHidden = card;
                if (card) foreach (Transform ch in transform) ch.gameObject.SetActive(false);
                else { _objective.gameObject.SetActive(true); _objBg.gameObject.SetActive(true); }
            }
            if (card) return;
            if (God) P.Health.ResetAll();
            Tutorial.Tick();
            Objective = Tutorial.Active ? $"ОБУЧЕНИЕ {Tutorial.Step + 1}/{Tutorial.Texts.Length}: {Tutorial.Current}" : "ЗАДАЧА: " + ComputeObjective();
            if (Tools.InDungeon && !HintsToggle.On) Objective = "";
            LayoutObjective();
            _objective.gameObject.SetActive(!Tools.InDungeon || HintsToggle.On);
            _objBg.gameObject.SetActive(!Tools.InDungeon || HintsToggle.On);
            UpdateRecipeCard();
            UpdateArrow();
            Coach();
            Idle();
        }

        string _objFor = ""; float _objAspect;
        /// <summary>Строка задачи: по центру сверху, ОДНОЙ строкой. Не влезает — шрифт уменьшается до 80%, дальше обрезается «…».</summary>
        void LayoutObjective()
        {
            float aspect = P.Cam != null ? P.Cam.aspect : 1.78f;
            if (_objFor == Objective && Mathf.Abs(_objAspect - aspect) < 0.01f) return;
            _objFor = Objective; _objAspect = aspect;
            float hw = HudPanel.HalfW, hh = HudPanel.HalfH;
            float maxW = 2f * hw - 2f * HudPanel.Margin - 0.04f;
            float y = hh - HudPanel.Margin - HudPanel.TopStrip * 0.5f;
            _objective.characterSize = ObjH * 10f / 64f;
            string t = Objective.Replace('\n', ' ');
            _objective.text = t;
            float w = HudPanel.TextWidth(_objective);
            if (w > maxW) { _objective.characterSize *= Mathf.Max(0.8f, maxW / w); w = HudPanel.TextWidth(_objective); }
            while (w > maxW && t.Length > 10) { t = t.Substring(0, t.Length - 2).TrimEnd() + "…"; _objective.text = t; w = HudPanel.TextWidth(_objective); }
            _objective.transform.localPosition = new Vector3(0, y, HudPanel.Z);
            _objBg.transform.localScale = new Vector3(Mathf.Min(maxW + 0.04f, w + 0.06f), HudPanel.TopStrip, 1f);
            _objBg.transform.localPosition = new Vector3(0, y, HudPanel.Z + 0.01f);
            ObjectiveArea = new Rect(-_objBg.transform.localScale.x * 0.5f, y + HudPanel.TopStrip * 0.5f, _objBg.transform.localScale.x, HudPanel.TopStrip);
        }

        static string Wrap(string s, int width)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= width) return s;
            var sb = new StringBuilder(); int line = 0;
            foreach (var w in s.Split(' '))
            {
                if (line > 0 && line + w.Length > width) { sb.Append('\n'); line = 0; }
                sb.Append(w).Append(' '); line += w.Length + 1;
            }
            return sb.ToString().TrimEnd();
        }

        string InfoText()
        {
            var dm = DungeonManager.I;
            string place = dm != null && dm.InDungeon ? $"{DungeonManager.GetBiome(dm.Level).Name}\n{dm.RoomNameAt(P.Feet)}" : "Таверна «Подземелье вкусностей»";
            var held = P.Right.HeldTool != null ? Tools.Name(P.Right.HeldTool.Type) : "пустая рука";
            return $"{place}\nHP {P.Health.HP:0}/{PlayerHealth.Max:0}   монеты {Wallet.Coins}\nВ руке: {held}   рюкзак {Inventory.Backpack.Total}" + (God ? "\n[F8] бессмертие" : "");
        }

        string ComputeObjective()
        {
            var dm = DungeonManager.I;
            if (dm != null && dm.InDungeon)
            {
                if (P.Health.HP < 35) return "Мало HP — ищи колодец или эликсир или иди в портал «В таверну»";
                var m = NearestMonster(12f);
                if (m != null)
                {
                    var t = RequiredTool(m.Kind);
                    return $"{Bestiary.Get(m.Kind).Name}: возьми {Tools.Name(t)} ({ToolKey(t)}) — {How(m.Kind)}";
                }
                if (Inventory.Backpack.Total >= 3) return "Рюкзак полон добычи — портал «В таверну» в стартовой комнате";
                return "Исследуй комнаты (таблички на стенах) и найди монстров этого этажа";
            }
            var gf = GameFlow.I;
            if (gf == null) return "";
            if (gf.State == DayState.Night) return "Ночь: нажми «Спать → новый день» на стойке";
            foreach (var bowl in Bowl.All) if (bowl != null && bowl.Courier == null && bowl.ServedTo == null) return "Поставь миску под номером нужного стола (E) и позвони в колокольчик";
            foreach (var c in Cauldron.All)
                if (c != null && c.gameObject.activeInHierarchy && (c.State == SoupState.Cooking || c.State == SoupState.Water))
                    return c.State == SoupState.Cooking ? $"Варится «{c.Recipe?.Name}» — мешай половником ({Tools.KitchenKey(ToolType.Ladle)}), когда котёл просит" : "Клади продукты в котёл по карточке рецепта";
            var r = CookableRecipe(null);
            if (Recipes.HasIngredients(r)) return $"Свари «{r.Name}»: подойди к котлу — рецепт картинками над ним (R — крупно)";
            if (gf.State == DayState.Prep) return "Спустись в подвал (дверь на юге) за добычей или «Открыть таверну»";
            return "Обслуживай гостей: заказы на табло, супы на окно раздачи";
        }

        void UpdateRecipeCard()
        {
            var dm = DungeonManager.I;
            var c = dm != null && dm.InDungeon ? null : ActiveCauldron();
            // баг-лист, пункт 4: левая HUD-панель рецепта под HP убрана совсем — рецепт теперь картинками на табличке над котлом
            // (RecipePicture) и крупной карточкой по R. Текст шагов считается дальше только для отчёта автотеста.
            if (_card.gameObject.activeSelf) _card.gameObject.SetActive(false);
            if (c == null) return;
            var r = CookableRecipe(c);
            var sb = new StringBuilder();
            sb.Append("РЕЦЕПТ: ").Append(r.Name).Append('\n');
            sb.Append($"огонь: {Recipes.HeatName(r.Heat)} · цена {r.Price}\n");
            int i = 1;
            // на экране — коротко (без пояснений в скобках), полный текст шагов — в книге рецептов и на экране у котла
            foreach (var s in RecipeSteps(c, r)) sb.Append(s.done ? "[x] " : "[  ] ").Append(i++).Append(". ").Append(Short(s.text)).Append('\n');
            if (c.State == SoupState.Cooking) sb.Append($"Готовность: {c.Progress01 * 100f:0}%\n");
            sb.Append(RecipeCardLine());
            var full = sb.ToString();
            if (full == _cardFor) return;
            _cardFor = full;
            var lines = new List<string>();
            foreach (var line in full.Split('\n')) foreach (var l in Wrap(line, 34).Split('\n')) lines.Add(l);
            _cardText.text = string.Join("\n", lines);
            float h = lines.Count * 0.042f + 0.03f;
            // слева под панелью HP: верх карточки — под HP, низ не ниже середины-низа экрана
            float top = HudPanel.HalfH - HudPanel.Margin - HudPanel.TopStrip - 0.01f - 0.25f - 0.02f;
            _card.localPosition = new Vector3(-HudPanel.HalfW + HudPanel.Margin, top, HudPanel.Z);
            _cardBg.transform.localScale = new Vector3(CardW, h, 1f);
            _cardBg.transform.localPosition = new Vector3(CardW * 0.5f, -h * 0.5f, 0.01f);
            _cardRect = new Rect(-HudPanel.HalfW + HudPanel.Margin, top, CardW, h);
        }
        string _cardFor = "";
        public static string Short(string t) { int a = t.IndexOf(" ("), b = t.LastIndexOf(')'); return a > 0 && b > a ? t.Substring(0, a) + t.Substring(b + 1) : t; }

        void UpdateArrow()
        {
            var dm = DungeonManager.I;
            Vector3? target = null;
            if (dm != null && dm.InDungeon && !BestiaryCard.Open && HintsToggle.On)
            {
                var m = NearestMonster(30f);
                if (m != null && (m.transform.position - P.Feet).magnitude > 3f) target = m.transform.position;
            }
            _arrow.gameObject.SetActive(target.HasValue);
            if (!target.HasValue) return;
            var d = target.Value - P.HeadPos; d.y = 0;
            var camF = P.Cam.transform.forward; camF.y = 0;
            float ang = Vector3.SignedAngle(camF, d, Vector3.up);
            _arrow.localRotation = Quaternion.Euler(-70f, ang, 0);
        }

        ToolType? _lastHeldType;
        void Coach()
        {
            var m = NearestMonster(4.5f);
            if (m == null) return;
            var need = RequiredTool(m.Kind);
            var held = P.Right.HeldTool;
            string s = m.State;
            bool window = s == "Оглушён" || s == "Устал" || s == "Загнан в угол" || s == "Клюв застрял" || s == "Меч застрял" || s == "Перегрев" || s == "Хвост пролетел" || s == "Треснул" || s == "Схвачена" || s == "Срезана" || s == "На колене";
            bool wrongTool = held != null && held.Type != need && held.Type != ToolType.Shield;
            var heldType = held != null ? held.Type : (ToolType?)null;
            if (heldType != _lastHeldType) { _lastHeldType = heldType; _coachSeen.Remove(m.Kind); } // сменил инструмент — подсказать сразу
            if (window && _lastWindowMonster != m.name + s)
            {
                _lastWindowMonster = m.name + s;
                // окно открылось, но в руке не тот инструмент — важнее сказать, какой нужен
                if (wrongTool)
                {
                    _coachSeen[m.Kind] = Time.time;
                    Say($"СЕЙЧАС! {Bestiary.Get(m.Kind).Name}: {s.ToLower()}, но нужен {Tools.Name(need)} — клавиша {ToolKey(need)}", HintPri.Error);
                }
                else Say($"СЕЙЧАС! {Bestiary.Get(m.Kind).Name}: {s.ToLower()} — {How(m.Kind)}", HintPri.Window);
                return;
            }
            if (wrongTool && (!_coachSeen.TryGetValue(m.Kind, out var t) || Time.time - t > 8f))
            {
                _coachSeen[m.Kind] = Time.time;
                Say($"Для «{Bestiary.Get(m.Kind).Name}» нужен {Tools.Name(need)} — клавиша {ToolKey(need)}", HintPri.Error);
            }
        }

        void Say(string msg, HintPri pri)
        {
            if (!HintsToggle.On) return; // пункт 10: подсказки выключены (H)
            CoachHintsShown++;
            LastCoach = msg;
            if (HuntCoach.Visible || KitchenCoach.Visible) return; // крупная подсказка по шагам уже говорит то же самое — не дублируем внизу
            Hints.Show(msg, pri, 4f);
        }

        void Idle()
        {
            var look = P.Cam.transform.rotation;
            bool moved = (P.Feet - _lastFeet).sqrMagnitude > 0.0004f || Quaternion.Angle(look, _lastLook) > 0.5f || P.Right.Swinging;
            _lastFeet = P.Feet; _lastLook = look;
            if (moved || BestiaryCard.Open || DTime.Paused || (Fader.I != null && Fader.I.Busy)) { _idleT = 0; return; }
            if (!IdleEnabled || (Tools.InDungeon && !HintsToggle.On)) return;
            _idleT += Time.unscaledDeltaTime;
            if (_idleT < IdleDelay) return;
            _idleT = -IdleDelay; // следующая подсказка через 10 с бездействия
            var msg = "Не знаешь, что делать? " + (Tutorial.Active ? Tutorial.Current : ComputeObjective());
            IdleHintsShown++;
            LastIdleHint = msg;
            Hints.Show(msg, HintPri.Tutorial, 5f);
        }

        void Cheats()
        {
            if (GameFlow.I != null && GameFlow.I.TavernPreview) return; // не менять инструменты/этаж/PlayerPrefs обычного профиля
            if (DIn.Down(Key.F7)) DemoKit();
            if (DIn.Down(Key.F8)) { God = !God; Hints.Show(God ? "Бессмертие включено (F8)" : "Бессмертие выключено", HintPri.Action); }
            if (DIn.Down(Key.F10) && DungeonManager.I != null)
            {
                var dm = DungeonManager.I;
                int next = dm.InDungeon ? Mathf.Min(4, dm.Level + 1) : 1;
                DemoKit(false);
                dm.Enter(next);
            }
            if (DIn.Down(Key.F11)) { Tutorial.Skip(); Hints.Show("Обучение пропущено (F11)", HintPri.Action); }
        }

        /// <summary>Набор для показа: все инструменты, +1000 монет, по 5 продуктов каждой добычи в леднике.</summary>
        public static void DemoKit(bool announce = true)
        {
            if (GameFlow.I != null && GameFlow.I.TavernPreview) return;
            if (PlayerRig.I == null) return;
            foreach (var t in new[] { ToolType.Cleaver, ToolType.Hammer, ToolType.Shield }) if (!PlayerRig.I.Has(t)) PlayerRig.I.GiveTool(t);
            if (GameFlow.I != null) { var s = GameFlow.I.Save; s.hasCleaver = s.hasHammer = s.hasShield = true; }
            if (announce)
            {
                Wallet.Add(1000);
                foreach (var id in Items.MonsterItems) Inventory.Fridge.Add(id, 5);
                Hints.Show("Демо-набор (F7): все инструменты, +1000 монет, ледник полон", HintPri.Window);
            }
        }
    }

    /// <summary>Обучение первого дня: шаг выполняется сам, когда игрок делает действие. Пропуск — F11.</summary>
    public class Tutorial
    {
        public static readonly string[] Texts =
        {
            "Ходи WASD, осматривайся мышью",
            "Иди к двери «Подвал» на юге и нажми «−1» (E)",
            "Возьми лопатку — клавиша 1 (охотятся только в подземелье)",
            "Найди слайма — открой бестиарий кнопкой B",
            "Добудь слайма: ударь лопаткой (ЛКМ) — он оглушён — ударь ещё раз",
            "Вернись в таверну через портал «В таверну»",
            "Подойди к котлу и нажми «Вода» (E)",
            "Свари «Желейную похлёбку» по карточке рецепта справа",
            "Поставь миску на окно раздачи и позвони в колокольчик",
        };
        public int Step { get; private set; }
        public bool Active { get; private set; }
        public bool Done => !Active;
        public string Current => Active ? Texts[Step] : "";
        Vector3 _start;
        int _coins0;
        const string Key = "podz_tutorial_done";

        public void Begin()
        {
            Active = PlayerPrefs.GetInt(Key, 0) == 0 && (GameFlow.I == null || GameFlow.I.Save.day <= 1);
            Step = 0;
            _start = PlayerRig.I != null ? PlayerRig.I.Feet : Vector3.zero;
            _coins0 = Wallet.Coins;
            if (Active) Hints.Show("Добро пожаловать в «Подземелье вкусностей»! Задача — сверху слева.", HintPri.Tutorial, 5f);
        }

        public void Restart() { PlayerPrefs.DeleteKey(Key); Begin(); Active = true; }

        public void Skip() { Active = false; PlayerPrefs.SetInt(Key, 1); }

        public void Tick()
        {
            if (!Active || PlayerRig.I == null || (GameFlow.I != null && GameFlow.I.TavernPreview)) return;
            var P = PlayerRig.I;
            var dm = DungeonManager.I;
            bool ok = false;
            switch (Step)
            {
                case 0: ok = (P.Feet - _start).magnitude > 1.5f; break;
                case 1: ok = dm != null && dm.InDungeon; break;
                case 2: ok = P.Right.HeldTool != null && P.Right.HeldTool.Type == ToolType.Spatula; break;
                case 3: ok = Bestiary.Seen(MonsterKind.Slime); break;
                case 4: ok = Inventory.Backpack.Count(ItemId.SlimeJelly) > 0 || Inventory.Fridge.Count(ItemId.SlimeJelly) > 0; break;
                case 5: ok = dm != null && !dm.InDungeon; break;
                case 6: foreach (var c in Cauldron.All) if (c != null && c.State != SoupState.Empty) ok = true; break;
                case 7: ok = Bowl.All.Count > 0; foreach (var c in Cauldron.All) if (c != null && c.State == SoupState.Ready) ok = true; break;
                case 8: ok = Wallet.Coins > _coins0; break;
            }
            if (!ok) return;
            if (Step == 7) _coins0 = Wallet.Coins;
            Step++;
            if (Step >= Texts.Length)
            {
                Active = false;
                PlayerPrefs.SetInt(Key, 1);
                Hints.Show("Обучение пройдено! Дальше — сам: копи на тесак и спускайся глубже.", HintPri.Window, 5f);
            }
            else Hints.Show("Готово! Дальше: " + Texts[Step], HintPri.Tutorial, 4f);
        }
    }
}
