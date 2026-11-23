using UnityEngine;
using UnityEngine.SceneManagement;

namespace Podz
{
    public enum DayState { Prep, Open, Closing, Night, Credits }

    /// <summary>
    /// Игровой день: Подготовка (вылазка, магазин) → Открыто (5:00, гости) → Закрытие (доедают) → Итог → Ночь (вылазка, магазин, сон).
    /// Сохранение: после сна, вылазки, покупки, победы над драконом.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow I;
        public SaveData Save { get; private set; }
        public DayState State { get; private set; }
        public int Reputation = 5;
        public float DayTimeLeft { get; private set; }
        public const float DayLength = 300f;
        int _dayCoinsStart;
        string _lastResult = "";

        int _previewStage;
        SaveData _normalSave;
        readonly System.Collections.Generic.List<ItemCount> _normalFridge = new System.Collections.Generic.List<ItemCount>();
        int _normalCoins, _normalReputation, _normalDayCoinsStart;
        DayState _normalState;
        float _normalTime;
        string _normalResult;
        Hearth _normalHearth;
        // Только runtime: SaveData.Stage по-прежнему определяется обычными покупками.
        public bool TavernPreview => _previewStage > 0;
        public int TavernStage => TavernPreview ? _previewStage : Save != null ? Save.Stage : 1;
        public int Goal => TavernStage == 1 ? SavedDungeonAssets.Active?.FirstGoal ?? 120 :
            TavernStage == 2 ? SavedDungeonAssets.Active?.SecondGoal ?? 350 : SavedDungeonAssets.Active?.ThirdGoal ?? 700;
        public static string StageTitle(int stage) => stage == 1 ? "Этап 1 — первые гости" : stage == 2 ? "Этап 2 — вечерний наплыв" : "Этап 3 — пир для знати";
        public string PreviewStatus => TavernStage == 1 ? "Базовый цикл: ещё не проверен на ПК" : TavernStage == 2 ? "Неполный этап: компании и напитки не подключены" : "Неполный этап: напитки, VIP-подача, остывание не подключены";

        public string StageSwitchBlocker
        {
            get
            {
                if (Save == null || Kitchen.I == null) return "Дождись загрузки игры";
                if (State != DayState.Prep && State != DayState.Night) return "Переключение только в подготовке или ночью";
                if (DungeonManager.I != null && DungeonManager.I.InDungeon) return "Сначала вернись в таверну";
                if (DTime.Paused || (Fader.I != null && Fader.I.Busy)) return "Дождись окончания паузы/перехода";
                // Не переносить суп и заготовки между обычным профилем и временной экономикой.
                foreach (var c in Cauldron.All) if (c != null && c.State != SoupState.Empty) return "Сначала опустоши котлы обычным действием";
                foreach (var b in Bowl.All) if (b != null) return "Сначала убери оставшиеся миски";
                foreach (var i in Ingredient.All) if (i != null && !i.Despawning) return "Сначала убери продукты со столов/из рук";
                return "";
            }
        }

        public bool SelectTavernStage(int stage)
        {
            if (stage < 0 || stage > 3) return false; //0 — возврат к обычной игре
            string blocker = StageSwitchBlocker;
            if (blocker.Length > 0) { Hints.Show(blocker, HintPri.Action); return false; }
            if ((stage == 0 && !TavernPreview) || stage == _previewStage) return true;
            GuestManager.I.ClearGuests(false); // отмена старых заданий до применения новых правил
            if (stage > 0 && !TavernPreview)
            {
                _normalSave = Save; Save = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(Save));
                _normalCoins = Wallet.Coins; _normalReputation = Reputation; _normalDayCoinsStart = _dayCoinsStart;
                _normalState = State; _normalTime = DayTimeLeft; _normalResult = _lastResult;
                _normalHearth = Kitchen.I.Hearth.Copy(); _normalFridge.Clear();
                foreach (var kv in Inventory.Fridge.All) _normalFridge.Add(new ItemCount { id = kv.Key, n = kv.Value });
            }
            if (stage == 0)
            {
                Save = _normalSave; _normalSave = null;
                Wallet.Set(_normalCoins); Inventory.Fridge.Clear();
                foreach (var item in _normalFridge) Inventory.Fridge.Add(item.id, item.n);
                _normalFridge.Clear(); Reputation = _normalReputation; _dayCoinsStart = _normalDayCoinsStart;
                State = _normalState; DayTimeLeft = _normalTime; _lastResult = _normalResult;
                Kitchen.I.Hearth.Restore(_normalHearth); _normalHearth = null;
            }
            else { State = DayState.Prep; DayTimeLeft = 0; Reputation = 5; _lastResult = ""; }
            _previewStage = stage;
            Kitchen.I.ApplyStage(TavernStage, Save.dragonDefeated);
            UpdateBoard();
            Debug.Log($"[TavernStage] preview={TavernPreview}; stage={TavernStage}; profileStage={Save.Stage}; coins={Wallet.Coins}; persistentWrite=false");
            Hints.Show(TavernPreview ? $"Проверка: {StageTitle(TavernStage)}. Сохранение отключено; деньги/инструменты не выданы" : "Обычная игра: профиль, деньги и ледник восстановлены", HintPri.Window, 5f);
            return true;
        }

        void Awake() { I = this; }

        void OnDestroy()
        {
            if (DungeonManager.I != null) DungeonManager.I.Returned -= OnReturned;
            if (I == this) I = null;
        }

        public void Init()
        {
            Save = SaveSystem.Load();
            Wallet.Set(Save.coins);
            Inventory.Fridge.Clear();
            foreach (var ic in Save.fridge) Inventory.Fridge.Add(ic.id, ic.n);
            Bestiary.Load(Save.seenMonsters);
            var rig = PlayerRig.I;
            rig.GiveTool(ToolType.Spatula);
            rig.GiveTool(ToolType.Net);
            if (Save.hasCleaver) rig.GiveTool(ToolType.Cleaver);
            if (Save.hasHammer) rig.GiveTool(ToolType.Hammer);
            if (Save.hasShield) rig.GiveTool(ToolType.Shield);
            DungeonManager.I.Returned += OnReturned;
            DungeonManager.I.TavernSpawn = () => Kitchen.I.Spawn;
            DungeonManager.I.TavernYaw = 0f;
            State = Save.finished ? DayState.Night : DayState.Prep;
            Hints.Show(Save.day == 1 && Save.coins == 0
                ? "Тоби: «Ледник пуст! Спустись в подвал (дверь сзади) на −1, добудь слайма и гриб»"
                : $"День {Save.day}. Подготовка: вылазка, магазин или «Открыть таверну»", HintPri.Tutorial, 6f);
        }

        void OnReturned(bool died)
        {
            SaveNow();
            if (!died && State == DayState.Prep && Inventory.Fridge.Total > 0)
                Hints.Show("Тоби: «Отлично! Теперь налей воду в котёл, подготовь продукты и открывай таверну»", HintPri.Tutorial, 6f);
        }

        public void SaveNow()
        {
            if (TavernPreview) return; // никогда не записывать проверочную смену в обычный профиль
            Save.coins = Wallet.Coins;
            Save.seenMonsters = Bestiary.ToList();
            var rig = PlayerRig.I;
            Save.hasCleaver = rig.Has(ToolType.Cleaver);
            Save.hasHammer = rig.Has(ToolType.Hammer);
            Save.hasShield = rig.Has(ToolType.Shield);
            SaveSystem.Save(Save, Inventory.Fridge);
        }

        // ---------------- кнопки таверны ----------------
        public void OpenTavern()
        {
            if (State != DayState.Prep) return;
            if (DungeonManager.I.InDungeon) return;
            State = DayState.Open;
            DayTimeLeft = SavedDungeonAssets.Active?.DaySeconds ?? DayLength;
            Reputation = 5;
            _dayCoinsStart = Wallet.Coins;
            GuestManager.I.BeginDay();
            bool stock = false;
            foreach (var recipe in Recipes.ForStage(TavernStage, Save.dragonDefeated)) if (Recipes.MissingIngredients(recipe).Length == 0) { stock = true; break; }
            if (!stock) Hints.Show("В леднике нет полного набора для супа. Бесплатных продуктов нет: заготовь добычу на охоте до открытия следующей смены", HintPri.Error, 7f);
            Hints.Show("Открыто! Талоны — над окном раздачи. Готовое блюдо — на окно и в колокольчик", HintPri.Tutorial, 5f);
        }

        public void Sleep()
        {
            if (State != DayState.Night) return;
            Fader.I.Run(() =>
            {
                GuestManager.I.ClearGuests(false);
                Save.day++;
                State = DayState.Prep;
                SaveNow();
                Hints.Show($"День {Save.day}. Утро: подготовка", HintPri.Window, 4f);
            });
        }

        public void GoDown(int level)
        {
            if (TavernPreview) { Hints.Show("Это проверка таверны, не выбор этажей. Выйди к обычному профилю для охоты", HintPri.Action); return; }
            if (State == DayState.Open || State == DayState.Closing) { Hints.Show("Тоби: «Сначала закончи смену!»", HintPri.Error); return; }
            DungeonManager.I.Enter(level);
        }

        public void Buy(ToolType t, int price)
        {
            if (TavernPreview) { Hints.Show("Покупки — только в обычной игре. Заверши проверку и вернись к профилю", HintPri.Action); return; }
            var rig = PlayerRig.I;
            if (rig.Has(t)) { Hints.Show("Уже куплено", HintPri.Action); return; }
            if (t == ToolType.Hammer && !rig.Has(ToolType.Cleaver)) { Hints.Show("Сначала купи тесак: это второй игровой этап", HintPri.Action); return; }
            if (t == ToolType.Shield && !rig.Has(ToolType.Hammer)) { Hints.Show("Сначала купи молоток: это третий игровой этап", HintPri.Action); return; }
            if (!Wallet.Spend(price)) { Hints.Show($"Не хватает монет: {Wallet.Coins}/{price}", HintPri.Error); return; }
            rig.GiveTool(t);
            SaveNow();
            string unlock = t == ToolType.Cleaver ? "Открыт этаж −2 и этап кухни 2: жар, помешивание, второй котёл"
                : t == ToolType.Hammer ? "Открыт этаж −3 и этап кухни 3: специи, критики"
                : "Котёл-щит защищает от огня. Открыто логово дракона";
            Hints.Show($"Куплено: {Tools.Name(t)}! {unlock}", HintPri.Window, 6f);
        }

        public void NewGame()
        {
            if (TavernPreview) { Hints.Show("Выйди из выбора этапов перед сбросом обычного профиля", HintPri.Action); return; }
            if (DungeonManager.I.InDungeon) { Hints.Show("Сначала вернись в таверну", HintPri.Error); return; }
            SaveSystem.Delete();
            Inventory.Fridge.Clear(); Inventory.Backpack.Clear();
            Wallet.Set(0);
            Bestiary.Load(new System.Collections.Generic.List<string>());
            DTime.Paused = false;
            Debug.Log("[Game] новая игра — перезагрузка сцены");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void LoseHeart()
        {
            Reputation--;
            if (Reputation > 0) return;
            // провал дня: монеты дня не сохраняются, день заново
            Wallet.Set(_dayCoinsStart);
            GuestManager.I.ClearGuests(false);
            State = DayState.Prep;
            SaveNow();
            Hints.Show("Репутация 0 — день провален. Монеты за день сгорели. Попробуй снова", HintPri.Danger, 6f);
        }

        public void EndDayNow() { if (State == DayState.Open) DayTimeLeft = 0; }

        public void OnDragonDefeated()
        {
            Save.dragonDefeated = true;
            SaveNow();
        }

        public void Credits()
        {
            State = DayState.Credits;
            Save.finished = true;
            SaveNow();
            var cam = PlayerRig.I.Cam.transform;
            var t = Prim.Text(null, cam.position + cam.forward * 2.5f, "ТЫ — ПОВАР ПОДЗЕМЕЛЬЯ!\n\nКороль доволен Драконьим супом.\nСпасибо за игру!\n\n«Подземелье вкусностей»", 0.15f, new Color(1f, 0.85f, 0.3f));
            Prim.FaceTo(t.transform, cam.position);
            Hints.Show("Финал! Можно продолжать играть или начать новую игру", HintPri.Window, 10f);
            Invoke(nameof(AfterCredits), 12f);
        }

        void AfterCredits() { if (State == DayState.Credits) State = DayState.Night; }

        void Update()
        {
            if (Save == null) return;
            if (DungeonManager.I != null && DungeonManager.I.InDungeon) return;
            float dt = DTime.Delta;
            if (State == DayState.Open)
            {
                DayTimeLeft -= dt;
                if (DayTimeLeft <= 60f && DayTimeLeft + dt > 60f) Hints.Show("Последняя минута смены!", HintPri.Action);
                if (DayTimeLeft <= 0) { State = DayState.Closing; Hints.Show("Смена окончена — гости доедают", HintPri.Action); }
            }
            else if (State == DayState.Closing)
            {
                if (GuestManager.I.Guests.Count == 0) ShowResult();
            }
            UpdateBoard();
        }

        void ShowResult()
        {
            var gm = GuestManager.I;
            GuestManager.I.ClearGuests(false);
            int stars = 1 + (gm.Missed == 0 ? 1 : 0) + (gm.Earned >= Goal ? 1 : 0);
            _lastResult = $"Итог дня {Save.day}: обслужено {gm.Served}, упущено {gm.Missed}, +{gm.Earned} мон. (чаевые {gm.Tips}), комбо {gm.BestStreak}, {new string('★', stars)}";
            Hints.Show(_lastResult, HintPri.Window, 8f);
            State = DayState.Night;
            SaveNow();
        }

        void UpdateBoard()
        {
            var k = Kitchen.I;
            if (k == null || k.StatusText == null) return;
            string st = State == DayState.Prep ? "Подготовка" : State == DayState.Open ? $"Открыто {Mathf.FloorToInt(DayTimeLeft / 60)}:{Mathf.FloorToInt(DayTimeLeft % 60):00}" :
                State == DayState.Closing ? "Закрытие" : State == DayState.Night ? "Ночь" : "Финал";
            var gm = GuestManager.I;
            string s = $"День {Save.day} · {StageTitle(TavernStage)}{(TavernPreview ? " [ПРОВЕРКА — НЕ СОХРАНЯЕТСЯ]" : "")}\n{st}\nМонеты: {Wallet.Coins}   Цель дня: {Goal}\nРепутация: {new string('♥', Mathf.Max(0, Reputation))}{new string('·', Mathf.Max(0, 5 - Reputation))}" +
                       $"   Комбо: {(gm != null ? gm.Streak : 0)} (×{(gm != null ? gm.ComboMul : 1f):0.0})\n{_lastResult}";
            if (k.StatusText.text != s) k.StatusText.text = s;
        }
    }
}
