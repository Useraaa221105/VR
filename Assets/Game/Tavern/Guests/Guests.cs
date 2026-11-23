using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Podz
{
    public enum GuestType { Miner, Elf, Knight, Critic, VIP, King }

    /// <summary>
    /// Гости и Тоби. Гость: Вход → Заказ (2 с) → Ждёт (терпение) → Несут → Ест 5 с → Платит/Злой → Уходит → Destroy.
    /// Ушедший гость удаляется из списка и уничтожается — список гостей не копит «мёртвые» записи.
    /// </summary>
    public class GuestManager : MonoBehaviour
    {
        public static GuestManager I;
        public readonly List<Guest> Guests = new List<Guest>();
        public int Served, Missed, Earned, Tips, Streak, BestStreak, VipServed;
        public bool KingSpawned;
        float _nextSpawn, _bowlSeenAt = -1;
        Toby _toby;
        public string DeliveryStatus(Guest g) => _toby != null ? _toby.StatusFor(g) : "ждёт";

        void Awake() { I = this; }
        void OnDestroy() { if (I == this) I = null; }

        public void Init()
        {
            _toby = Toby.Create(Kitchen.I.TobyHome.position);
        }

        public void BeginDay()
        {
            ClearGuests(false); // миски, сваренные в подготовке, остаются — их можно сразу подавать
            Served = Missed = Earned = Tips = Streak = BestStreak = VipServed = 0;
            KingSpawned = false;
            _nextSpawn = DTime.Now + 3f;
        }

        public void ClearGuests(bool clearBowls = true)
        {
            if (_toby != null) _toby.ResetAll();
            _bowlSeenAt = -1;
            for (int i = Guests.Count - 1; i >= 0; i--) if (Guests[i] != null) Destroy(Guests[i].gameObject);
            Guests.Clear();
            if (clearBowls) foreach (var b in new List<Bowl>(Bowl.All)) if (b != null) Destroy(b.gameObject);
        }

        public void Remove(Guest g) { Guests.Remove(g); }

        /// <summary>Для автотеста: посадить гостя за стол с заданным заказом.</summary>
        public Guest DebugSpawn(GuestType type, int table, Recipe order)
        {
            var k = Kitchen.I;
            var g = Guest.Create(type, table, k.GuestDoor, k.Seats[table], order, this);
            Guests.Add(g);
            return g;
        }

        int Stage => GameFlow.I.TavernStage;
        int TableCount => Stage == 1 ? 2 : Stage == 2 ? 3 : 4;

        void Update()
        {
            var gf = GameFlow.I;
            if (gf == null) return;
            UpdateTickets();
            if (DTime.Delta <= 0 || (DungeonManager.I != null && DungeonManager.I.InDungeon)) return;
            if (gf.State == DayState.Open && DTime.Now >= _nextSpawn)
            {
                Vector2 iv = Stage == 1 ? new Vector2(20, 25) : Stage == 2 ? new Vector2(14, 18) : new Vector2(12, 15);
                _nextSpawn = DTime.Now + Random.Range(iv.x, iv.y);
                TrySpawn(PickType());
            }
            if (gf.State == DayState.Open && !KingSpawned && gf.Save.dragonDefeated && Stage >= 3 && VipServed >= 3)
            {
                if (TrySpawn(GuestType.King)) { KingSpawned = true; Hints.Show("В таверну вошёл Король подземелья!", HintPri.Window, 5f); }
            }
            if (gf.State != DayState.Open && gf.State != DayState.Closing) return;
            // Тоби сам замечает миски на окне через 2 с (этап 1) или 6 с (этап 2–3)
            bool anyBowl = false;
            foreach (var b in Bowl.All) if (b != null && b.Recipe != null && !b.IsHeld && b.Courier == null && b.ServedTo == null && !b.Returned && Kitchen.I.InWindow(b.transform.position)) { anyBowl = true; break; }
            if (!anyBowl) _bowlSeenAt = -1;
            else if (_bowlSeenAt < 0) _bowlSeenAt = DTime.Now;
            else if (DTime.Now - _bowlSeenAt > (Stage == 1 ? 2f : 6f)) { _bowlSeenAt = -1; Bell(true); }
        }

        GuestType PickType()
        {
            var gf = GameFlow.I;
            float r = Random.value;
            if (Stage >= 3 && gf.Save.dragonDefeated && r < 0.3f) return GuestType.VIP;
            if (Stage >= 3 && r < 0.45f) return GuestType.Critic;
            if (Stage >= 2 && r < 0.6f) return GuestType.Knight;
            return Random.value < 0.5f ? GuestType.Miner : GuestType.Elf;
        }

        bool TrySpawn(GuestType type)
        {
            var k = Kitchen.I;
            for (int i = 0; i < TableCount; i++)
            {
                bool busy = false;
                foreach (var g in Guests) if (g != null && g.Table == i) { busy = true; break; }
                if (busy) continue;
                var recipe = PickRecipe(type);
                if (recipe == null) return false;
                var guest = Guest.Create(type, i, k.GuestDoor, k.Seats[i], recipe, this);
                Guests.Add(guest);
                return true;
            }
            return false; // мест нет — гость не заходит (упрощение очереди)
        }

        Recipe PickRecipe(GuestType type)
        {
            var gf = GameFlow.I;
            var list = Recipes.ForStage(Stage, gf.Save.dragonDefeated);
            if (type == GuestType.VIP || type == GuestType.King) return list.Find(r => r.NeedsDragon);
            list.RemoveAll(r => r.NeedsDragon);
            if (type == GuestType.Miner) list.RemoveAll(r => r.Stage > 1);
            if (type == GuestType.Knight) { var meat = list.FindAll(r => r.Index == 3 || r.Index == 4 || r.Index == 5); if (meat.Count > 0) list = meat; }
            // не больше 30% заказов на то, чего нет в леднике
            var have = list.FindAll(Recipes.HasIngredients);
            if (have.Count > 0 && Random.value < 0.7f) list = have;
            return list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
        }

        /// <summary>Звонок резервирует конкретную миску и стол; само блюдо не клонируется и не уничтожается.</summary>
        public void Bell(bool auto = false)
        {
            var gf = GameFlow.I;
            if (gf == null || (gf.State != DayState.Open && gf.State != DayState.Closing) || (DungeonManager.I != null && DungeonManager.I.InDungeon))
            { if (!auto) Hints.Show("Сначала открой таверну: сейчас подготовка или ночь", HintPri.Action); return; }
            if (!auto) Sfx.Play(SfxId.Bell, Kitchen.I.BellPos);
            if (_toby == null) return;
            int taken = 0, seen = 0;
            foreach (var bowl in new List<Bowl>(Bowl.All))
            {
                if (bowl == null || bowl.IsHeld || bowl.Courier != null || bowl.ServedTo != null || !Kitchen.I.InWindow(bowl.transform.position)) continue;
                seen++;
                if (auto && bowl.Returned) continue; // отказ не создаёт бесконечный автоматический повтор
                if (bowl.Recipe == null) { if (!auto) Hints.Show("Пустая миска не завершает заказ", HintPri.Error); continue; }
                int table = Kitchen.I.WindowTable(bowl.transform.position);
                Guest target = Guests.Find(x => x != null && x.Table == table && x.Waiting);
                if (target == null) { if (!auto) Hints.Show($"Стол {table + 1}: нет ожидающего заказа — переставь миску под нужный номер", HintPri.Error); continue; }
                if (target.Assigned) { if (!auto) Hints.Show($"Стол {table + 1}: Тоби уже получил задание", HintPri.Action); continue; }
                if (target.Order != bowl.Recipe)
                {
                    // Слайд51: один неверный заказ = −1 сердце/−10с. Повторный звонок по той же миске не штрафует снова.
                    if (bowl.MarkWrongOrder(target)) target.WrongOrder();
                    Hints.Show($"Стол {table + 1}: заказан {target.Order.Name}, а на стойке {bowl.Recipe.Name}. Блюдо остаётся на окне", HintPri.Error, 4f);
                    if (gf.State == DayState.Prep) break; // нулевая репутация уже завершила смену
                    continue;
                }
                if (_toby.Deliver(target, bowl)) taken++;
                else if (!auto) Hints.Show("Тоби занят: две доставки уже назначены. Миска остаётся на стойке", HintPri.Action);
            }
            if (!auto && seen == 0 && taken == 0) Hints.Show("Тоби: «Поставь миску на стойку и отпусти её»", HintPri.Action);
        }

        public void OnPaid(Guest g, int coins, int tip, bool perfect)
        {
            if (g == null || !Guests.Contains(g) || GameFlow.I == null || (GameFlow.I.State != DayState.Open && GameFlow.I.State != DayState.Closing && GameFlow.I.State != DayState.Credits) || !g.ClaimPayment()) return; // защита оплаты, не только защита повторного звонка
            Served++; Earned += coins + tip; Tips += tip;
            int before = Wallet.Coins;
            Wallet.Add(coins + tip);
            Debug.Log($"[TavernPayment] table={g.Table + 1} recipe={g.Order.Name} coins={coins} tip={tip} before={before} after={Wallet.Coins}");
            if (perfect)
            {
                Streak++; BestStreak = Mathf.Max(BestStreak, Streak);
                if (Streak % 5 == 0) GameFlow.I.Reputation = Mathf.Min(5, GameFlow.I.Reputation + 1);
            }
            else Streak = 0;
            if (g.Type == GuestType.VIP) VipServed++;
        }

        public void OnAngry(Guest g, bool loseHeart)
        {
            if (g == null || !Guests.Contains(g) || GameFlow.I == null || (GameFlow.I.State != DayState.Open && GameFlow.I.State != DayState.Closing && GameFlow.I.State != DayState.Credits)) return;
            Missed++; Streak = 0;
            if (loseHeart) GameFlow.I.LoseHeart();
        }

        public float ComboMul => Streak < 3 ? 1f : Mathf.Min(1.5f, 1.2f + 0.1f * ((Streak - 3) / 3));

        void UpdateTickets()
        {
            var k = Kitchen.I;
            if (k == null || k.TicketText == null) return;
            var sb = new StringBuilder("ТАЛОНЫ\n");
            int n = 0;
            foreach (var g in Guests)
            {
                if (g == null || !g.HasOrdered) continue;
                n++;
                string bar = new string('█', Mathf.CeilToInt(g.PatienceFrac * 8)).PadRight(8, '·');
                sb.Append($"Стол {g.Table + 1}: {g.Order.Name} {(g.Assigned ? "(" + DeliveryStatus(g) + ")" : g.Eating ? "(ест)" : bar)}\n");
            }
            if (n == 0) sb.Append(GameFlow.I != null && GameFlow.I.State == DayState.Open ? "ждём гостей…" : "таверна закрыта");
            var s = sb.ToString();
            if (k.TicketText.text != s) k.TicketText.text = s;
        }
    }
}
