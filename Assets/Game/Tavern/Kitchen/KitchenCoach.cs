using UnityEngine;
using UnityEngine.InputSystem;

namespace Podz
{
    /// <summary>
    /// Тренер на кухне (баг-лист, пункт 6) — как HuntCoach в подземелье: крупно по центру «Шаг 2/6: …» что делать СЕЙЧАС,
    /// а на 3 с красным — что НЕЛЬЗЯ («Этим не режут — возьми нож (1)», «Сначала налей воду»).
    /// Логика: вода → продукт из ледника → на доску → режь → в котёл → варка → «Разлить» → миска на окно → колокольчик;
    /// упал продукт — «подними (E) → в урну → возьми новый из ледника». R — крупная карточка рецепта картинками.
    /// </summary>
    public class KitchenCoach : MonoBehaviour
    {
        public static KitchenCoach I;
        public static string LastForbid = "";
        public static float ForbidAt = -99f;
        public static int Forbids;
        public const float ForbidTime = 3f;
        [SerializeField] TextMesh _text;
        [SerializeField] Renderer _bg;
        string _shown = "";

        public static string Shown => I != null && I.Visible_ ? I._shown : "";
        public static bool Visible => I != null && I.Visible_;
        public static bool ForbidVisible => Visible && Time.unscaledTime - ForbidAt < ForbidTime;
        public static Rect Area => !Visible ? Rect.zero :
            new Rect(-I._bg.transform.localScale.x * 0.5f, I.transform.localPosition.y + I._bg.transform.localScale.y * 0.5f, I._bg.transform.localScale.x, I._bg.transform.localScale.y);
        bool Visible_ => _bg != null && _bg.gameObject.activeSelf;

        /// <summary>Сказать, что так НЕЛЬЗЯ (красным крупно на 3 с).</summary>
        public static void Forbid(string text)
        {
            LastForbid = text; ForbidAt = Time.unscaledTime; Forbids++;
        }

        public static KitchenCoach Create(Transform camera)
        {
            var template = Application.isPlaying ? SavedDungeonAssets.Active?.KitchenCoachCanvas : null;
            var panel = template != null ? Instantiate(template).GetComponent<KitchenCoach>() :
                SavedUi.CreateRoot("KitchenCoach").AddComponent<KitchenCoach>();
            panel.AttachTo(camera);
            return panel;
        }

        public void AttachTo(Transform cam)
        {
            I = this;
            transform.SetParent(cam, false);
            transform.localPosition = new Vector3(0, HuntCoach.Y, 1.2f);
            transform.localRotation = Quaternion.identity;
            if (_text == null) BuildVisual();
            else SavedDungeonAssets.RestoreText(gameObject, true);
        }

        void BuildVisual()
        {
            _bg = Prim.Quad("KitchenCoachBg", transform, new Vector3(0, 0, 0.01f), new Vector2(1f, 0.1f), new Color(0, 0, 0, 0.8f));
            _bg.sortingOrder = 2; Prim.Hud(_bg); Prim.SetOverlayColor(_bg, new Color(0.03f, 0.05f, 0.03f, 0.88f));
            _text = Prim.Text(transform, Vector3.zero, "", 0.058f, Color.white);
            _text.GetComponent<MeshRenderer>().sortingOrder = 7;
            Show(false);
        }
        void OnDestroy() { if (I == this) I = null; }

        void Show(bool v) { if (_bg.gameObject.activeSelf != v) { _bg.gameObject.SetActive(v); _text.gameObject.SetActive(v); } }

        void Update()
        {
            var P = PlayerRig.I;
            if (P != null && !P.IsXR && DIn.Down(Key.R)) RecipeCardHud.Toggle();
        }

        void LateUpdate()
        {
            var P = PlayerRig.I;
            var k = Kitchen.I;
            bool on = P != null && k != null && !(DungeonManager.I != null && DungeonManager.I.InDungeon) && k.InKitchen(P.Feet)
                      && !HudPanel.ModalOpen && !RecipeCardHud.Open && !(Fader.I != null && Fader.I.Busy);
            bool bad = false;
            string msg = on ? Compute(out bad) : null;
            if (string.IsNullOrEmpty(msg)) { Show(false); return; }
            bool forbid = Time.unscaledTime - ForbidAt < ForbidTime;
            if (forbid) { msg = "НЕЛЬЗЯ: " + LastForbid; bad = true; }
            else if (HurryPrefix().Length > 0) { msg = HurryPrefix() + msg; bad = true; } // К5: гость скоро уйдёт
            if (msg != _shown)
            {
                _shown = msg;
                string text = msg;
                if (msg.Length > 52)
                {
                    int mid = msg.Length / 2, a = msg.LastIndexOf(' ', mid), b = msg.IndexOf(' ', mid);
                    int cut = b > 0 && (a < 0 || b - mid < mid - a) ? b : a;
                    if (cut > 0) text = msg.Substring(0, cut) + "\n" + msg.Substring(cut + 1);
                }
                _text.text = text;
                int lines = text.Split('\n').Length;
                _bg.transform.localScale = new Vector3(Mathf.Clamp(HudPanel.TextWidth(_text) + 0.07f, 0.4f, 2f * HudPanel.HalfW - 0.06f), 0.068f * lines + 0.035f, 1f);
            }
            _text.color = bad ? new Color(1f, 0.5f, 0.45f) : new Color(0.85f, 1f, 0.8f);
            Show(true);
        }

        static bool HasReady(ItemId id)
        {
            foreach (var i in Ingredient.All) if (i != null && i.Id == id && i.Prepared && !i.Spoiled) return true;
            return false;
        }

        /// <summary>К5: гость скоро уйдёт — «Поторопись! » перед подсказкой.</summary>
        public static string HurryPrefix()
        {
            var gm = GuestManager.I;
            if (gm == null) return "";
            foreach (var g in gm.Guests) if (g != null && g.Hurry) return "Поторопись! ";
            return "";
        }

        static Ingredient HeldIngredient(PlayerRig P)
        {
            var h = P.Right.Held as Ingredient;
            if (h == null && P.Left != null) h = P.Left.Held as Ingredient;
            return h;
        }

        /// <summary>Строка тренера: что делать сейчас. bad — красная (ошибка/нельзя).</summary>
        public static string Compute(out bool bad)
        {
            bad = false;
            var P = PlayerRig.I; var k = Kitchen.I;
            if (P == null || k == null) return null;
            var gf = GameFlow.I;
            // упавший продукт — сначала убрать
            var held = HeldIngredient(P);
            if (held != null && held.Spoiled) { bad = true; return $"{Items.Name(held.Id)} {(held.Dropped ? "упал" : "сгорел")} — {TrashBin.Tip.ToLower()} (смотри на урну → E)"; }
            foreach (var i in Ingredient.All)
                if (i != null && i.Dropped && !i.IsHeld) { bad = true; return $"{Items.Name(i.Id)} упал на пол — подойди и жми E (поднять), потом в урну"; }
            // миска с супом — отнести на окно и позвонить
            Bowl bowl0 = null; foreach (var b in Bowl.All) if (b != null && b.Courier == null && b.ServedTo == null) { bowl0 = b; break; }
            if (bowl0 != null)
            {
                if (bowl0.IsHeld) return "Последний шаг: смотри на окно раздачи и жми E (поставить)";
                if (!k.InWindow(bowl0.transform.position)) return "Последний шаг: возьми миску (E) и поставь на окно раздачи";
                return "Последний шаг: позвони в колокольчик (E)";
            }
            var c = Cauldron.LabelOwner();
            if (c == null && StepMarker.I != null) c = StepMarker.I.Current;
            if (c == null) return gf != null && gf.State == DayState.Night ? null : "Подойди к котлу — над ним рецепт картинками (R — крупно)";
            var r = Guide.CookableRecipe(c);
            var steps = Guide.RecipeSteps(c, r);
            int cur = Guide.CurrentStep(c, r, steps);
            int n = steps.Count;
            if (cur < 0) return null;
            string head = $"Шаг {cur + 1}/{n}: ";
            if (c.State == SoupState.Ruined || c.State == SoupState.Burnt) { bad = true; return "Суп испорчен — нажми «Слить» и начни заново"; }
            if (cur == 0)
            {
                var first = RecipeScreen.StepItem(r, 1);
                return head + "нажми «Вода» на котле (E) — вода станет синей" + (first.HasValue && Guide.WaterDeferred(c, r, steps) == false && HasReady(first.Value) ? $", потом {Items.Name(first.Value).ToLower()} — в котёл" : "");
            }
            var item = RecipeScreen.StepItem(r, cur);
            string knifeKey = Tools.KitchenKey(ToolType.Knife);
            if (item.HasValue)
            {
                var id = item.Value; var info = Items.Get(id); string name = Items.Name(id);
                if (held != null && held.Id == id)
                {
                    if (held.Prepared) return head + $"{name} готов — смотри на котёл и жми E (в котёл)";
                    if (info.Prep == PrepKind.WashOnly || (info.Prep == PrepKind.WashThenKnife && !held.Washed)) return head + $"положи {name.ToLower()} в мойку (смотри на мойку → E)";
                    if (info.Prep == PrepKind.Grill) return head + "положи мясо на гриль на 4 с (смотри на гриль → E)";
                    return head + $"положи {name.ToLower()} на доску (смотри на доску → E)";
                }
                Ingredient lying = null;
                foreach (var i in Ingredient.All) if (i != null && i.Id == id && !i.Spoiled && !i.IsHeld && (lying == null || (i.Prepared && !lying.Prepared))) lying = i;
                if (lying != null)
                {
                    if (lying.Prepared) return head + $"{name} готов — возьми (E) и брось в котёл";
                    var need = info.Prep == PrepKind.Cleaver ? ToolType.Cleaver : info.Prep == PrepKind.Hammer ? ToolType.Hammer : ToolType.Knife;
                    bool cut = info.Prep == PrepKind.Knife || info.Prep == PrepKind.Cleaver || info.Prep == PrepKind.Hammer || (info.Prep == PrepKind.WashThenKnife && lying.Washed);
                    if (cut && k.OnBoard(lying.transform.position))
                    {
                        var ht = P.Right.HeldTool != null ? P.Right.HeldTool.Type : (ToolType?)null;
                        if (ht != need) return head + $"возьми {Tools.Name(need).ToLower()} ({Tools.KitchenKey(need)})";
                        return head + $"бей ЛКМ по продукту на доске ({lying.Hits}/{info.Hits})";
                    }
                    if (info.Prep == PrepKind.WashOnly || info.Prep == PrepKind.WashThenKnife && !lying.Washed) return head + $"возьми {name.ToLower()} (E) и положи в мойку";
                    if (info.Prep == PrepKind.Grill) return head + "возьми мясо (E) и положи на гриль";
                    return head + $"возьми {name.ToLower()} (E) и положи на доску";
                }
                if (Fridge.Count(id) > 0)
                {
                    // К4: только что пришёл заказ — «Заказ: <блюдо> → иди к леднику»
                    bool fresh = cur == 1 && !steps[0].done && Guide.WaitingFor(r) != null;
                    if (fresh) return $"Заказ: {r.Name} → иди к {(Fridge.Infinite(id) ? "кладовой" : "леднику")}, возьми «{name}» (E по полке)";
                    return head + $"возьми «{name}» из {(Fridge.Infinite(id) ? "кладовой" : "ледника")} (E по полке)";
                }
                bad = true; return head + $"нет «{name}» — добудь: {FoodModels.SourceText(id)}";
            }
            if (c.State == SoupState.Cooking)
            {
                // К3: окно «Мешай!» — сразу и крупно; долго не мешаешь — «Помешай суп — клавиша»
                if (c.StirNow)
                {
                    string key = Cauldron.StirKey();
                    if (c.StirOpenFor >= Cauldron.StirNagAfter) { bad = true; return head + $"Помешай суп — {key}! (помешано {c.Stirs}/{Cauldron.StirsNeeded})"; }
                    return head + $"Мешать — {key} (помешано {c.Stirs}/{Cauldron.StirsNeeded})";
                }
                if (!c.NeedsStir) return head + $"варится {c.Progress01 * 100f:0}% — мешать не нужно, просто жди";
                return head + $"варится {c.Progress01 * 100f:0}% — жди сигнала «Мешать»";
            }
            if (c.State == SoupState.Ready) return head + "готово! нажми «Разлить» (E)";
            Bowl bowl = null; foreach (var b in Bowl.All) if (b != null && b.Courier == null && b.ServedTo == null) { bowl = b; break; }
            if (bowl != null)
            {
                if (bowl.IsHeld) return head + "смотри на окно раздачи и жми E (поставить)";
                if (!k.InWindow(bowl.transform.position)) return head + "возьми миску (E) и поставь на окно раздачи";
                return head + "позвони в колокольчик (E)";
            }
            return null;
        }
    }
}
