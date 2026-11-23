using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Мимик (−3). Притворяется сундуком: крышка «дышит» раз в 6–8 с, на нём нет пыли.
    /// Медленно постучать (&lt;0.5 м/с) → раскрыт. Открыть рукой → засада (20 урона).
    /// 3 удара молотком (≥2 м/с) → «Треснул» на 4 с → схватить ногу → мясо мимика.
    /// </summary>
    public class Mimic : Monster
    {
        [SerializeField] Transform _lid, _clawL, _clawR;
        [SerializeField] Transform[] _legs;
        [SerializeField] MonsterGrabPoint _leg;
        [SerializeField] Pressable _open;
        Vector3 _lidRest;
        float _nextBreath;
        int _hits, _swings;
        public int Hits => _hits;
        [SerializeField, Min(.01f)] float crackTime = 3f;
        Vector3 _fleeDir;

        protected override void OnSavedModelBound()
        {
            if (_lid == null || _clawL == null || _clawR == null || _leg == null || _open == null || _legs == null || _legs.Length != 6 || _leg.Owner != this)
                throw new System.InvalidOperationException("Мимик: потеряны ссылки сохранённой модели.");
            foreach (var leg in _legs) if (leg == null) throw new System.InvalidOperationException("Мимик: потеряна нога.");
            _lidRest = _lid.localPosition; _open.OnPress = Ambush;
            _nextBreath = DTime.Now + Random.Range(6f, 8f); SetState("Маскировка");
        }

        protected override void Build()
        {
            Height = 0.55f; Radius = 0.4f;
            var wood = new Color(0.45f, 0.3f, 0.16f);
            var box = Prim.Cube("Box", transform, new Vector3(0, 0.22f, 0), new Vector3(0.7f, 0.44f, 0.45f), wood);
            _lid = Prim.Cube("Lid", transform, new Vector3(0, 0.5f, 0), new Vector3(0.72f, 0.12f, 0.47f), wood * 1.1f).transform;
            _lidRest = _lid.localPosition;
            Prim.Cube("Lock", transform, new Vector3(0, 0.42f, 0.235f), new Vector3(0.08f, 0.1f, 0.02f), new Color(0.8f, 0.7f, 0.2f), false);
            HitZone.Add(box, this, Zone.Body);
            HitZone.Add(_lid.gameObject, this, Zone.Lid);
            _legs = new Transform[6];
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector3(i % 2 == 0 ? -0.3f : 0.3f, -0.15f, 0.15f - (i / 2) * 0.15f);
                _legs[i] = Prim.Cube("Leg" + i, transform, p, new Vector3(0.08f, 0.3f, 0.08f), new Color(0.75f, 0.55f, 0.45f), i == 0).transform;
                _legs[i].gameObject.SetActive(false);
            }
            _leg = MonsterGrabPoint.Add(_legs[0].gameObject, this, false);
            _clawL = Prim.Cube("ClawL", transform, new Vector3(-0.25f, 0.35f, 0.3f), new Vector3(0.1f, 0.08f, 0.25f), new Color(0.85f, 0.4f, 0.35f), false).transform;
            _clawR = Prim.Cube("ClawR", transform, new Vector3(0.25f, 0.35f, 0.3f), new Vector3(0.1f, 0.08f, 0.25f), new Color(0.85f, 0.4f, 0.35f), false).transform;
            _clawL.gameObject.SetActive(false); _clawR.gameObject.SetActive(false);
            _open = Pressable.Create("Открыть", transform, new Vector3(0, 0.3f, 0.26f), wood * 0.8f, Ambush, new Vector3(0.2f, 0.08f, 0.02f), 0.03f);
            _nextBreath = DTime.Now + Random.Range(6f, 8f);
            SetState("Маскировка");
        }

        void Reveal(bool stand)
        {
            if (_open != null) { MonsterCache.Forget(_open.GetComponent<Collider>()); Destroy(_open.gameObject); _open = null; }
            foreach (var l in _legs) l.gameObject.SetActive(true);
            _clawL.gameObject.SetActive(true); _clawR.gameObject.SetActive(true);
            transform.position = new Vector3(transform.position.x, Room.min.y + 0.3f, transform.position.z);
            _lid.localPosition = _lidRest + new Vector3(0, 0.12f, -0.05f);
            if (stand) SetState("Раскрыт");
        }

        void Ambush()
        {
            if (State != "Маскировка") return;
            Reveal(false);
            DamagePlayer(Dmg.MimicAmbush, "засада мимика");
            Hints.Show("Это мимик! На сундуке не было пыли…", HintPri.Danger);
            SetState("Раскрыт");
        }

        protected override bool Disguised => State == "Маскировка";

        protected override void Tick(float dt)
        {
            switch (State)
            {
                case "Маскировка":
                    NoGlow();
                    // «дыхание» крышки — единственная подсказка
                    if (DTime.Now > _nextBreath) _nextBreath = DTime.Now + Random.Range(6f, 8f);
                    float k = Mathf.Clamp01(1f - Mathf.Abs(DTime.Now - _nextBreath + 0.4f) / 0.4f);
                    _lid.localPosition = _lidRest + Vector3.up * 0.03f * k;
                    break;
                case "Раскрыт":
                    NoGlow();
                    Face(ToPlayerFlat, dt, 200f);
                    if (DistToPlayer > 6f) { SetState("Бегство"); _fleeDir = -ToPlayerFlat; break; }
                    if (DistToPlayer < 1.2f && StateTime > 0.8f) SetState("Замах");
                    else if (DistToPlayer >= 1.2f) Move(ToPlayerFlat, 1.6f, dt);
                    break;
                case "Замах":
                    Telegraph();
                    _clawL.localPosition = new Vector3(-0.25f, 0.6f, 0.2f);
                    _clawR.localPosition = new Vector3(0.25f, 0.6f, 0.2f);
                    if (StateTime > 0.7f)
                    {
                        _clawL.localPosition = new Vector3(-0.2f, 0.3f, 0.45f);
                        _clawR.localPosition = new Vector3(0.2f, 0.3f, 0.45f);
                        if (DistToPlayer < 1.4f && PlayerInFront(0.3f)) DamagePlayer(Dmg.MimicClaws, "клешни мимика");
                        _swings++;
                        SetState(_swings % 3 == 0 ? "Бегство" : "Раскрыт");
                        if (State == "Бегство") _fleeDir = -ToPlayerFlat;
                    }
                    break;
                case "Бегство":
                    NoGlow();
                    Face(_fleeDir, dt, 300f);
                    if (!Move(_fleeDir, 2.2f, dt)) _fleeDir = Quaternion.Euler(0, Random.Range(90f, 270f), 0) * _fleeDir;
                    if (StateTime > 3f) SetState("Раскрыт");
                    break;
                case "Треснул":
                    Window();
                    WindowTimer(crackTime - StateTime, crackTime);
                    if (_leg.Held && _leg.PullDistance > 0.2f) { Capture(ItemId.MimicMeat); return; }
                    if (StateTime > crackTime)
                    {
                        _leg.ForceDrop(); _leg.Active = false;
                        _hits = 0;
                        Tint(1f);
                        SetState("Раскрыт");
                        Hints.Show("Мимик опомнился — снова 3 удара", HintPri.Error);
                    }
                    break;
            }
        }

        public override HuntStep Hunt()
        {
            switch (State)
            {
                case "Маскировка": return default; // сундук не выдаём: мимика надо распознать самому (нет пыли, «дышит»)
                case "Треснул": return HuntStep.Of(3, 3, $"Треснул! {GrabCmd} — хватай за ногу!", _legs[0], true, true);
                case "Замах": return HuntStep.Of(2, 3, $"Клешни! Шаг назад ({_hits}/3)");
                default: return HuntStep.Of(2, 3, $"Бей молотком {Lmb} — 3 раза ({_hits}/3)", _lid, DistToPlayer < 2.2f);
            }
        }

        public override void OnHit(HitInfo h, Zone zone)
        {
            if (State == "Маскировка")
            {
                if (h.Speed < 0.5f) { Reveal(true); Hints.Show("Звонко! Внутри что-то живое — это мимик!", HintPri.Window); return; }
                if (h.Type != ToolType.Hammer) { Reveal(true); Hints.Show("Сундук ожил!", HintPri.Danger); return; }
                Reveal(true);
            }
            if (State == "Треснул") return;
            if (h.Type == ToolType.Hammer)
            {
                if (h.Speed < 2f) { if (h.Speed > 0.8f) Hints.Show($"Слабо ({h.Speed:0.0} м/с) — нужно 2", HintPri.Error); return; }
                _hits++;
                Hints.Show($"Раковина трещит {_hits}/3", HintPri.Window);
                if (_hits >= 3)
                {
                    SetState("Треснул");
                    Tint(0.6f);
                    _leg.Active = true;
                    Hints.Show($"Треснул! Хватай за ногу (ПК: E) — {crackTime:0} секунды", HintPri.Window);
                }
            }
            else if (h.Type == ToolType.Cleaver && h.Speed >= 1.5f) Ruin("острый тесак разрубил панцирь вместе с мясом. Мимика простукивают и бьют молотком (клавиша 4)");
            else if (h.IsHuntingTool && h.Speed >= 1f) Hints.Show("Отскок — раковину берёт только молоток", HintPri.Error);
        }

        public override void OnGrab(MonsterGrabPoint p, HandController hand)
        {
            if (State == "Треснул" && (P == null || !P.IsXR)) Capture(ItemId.MimicMeat);
        }

        public override void OnGrabRelease(MonsterGrabPoint p, HandController hand, bool pulled)
        {
            if (State == "Треснул" && pulled) Capture(ItemId.MimicMeat);
        }

        protected override void OnCleanup()
        {
            _open = null; _leg = null; _legs = null; _lid = null; _clawL = null; _clawR = null;
        }
    }

    /// <summary>Настоящий сундук: пыльный, внутри монеты. Не монстр.</summary>

}
