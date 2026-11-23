using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    public class Guest : MonoBehaviour
    {
        [field: SerializeField] public GuestType Type { get; private set; }
        public int Table { get; private set; }
        public Recipe Order { get; private set; }
        public bool HasOrdered => _state >= 2 && _state <= 4;
        public bool Waiting => _state == 2;
        public bool Eating => _state == 4;
        public bool Assigned;
        public bool DishInTransit;
        bool _paid;
        Bowl _dish;
        public bool ClaimPayment() { if (_state != 4 || _paid) return false; _paid = true; return true; }
        /// <summary>Последнее разрешение: +8с один раз обычным гостям этапов1–3; не VIP/Королю.</summary>
        public const float PatienceBonus = 8f;
        /// <summary>К5: меньше этой доли терпения — «скоро уйдёт»: карточка мигает красным, тренер «Поторопись!».</summary>
        public const float HurryFrac = 0.25f;
        /// <summary>Терпение без прибавки К2 и полное терпение (с прибавкой), секунды.</summary>
        public float BasePatience { get; private set; }
        public float PatienceMax => _patienceMax;
        public bool Hurry => Waiting && !DishInTransit && PatienceFrac < HurryFrac;
        public void DebugPatience(float frac) { _patience = _patienceMax * Mathf.Clamp01(frac); }
        public float PatienceFrac => _patienceMax > 0 ? Mathf.Clamp01(_patience / _patienceMax) : 0;

        int _state; // 0 вход, 1 заказ, 2 ждёт, 3 несут, 4 ест, 5 уходит
        float _t, _patience, _patienceMax;
        Vector3 _seat, _door;
        GuestManager _mgr;
        [SerializeField] TextMesh _bubble;
        [SerializeField] Transform _bar;
        int _pay, _tip;
        bool _perfect;

        public static string TypeName(GuestType t)
        {
            switch (t) { case GuestType.Miner: return "Шахтёр"; case GuestType.Elf: return "Эльф-маг"; case GuestType.Knight: return "Рыцарь"; case GuestType.Critic: return "Гурман-критик"; case GuestType.VIP: return "VIP"; default: return "Король подземелья"; }
        }

        static Color TypeColor(GuestType t)
        {
            switch (t) { case GuestType.Miner: return new Color(0.5f, 0.4f, 0.3f); case GuestType.Elf: return new Color(0.4f, 0.7f, 0.5f); case GuestType.Knight: return new Color(0.6f, 0.6f, 0.7f); case GuestType.Critic: return new Color(0.6f, 0.3f, 0.6f); case GuestType.VIP: return new Color(0.9f, 0.75f, 0.2f); default: return new Color(1f, 0.85f, 0.1f); }
        }

        public static Guest Create(GuestType type, int table, Vector3 door, Vector3 seat, Recipe order, GuestManager mgr)
        {
            var prefab = SavedDungeonAssets.Active?.FindGuest(type);
            var g = prefab != null ? Instantiate(prefab, door, Quaternion.identity) : NewVisual(type);
            g.transform.position = door;
            g.Table = table; g.Order = order; g._seat = seat; g._door = door; g._mgr = mgr;
            if (prefab != null) SavedDungeonAssets.RestoreText(g.gameObject);
            float basePat = type == GuestType.King ? 90 : type == GuestType.VIP ? 70 : GameFlow.I.TavernStage == 1 ? Random.Range(55f, 65f) : GameFlow.I.TavernStage == 2 ? Random.Range(40f, 50f) : Random.Range(35f, 45f);
            if (type == GuestType.Miner) basePat += 5; if (type == GuestType.Knight) basePat -= 5;
            g.BasePatience = basePat;
            int stage = GameFlow.I.TavernStage;
            if (stage >= 1 && stage <= 3 && type != GuestType.VIP && type != GuestType.King) basePat += PatienceBonus;
            g._patienceMax = g._patience = basePat;
            return g;
        }

        static Guest NewVisual(GuestType type)
        {
            var root = new GameObject("Guest_" + type);
            var guest = root.AddComponent<Guest>();
            guest.Type = type;
            guest.BuildVisual(type);
            return guest;
        }

#if UNITY_EDITOR
        public static Guest BuildForEditor(GuestType type) => NewVisual(type);
#endif

        void BuildVisual(GuestType type)
        {
            var c = TypeColor(type);
            Prim.Cube("Body", transform, new Vector3(0, 0.6f, 0), new Vector3(0.45f, 1.2f, 0.35f), c, false);
            Prim.Cube("Head", transform, new Vector3(0, 1.4f, 0), new Vector3(0.3f, 0.3f, 0.3f), new Color(0.95f, 0.8f, 0.65f), false);
            if (type == GuestType.King || type == GuestType.VIP) Prim.Cube("Crown", transform, new Vector3(0, 1.62f, 0), new Vector3(0.28f, 0.12f, 0.28f), new Color(1f, 0.85f, 0.1f), false);
            _bubble = Prim.Text(transform, new Vector3(0, 2.0f, 0), TypeName(type), 0.07f, Color.white);
            var barRoot = new GameObject("BarRoot").transform;
            barRoot.SetParent(transform, false);
            barRoot.localPosition = new Vector3(0, 1.8f, 0);
            Prim.Cube("BarBg", barRoot, Vector3.zero, new Vector3(0.5f, 0.05f, 0.02f), new Color(0.15f, 0.15f, 0.15f), false);
            _bar = Prim.Cube("Bar", barRoot, new Vector3(0, 0, -0.012f), new Vector3(0.5f, 0.05f, 0.02f), Color.green, false).transform;
        }

        void Update()
        {
            var P = PlayerRig.I;
            if (P != null) { Prim.FaceTo(_bubble.transform, P.HeadPos); Prim.FaceTo(_bar.parent, P.HeadPos); }
            float dt = DTime.Delta;
            if (dt <= 0 || (DungeonManager.I != null && DungeonManager.I.InDungeon)) return;
            _t += dt;
            // смена закрыта: кто ещё не успел заказать — тихо уходит без штрафа
            if (_state <= 1 && GameFlow.I != null && GameFlow.I.State == DayState.Closing)
            {
                _bubble.text = "Закрыто? Ну, завтра зайду";
                _state = 5;
            }
            switch (_state)
            {
                case 0:
                    if (Walk(_seat, dt)) { _state = 1; _t = 0; }
                    break;
                case 1:
                    if (_t > 2f) { _state = 2; _t = 0; _bubble.text = $"{TypeName(Type)}\n«{Order.Name}»"; }
                    break;
                case 2:
                    if (!DishInTransit) _patience -= dt; // пауза терпения только с фактического получения миски Тоби
                    UpdateBar();
                    if (_patience <= 0)
                    {
                        _bubble.text = "Ну и сервис!";
                        Hints.Show($"Стол {Table + 1}: гость ушёл злым (−❤)", HintPri.Error);
                        _mgr.OnAngry(this, true);
                        _state = 5;
                    }
                    else if (_patience < _patienceMax * 0.2f) Hints.Show($"Стол {Table + 1} сейчас уйдёт!", HintPri.Danger);
                    break;
                case 4:
                    if (_t > 5f) Pay();
                    break;
                case 5:
                    if (Walk(_door, dt)) { _mgr.Remove(this); Destroy(gameObject); }
                    break;
            }
        }

        void UpdateBar()
        {
            float f = PatienceFrac;
            _bar.localScale = new Vector3(0.5f * f, 0.05f, 0.02f);
            Prim.SetColor(_bar.GetComponent<Renderer>(), f > 0.5f ? Color.green : f > 0.2f ? Color.yellow : Color.red);
        }

        bool Walk(Vector3 target, float dt)
        {
            var d = target - transform.position; d.y = 0;
            if (d.magnitude < 0.05f) return true;
            transform.position += Vector3.ClampMagnitude(d.normalized * 1.4f * dt, d.magnitude);
            transform.rotation = Quaternion.LookRotation(d.normalized);
            return false;
        }

        /// <summary>Тоби принёс блюдо.</summary>
        public bool Receive(Recipe r, int stars, bool halfPrice)
        {
            if (_state != 2 || r == null || r != Order) return false;
            float frac = PatienceFrac;
            if (Type == GuestType.Critic && stars < 3)
            {
                _bubble.text = "Не ★★★ — не буду платить";
                _mgr.OnAngry(this, false);
                _state = 5; return false;
            }
            if (Type == GuestType.King && stars < 3)
            {
                _bubble.text = "Королю — только ★★★!";
                Hints.Show("Король ждёт Драконий суп ★★★ — свари ещё раз", HintPri.Error);
                Assigned = false; return false;
            }
            float price = r.Price * (stars == 1 ? 0.5f : 1f) * (halfPrice ? 0.5f : 1f) * _mgr.ComboMul;
            if (Type == GuestType.Critic) price *= 2f;
            _perfect = stars == 3 && frac > 0.5f && !halfPrice;
            _pay = Mathf.RoundToInt(price);
            _tip = _perfect ? Mathf.RoundToInt(price * 0.2f) : 0;
            _state = 4; _t = 0;
            _bubble.text = $"Ням! {new string('★', stars)}";
            return true;
        }

        public void WrongOrder()
        {
            if (!Waiting) return;
            _patience = Mathf.Max(0, _patience - 10f);
            if (_patience <= 0) { _bubble.text = "Неверный заказ — ухожу"; _state = 5; } // не повторять штраф как timeout на следующем кадре
            _mgr.OnAngry(this, true);
        }

        public void SetDish(Bowl b) { _dish = b; }
        void OnDestroy()
        {
            // Не терять существующий предмет при отмене смены/заказа.
            if (_dish != null && Kitchen.I != null) _dish.ReleaseDelivery(Kitchen.I.WindowSlot(Table), true);
            _dish = null;
        }

        void Pay()
        {
            if (_state != 4 || _paid) return;
            _mgr.OnPaid(this, _pay, _tip, _perfect);
            _bubble.text = $"+{_pay}{(_tip > 0 ? $" +{_tip} чаевые" : "")}";
            Hints.Show($"Стол {Table + 1}: +{_pay + _tip} монет", HintPri.Action);
            _state = 5;
            if (_dish != null) { Bowl.All.Remove(_dish); Destroy(_dish.gameObject); _dish = null; }
            if (Type == GuestType.King && GameFlow.I != null) GameFlow.I.Credits();
        }
    }
}
