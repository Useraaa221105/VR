using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Podz
{
    /// <summary>
    /// Отладка: F1 — панель (HP, монеты, рюкзак, монстры в реестре/кеше, подписчики событий, память, последний удар);
    /// F2 — аудит кеша монстров; F3 — закончить смену; F4 — +200 монет; F5 — по 3 шт. каждой добычи в ледник;
    /// F6 — монстр перед игроком (в подземелье); F9 — удалить сохранение и начать заново.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        TextMesh _text;
        Renderer _bg;
        public static bool Visible; // скрыта по умолчанию (F1), чтобы не закрывать игру и скриншоты
        // При каждом запуске Play — снова скрыта (в Unity 6 с выключенной перезагрузкой домена static не сбрасывается сам)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Visible = false; }
        float _next;
        string _audit = "";

        public void AttachTo(Transform cam)
        {
            var root = new GameObject("DebugOverlay").transform;
            root.SetParent(cam, false);
            root.localPosition = new Vector3(-0.3f, 0.2f, 1.0f); // по центру слева — не на месте HP/задачи/инструментов
            _bg = Prim.Quad("Bg", root, new Vector3(0.37f, -0.13f, 0.005f), new Vector2(0.76f, 0.29f), new Color(0, 0, 0, 0.8f));
            _bg.sortingOrder = 20; Prim.Hud(_bg); Prim.SetOverlayColor(_bg, new Color(0, 0, 0, 0.8f));
            _text = Prim.Text(root, Vector3.zero, "", 0.017f, new Color(0.7f, 1f, 0.7f), TextAnchor.UpperLeft);
            _text.GetComponent<MeshRenderer>().sortingOrder = 21;
        }

        // Кнопки в существующей F1-панели разработки: только проверка таверны, не порталы этажей.
        void OnGUI()
        {
            if (!Visible || GameFlow.I == null || GameFlow.I.Save == null) return;
            var gf = GameFlow.I;
            if (PlayerRig.I != null && !PlayerRig.I.IsXR) PlayerRig.I.Capture(false);
            float width = Mathf.Min(420f, Screen.width - 20f);
            GUILayout.BeginArea(new Rect(Mathf.Max(10f, Screen.width - width - 10f), 20f, width, Mathf.Max(200f, Screen.height - 40f)), GUI.skin.box);
            GUILayout.Label("ПРОВЕРКА ЭТАПОВ ТАВЕРНЫ · F1 закрыть");
            GUILayout.Label("Сейчас: " + GameFlow.StageTitle(gf.TavernStage));
            GUILayout.Label(gf.TavernPreview ? "Временный режим — обычное сохранение отключено" : "Обычная игра — этап определяется покупками");
            GUILayout.Label(gf.PreviewStatus);
            GUILayout.Label("Смена:300с; цели120/350/700; рецепты2/4/6. Поздние VIP/Король только при обычном условии дракона.");
            string blocker = gf.StageSwitchBlocker;
            bool enabled = GUI.enabled;
            GUI.enabled = enabled && blocker.Length == 0;
            for (int stage = 1; stage <= 3; stage++)
                if (GUILayout.Button(GameFlow.StageTitle(stage), GUILayout.Height(34f))) gf.SelectTavernStage(stage);
            if (GUILayout.Button("Вернуться к обычной игре", GUILayout.Height(34f))) gf.SelectTavernStage(0);
            GUI.enabled = enabled;
            if (blocker.Length > 0) GUILayout.Label("Недоступно: " + blocker);
            GUILayout.Label("Без выдачи денег, продуктов и инструментов. Покупки и охота — в обычной игре; перед проверкой заготовь запасы и приобрети нужную утварь.");
            GUILayout.Label("Результаты/расход запасов временные: при возврате восстанавливаются исходные деньги/ледник. Котлы, миски и продукты сначала убери обычными действиями.");
            GUILayout.Label("1: шахтёры/эльфы. 2:+рыцари; компании отсутствуют. 3:+критики; VIP-подача и напитки не готовы. Выбор НЕ подтверждает готовность этапа.");
            GUILayout.EndArea();
        }

        void Update()
        {
            if (DIn.Down(Key.F1))
            {
                Visible = !Visible;
                if (PlayerRig.I != null && !PlayerRig.I.IsXR) PlayerRig.I.Capture(!Visible);
            }
            if (DIn.Down(Key.F2)) { _audit = MonsterRegistry.Audit(); Debug.Log("[Audit] " + _audit); Hints.Show(_audit, HintPri.Window, 5f); }
            if (DIn.Down(Key.F3) && GameFlow.I != null) GameFlow.I.EndDayNow();
            if (DIn.Down(Key.F4)) { Wallet.Add(200); Hints.Show("Отладка: +200 монет", HintPri.Action); }
            if (DIn.Down(Key.F5)) { foreach (var id in Items.MonsterItems) Inventory.Fridge.Add(id, 3); Hints.Show("Отладка: ледник пополнен", HintPri.Action); }
            if (DIn.Down(Key.F6) && DungeonManager.I != null) DungeonManager.I.DebugSpawnInFront();
            if (DIn.Down(Key.F9) && GameFlow.I != null) GameFlow.I.NewGame();
            if (_text == null) return;
            _text.gameObject.SetActive(Visible);
            _bg.enabled = Visible;
            if (!Visible || Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.25f;
            var P = PlayerRig.I;
            var dm = DungeonManager.I;
            string bp = $"{Inventory.Backpack.Total}/{Inventory.Backpack.Capacity}";
            string hit = Time.time - Tool.LastHitTime < 5f ? Tool.LastHit.ToString() : "—";
            _text.text =
                $"[F1] ОТЛАДКА   FPS {1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f):0}\n" +
                $"HP {(P != null ? P.Health.HP : 0):0}  Монеты {Wallet.Coins}  Рюкзак {bp}  Ледник {Inventory.Fridge.Total}\n" +
                $"Где: {(dm != null && dm.InDungeon ? DungeonManager.GetBiome(dm.Level).Name : "таверна")}\n" +
                $"Монстры: живых {MonsterRegistry.AliveCount}  кеш(словарь) {MonsterRegistry.CacheCount}  список этажа {(dm != null ? dm.LocalCount : 0)}\n" +
                $"Создано {MonsterRegistry.TotalSpawned}  удалено {MonsterRegistry.TotalRemoved}\n" +
                $"Кеш монстров: записей {MonsterCache.EntryCount}  коллайдеров {MonsterCache.ColliderCount}  очищено {MonsterCache.Released}  кулдаунов {Tool.CooldownEntries}\n" +
                $"Подписчики: PlayerDied {GameEvents.PlayerDiedSubscribers}  MonsterDied {GameEvents.MonsterDiedSubscribers}\n" +
                $"Продукты {Ingredient.All.Count}  миски {Bowl.All.Count}  гости {(GuestManager.I != null ? GuestManager.I.Guests.Count : 0)}\n" +
                $"Память (managed) {GC.GetTotalMemory(false) / 1048576f:0.0} МБ\n" +
                $"Удар: {hit}\n" +
                $"F2 аудит · F3 конец смены · F4 +200 · F5 ледник · F6 монстр · F9 сброс\n{_audit}";
        }
    }
}
