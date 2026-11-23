using UnityEngine;

namespace Podz
{
    /// <summary>Живые доспехи (−3). Внутри моллюски — из щелей шлема блестит слизь. 3 удара молотком → на колено → сорвать шлем.</summary>
    public class LivingArmor : Monster
    {
        [SerializeField] Transform _sword;
        Vector3 _swordRest;
        [SerializeField] MonsterGrabPoint _helmet;
        int _hits;
        [SerializeField] Transform _torso, _helm;
        public int Hits => _hits;
        [SerializeField, Min(.01f)] float stuckTime = 3f;
        [SerializeField, Min(.01f)] float kneelTime = 3f;

        protected override void OnSavedModelBound()
        {
            if (_sword == null || _helmet == null || _torso == null || _helm == null || _helmet.Owner != this)
                throw new System.InvalidOperationException("Доспехи: потеряны ссылки сохранённой модели.");
            _swordRest = _sword.localPosition; SetState("Спит");
        }

        protected override void Build()
        {
            Height = 1.9f; Radius = 0.4f;
            BuildArmorShape(transform, true, this, out _sword, out _helmet);
            _swordRest = _sword.localPosition;
            _torso = transform.Find("Torso"); _helm = transform.Find("Helmet");
            SetState("Спит");
        }

        /// <summary>Общая геометрия для живых и ложных доспехов. Ложные — без слизи.</summary>
        public static void BuildArmorShape(Transform t, bool alive, LivingArmor owner, out Transform sword, out MonsterGrabPoint helmet)
        {
            var steel = new Color(0.5f, 0.52f, 0.55f);
            var torso = Prim.Cube("Torso", t, new Vector3(0, 0.95f, 0), new Vector3(0.6f, 1.1f, 0.4f), steel);
            var helm = Prim.Cube("Helmet", t, new Vector3(0, 1.7f, 0), new Vector3(0.34f, 0.34f, 0.34f), steel * 0.9f);
            var shield = Prim.Cube("Shield", t, new Vector3(-0.38f, 1.0f, 0.28f), new Vector3(0.08f, 0.7f, 0.5f), new Color(0.4f, 0.3f, 0.2f));
            sword = Prim.Cube("Sword", t, new Vector3(0.4f, 1.0f, 0.35f), new Vector3(0.06f, 0.06f, 0.9f), new Color(0.8f, 0.8f, 0.85f), false).transform;
            Prim.Cube("LegL", t, new Vector3(-0.15f, 0.2f, 0), new Vector3(0.18f, 0.4f, 0.2f), steel * 0.8f, false);
            Prim.Cube("LegR", t, new Vector3(0.15f, 0.2f, 0), new Vector3(0.18f, 0.4f, 0.2f), steel * 0.8f, false);
            helmet = null;
            if (alive)
            {
                var slime = Prim.Cube("SlimeGlint", t, new Vector3(0, 1.72f, 0.18f), new Vector3(0.22f, 0.05f, 0.02f), new Color(0.3f, 1f, 0.4f), false);
                Prim.SetEmission(slime.GetComponent<Renderer>(), new Color(0.3f, 1f, 0.4f) * 1.5f);
                HitZone.Add(torso, owner, Zone.Body);
                HitZone.Add(helm, owner, Zone.Head);
                HitZone.Add(shield, owner, Zone.Shield);
                helmet = MonsterGrabPoint.Add(helm, owner, false);
            }
        }

        protected override void Tick(float dt)
        {
            switch (State)
            {
                case "Спит":
                    NoGlow();
                    if (CanSeePlayer(6f)) SetState("Наступает");
                    break;
                case "Патруль":
                    NoGlow();
                    Wander(0.5f, dt);
                    if (CanSeePlayer(8f)) SetState("Наступает");
                    break;
                case "Наступает":
                    NoGlow();
                    Face(ToPlayerFlat, dt, 120f);
                    if (DistToPlayer > 12f) SetState("Патруль");
                    else if (DistToPlayer < 1.9f) SetState("Замах");
                    else Move(ToPlayerFlat, 1.0f, dt);
                    break;
                case "Замах":
                    Telegraph();
                    _sword.localPosition = _swordRest + new Vector3(0, 0.6f, -0.2f);
                    if (StateTime > 0.8f)
                    {
                        _sword.localPosition = _swordRest + new Vector3(0, -0.55f, 0.3f);
                        if (DistToPlayer < 2.0f && PlayerInFront(0.3f)) { DamagePlayer(Dmg.ArmorSword, "меч доспеха"); SetState("Пауза"); }
                        else { SetState("Меч застрял"); Hints.Show("Меч застрял — бей молотком сбоку или сзади!", HintPri.Window); }
                    }
                    break;
                case "Меч застрял":
                    Window();
                    WindowTimer(stuckTime - StateTime, stuckTime);
                    if (StateTime > stuckTime) { _sword.localPosition = _swordRest; SetState("Наступает"); }
                    break;
                case "Пауза":
                    NoGlow();
                    _sword.localPosition = _swordRest;
                    if (StateTime > 1f) SetState("Наступает");
                    break;
                case "На колене":
                    Window();
                    WindowTimer(kneelTime - StateTime, kneelTime);
                    if (_helmet.Held && _helmet.PullDistance > 0.2f) { Capture(ItemId.ShellfishMeat); return; }
                    if (StateTime > kneelTime)
                    {
                        _helmet.ForceDrop(); _helmet.Active = false;
                        transform.position += Vector3.up * 0.4f;
                        _hits = 0;
                        SetState("Наступает");
                    }
                    break;
            }
        }

        bool PlayerBehind => DistToPlayer < 2.6f && transform.InverseTransformPoint(P.Feet).z < -0.15f;

        public override HuntStep Hunt()
        {
            switch (State)
            {
                case "На колене": return HuntStep.Of(4, 4, $"На колене! {GrabCmd} — сорви шлем!", _helm, true, true);
                case "Меч застрял": return HuntStep.Of(3, 4, $"Меч застрял! Бей молотком сбоку или в спину — {Lmb} ({_hits}/3)", _torso, true);
                case "Замах": return HuntStep.Of(2, 4, "Замах мечом! Шаг назад или вбок — меч застрянет в полу");
            }
            if (PlayerBehind) return HuntStep.Of(3, 4, $"Ты за спиной — бей молотком {Lmb} ({_hits}/3)", _torso, true);
            return HuntStep.Of(2, 4, $"Зайди со спины — или дождись удара мечом и увернись ({_hits}/3)");
        }

        public override void OnHit(HitInfo h, Zone zone)
        {
            if (h.Type == ToolType.Hammer)
            {
                if (State == "На колене") return;
                if (h.Speed < 2f) { if (h.Speed > 0.8f) Hints.Show($"Слабо ({h.Speed:0.0} м/с) — нужно 2", HintPri.Error); return; }
                var local = transform.InverseTransformPoint(h.Point);
                bool front = local.z > 0.05f;
                if (zone == Zone.Shield || (front && State != "Меч застрял")) { Hints.Show("Отскок от щита — бей со спины", HintPri.Error); return; }
                _hits++;
                Hints.Show($"Удар молотком {_hits}/3", HintPri.Window);
                if (State == "Спит" || State == "Патруль") SetState("Наступает");
                if (_hits >= 3)
                {
                    SetState("На колене");
                    transform.position -= Vector3.up * 0.4f;
                    _sword.localPosition = _swordRest;
                    _helmet.Active = true;
                    Hints.Show("На колене! Сорви шлем рукой (ПК: E)", HintPri.Window);
                }
            }
            else if (h.IsHuntingTool && h.Speed >= 1f)
            {
                h.Tool.Break(10f);
                Hints.Show("О броню ломается всё, кроме молотка", HintPri.Error);
            }
        }

        public override void OnGrab(MonsterGrabPoint p, HandController hand)
        {
            if (State == "На колене" && (P == null || !P.IsXR)) Capture(ItemId.ShellfishMeat); // ПК: E = сорвать
        }

        public override void OnGrabRelease(MonsterGrabPoint p, HandController hand, bool pulled)
        {
            if (State == "На колене" && pulled) Capture(ItemId.ShellfishMeat);
        }
    }

    /// <summary>Ложный доспех — декорация без слизи. Не монстр, в реестр не попадает.</summary>

}
