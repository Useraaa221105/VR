using UnityEngine;

namespace Podz
{
    /// <summary>Мандрагора (−2). Листья над землёй. Спит → Настороже → Схвачена (2 с) → Срезана → корень рукой.</summary>
    public class Mandrake : Monster
    {
        [SerializeField] MonsterGrabPoint _leaves, _root;
        [SerializeField] GameObject _leavesGo, _rootGo;
        public const float HoldTime = 3f;   // пункт 12: окно — 3 с держишь листья до среза
        [SerializeField, Min(0)] float holdTime = HoldTime;
        public const float WiltTime = 3f;  // через сколько завянет срезанный корень
        [SerializeField, Min(0)] float wiltTime = WiltTime;

        protected override void Build()
        {
            Height = 0.3f; Radius = 0.18f;
            _leavesGo = Prim.Cube("Leaves", transform, new Vector3(0, 0.13f, 0), new Vector3(0.3f, 0.25f, 0.3f), new Color(0.25f, 0.75f, 0.2f));
            HitZone.Add(_leavesGo, this, Zone.Leaves);
            _leaves = MonsterGrabPoint.Add(_leavesGo, this);
            _rootGo = Prim.Cube("Root", transform, new Vector3(0, -0.2f, 0), new Vector3(0.18f, 0.3f, 0.18f), new Color(0.85f, 0.65f, 0.4f));
            _rootGo.GetComponent<Renderer>().enabled = false;
            _rootGo.GetComponent<Collider>().enabled = false;
            _root = MonsterGrabPoint.Add(_rootGo, this, false);
            SetState("Спит");
        }

        protected override void OnSavedModelBound()
        {
            if (_leaves == null || _root == null || _leavesGo == null || _rootGo == null ||
                _leaves.Owner != this || _root.Owner != this ||
                !_leavesGo.transform.IsChildOf(transform) || !_rootGo.transform.IsChildOf(transform))
                throw new System.InvalidOperationException("Mandrake: потеряны ссылки Leaves/Root сохранённой модели.");
            SetState("Спит");
        }

        bool _standingInRange;
        void CheckApproach()
        {
            // Дополнительное правило пользователя: стоя в прежней зоне3м, даже медленно.
            // Это звук предупреждения, НЕ атака. Захват/срез не переводим в другое состояние.
            bool standing = DistToPlayer < 3f && !P.Crouched;
            bool entered = standing && !_standingInRange;
            _standingInRange = standing;
            if (!entered || (State != "Спит" && State != "Настороже" && State != "Схвачена")) return;
            if (State != "Схвачена") SetState("Настороже"); // захват в том же кадре не отменяем
            MandrakeCry.Enqueue(transform.position + Vector3.up * .35f, 0f,
                Dungeon != null ? Dungeon.WorldRoot : null, Id, true);
            Hints.Show("Мандрагора кричит: присядь для тихого подхода", HintPri.Action);
        }

        protected override void Tick(float dt)
        {
            CheckApproach();
            switch (State)
            {
                case "Спит":
                    NoGlow();
                    // Вход стоя обработан однократно в CheckApproach; расстояние присевшего не тревожит её.
                    break;
                case "Настороже":
                    NoGlow(); // пункт 9: бить ещё нельзя — зелёный
                    if (StateTime > 2f) SetState("Спит"); // стоя рядом не запускаем новую атаку/крик каждые2с
                    break;
                case "Схвачена":
                    Window(); // пункт 9: листья схвачены — жёлтый, режь тесаком (таймер над ней)
                    WindowTimer(holdTime - StateTime, holdTime);
                    if (_leaves.Held && _leaves.PullUp > 0.25f) { Scream(Dmg.MandrakePulled); Ruin("ты выдернул её, не срезав листья — она закричала и испортилась. Сначала срежь тесаком (клавиша 3)"); return; }
                    if (StateTime > holdTime)
                    {
                        _leaves.ForceDrop();
                        Scream(Dmg.MandrakeScream);
                        SetState("Настороже");
                        Hints.Show($"Не успел! Срезать надо за {holdTime:0} секунды", HintPri.Error);
                    }
                    break;
                case "Срезана":
                    Window();
                    WindowTimer(wiltTime - StateTime, wiltTime);
                    if (StateTime > wiltTime) Ruin("корень слишком долго ждал и завял. Срезал листья — сразу вытягивай корень (E)");
                    break;
            }
        }

        public override HuntStep Hunt()
        {
            switch (State)
            {
                case "Схвачена": return HuntStep.Of(3, 4, $"Держишь! Срежь листья тесаком — {Rmb} у земли!", _leavesGo.transform, true);
                case "Срезана": return HuntStep.Of(4, 4, $"Срезано! {GrabCmd} — вытяни корень!", _rootGo.transform, true, true);
                default: return HuntStep.Of(2, 4, $"Подойди тихо ({CrouchCmd}) и {(IsXR ? "схвати листья рукой" : "ЖМИ E — схвати листья")}", _leavesGo.transform, DistToPlayer < 2f);
            }
        }

        float _lastCry = -99f;
        void Scream(float dmg)
        {
            if(Time.time-_lastCry<3f) return;
            _lastCry=Time.time;
            MandrakeCry.Enqueue(transform.position+Vector3.up*.35f,dmg,Dungeon!=null?Dungeon.WorldRoot:null,Id);
            FX.Sparkle(Center,new Color(.9f,.75f,.35f));
            Hints.Show("А-А-А-А! Крик мандрагоры", HintPri.Danger);
        }

        public override void OnGrab(MonsterGrabPoint p, HandController hand)
        {
            if (p == _leaves && (State == "Спит" || State == "Настороже"))
            {
                SetState("Схвачена");
                Hints.Show($"Держишь! Срежь тесаком горизонтально — {holdTime:0} с (ПК: ПКМ)", HintPri.Window);
            }
            else if (p == _root && State == "Срезана") Capture(ItemId.MandrakeRoot);
        }

        public override void OnGrabRelease(MonsterGrabPoint p, HandController hand, bool pulled)
        {
            if (p != _leaves || State != "Схвачена") return;
            if (pulled) { Scream(Dmg.MandrakePulled); Ruin("ты выдернул её, не срезав листья — она закричала и испортилась. Сначала срежь тесаком (клавиша 3)"); }
            else SetState("Настороже");
        }

        public override void OnHit(HitInfo h, Zone zone)
        {
            if (h.Type == ToolType.Cleaver)
            {
                if (h.Speed < 0.8f || State == "Срезана") return;
                if (State != "Схвачена") { Ruin("ты рубил её прямо в земле. Сначала схвати листья рукой (E), потом режь тесаком"); return; }
                if (!h.Horizontal) { Hints.Show("Срезай горизонтально у земли", HintPri.Error); return; }
                if (!h.Edge) { Hints.Show("Режь лезвием, а не плашмя", HintPri.Error); return; }
                if (h.Speed < 1.5f) { Hints.Show($"Слишком слабо ({h.Speed:0.0} м/с) — нужно 1,5", HintPri.Error); return; }
                _leaves.ForceDrop();
                _leavesGo.SetActive(false);
                _rootGo.transform.localPosition = new Vector3(0, 0.15f, 0);
                _rootGo.GetComponent<Renderer>().enabled = true;
                _rootGo.GetComponent<Collider>().enabled = true;
                _root.Active = true;
                SetState("Срезана");
                Hints.Show("Срезано! Вытяни корень рукой (ПК: E)", HintPri.Window);
            }
            else if (h.IsHuntingTool && h.Speed >= 1f) Ruin("этим инструментом её разбили в щепки. Мандрагору срезают только тесаком (клавиша 3)");
        }
    }
}
