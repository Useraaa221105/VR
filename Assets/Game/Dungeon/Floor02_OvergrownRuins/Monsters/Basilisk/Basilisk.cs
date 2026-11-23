using UnityEngine;

namespace Podz
{
    /// <summary>Василиск (−2). Патруль → Наступает → Замах → Клевок → Клюв застрял 5 с (таймер над ним) → (2 удара по шее) Оглушён 6 с → схватить (E).</summary>
    public class Basilisk : Monster
    {
        [SerializeField] Transform _neck, _head;
        Vector3 _neckRest, _headRest;
        [SerializeField] MonsterGrabPoint _grab;
        [SerializeField] Renderer _eyes;
        float _gaze, _tailCd;
        int _neckHits;
        float _stuckLeft;
        public const float StuckTime = 5f, StunTime = 5f, AfterHitMin = 3f; // пункт 12: у василиска окна по 5 с
        [SerializeField, Min(0)] float stuckTime = StuckTime;
        [SerializeField, Min(0)] float stunTime = StunTime;
        [SerializeField, Min(0)] float afterHitMinimum = AfterHitMin;
        public int NeckHits => _neckHits;
        Vector3 _peckPoint;

        protected override void Build()
        {
            Height = 1.8f; Radius = 0.45f;
            var body = Prim.Cube("Body", transform, new Vector3(0, 0.85f, 0), new Vector3(0.7f, 0.8f, 1.0f), new Color(0.85f, 0.62f, 0.25f));
            HitZone.Add(body, this, Zone.Body);
            Prim.Cube("LegL", transform, new Vector3(-0.2f, 0.25f, 0), new Vector3(0.1f, 0.5f, 0.1f), new Color(0.9f, 0.8f, 0.2f), false);
            Prim.Cube("LegR", transform, new Vector3(0.2f, 0.25f, 0), new Vector3(0.1f, 0.5f, 0.1f), new Color(0.9f, 0.8f, 0.2f), false);
            var neck = Prim.Cube("Neck", transform, new Vector3(0, 1.45f, 0.4f), new Vector3(0.25f, 0.5f, 0.25f), new Color(0.8f, 0.55f, 0.2f));
            HitZone.Add(neck, this, Zone.Neck);
            _neck = neck.transform; _neckRest = _neck.localPosition;
            var head = Prim.Cube("Head", transform, new Vector3(0, 1.78f, 0.55f), new Vector3(0.3f, 0.28f, 0.42f), new Color(0.9f, 0.7f, 0.3f));
            HitZone.Add(head, this, Zone.Head);
            _grab = MonsterGrabPoint.Add(head, this, false);
            _head = head.transform; _headRest = _head.localPosition;
            Prim.Cube("Comb", _head, new Vector3(0, 0.6f, 0), new Vector3(0.3f, 0.5f, 0.6f), new Color(0.9f, 0.1f, 0.1f), false);
            _eyes = Prim.Cube("Eyes", _head, new Vector3(0, 0.15f, 0.45f), new Vector3(1.1f, 0.2f, 0.2f), new Color(0.6f, 1f, 0.3f), false).GetComponent<Renderer>();
            var tail = Prim.Cube("TailSnake", transform, new Vector3(0, 0.9f, -0.9f), new Vector3(0.18f, 0.18f, 0.9f), new Color(0.2f, 0.6f, 0.25f));
            HitZone.Add(tail, this, Zone.Tail);
            SetState("Патруль");
        }

        protected override void OnSavedModelBound()
        {
            if (_neck == null || _head == null || _grab == null || _eyes == null ||
                !_neck.IsChildOf(transform) || !_head.IsChildOf(transform) || _grab.Owner != this)
                throw new System.InvalidOperationException("Basilisk: потеряны ссылки шеи/головы/захвата/глаз.");
            _neckRest = _neck.localPosition; _headRest = _head.localPosition;
            SetState("Патруль");
        }

        protected override void Tick(float dt)
        {
            _tailCd -= dt;
            bool stuck = State == "Клюв застрял" || State == "Оглушён";

            // Взгляд: смотришь в глаза 2 с — окаменение
            if (!stuck && P.Health.CanPetrify && PlayerInFront(0.5f) && CanSeePlayer(10f) && Vector3.Dot(P.HeadForward, (_head.position - P.HeadPos).normalized) > .94f && AttackGate.Request(this))
            {
                Prim.SetEmission(_eyes, new Color(0.6f, 1f, 0.3f) * 2f);
                var toHead = (_head.position - P.HeadPos).normalized;
                if (Vector3.Dot(P.HeadForward, toHead) > 0.94f) _gaze += dt; else _gaze = Mathf.Max(0, _gaze - dt);
                if (_gaze > 0.8f && P.Health.HP > 0 && !Fader.I.Busy) Hints.Show("Не смотри ему в глаза!", HintPri.Danger, 1.5f); // пункт 6: коротко и не во время смерти/перехода
                if (_gaze >= 2f) { _gaze = 0; P.Health.Apply(Status.Petrified, 3f); Hints.Show("Окаменение! (3 с)", HintPri.Danger); }
            }
            else { Prim.SetEmission(_eyes, Color.black); _gaze = 0; if (!IsAttackState(State)) AttackGate.Release(this); }

            // хвост-змея кусает сзади
            if (!stuck && _tailCd <= 0 && DistToPlayer < 1.6f && Vector3.Dot(transform.forward, ToPlayerFlat) < -0.4f)
            {
                _tailCd = 2f;
                DamagePlayer(Dmg.BasiliskTail, "хвост-змея (не заходи сзади)");
            }

            switch (State)
            {
                case "Патруль":
                    NoGlow();
                    Wander(0.6f, dt);
                    if (CanSeePlayer(10f)) SetState("Наступает");
                    break;
                case "Наступает":
                    NoGlow();
                    Face(ToPlayerFlat, dt, 180f);
                    if (DistToPlayer > 14f) SetState("Патруль");
                    else if (DistToPlayer < 2.3f) SetState("Замах");
                    else Move(ToPlayerFlat, 1.3f, dt);
                    break;
                case "Замах":
                    Telegraph();
                    Face(ToPlayerFlat, dt, 240f);
                    if (StateTime > 0.7f) { _peckPoint = transform.position + transform.forward * 1.6f; SetState("Клевок"); }
                    break;
                case "Клевок":
                    LowerHead(Mathf.Clamp01(StateTime / 0.15f));
                    if (StateTime > 0.15f)
                    {
                        if (Flat(P.Feet - _peckPoint).magnitude < 0.8f) { DamagePlayer(Dmg.BasiliskPeck, "клевок василиска"); RaiseHead(); SetState("Пауза"); }
                        else { SetState("Клюв застрял"); Hints.Show("Сейчас! Клюв застрял — руби по шее!", HintPri.Window); }
                    }
                    break;
                case "Клюв застрял":
                    Window();
                    _stuckLeft -= dt;
                    WindowTimer(_stuckLeft, stuckTime);
                    if (_stuckLeft <= 0) { RaiseHead(); _neckHits = 0; SetState("Наступает"); }
                    break;
                case "Пауза":
                    NoGlow();
                    if (StateTime > 1f) SetState("Наступает");
                    break;
                case "Оглушён":
                    Window();
                    WindowTimer(stunTime - StateTime, stunTime);
                    if (StateTime > stunTime) { RaiseHead(); _neckHits = 0; _grab.Active = false; SetState("Наступает"); }
                    break;
            }
        }

        public override HuntStep Hunt()
        {
            switch (State)
            {
                case "Клюв застрял": return HuntStep.Of(3, 4, $"Клюв застрял! Руби тесаком по шее — {Lmb} ({_neckHits}/2)", _neck, true);
                case "Оглушён": return HuntStep.Of(4, 4, $"Оглушён! {GrabCmd} — хватай за голову!", _head, true, true);
                case "Замах": case "Клевок": return HuntStep.Of(2, 4, IsXR ? "Клюёт! Шагни вбок!" : "Клюёт! Шагни вбок (A/D)!");
                default: return HuntStep.Of(2, 4, IsXR ? "Не смотри в глаза. Подойди, жди клевка — и шагни вбок" : "Не смотри в глаза. Подойди, жди клевка — и шагни вбок (A/D)");
            }
        }

        void LowerHead(float t)
        {
            _neck.localPosition = Vector3.Lerp(_neckRest, new Vector3(0, 0.7f, 1.0f), t);
            _head.localPosition = Vector3.Lerp(_headRest, new Vector3(0, 0.25f, 1.45f), t);
        }

        void RaiseHead()
        {
            _neck.localPosition = _neckRest;
            _head.localPosition = _headRest;
        }

        public override void OnHit(HitInfo h, Zone zone)
        {
            if (h.Type == ToolType.Cleaver)
            {
                if (h.Speed < 0.8f) return;
                if (State != "Клюв застрял") { Hints.Show("Выманивай клевок и шагни в сторону", HintPri.Action); return; }
                if (zone != Zone.Neck && zone != Zone.Head) { Hints.Show("Руби по шее!", HintPri.Error); return; }
                if (h.Speed < 2f) { Hints.Show($"Слишком слабо ({h.Speed:0.0} м/с) — нужно 2", HintPri.Error); return; }
                if (!h.Edge) { Hints.Show("Руби лезвием, не плашмя", HintPri.Error); return; }
                _neckHits++;
                _stuckLeft = Mathf.Max(_stuckLeft, afterHitMinimum); // окно НЕ сбрасывается: на 2-й удар всегда есть ≥3 с
                Hints.Show($"Удар по шее {_neckHits}/2 — ещё один!", HintPri.Window);
                if (_neckHits >= 2)
                {
                    SetState("Оглушён");
                    _grab.Active = true;
                    Hints.Show($"Оглушён! Схвати его за голову рукой (ПК: E) — {stunTime:0} с", HintPri.Window);
                }
            }
            else if (h.IsHuntingTool && h.Speed >= 1f) Ruin($"не тот инструмент — мясо окаменело и рассыпалось. Василиска бьют тесаком (клавиша {Tools.Key(ToolType.Cleaver)}) по шее");
        }

        protected override void OnStateChanged(string from, string to)
        {
            if (to == "Клюв застрял") { _stuckLeft = stuckTime; _neckHits = 0; }
        }

        public override void OnGrab(MonsterGrabPoint p, HandController hand)
        {
            if (State == "Оглушён") Capture(ItemId.BasiliskMeat);
        }
    }
}
