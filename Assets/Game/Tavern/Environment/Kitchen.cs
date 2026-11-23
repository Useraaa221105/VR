using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Таверна (greybox). Строит кухню и зал из кубов: стол подготовки (мойка, доска, гриль), панель ледника/кладовой,
    /// котлы, очаг, окно раздачи с колокольчиком, талоны, зал со столами, лавку, табло дня и дверь в подвал.
    /// </summary>
    public class Kitchen : MonoBehaviour
    {
        public static Kitchen I;
        public readonly Hearth Hearth = new Hearth();
        public float FloorY => 0f;
        public Vector3 Spawn => new Vector3(0, 0, 0.3f);
        public Transform TobyHome { get; private set; }
        public Vector3 GuestDoor => new Vector3(10.3f, 0, -2.6f);
        public readonly List<Vector3> Seats = new List<Vector3>();
        public readonly List<Transform> Tables = new List<Transform>();
        public TextMesh TicketText { get; private set; }
        public TextMesh StatusText { get; private set; }

        Bounds _sink, _board, _grill, _window;
        Vector3 _ingredientSpot;
        readonly List<Cauldron> _cauldrons = new List<Cauldron>();
        GameObject _hearthPanel;
        TextMesh _hearthText, _recipeText;
        Transform _bookRows;
        public int BookRows => _bookRows != null ? _bookRows.childCount : 0;
        readonly Dictionary<ItemId, Pressable> _fridgeButtons = new Dictionary<ItemId, Pressable>();
        readonly Dictionary<ToolType, Pressable> _shopButtons = new Dictionary<ToolType, Pressable>();
        readonly Dictionary<int, Pressable> _floorButtons = new Dictionary<int, Pressable>();
        Pressable _openBtn, _sleepBtn;
        static readonly string[] FloorNames = { "", "−1 Пещеры", "−2 Руины", "−3 Крепость", "Логово" };
        int _stage = -1;
        bool _dragon;

        public bool InSink(Vector3 p) => _sink.Contains(p);
        // места на кухне — для подсветки текущего шага рецепта
        public Vector3 SinkPos => _sink.center;
        public Vector3 BoardPos => _board.center;
        public Vector3 GrillPos => _grill.center;
        public Vector3 WindowPos => _window.center;
        public Vector3 BellPos { get; private set; }
        public IReadOnlyList<Cauldron> CauldronsList => _cauldrons;
        /// <summary>Игрок на кухне (а не в зале таверны): за перегородкой с окном раздачи.</summary>
        public bool InKitchen(Vector3 p) { var l = transform.InverseTransformPoint(p); return l.x < 4f && l.x > -4.2f && Mathf.Abs(l.z) < 3.4f; }
        public bool OnBoard(Vector3 p) => _board.Contains(p);
        public bool OnGrill(Vector3 p) => _grill.Contains(p);
        public bool InWindow(Vector3 p) => _window.Contains(p);
        public Vector3 WindowSlot(int table) => new Vector3(3.7f, .95f, .51f - Mathf.Clamp(table, 0, 3) * .34f);
        public int WindowTable(Vector3 p) => Mathf.Clamp(Mathf.RoundToInt((.51f - p.z) / .34f), 0, 3);
        public bool TryReturnSlot(Bowl returning, out Vector3 point)
        {
            for (int i = 0; i < 4; i++)
            {
                point = WindowSlot(i); bool occupied = false;
                foreach (var b in Bowl.All) if (b != null && b != returning && !b.IsHeld && (b.transform.position - point).sqrMagnitude < .28f * .28f) { occupied = true; break; }
                if (!occupied) return true;
            }
            point = default; return false;
        }

        static readonly Dictionary<ItemId, string> Short = new Dictionary<ItemId, string>
        {
            { ItemId.SlimeJelly, "Желе" }, { ItemId.MushroomLegs, "Ножки гриба" }, { ItemId.MandrakeRoot, "Мандрагора" },
            { ItemId.BasiliskMeat, "Василиск" }, { ItemId.ShellfishMeat, "Моллюски" }, { ItemId.MimicMeat, "Мимик" }, { ItemId.DragonMeat, "Дракон" },
            { ItemId.Cabbage, "Капуста" }, { ItemId.Potato, "Картофель" }, { ItemId.Carrot, "Морковь" }, { ItemId.Onion, "Лук" },
        };

        public static Kitchen Create(Transform architecture = null)
        {
            var go = new GameObject("Tavern");
            var k = go.AddComponent<Kitchen>();
            I = k;
            k.Build(architecture);
            return k;
        }

        void OnDestroy() { if (I == this) I = null; }

        Transform Panel(string name, Vector3 pos, float yaw)
        {
            var t = new GameObject(name).transform;
            t.SetParent(transform, false);
            t.localPosition = pos;
            t.localRotation = Quaternion.Euler(0, yaw, 0);
            return t;
        }

        void Build(Transform architecture)
        {
            var t = transform;
            var wood = new Color(0.45f, 0.3f, 0.18f);
            var wall = new Color(0.55f, 0.42f, 0.3f);
            var stone = new Color(0.35f, 0.3f, 0.27f);
            if (architecture == null)
            {
                // ---------- коробка помещения ----------
                Prim.Cube("Floor", t, new Vector3(3.5f, -0.1f, 0), new Vector3(15.4f, 0.2f, 6.8f), new Color(0.38f, 0.27f, 0.18f));
                Prim.Cube("Ceiling", t, new Vector3(3.5f, 3.3f, 0), new Vector3(15.4f, 0.2f, 6.8f), wood * 0.7f);
                Prim.Cube("WallN", t, new Vector3(3.5f, 1.6f, 3.3f), new Vector3(15.4f, 3.2f, 0.2f), wall);
                Prim.Cube("WallS", t, new Vector3(3.5f, 1.6f, -3.3f), new Vector3(15.4f, 3.2f, 0.2f), wall);
                Prim.Cube("WallW", t, new Vector3(-4.1f, 1.6f, 0), new Vector3(0.2f, 3.2f, 6.8f), wall);
                Prim.Cube("WallE", t, new Vector3(11.1f, 1.6f, 0), new Vector3(0.2f, 3.2f, 6.8f), wall);
                // перегородка кухня | зал с окном раздачи
                Prim.Cube("PartA", t, new Vector3(4f, 1.6f, -2.0f), new Vector3(0.2f, 3.2f, 2.6f), wall * 0.9f);
                Prim.Cube("PartB", t, new Vector3(4f, 1.6f, 2.0f), new Vector3(0.2f, 3.2f, 2.6f), wall * 0.9f);
                Prim.Cube("PartLow", t, new Vector3(4f, 0.4f, 0), new Vector3(0.2f, 0.8f, 1.4f), wall * 0.9f);
                Prim.Cube("PartTop", t, new Vector3(4f, 2.6f, 0), new Vector3(0.2f, 1.2f, 1.4f), wall * 0.9f);
                foreach (var p in new[] { new Vector3(-1.5f, 2.9f, 0.5f), new Vector3(1.8f, 2.9f, -1f), new Vector3(6.5f, 2.9f, 0), new Vector3(9.5f, 2.9f, 0) })
                    Prim.PointLight(t, p, new Color(1f, 0.75f, 0.45f), 7f, 1.6f);
            }
            else architecture.SetParent(t, false);

            var preparation = SavedDungeonAssets.Active?.Preparation;
            var worktop = preparation != null && preparation.TryGetComponent<KitchenPreparation>(out var worktopPrefab)
                ? Instantiate(worktopPrefab, t, false) : null;
            if (worktop != null)
            {
                _sink = worktop.WorldBounds(worktop.Sink);
                _board = worktop.WorldBounds(worktop.Board);
                _grill = worktop.WorldBounds(worktop.Grill);
            }
            else
            {
                BuildPreparationGeometry(t);
                _sink = new Bounds(new Vector3(-2.3f, 1.1f, 2.55f), new Vector3(0.7f, 0.45f, 0.6f));
                _board = new Bounds(new Vector3(-0.9f, 1.05f, 2.55f), new Vector3(0.7f, 0.35f, 0.55f));
                _grill = new Bounds(new Vector3(0.6f, 1.05f, 2.55f), new Vector3(0.6f, 0.35f, 0.55f));
            }
            // пункт 2 (баги): урна у стола — упавший/испорченный продукт выбросить и взять новый из ледника
            TrashBin.Create(t, new Vector3(-3.55f, 0, 1.95f));
            Prim.Text(t, new Vector3(-2.3f, 0.97f, 2.3f), "Мойка", 0.04f).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Prim.Text(t, new Vector3(-0.9f, 0.97f, 2.3f), "Доска", 0.04f).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Prim.Text(t, new Vector3(0.6f, 0.97f, 2.3f), "Гриль", 0.04f).transform.localRotation = Quaternion.Euler(90, 0, 0);
            _ingredientSpot = new Vector3(-1.6f, 1.05f, 2.5f);
            var knifeHome = Panel("KnifeHome", new Vector3(-0.35f, 0.94f, 2.4f), 90f); knifeHome.localRotation = Quaternion.Euler(0, 90, 90);
            Tool.Create(ToolType.Knife, knifeHome, true);
            var ladleHome = Panel("LadleHome", new Vector3(1.5f, 0.94f, 2.4f), 90f);
            Tool.Create(ToolType.Ladle, ladleHome, true);

            // ЛЕДНИК — шкаф со стеклянными дверцами у северо-восточной стены: вся добыча фигурками на полках + кладовая овощей
            Fridge.Create(t, new Vector3(2.95f, 0, 2.95f), 180f, _ingredientSpot);

            // книга рецептов на северной стене
            var rb = Panel("RecipeBoard", new Vector3(-0.6f, 2.35f, 3.18f), 0);
            Prim.Cube("Board", rb, new Vector3(0, 0, 0.02f), new Vector3(4.6f, 1.3f, 0.02f), new Color(0.25f, 0.18f, 0.12f), false);
            _recipeText = Prim.Text(rb, new Vector3(0, 0.56f, -0.01f), "", 0.07f, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter);
            _recipeText.fontStyle = FontStyle.Bold;
            _bookRows = new GameObject("BookRows").transform; _bookRows.SetParent(rb, false);

            // ---------- котлы и очаг (запад) ----------
            var serve = Panel("ServeTable", new Vector3(-2.6f, 0, 0.15f), 0);
            Prim.Cube("Table", serve, new Vector3(0, 0.4f, 0), new Vector3(0.5f, 0.8f, 0.5f), wood);
            var spot = new GameObject("ServeSpot").transform; spot.SetParent(serve, false); spot.localPosition = new Vector3(0, 0.95f, 0);
            _cauldrons.Add(Cauldron.Create(t, new Vector3(-3.4f, 0, 1.0f), -90f, spot));
            _cauldrons.Add(Cauldron.Create(t, new Vector3(-3.4f, 0, -0.7f), -90f, spot, false));
            // экраны рецепта на западной стене над котлами + подсветка текущего шага в мире
            foreach (var cc in _cauldrons) RecipeScreen.Create(t, cc, new Vector3(-3.97f, 2.42f, cc.transform.localPosition.z), -90f);
            StepMarker.Create(t);
            _hearthPanel = Panel("Hearth", new Vector3(-3.95f, 0, 0.15f), -90f).gameObject;
            var hp = _hearthPanel.transform;
            Prim.Cube("Stove", hp, new Vector3(0, 0.3f, 0.05f), new Vector3(0.6f, 0.6f, 0.2f), stone, false);
            _hearthText = Prim.Text(hp, new Vector3(0, 1.55f, -0.02f), "", 0.05f, new Color(1f, 0.7f, 0.4f));
            Pressable.Create("Полено +1", hp, new Vector3(0, 1.35f, 0), new Color(0.5f, 0.3f, 0.15f), () => { Hearth.AddLog(); Hints.Show($"Жар {Hearth.Heat}/3", HintPri.Action, 1.5f); }, new Vector3(0.3f, 0.1f, 0.03f), 0.03f);

            // ---------- окно раздачи (восток) ----------
            Prim.Cube("WindowShelf", t, new Vector3(3.7f, 0.85f, 0), new Vector3(0.5f, 0.1f, 1.4f), wood * 1.2f);
            _window = new Bounds(new Vector3(3.7f, 1.05f, 0), new Vector3(0.65f, 0.4f, 1.4f));
            for (int slot = 0; slot < 4; slot++)
            {
                var label = Prim.Text(t, WindowSlot(slot) + new Vector3(-.22f, .12f, 0), $"Стол {slot + 1}", .032f, Color.white);
                label.transform.localRotation = Quaternion.Euler(0, 90f, 0);
            }
            var bell = Panel("Bell", new Vector3(3.55f, 0, 1.0f), 90f);
            BellPos = bell.position + Vector3.up * 1.0f;
            Prim.Cube("Stand", bell, new Vector3(0, 0.45f, 0.05f), new Vector3(0.1f, 0.9f, 0.1f), wood, false);
            Pressable.Create("Колокольчик", bell, new Vector3(0, 1.0f, 0), new Color(0.85f, 0.7f, 0.2f), () => { if (GuestManager.I != null) GuestManager.I.Bell(); }, new Vector3(0.3f, 0.1f, 0.04f), 0.03f);
            var tb = Panel("Tickets", new Vector3(3.88f, 2.48f, 0), 90f);
            Prim.Cube("Board", tb, new Vector3(0, 0, 0.02f), new Vector3(2.75f, 1.12f, 0.02f), new Color(0.62f, 0.45f, 0.28f), false);
            TicketText = Prim.Text(tb, new Vector3(-1.25f, 0, -0.01f), "", 0.06f, new Color(0.15f, 0.1f, 0.05f), TextAnchor.MiddleLeft);
            TicketText.gameObject.SetActive(false); // старые текстовые талоны → карточки заказов (OrderBoard), текст остаётся для отладки
            OrderBoard.Create(tb);

            // ---------- зал ----------
            TobyHome = new GameObject("TobyHome").transform; TobyHome.SetParent(t, false); TobyHome.localPosition = new Vector3(4.6f, 0, 0.6f);
            Vector3[] tables = { new Vector3(6.3f, 0, -1.6f), new Vector3(6.3f, 0, 1.6f), new Vector3(9f, 0, -1.6f), new Vector3(9f, 0, 1.6f) };
            for (int i = 0; i < tables.Length; i++)
            {
                var tt = Panel("Table" + (i + 1), tables[i], 0);
                Prim.Cube("Top", tt, new Vector3(0, 0.75f, 0), new Vector3(1f, 0.08f, 1f), wood * 1.1f, false);
                Prim.Cube("Leg", tt, new Vector3(0, 0.37f, 0), new Vector3(0.15f, 0.74f, 0.15f), wood, false);
                var lbl = Prim.Text(tt, new Vector3(0, 1.5f, 0), $"Стол {i + 1}", 0.1f, new Color(1, 1, 1, 0.7f));
                lbl.transform.localRotation = Quaternion.Euler(0, 90, 0); // читается со стороны кухни (через окно раздачи)
                Tables.Add(tt);
                Seats.Add(tables[i] + new Vector3(0.75f, 0, 0));
            }
            Prim.Cube("GuestDoor", t, GuestDoor + new Vector3(0.6f, 1.1f, 0), new Vector3(0.1f, 2.2f, 1.1f), wood * 0.6f, false);

            // ---------- юг: лавка, табло дня, подвал ----------
            var shopPrefab = SavedDungeonAssets.Active?.Shop;
            var shop = shopPrefab != null ? Instantiate(shopPrefab, t, false) : TavernShop.Build(t);
            shop.transform.SetLocalPositionAndRotation(new Vector3(-2.3f, 0, -3.15f), Quaternion.Euler(0, 180f, 0));
            if (shopPrefab != null) SavedDungeonAssets.RestoreText(shop.gameObject);
            shop.Bind(_shopButtons, SavedDungeonAssets.Active);

            var ctrl = Panel("DayBoard", new Vector3(0.6f, 0, -3.15f), 180f);
            Prim.Cube("Board", ctrl, new Vector3(0, 1.55f, 0.03f), new Vector3(2.1f, 1.3f, 0.02f), new Color(0.2f, 0.15f, 0.1f), false);
            StatusText = Prim.Text(ctrl, new Vector3(-0.95f, 1.85f, -0.01f), "", 0.055f, new Color(1f, 0.92f, 0.75f), TextAnchor.MiddleLeft);
            _openBtn = Pressable.Create("Открыть таверну", ctrl, new Vector3(-0.55f, 1.12f, 0), new Color(0.3f, 0.6f, 0.3f), () => GameFlow.I.OpenTavern(), new Vector3(0.5f, 0.1f, 0.03f), 0.028f);
            _sleepBtn = Pressable.Create("Спать → новый день", ctrl, new Vector3(0.05f, 1.12f, 0), new Color(0.3f, 0.35f, 0.6f), () => GameFlow.I.Sleep(), new Vector3(0.5f, 0.1f, 0.03f), 0.028f);
            Pressable.Create("Новая игра", ctrl, new Vector3(0.65f, 1.12f, 0), new Color(0.5f, 0.25f, 0.25f), () => GameFlow.I.NewGame(), new Vector3(0.45f, 0.1f, 0.03f), 0.028f);

            var door = Panel("Basement", new Vector3(3.0f, 0, -3.15f), 180f);
            Prim.Cube("Door", door, new Vector3(0, 1.05f, 0.04f), new Vector3(1.0f, 2.1f, 0.06f), wood * 0.5f, false);
            Prim.Text(door, new Vector3(0, 2.0f, -0.01f), "Подвал · Охота", 0.06f, new Color(1f, 0.85f, 0.5f));
            _floorButtons[1] = Pressable.Create("Войти в подземелье", door, new Vector3(0,1.2f,0), new Color(.35f,.3f,.45f), () => GameFlow.I.GoDown(1), new Vector3(.9f,.16f,.03f), .035f);
            Pressable.Create("Настройки",door,new Vector3(0,.85f,0),new Color(.25f,.4f,.35f),DungeonSettings.Toggle,new Vector3(.8f,.12f,.03f),.035f);

        }

        public static void BuildPreparationGeometry(Transform t)
        {
            var wood = new Color(.45f,.3f,.18f);
            // ---------- стол подготовки (север) ----------
            Prim.Cube("PrepCounter", t, new Vector3(-0.6f, 0.45f, 2.55f), new Vector3(5f, 0.9f, 0.6f), wood);
            var sink = Prim.Cube("Sink", t, new Vector3(-2.3f, 0.915f, 2.55f), new Vector3(0.6f, 0.03f, 0.45f), new Color(0.3f, 0.55f, 0.9f));
            Prim.SetEmission(sink.GetComponent<Renderer>(), new Color(0.1f, 0.25f, 0.5f));
            Prim.Cube("Board", t, new Vector3(-0.9f, 0.915f, 2.55f), new Vector3(0.6f, 0.03f, 0.4f), new Color(0.8f, 0.65f, 0.45f));
            var grill = Prim.Cube("Grill", t, new Vector3(0.6f, 0.92f, 2.55f), new Vector3(0.5f, 0.04f, 0.45f), new Color(0.15f, 0.12f, 0.1f));
            Prim.SetEmission(grill.GetComponent<Renderer>(), new Color(0.6f, 0.12f, 0.02f));
            // пункт 1 (баги): бортики и заполненная щель за столом — продукт не падает за стол
            Prim.Cube("Backsplash", t, new Vector3(-0.6f, 0.55f, 3.03f), new Vector3(5.0f, 1.1f, 0.36f), wood * 0.8f);
            Prim.Cube("RimFront", t, new Vector3(-0.6f, 0.93f, 2.265f), new Vector3(5.0f, 0.06f, 0.03f), wood * 1.25f);
            Prim.Cube("RimW", t, new Vector3(-3.085f, 0.96f, 2.55f), new Vector3(0.03f, 0.12f, 0.6f), wood * 1.25f);
            Prim.Cube("RimE", t, new Vector3(1.885f, 0.96f, 2.55f), new Vector3(0.03f, 0.12f, 0.6f), wood * 1.25f);
            foreach (var bx in new[] { -0.9f })
            {
                var bc = new Color(0.62f, 0.48f, 0.3f);
                Prim.Cube("BoardRimN", t, new Vector3(bx, 0.94f, 2.765f), new Vector3(0.64f, 0.035f, 0.02f), bc);
                Prim.Cube("BoardRimW", t, new Vector3(bx - 0.31f, 0.94f, 2.55f), new Vector3(0.02f, 0.035f, 0.44f), bc);
                Prim.Cube("BoardRimE", t, new Vector3(bx + 0.31f, 0.94f, 2.55f), new Vector3(0.02f, 0.035f, 0.44f), bc);
            }
            // невидимые стенки над бортиками по бокам и сзади (без рендера — не мешают смотреть на доску)
            InvisibleWall(t, new Vector3(-0.6f, 1.3f, 2.9f), new Vector3(5.0f, 0.6f, 0.1f));
            InvisibleWall(t, new Vector3(-3.12f, 1.15f, 2.55f), new Vector3(0.06f, 0.5f, 0.66f));
            InvisibleWall(t, new Vector3(1.92f, 1.15f, 2.55f), new Vector3(0.06f, 0.5f, 0.66f));
        }

        static void InvisibleWall(Transform t, Vector3 pos, Vector3 size)
        {
            var g = new GameObject("InvisibleWall");
            g.transform.SetParent(t, false); g.transform.localPosition = pos;
            g.AddComponent<BoxCollider>().size = size;
            g.layer = 2; // Ignore Raycast: E-луч проходит насквозь, а продукты отскакивают
        }

        void TakeFromFridge(ItemId id)
        {
            if (Inventory.Fridge.Count(id) <= 0) { Hints.Show($"{Items.Name(id)}: в леднике пусто — добудь в подземелье", HintPri.Error); return; }
            var ing = Ingredient.Spawn(id, _ingredientSpot + Random.insideUnitSphere * 0.05f);
            if (ing != null) { Inventory.Fridge.Take(id); Hints.Show($"{Items.Name(id)}: {Items.Get(id).PrepText}", HintPri.Tutorial); }
        }

        public void ApplyStage(int stage, bool dragon)
        {
            if (stage == _stage && dragon == _dragon) return;
            _stage = stage; _dragon = dragon;
            for (int i = 0; i < _cauldrons.Count; i++)
            {
                _cauldrons[i].SetStage(stage);
                _cauldrons[i].gameObject.SetActive(true);
            }
            _hearthPanel.SetActive(false); // баг-лист, пункт 3: очаг теперь — каменная печь под каждым котлом с кнопкой «Огонь»
            // баг-лист, пункт 5: книга рецептов на стене — КАРТИНКАМИ: блюдо = продукты (значок подготовки) → котёл, цена монетой
            _recipeText.text = $"КНИГА РЕЦЕПТОВ (этап {stage})";
            for (int k = _bookRows.childCount - 1; k >= 0; k--) { var ch = _bookRows.GetChild(k); ch.SetParent(null); Destroy(ch.gameObject); }
            var list = Recipes.ForStage(stage, dragon);
            float rowH = Mathf.Min(0.3f, 1.0f / Mathf.Max(1, list.Count));
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                var row = new GameObject("Row_" + r.Name).transform; row.SetParent(_bookRows, false);
                row.localPosition = new Vector3(0, 0.4f - rowH * (i + 0.5f), 0);
                float sc = rowH / 0.3f;
                row.localScale = Vector3.one * sc;
                KIcons.Dish(row, r, new Vector3(-2.05f, 0, -0.06f), 0.2f);
                var nm = Prim.Text(row, new Vector3(-1.88f, 0.0f, -0.01f), r.Name, 0.06f, new Color(1f, 0.92f, 0.75f), TextAnchor.MiddleLeft);
                Prim.FitWidth(nm, 0.95f);
                float x = -0.8f;
                PicIcons.Cauldron(row, new Vector3(x, 0, -0.012f), 0.13f); PicIcons.Drop(row, new Vector3(x + 0.1f, 0.06f, -0.014f), 0.08f);
                x += 0.22f;
                foreach (var id in r.Items)
                {
                    Prim.Text(row, new Vector3(x, 0, -0.01f), "+", 0.08f, new Color(1f, 0.85f, 0.4f));
                    x += 0.09f;
                    var m = FoodModels.Build(id, row, 0.13f);
                    foreach (var col in m.GetComponentsInChildren<Collider>()) Destroy(col);
                    m.localPosition = new Vector3(x, 0.02f, -0.07f); m.localRotation = Quaternion.Euler(-25, 30, 0);
                    var prep = Items.Get(id).Prep;
                    if (prep != PrepKind.None) { PicIcons.Prep(row, prep, new Vector3(x + 0.19f, 0, -0.012f), 0.11f); x += 0.34f; } else x += 0.14f;
                }
                PicIcons.ArrowR(row, new Vector3(x, 0, -0.012f), 0.08f); x += 0.12f;
                PicIcons.Timer(row, new Vector3(x, 0, -0.012f), 0.15f); x += 0.15f;
                if (stage >= 2) { PicIcons.Flame(row, new Vector3(x, 0, -0.012f), 0.09f); Prim.Text(row, new Vector3(x + 0.05f, -0.07f, -0.01f), r.Heat.ToString(), 0.05f, Color.white); x += 0.13f; }
                PicIcons.ArrowR(row, new Vector3(x, 0, -0.012f), 0.08f); x += 0.12f;
                PicIcons.Bowl(row, new Vector3(x, 0, -0.012f), 0.13f, r.SoupColor); x += 0.18f;
                var coin = Prim.Shape(PrimitiveType.Cylinder, "Coin", row, new Vector3(1.95f, 0, -0.01f), new Vector3(0.1f, 0.004f, 0.1f), new Color(0.95f, 0.78f, 0.2f), false);
                coin.transform.localRotation = Quaternion.Euler(90, 0, 0); Prim.SetEmission(coin.GetComponent<Renderer>(), new Color(0.5f, 0.4f, 0.1f));
                Prim.Text(row, new Vector3(2.02f, 0, -0.01f), r.Price.ToString(), 0.07f, new Color(1f, 0.85f, 0.3f), TextAnchor.MiddleLeft);
            }
        }

        void Update()
        {
            float dt = DTime.Delta;
            var gf = GameFlow.I;
            if (gf == null) return;
            int stage = gf.TavernStage;
            ApplyStage(stage, gf.Save.dragonDefeated);
            if (dt > 0 && gf.State == DayState.Open) Hearth.Tick(dt, stage >= 2);
            if (_hearthText != null) _hearthText.text = $"Очаг: жар {Hearth.Heat}/3 {new string('▮', Hearth.Heat)}";
            foreach (var kv in _shopButtons) kv.Value.SetInteractable(!gf.TavernPreview && PlayerRig.I != null && !PlayerRig.I.Has(kv.Key));
            foreach (var kv in _floorButtons)
            {
                var req = DungeonManager.Requirement(kv.Key);
                bool open = req == null || (PlayerRig.I != null && PlayerRig.I.Has(req.Value));
                kv.Value.SetLabel("Войти в подземелье");
                kv.Value.SetInteractable(!gf.TavernPreview && gf.State != DayState.Open);
            }
            _openBtn.SetInteractable(gf.State == DayState.Prep);
            _sleepBtn.SetInteractable(gf.State == DayState.Night);
        }
    }
}
