using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Ходячий гриб (−1). Куб-ножка + красная шляпка. Видит игрока → убегает зигзагом.
    /// Покой → Бегство → Устал 3 с / Загнан в угол → (сачок сверху) добыт.
    /// </summary>
    public class Mushroom : Monster
    {
        float _fleeTime, _zig, _zigSign = 1f, _blocked, _near;
        float _escapeBias; Vector3 _escapeDir; float _repath;
        public Vector3 EscapeDirection => _escapeDir;
        static readonly RaycastHit[] _escapeHits = new RaycastHit[32];
        [SerializeField] Transform _cap;
        public const float TiredTime = 3f; // пункт 12: окно «Устал» 3 с
        [SerializeField, Min(0)] float tiredTime = TiredTime;

        protected override void Build()
        {
            Height = 0.7f; Radius = 0.25f;
            _escapeBias = Random.Range(-80f,80f); _zigSign = Random.value < .5f ? -1f : 1f;
            var stem = Prim.Cube("Stem", transform, new Vector3(0, 0.2f, 0), new Vector3(0.26f, 0.4f, 0.26f), new Color(0.95f, 0.88f, 0.7f));
            var cap = Prim.Cube("Cap", transform, new Vector3(0, 0.5f, 0), new Vector3(0.6f, 0.22f, 0.6f), new Color(0.85f, 0.15f, 0.12f));
            HitZone.Add(stem, this, Zone.Body);
            HitZone.Add(cap, this, Zone.Head); _cap = cap.transform;
            Prim.Cube("Dot1", transform, new Vector3(0.15f, 0.62f, 0.1f), Vector3.one * 0.08f, Color.white, false);
            Prim.Cube("Dot2", transform, new Vector3(-0.12f, 0.62f, -0.12f), Vector3.one * 0.08f, Color.white, false);
        }

        protected override void OnSavedModelBound()
        {
            if (_cap == null || !_cap.IsChildOf(transform))
                throw new System.InvalidOperationException("Mushroom: потеряна ссылка Cap сохранённой модели.");
            _escapeBias = Random.Range(-80f, 80f); _zigSign = Random.value < .5f ? -1f : 1f;
        }

        bool Detects()
        {
            float range = (P.Crouched && P.Speed < 1.3f) ? 2f : 5f; // присел и идёшь медленно — заметит только с 2 м
            return DistToPlayer < range && CanSeePlayer(range);
        }

        protected override void Tick(float dt)
        {
            switch (State)
            {
                case "Покой":
                    NoGlow();
                    Wander(0.5f, dt);
                    if (Detects()) StartFlee();
                    break;

                case "Бегство":
                {
                    NoGlow();
                    _fleeTime += dt;
                    _zig -= dt;
                    if (_zig <= 0) { _zig = 0.6f; _zigSign = -_zigSign; }
                    _repath -= dt;
                    if (_repath <= 0) { _escapeDir = ChooseEscape(); _repath = .2f + (Id % 4) * .025f; }
                    var dir = _escapeDir;
                    bool moved = Move(dir, 2.8f, dt);
                    if (!moved) { _repath = 0; _escapeBias = Mathf.Repeat(_escapeBias + _zigSign * 47f + 180f, 360f) - 180f; }
                    _blocked = moved ? 0f : _blocked + dt;
                    Face(dir, dt, 720f);
                    if (_blocked > 0.5f) { SetState("Загнан в угол"); Hints.Show("Гриб загнан в угол! Накрой сачком сверху", HintPri.Window); }
                    else if (_fleeTime > 6f) { SetState("Устал"); Hints.Show($"Гриб устал — {tiredTime:0} секунды! Накрой сачком сверху", HintPri.Window); }
                    else if (DistToPlayer > 10f) SetState("Покой");
                    break;
                }

                case "Устал":
                    Window();
                    WindowTimer(tiredTime - StateTime, tiredTime);
                    if (StateTime > tiredTime) { if (DistToPlayer < 5f) StartFlee(); else SetState("Покой"); }
                    break;

                case "Загнан в угол":
                    Window();
                    WindowTimer(tiredTime - StateTime, tiredTime);
                    if (StateTime > tiredTime) StartFlee();
                    else if (DistToPlayer > 4f) SetState("Покой");
                    break;
            }

            // споры, если стоять вплотную > 3 с
            _near = DistToPlayer < 1.0f ? _near + dt : 0f;
            if (_near > 3f)
            {
                _near = 0f;
                if (AttackGate.MayHurt(this) && CanSeePlayer(2f)) P.Health.Apply(Status.Spores, 4f);
                DamagePlayer(Dmg.MushroomSpores, "споры гриба");
                Hints.Show("Споры! Не стой вплотную к грибу", HintPri.Danger);
                if (State == "Загнан в угол") StartFlee();
            }
        }

        public override HuntStep Hunt()
        {
            switch (State)
            {
                case "Устал": case "Загнан в угол": return HuntStep.Of(3, 3, $"СЕЙЧАС! Накрой гриба сачком сверху — {Lmb}!", _cap, true);
                case "Бегство": return HuntStep.Of(2, 3, $"Гони гриба в угол — или жди: устанет через {Mathf.Max(1, Mathf.CeilToInt(6f - _fleeTime))} с", _cap, false);
                default: return HuntStep.Of(2, 3, $"Подкрадись: {CrouchCmd} — и накрой сачком сверху ({Lmb})", _cap, DistToPlayer < 2.2f);
            }
        }

        Vector3 ChooseEscape()
        {
            Vector3 away = -ToPlayerFlat, separation = Vector3.zero;
            foreach (var m in MonsterRegistry.Alive)
            {
                if (m == this || m == null || m.Kind != MonsterKind.Mushroom) continue;
                var delta = Flat(transform.position - m.transform.position);
                float dd = delta.sqrMagnitude;
                if (dd < 4f) separation += dd > .001f ? delta / dd : Quaternion.Euler(0, Id*137f,0)*Vector3.forward;
            }
            var preferred = (Quaternion.Euler(0,_escapeBias,0)*away + separation*1.4f).normalized;
            Vector3 best = preferred; float bestScore = -999;
            for (int i=0; i<16; i++)
            {
                Vector3 dir = Quaternion.Euler(0,i*22.5f,0)*Vector3.forward;
                float clearance = 2f;
                int n = Physics.SphereCastNonAlloc(transform.position + Vector3.up*.3f, Radius*.9f, dir, _escapeHits, 2f, ~0, QueryTriggerInteraction.Ignore);
                for (int j=0;j<n;j++)
                {
                    var h = _escapeHits[j];
                    if (h.collider.transform.IsChildOf(transform) || h.collider.GetComponentInParent<Monster>() != null) continue;
                    clearance = Mathf.Min(clearance,h.distance);
                }
                var next = transform.position + dir * .65f;
                if (next.x < Room.min.x+.4f || next.x > Room.max.x-.4f || next.z < Room.min.z+.4f || next.z > Room.max.z-.4f) clearance = 0;
                float score = clearance*2.5f + Vector3.Dot(dir,away) + Vector3.Dot(dir,preferred)*1.8f;
                if (clearance < .25f) score -= 20f;
                if (score > bestScore) { bestScore = score; best = dir; }
            }
            return best;
        }

        void StartFlee()
        {
            _fleeTime = 0f; _blocked = 0f;
            SetState("Бегство");
            Hints.Show("Гриб убегает! Загони в угол или дай устать", HintPri.Action);
        }

        public override void OnHit(HitInfo h, Zone zone)
        {
            if (h.Type == ToolType.Net)
            {
                if (h.Speed < 0.5f) return;
                if (!h.Down) { Hints.Show("Накрой сачком СВЕРХУ", HintPri.Error); return; }
                if (h.Speed < 1f) { Hints.Show($"Слишком медленно ({h.Speed:0.0} м/с) — нужно 1", HintPri.Error); return; }
                if (State == "Устал" || State == "Загнан в угол" || State == "Покой") Capture(ItemId.MushroomLegs);
                else { Hints.Show("Выскользнул! Он ещё бодрый — загони в угол или дай устать", HintPri.Error); _fleeTime += 1.5f; }
            }
            else if (h.IsHuntingTool && h.Speed >= 1f)
            {
                Ruin("гриб ударили — он растёкся споровой жижей. Гриб ловят сачком (клавиша 2): накрой сверху");
            }
        }
    }
}
