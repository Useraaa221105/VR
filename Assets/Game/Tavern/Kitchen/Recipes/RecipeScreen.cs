using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Большой экран рецепта на стене над котлом: название, цена, огонь, заказ; слева ингредиенты с иконками и ✅/❌
    /// (где лежит или где добыть), справа шаги: выполненные — зелёной галочкой, текущий — жёлтой полосой со стрелкой.
    /// Внизу — подсказка Сенши к текущему шагу.
    /// </summary>
    public class RecipeScreen : MonoBehaviour
    {
        public static readonly List<RecipeScreen> All = new List<RecipeScreen>();
        public const int MaxIng = 4, MaxSteps = 8;
        public const float W = 1.6f, H = 1.22f;

        public Cauldron Cauldron { get; private set; }
        public Recipe Recipe { get; private set; }
        public int CurrentStep { get; private set; } = -1;
        public int DoneCount { get; private set; }
        public int StepCount { get; private set; }
        public int OkCount { get; private set; }
        public int MissingCount { get; private set; }
        public string Senshi { get; private set; } = "";
        public string Text { get; private set; } = "";
        public bool Visible => _root != null && _root.gameObject.activeSelf;
        public Vector3 Center => _root.position;

        [SerializeField] Transform _root;
        Transform _grid, _dish;
        [SerializeField] TextMesh _title, _order, _senshi;
        float _next;
        string _last = "";
        public int Tiles { get; private set; }

        static readonly Color Ink = new Color(1f, 0.93f, 0.78f), Yellow = new Color(1f, 0.82f, 0.25f);

        public static RecipeScreen Create(Transform parent, Cauldron c, Vector3 localPos, float yaw)
        {
            var template = Application.isPlaying ? SavedDungeonAssets.Active?.RecipeDisplay : null;
            bool useSaved = template != null && template._root != null;
            var s = useSaved ? Instantiate(template, parent, false) :
                SavedUi.CreateRoot("RecipeScreen").AddComponent<RecipeScreen>();
            if (!useSaved) s.transform.SetParent(parent, false);
            s.Cauldron = c;
            if (!useSaved)
            {
                s._root = new GameObject("Screen").transform;
                s._root.SetParent(s.transform, false);
                s.Build();
            }
            else SavedDungeonAssets.RestoreText(s.gameObject);
            s._root.localPosition = localPos;
            s._root.localRotation = Quaternion.Euler(0, yaw, 0);
            if (Application.isPlaying) All.Add(s);
            return s;
        }
        void OnDestroy() { All.Remove(this); }

        void Build()
        {
            var t = _root;
            Prim.Cube("Frame", t, new Vector3(0, 0, 0.02f), new Vector3(W + 0.08f, H + 0.08f, 0.02f), new Color(0.42f, 0.27f, 0.14f), false);
            var bg = Prim.Cube("Bg", t, new Vector3(0, 0, 0.005f), new Vector3(W, H, 0.01f), new Color(0.09f, 0.08f, 0.07f), false);
            Prim.SetEmission(bg.GetComponent<Renderer>(), new Color(0.03f, 0.025f, 0.02f));
            const float z = -0.012f;
            _title = Prim.Text(t, new Vector3(-0.6f, 0.52f, z), "", 0.085f, Yellow, TextAnchor.MiddleLeft); _title.fontStyle = FontStyle.Bold;
            _order = Prim.Text(t, new Vector3(0.76f, 0.52f, z), "", 0.06f, Yellow, TextAnchor.MiddleRight); _order.fontStyle = FontStyle.Bold;
            _senshi = Prim.Text(t, new Vector3(0, -0.545f, z), "", 0.045f, new Color(0.75f, 1f, 0.75f), TextAnchor.MiddleCenter);
            _root.gameObject.SetActive(false);
        }

        static string Wrap2(string s, int max)
        {
            if (s.Length <= max) return s;
            int cut = s.LastIndexOf(' ', max);
            if (cut < 4) return Clip(s, max);
            var rest = s.Substring(cut + 1);
            return s.Substring(0, cut) + "\n" + Clip(rest, max);
        }
        static string Clip(string s, int max) => s.Length <= max ? s : s.Substring(0, max - 1) + "…";
        static void Act(Transform t, bool on) { if (t.gameObject.activeSelf != on) t.gameObject.SetActive(on); }

        /// <summary>Шаг рецепта → продукт (для шагов-ингредиентов), иначе null.</summary>
        public static ItemId? StepItem(Recipe r, int step) => step >= 1 && step <= r.Items.Length ? r.Items[step - 1] : (ItemId?)null;

        static int OnKitchen(ItemId id)
        {
            int n = 0;
            foreach (var i in Ingredient.All) if (i != null && i.Id == id && !i.Burnt) n++;
            return n;
        }

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.2f;
            bool show = Cauldron != null && Cauldron.gameObject.activeInHierarchy && !(DungeonManager.I != null && DungeonManager.I.InDungeon);
            Act(_root, show);
            if (!show) return;
            Refresh();
        }

        public void Refresh()
        {
            var c = Cauldron;
            var r = Guide.CookableRecipe(c);
            var steps = Guide.RecipeSteps(c, r);
            int cur = Guide.CurrentStep(c, r, steps);
            var who = Guide.WaitingFor(r);
            // ингредиенты: в котле / на кухне / в леднике ×N / кладовая ∞ / нет — где добыть
            var left = new List<ItemId>(c.Contents);
            bool cooking = c.State == SoupState.Cooking || c.State == SoupState.Ready;
            var need = new Dictionary<ItemId, int>();
            var rows = new List<(ItemId id, bool ok, string where)>();
            foreach (var id in r.Items)
            {
                if (left.Remove(id) || cooking) { rows.Add((id, true, "в котле")); continue; }
                need.TryGetValue(id, out var k); need[id] = k + 1;
                int kitchen = OnKitchen(id);
                if (k < kitchen) rows.Add((id, true, "взят, лежит на кухне"));
                else if (Fridge.Infinite(id)) rows.Add((id, true, "кладовая ∞"));
                else if (k - kitchen < Fridge.Count(id)) rows.Add((id, true, $"в леднике ×{Fridge.Count(id)}"));
                else rows.Add((id, false, "нет — добудь: " + FoodModels.SourceText(id)));
            }
            var sb = new StringBuilder();
            sb.Append(GameFlow.I != null ? GameFlow.I.TavernStage : 1).Append('|').Append(r.Index).Append('|').Append(cur).Append('|').Append(who != null ? who.Table : -1).Append('|').Append((int)(c.Progress01 * 20)).Append('|').Append(c.State);
            foreach (var x in rows) sb.Append('|').Append(x.ok).Append(x.where);
            var key = sb.ToString();
            if (key == _last && Recipe == r) return;
            _last = key;
            Recipe = r;

            int stage = GameFlow.I != null ? GameFlow.I.TavernStage : 1;
            string sub = $"цена {r.Price}  ·  огонь: {Recipes.HeatName(r.Heat)}";
            if (stage >= 3 && r.Spices.Count > 0) sub += "  ·  " + r.SpiceText;
            if (c.State == SoupState.Cooking) sub += $"  ·  готово {c.Progress01 * 100f:0}%";
            _title.text = r.Name;
            Prim.FitWidth(_title, W - 0.62f);
            _order.text = who != null ? $"стол {who.Table + 1}" : "";
            OkCount = 0; MissingCount = 0;
            var all = new StringBuilder();
            all.Append("РЕЦЕПТ: ").Append(r.Name).Append('\n').Append(sub).Append('\n').Append(who != null ? $"Заказ: стол {who.Table + 1}" : "").Append('\n');
            foreach (var x in rows)
            {
                if (x.ok) OkCount++; else MissingCount++;
                all.Append(x.ok ? "[+] " : "[-] ").Append(Items.Name(x.id)).Append(" — ").Append(x.where).Append('\n');
            }
            StepCount = steps.Count; DoneCount = 0; CurrentStep = cur;
            for (int i = 0; i < steps.Count; i++)
            {
                var st = steps[i];
                if (st.done) DoneCount++;
                all.Append(st.done ? "[x] " : i == cur ? "[>] " : "[ ] ").Append(i + 1).Append(". ").Append(st.text).Append('\n');
            }
            Senshi = SenshiLine(c, r, cur, rows);
            _senshi.text = Clip(Senshi, 70);
            all.Append("Сенши: «").Append(Senshi).Append('»');
            Text = all.ToString();
            // картинки: блюдо + плитки шагов (перестраиваются только при изменении)
            if (_dish == null || _dish.name != "Dish_" + r.Index)
            {
                if (_dish != null) { _dish.SetParent(null); Destroy(_dish.gameObject); }
                _dish = KIcons.Dish(_root, r, new Vector3(-0.7f, 0.5f, -0.05f), 0.13f);
            }
            if (_grid != null) { _grid.SetParent(null); Destroy(_grid.gameObject); }
            _grid = new GameObject("Grid").transform; _grid.SetParent(_root, false);
            Tiles = RecipePicture.Grid(_grid, RecipePicture.Steps(c, r, stage), r, 0.42f, 1f);
        }

        static string SenshiLine(Cauldron c, Recipe r, int cur, List<(ItemId id, bool ok, string where)> rows)
        {
            if (cur < 0) return "Всё сделано! Гость будет доволен.";
            if (cur == 0) return "Налей воду: нажми «Вода» на котле (E), потом продукт — в котёл.";
            var item = StepItem(r, cur);
            if (item.HasValue)
            {
                var id = item.Value; var info = Items.Get(id); string n = info.Name;
                if (cur - 1 < rows.Count && !rows[cur - 1].ok) return Clip($"Нет «{n}»! Добудь: {FoodModels.SourceText(id)}", 64);
                string knife = Tools.KitchenKey(ToolType.Knife), cleaver = Tools.KitchenKey(ToolType.Cleaver), hammer = Tools.KitchenKey(ToolType.Hammer);
                if (id == ItemId.MushroomLegs) return "Ножки: плашмя15см (ПКМ), затем2реза сверху (ЛКМ).";
                switch (info.Prep)
                {
                    case PrepKind.Knife: return $"{n}: на доску и режь ножом ({knife}), ЛКМ ×{info.Hits}.";
                    case PrepKind.WashThenKnife: return $"{n}: помой в мойке, потом нарежь ножом ({knife}).";
                    case PrepKind.Cleaver: return $"{n}: на доску и руби тесаком ({cleaver}), ЛКМ ×{info.Hits}.";
                    case PrepKind.Hammer: return $"{n}: отбей молотком ({hammer}) на доске, ЛКМ ×{info.Hits}.";
                    case PrepKind.WashOnly: return $"{n}: промой в мойке — секунды хватит.";
                    case PrepKind.Grill: return $"{n}: обжарь на гриле 4 секунды, не дольше!";
                    default: return $"{n}: просто брось в котёл.";
                }
            }
            if (cur == r.Items.Length + 1) return $"Пусть кипит. Котёл позовёт — мешай половником ({Tools.KitchenKey(ToolType.Ladle)}).";
            return "Готово! «Разлить» → миску на окно → колокольчик.";
        }
    }

    /// <summary>
    /// Подсветка текущего шага в мире: жёлтая подпрыгивающая стрелка и пульсирующее кольцо там, что делать сейчас
    /// (кнопка «Вода», полка ледника, мойка/доска/гриль, горло котла, «Разлить», миска, окно, колокольчик).
    /// </summary>
    public class StepMarker : MonoBehaviour
    {
        public static StepMarker I;
        public bool Shown { get; private set; }
        public Vector3 Target { get; private set; }
        public string TargetName { get; private set; } = "";
        Transform _arrow, _ring;
        Cauldron _last;
        static readonly Color Col = new Color(1f, 0.85f, 0.15f);

        public static StepMarker Create(Transform parent)
        {
            var go = new GameObject("StepMarker");
            go.transform.SetParent(parent, false);
            var m = go.AddComponent<StepMarker>();
            I = m;
            m.Build();
            return m;
        }
        void OnDestroy() { if (I == this) I = null; }

        GameObject Bar(Transform p, Vector3 pos, Vector3 size, float rotZ)
        {
            var g = Prim.Cube("MarkBar", p, pos, size, Col, false);
            g.transform.localRotation = Quaternion.Euler(0, 0, rotZ);
            Prim.SetEmission(g.GetComponent<Renderer>(), Col * 1.6f);
            return g;
        }

        void Build()
        {
            _arrow = new GameObject("Arrow").transform; _arrow.SetParent(transform, false);
            Bar(_arrow, new Vector3(0, 0.12f, 0), new Vector3(0.05f, 0.2f, 0.03f), 0);
            Bar(_arrow, new Vector3(-0.045f, 0.02f, 0), new Vector3(0.05f, 0.14f, 0.03f), 45f);
            Bar(_arrow, new Vector3(0.045f, 0.02f, 0), new Vector3(0.05f, 0.14f, 0.03f), -45f);
            _ring = new GameObject("Ring").transform; _ring.SetParent(transform, false);
            for (int i = 0; i < 14; i++)
            {
                float a = i * Mathf.PI * 2f / 14f;
                var g = Prim.Cube("RingSeg", _ring, new Vector3(Mathf.Sin(a) * 0.17f, 0, Mathf.Cos(a) * 0.17f), new Vector3(0.07f, 0.012f, 0.025f), Col, false);
                g.transform.localRotation = Quaternion.Euler(0, a * Mathf.Rad2Deg, 0);
                Prim.SetEmission(g.GetComponent<Renderer>(), Col * 1.6f);
            }
            Show(false);
        }

        void Show(bool on)
        {
            Shown = on;
            if (_arrow.gameObject.activeSelf != on) _arrow.gameObject.SetActive(on);
            if (_ring.gameObject.activeSelf != on) _ring.gameObject.SetActive(on);
        }

        static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform ch in t) { var r = FindDeep(ch, name); if (r != null) return r; }
            return null;
        }

        /// <summary>Котёл, для которого подсвечиваем шаг: ближний, иначе последний, пока игрок на кухне.</summary>
        public Cauldron Current
        {
            get
            {
                var P = PlayerRig.I;
                if (P == null || Kitchen.I == null || !Kitchen.I.InKitchen(P.Feet)) { _last = null; return null; }
                var c = Guide.ActiveCauldron();
                if (c != null) _last = c;
                else if (_last == null)
                    foreach (var x in Cauldron.All) if (x != null && x.gameObject.activeInHierarchy && x.State != SoupState.Empty) { _last = x; break; }
                // К4: пришёл заказ, а игрок далеко от котла — всё равно ведём по шагам (к леднику) от первого котла
                if (_last == null && AnyOrderWaiting())
                    foreach (var x in Cauldron.All) if (x != null && x.gameObject.activeInHierarchy) { _last = x; break; }
                return _last;
            }
        }

        public static bool AnyOrderWaiting()
        {
            var gm = GuestManager.I;
            if (gm == null) return false;
            foreach (var g in gm.Guests) if (g != null && g.Waiting && !g.Assigned) return true;
            return false;
        }

        /// <summary>Куда показывать для текущего шага. false — показывать нечего.</summary>
        public bool Compute(out Vector3 pos, out string name)
        {
            pos = Vector3.zero; name = "";
            var P = PlayerRig.I;
            var c = Current;
            if (c == null || P == null) return false;
            var k0 = Kitchen.I;
            // миска уже есть — сначала донести её: миска → окно → колокольчик (как у тренера «Последний шаг»)
            foreach (var b0 in Bowl.All)
            {
                if (b0 == null || b0.Courier != null || b0.ServedTo != null) continue;
                var guest = GuestManager.I != null ? GuestManager.I.Guests.Find(x => x != null && x.Waiting && !x.Assigned && x.Order == b0.Recipe) : null;
                if (b0.IsHeld) { pos = guest != null ? k0.WindowSlot(guest.Table) : k0.WindowPos; name = guest != null ? $"Стол {guest.Table + 1}" : "окно"; return true; }
                if (!k0.InWindow(b0.transform.position)) { pos = b0.transform.position + Vector3.up * 0.05f; name = "миска"; return true; }
                pos = k0.BellPos; name = "колокольчик"; return true;
            }
            var r = Guide.CookableRecipe(c);
            var steps = Guide.RecipeSteps(c, r);
            int cur = Guide.CurrentStep(c, r, steps);
            if (cur < 0) return false;
            var k = Kitchen.I;
            Vector3 mouth = c.transform.position + Vector3.up * 0.95f;
            if (cur == 0) { var b = FindDeep(c.transform, "Btn_Вода"); pos = b != null ? b.position + Vector3.up * 0.06f : mouth; name = "Вода"; return true; }
            var item = RecipeScreen.StepItem(r, cur);
            if (item.HasValue)
            {
                var id = item.Value; var info = Items.Get(id);
                Ingredient held = (P.Right.Held as Ingredient);
                if (held == null || held.Id != id) held = P.Left != null ? P.Left.Held as Ingredient : null;
                if (held != null && held.Id != id) held = null;
                if (held != null)
                {
                    if (held.Prepared) { pos = mouth; name = "котёл"; return true; }
                    if (info.Prep == PrepKind.WashOnly || (info.Prep == PrepKind.WashThenKnife && !held.Washed)) { pos = k.SinkPos; name = "мойка"; return true; }
                    if (info.Prep == PrepKind.Grill) { pos = k.GrillPos; name = "гриль"; return true; }
                    pos = k.BoardPos; name = "доска"; return true;
                }
                // продукт уже лежит на кухне — показать на него (подготовленный — в первую очередь)
                Ingredient best = null;
                foreach (var i in Ingredient.All) if (i != null && i.Id == id && !i.Burnt && !i.IsHeld && (best == null || (i.Prepared && !best.Prepared))) best = i;
                if (best != null) { pos = best.transform.position + Vector3.up * 0.05f; name = "продукт"; return true; }
                if (Fridge.I != null && Fridge.Count(id) > 0 && Fridge.I.Slots.TryGetValue(id, out var slot)) { pos = slot.transform.position; name = "ледник"; return true; }
                return false; // продукта нет — показывать некуда, экран говорит, где добыть
            }
            if (c.State == SoupState.Cooking || c.State == SoupState.Water) { pos = mouth; name = "котёл"; return true; }
            if (c.State == SoupState.Ready) { var b = FindDeep(c.transform, "Btn_Разлить"); pos = b != null ? b.position + Vector3.up * 0.06f : mouth; name = "Разлить"; return true; }
            Bowl bowl = null;
            foreach (var b in Bowl.All) if (b != null && b.Courier == null && b.ServedTo == null) { bowl = b; break; }
            if (bowl == null) return false;
            if (!k.InWindow(bowl.transform.position) || bowl.IsHeld)
            {
                if (bowl.IsHeld) { pos = k.WindowPos; name = "окно"; }
                else { pos = bowl.transform.position + Vector3.up * 0.05f; name = "миска"; }
                return true;
            }
            pos = k.BellPos; name = "колокольчик"; return true;
        }

        void LateUpdate()
        {
            bool modal = HudPanel.ModalOpen || FridgeScreen.IsOpen || BestiaryCard.Open;
            if (modal || !Compute(out var pos, out var name)) { Show(false); TargetName = ""; return; }
            Show(true);
            Target = pos; TargetName = name;
            float t = Time.unscaledTime;
            _arrow.position = pos + Vector3.up * (0.22f + Mathf.Abs(Mathf.Sin(t * 4f)) * 0.08f);
            var cam = PlayerRig.I != null ? PlayerRig.I.HeadPos : pos + Vector3.back;
            var d = cam - _arrow.position; d.y = 0;
            if (d.sqrMagnitude > 0.001f) _arrow.rotation = Quaternion.LookRotation(-d.normalized);
            _ring.position = pos + Vector3.up * 0.01f;
            _ring.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(t * 5f));
            _ring.Rotate(0, 60f * Time.unscaledDeltaTime, 0, Space.World);
        }
    }
}
