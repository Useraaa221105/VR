using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>Boss FSM: ground deck, bite, perch flight, three rain waves, landing and distinct two-hand finisher.</summary>
    public class Dragon : Monster
    {
        static readonly Vector3 HeadRest = new Vector3(0, 3.4f, 3.4f);
        static readonly Vector3 HeadLow = new Vector3(0, 1.7f, 4.2f); // throat collider centre at 1.35 m
        [SerializeField] Transform _head, _flame, _paw, _tail;
        [SerializeField] Renderer _throatR;
        Vector3 _pawRest, _tailRest;
        Collider[] _physical;
        int _phase = 1, _throatHits, _perch = -1, _wave;
        public int Phase => _phase;
        public int ThroatHits => _throatHits;
        public int TotalThroatHits => (_phase - 1) * 3 + _throatHits;
        public bool Airborne { get; private set; }
        public bool Vulnerable => State == "Перегрев" || State == "Хвост пролетел" || State == "После посадки";
        public static bool AttackState(string s) => s == "Укус: замах" || s == "Укус" || s == "Перелёт" || s == "Тень посадки" || s == "Снижение";
        public const float OverheatTime = 5f, TailOpenTime = 4f, LandingOpenTime = 3f;
        [SerializeField, Min(0)] float overheatTime = OverheatTime;
        [SerializeField, Min(0)] float tailOpenTime = TailOpenTime;
        [SerializeField, Min(0)] float landingOpenTime = LandingOpenTime;
        public const float BiteDamage = 25f, ShieldedBiteDamage = 5f, LandingDamage = 25f;
        [SerializeField, Min(0)] float biteDamage = BiteDamage;
        [SerializeField, Min(0)] float shieldedBiteDamage = ShieldedBiteDamage;
        [SerializeField, Min(0)] float landingDamage = LandingDamage;
        float K => _phase >= 3 ? .8f : 1f;
        float FlightSpeed => _phase >= 3 ? 1.25f : 1f;
        float _breathIn, _breathBlocked, _flightDuration;
        Vector3 _rainSource, _breathDir, _biteTarget, _biteStart, _biteEnd, _flightStart, _flightEnd, _landingPoint;
        string _lastAttack;
        bool _hitDone;
        Renderer[] _stoneRenderers;
        Color[] _stoneColors;
        GameObject _landingShadow;
        readonly List<GameObject> _circles = new List<GameObject>();
        readonly List<GameObject> _balls = new List<GameObject>();
        readonly List<string> _deck = new List<string>();
        protected override float CardDistance => 30f;
        Vector3 Mouth => _head.TransformPoint(new Vector3(0, .1f, 1f));

        protected override void OnSavedModelBound()
        {
            if (_head == null || _flame == null || _paw == null || _tail == null || _throatR == null)
                throw new System.InvalidOperationException("Дракон: потеряны ссылки сохранённой модели.");
            _pawRest = _paw.localPosition; _tailRest = _tail.localPosition;
            _physical = GetComponentsInChildren<Collider>(true); SetAirborne(false); SetState("Ждёт");
        }

        protected override void Build()
        {
            Height = 4f; Radius = 2.2f;
            var red = new Color(.42f, .13f, .075f);
            var body = Prim.Cube("Body", transform, new Vector3(0, 1.4f, 0), new Vector3(3, 2.4f, 4.6f), red);
            HitZone.Add(body, this, Zone.Body);
            Prim.Cube("WingL", transform, new Vector3(-1.35f, 2.6f, 0), new Vector3(2.2f, .1f, 2.6f), red * .7f, false);
            Prim.Cube("WingR", transform, new Vector3(1.35f, 2.6f, 0), new Vector3(2.2f, .1f, 2.6f), red * .7f, false);
            _head = new GameObject("HeadPivot").transform; _head.SetParent(transform, false); _head.localPosition = HeadRest;
            var head = Prim.Cube("Head", _head, new Vector3(0, .2f, .4f), new Vector3(.9f, .7f, 1.3f), red);
            HitZone.Add(head, this, Zone.Head);
            var throat = Prim.Cube("Throat", _head, new Vector3(0, -.35f, -.1f), new Vector3(.6f, .5f, .6f), new Color(.72f, .48f, .12f));
            HitZone.Add(throat, this, Zone.Throat); _throatR = throat.GetComponent<Renderer>();
            Prim.Cube("Neck", _head, new Vector3(0, -.3f, -.9f), new Vector3(.6f, .6f, 1.4f), red, false);
            // Small segmented fire stream rather than an opaque ten-metre cuboid.
            _flame = new GameObject("BreathStream").transform; _flame.SetParent(_head, false);
            for(int j=0;j<7;j++)
            {
                var f=Prim.Sphere("BreathEmber",_flame,new Vector3(0,0,1.6f+j*1.3f),new Vector3(.32f+j*.09f,.25f+j*.045f,.65f),new Color(.74f,.40f,.08f));
                Prim.SetEmission(f.GetComponent<Renderer>(),new Color(.14f,.06f,.003f));
            }
            _flame.gameObject.SetActive(false);
            _paw = Prim.Cube("Paw", transform, new Vector3(1.2f, .4f, 2.6f), new Vector3(.8f, .8f, .9f), red).transform;
            HitZone.Add(_paw.gameObject, this, Zone.Front); _pawRest = _paw.localPosition;
            _tail = Prim.Cube("Tail", transform, new Vector3(0, .8f, -4.2f), new Vector3(.7f, .6f, 4), red).transform;
            HitZone.Add(_tail.gameObject, this, Zone.Tail); _tailRest = _tail.localPosition;
            _physical=GetComponentsInChildren<Collider>(true);
            SetAirborne(false);
            SetState("Ждёт");
        }
        void HeadTo(Vector3 target,float dt) => _head.localPosition=Vector3.MoveTowards(_head.localPosition,target,4*dt);
        void SetAirborne(bool value)
        {
            Airborne=value;
            foreach(var c in _physical)if(c!=null)c.isTrigger=value || c.name!="Body"; // solid torso only; head/tail/paw remain hittable triggers
        }
        void HurtFrom(Vector3 source,float amount,string reason)
        {
            if(P!=null && !Rays.Blocked(source,P.HeadPos,transform) && AttackGate.MayHurt(this))P.Health.Damage(amount,reason);
        }
        protected override void OnStateChanged(string from,string to)
        {
            if(to=="Вдох" || to=="Лапа: замах" || to=="Хвост: замах" || to=="Укус: замах" || to=="Тень посадки")
                Sfx.Play(SfxId.Warn,transform.position,.65f);
            if(to=="Перелёт")Sfx.Play(SfxId.Whoosh,transform.position,.55f);
            if(to=="Укус" || to=="Снижение")Sfx.Play(SfxId.Strike,transform.position,.55f);
            if(to=="Рёв")Sfx.Play(SfxId.Whoosh,transform.position,.75f);
        }
        protected override void Tick(float dt)
        {
            Prim.SetEmission(_throatR,Vulnerable?new Color(.18f,.13f,.015f):Color.black);
            switch(State)
            {
                case "Ждёт":
                    NoGlow(); HeadTo(HeadRest,dt);_head.localRotation=Quaternion.identity;
                    _paw.localPosition=_pawRest;_tail.localPosition=_tailRest;_tail.localRotation=Quaternion.identity;
                    Face(ToPlayerFlat,dt,40);if(StateTime>1.5f*K)ChooseAttack();break;
                case "Вдох":
                    Telegraph();HeadTo(HeadRest+Vector3.up*.4f,dt);Face(ToPlayerFlat,dt,60);
                    if(StateTime>1.5f*K)
                    {
                        _breathDir=(P.HeadPos-Mouth).normalized;
                        _head.rotation=Quaternion.LookRotation(_breathDir,Vector3.up);
                        _flame.gameObject.SetActive(true);_breathIn=0;_breathBlocked=0;SetState("Огонь");
                    }
                    break;
                case "Огонь":
                    Telegraph();
                    var to=P.HeadPos-Mouth;
                    if(to.magnitude<12 && Vector3.Angle(_breathDir,to)<(_phase>=3?27.5f:22f))
                    {
                        _breathIn+=dt;
                        bool shield=P.ShieldRaisedTowards(Mouth);
                        bool cover=FireCover.Between(Mouth,P.HeadPos,out _)!=null || Rays.Blocked(Mouth,P.HeadPos,transform);
                        if(shield || cover)_breathBlocked+=dt;
                        else HurtFrom(Mouth,Dmg.DragonBreathPerSec*dt,"дыхание дракона (щит или камень!)");
                    }
                    if(StateTime>2.5f*K)
                    {
                        _flame.gameObject.SetActive(false);_head.localRotation=Quaternion.identity;
                        if(_breathIn>.5f && _breathBlocked/_breathIn>=.9f){SetState("Перегрев");Hints.Show("Перегрев! Тесаком по золотой чешуе",HintPri.Window);}
                        else SetState("Ждёт");
                    }
                    break;
                case "Перегрев": OpenWindow(overheatTime,dt);break;
                case "Хвост пролетел": OpenWindow(tailOpenTime,dt);break;
                case "После посадки": OpenWindow(landingOpenTime,dt);break;
                case "Лапа: замах":
                    Telegraph();_paw.localPosition=_pawRest+new Vector3(0,1.2f,-.3f);
                    if(StateTime>.8f*K){_paw.localPosition=_pawRest+new Vector3(0,-.1f,.6f);if(DistToPlayer<4.5f && PlayerInFront(.4f))HurtFrom(_paw.position,Dmg.DragonPaw,"лапа дракона");SetState("Ждёт");}break;
                case "Хвост: замах":
                    Telegraph();_tail.localRotation=Quaternion.Euler(0,25,0);
                    if(StateTime>1*K)
                    {
                        _tail.localRotation=Quaternion.Euler(0,-35,0);
                        if(DistToPlayer<6.5f && !Rays.Blocked(_tail.position,P.HeadPos,transform))
                        {
                            if(P.Crouched || P.HeadPos.y-P.Feet.y<1.1f){SetState("Хвост пролетел");Hints.Show("Пригнулся! Золотая чешуя открыта",HintPri.Window);break;}
                            HurtFrom(_tail.position,Dmg.DragonTail,"хвост дракона — пригнись");
                        }
                        SetState("Ждёт");
                    }
                    break;
                case "Укус: замах":
                    Telegraph();HeadTo(HeadRest+new Vector3(0,.2f,-.9f),dt*2);
                    if(StateTime>.7f*K)
                    {
                        _biteStart=_head.localPosition;
                        var end=transform.InverseTransformPoint(_biteTarget)-Vector3.forward*.8f;
                        _biteEnd=new Vector3(Mathf.Clamp(end.x,-.65f,.65f),Mathf.Clamp(end.y,1,2.3f),Mathf.Clamp(end.z,1.8f,4.8f));
                        _hitDone=false;SetState("Укус");
                    }
                    break;
                case "Укус":
                    Telegraph();_head.localPosition=Vector3.Lerp(_biteStart,_biteEnd,Mathf.Clamp01(StateTime/(.22f*K)));
                    if(!_hitDone && StateTime>=.22f*K)
                    {
                        _hitDone=true;
                        if(Vector3.Distance(Mouth,P.HeadPos)<1.15f && PlayerInFront(.2f))
                            HurtFrom(Mouth,P.ShieldRaisedTowards(Mouth)?shieldedBiteDamage:biteDamage,"укус дракона");
                    }
                    if(StateTime>.5f*K)SetState("Ждёт");break;
                case "Перелёт":
                    Telegraph();HeadTo(HeadRest,dt);
                    float flightT=Mathf.Clamp01(StateTime/_flightDuration);
                    transform.position=Vector3.Lerp(_flightStart,_flightEnd,Mathf.SmoothStep(0,1,flightT))+Vector3.up*(Mathf.Sin(flightT*Mathf.PI)*2);
                    Face(ToPlayerFlat,dt,70);
                    if(flightT>=1){transform.position=_flightEnd;_wave=0;StartRainWave();}break;
                case "Дождь":
                    Telegraph();HeadTo(HeadRest,dt);Face(ToPlayerFlat,dt,45);
                    float t=Mathf.Clamp01((StateTime-1.5f*K)/(.55f*K));
                    for(int j=0;j<_balls.Count;j++)if(_balls[j]!=null){_balls[j].SetActive(StateTime>=1.5f*K);var end=_circles[j].transform.position+Vector3.up*.2f;FireCover.Between(_rainSource,end,out var impact);_balls[j].transform.position=Vector3.Lerp(_rainSource,impact,t);}
                    if(!_hitDone && t>=1){_hitDone=true;ResolveRain();}
                    if(StateTime>2.25f*K){ClearRain();if(_wave<3)StartRainWave();else StartLanding();}break;
                case "Тень посадки":
                    Telegraph();
                    if(StateTime>1.5f*K){_flightStart=transform.position;_flightEnd=_landingPoint;_flightDuration=1.15f/FlightSpeed;SetState("Снижение");}break;
                case "Снижение":
                    Telegraph();float landingT=Mathf.Clamp01(StateTime/_flightDuration);
                    transform.position=Vector3.Lerp(_flightStart,_flightEnd,Mathf.SmoothStep(0,1,landingT));Face(ToPlayerFlat,dt,100);
                    if(landingT>=1)
                    {
                        transform.position=_landingPoint;SetAirborne(false);ClearShadow();
                        if(DistToPlayer<4)HurtFrom(transform.position+Vector3.up*.3f,landingDamage,"приземление дракона — отойди от тени");
                        SetState("После посадки");Hints.Show("Приземлился! Бей золотую чешую",HintPri.Window);
                    }
                    break;
                case "Рёв":
                    NoGlow();HeadTo(HeadRest+Vector3.up*.6f,dt);if(StateTime>=3)SetState("Ждёт");break;
                case "Каменеет":
                    NoGlow();
                    float stoneT=Mathf.Clamp01(StateTime/2);
                    if(_stoneRenderers!=null)for(int j=0;j<_stoneRenderers.Length;j++)
                        if(_stoneRenderers[j]!=null)Prim.SetColor(_stoneRenderers[j],Color.Lerp(_stoneColors[j],new Color(.43f,.43f,.40f),stoneT),Color.black);
                    if(StateTime>=2){var d=Dungeon;Capture(ItemId.DragonMeat,2);if(d!=null)d.OnDragonDefeated();}break;
            }
        }
        void OpenWindow(float duration,float dt)
        {
            Window();HeadTo(HeadLow,dt*2);Face(ToPlayerFlat,dt,80);
            WindowTimer(duration-StateTime,duration);if(StateTime>duration)SetState("Ждёт");
        }
        void ChooseAttack()
        {
            // Request before changing attack-specific data; respects the existing shared FIFO/gap.
            if(!AttackGate.Request(this))return;
            if(_phase==2){StartFlight();return;}
            if(_deck.Count==0)
            {
                _deck.AddRange(new[]{"Вдох","Лапа: замах","Хвост: замах","Укус: замах"});
                if(_phase>=3)_deck.Add("Перелёт");
            }
            var candidates=new List<string>();foreach(var name in _deck)if(name!=_lastAttack)candidates.Add(name);
            if(candidates.Count==0){_deck.Clear();AttackGate.Release(this);return;}
            string selected=candidates[Random.Range(0,candidates.Count)];_deck.Remove(selected);_lastAttack=selected;
            if(selected=="Перелёт"){StartFlight();return;}
            if(selected=="Укус: замах")_biteTarget=P.HeadPos;
            SetState(selected);
            Hints.Show(selected=="Вдох"?"Огонь! Щит или каменное укрытие":selected=="Укус: замах"?"Укус! Отойди или закройся щитом":selected=="Хвост: замах"?"Хвост! Пригнись":"Лапа! Шаг в сторону",HintPri.Danger);
        }
        void StartFlight()
        {
            _perch=(_perch+1)%DragonLair.Perches.Length;_flightStart=transform.position;
            _flightEnd=DungeonManager.Origin+DragonLair.Perches[_perch];
            _flightDuration=Mathf.Max(2.5f,Vector3.Distance(_flightStart,_flightEnd)/6)/FlightSpeed;
            SetAirborne(true);SetState("Перелёт");Hints.Show("Дракон взлетает на насест",HintPri.Danger);
        }
        void StartRainWave()
        {
            ClearRain();_wave++;_hitDone=false;_rainSource=Mouth;
            for(int j=0;j<3;j++)
            {
                var off=j==0?Vector3.zero:new Vector3(Random.Range(-2.5f,2.5f),0,Random.Range(-2.5f,2.5f));
                var pos=P.Feet+off;pos.x=Mathf.Clamp(pos.x,DungeonManager.Origin.x-16.5f,DungeonManager.Origin.x+16.5f);pos.z=Mathf.Clamp(pos.z,-16.5f,16.5f);pos.y=DungeonManager.Origin.y+.02f;
                _circles.Add(WorldDisc(pos,1.2f,new Color(.68f,.38f,.06f,.35f)));
                var ball=Prim.Sphere("RainFireball",Dungeon!=null?Dungeon.WorldRoot:null,Vector3.zero,Vector3.one*.45f,new Color(.8f,.42f,.09f));
                ball.transform.position=Mouth;ball.SetActive(false);_balls.Add(ball);
            }
            // Consecutive waves keep one attack token; reset timer without releasing the queue.
            if(State=="Дождь")StateTime=0;else SetState("Дождь");
            Sfx.Play(SfxId.Warn,_rainSource,.65f);
            Hints.Show("Огненный дождь — выйди из кругов",HintPri.Danger);
        }
        void ResolveRain()
        {
            foreach(var circle in _circles)
            {
                var target=circle.transform.position+Vector3.up*.2f;
                var cover=FireCover.Between(_rainSource,target,out var impact);
                if(cover!=null){cover.InterceptFireball();FX.Sparkle(impact,new Color(.42f,.32f,.14f));continue;}
                if(Rays.Blocked(_rainSource,target,transform))continue;
                var delta=P.Feet-target;delta.y=0;
                if(delta.magnitude<1.2f)HurtFrom(target,Dmg.DragonRain,"огненный дождь");
            }
        }
        void StartLanding()
        {
            float best=float.MaxValue;
            foreach(var p in DragonLair.GroundPoints){var world=DungeonManager.Origin+p;float d=(P.Feet-world).sqrMagnitude;if(d<best){best=d;_landingPoint=world;}}
            _landingShadow=WorldDisc(_landingPoint+Vector3.up*.025f,4,new Color(.30f,.24f,.12f,.3f));
            SetState("Тень посадки");Hints.Show("Приземление! Отойди от большой тени",HintPri.Danger);
        }
        GameObject WorldDisc(Vector3 pos,float radius,Color color)
        {
            var disc=FX.Disc(Dungeon!=null?Dungeon.WorldRoot:null,Vector3.zero,radius,color);
            disc.transform.position=pos;return disc;
        }
        void ClearRain(){foreach(var g in _circles)if(g!=null)Destroy(g);foreach(var g in _balls)if(g!=null)Destroy(g);_circles.Clear();_balls.Clear();}
        void ClearShadow(){if(_landingShadow!=null)Destroy(_landingShadow);_landingShadow=null;}
        public override HuntStep Hunt()
        {
            if(Vulnerable)
            {
                if(TotalThroatHits==8)return HuntStep.Of(3,3,IsXR?"Финал: две руки на тесаке — укол вперёд!":"Финал: убери щит (5), держи Shift + F",_throatR.transform,true);
                return HuntStep.Of(3,3,$"Золотая чешуя! Тесаком ({TotalThroatHits}/9)",_throatR.transform,true);
            }
            switch(State)
            {
                case "Вдох":case "Огонь":return HuntStep.Of(2,3,"Огонь! Щит или за каменное укрытие",null,false,true);
                case "Укус: замах":case "Укус":return HuntStep.Of(2,3,"Укус! Отойди или щит",null,false,true);
                case "Перелёт":return HuntStep.Of(2,3,"Дракон летит на насест — готовься к дождю");
                case "Дождь":return HuntStep.Of(2,3,"Огненный дождь — уйди из кругов",null,false,true);
                case "Тень посадки":case "Снижение":return HuntStep.Of(2,3,"Приземление! Уйди из тени",null,false,true);
                case "Хвост: замах":return HuntStep.Of(2,3,IsXR?"Хвост! Пригнись":"Хвост! Присядь — C",null,false,true);
                case "Лапа: замах":return HuntStep.Of(2,3,"Лапа! Шаг в сторону");
                case "Каменеет":return default;
                default:return HuntStep.Of(2,3,$"Жди открытой чешуи. Фаза {_phase}/3");
            }
        }
        public override void OnHit(HitInfo h,Zone zone)
        {
            if(State=="Каменеет" || Dead)return;
            if(zone!=Zone.Throat || h.Type!=ToolType.Cleaver){if(h.IsHuntingTool && h.Speed>=1.5f)Hints.Show("Только тесаком по золотой чешуе",HintPri.Error);return;}
            if(!Vulnerable){Hints.Show("Чешуя закрыта — жди окна",HintPri.Error);return;}
            if(TotalThroatHits==8)
            {
                if(!DragonFinisher.TwoHands(h) || !DragonFinisher.ForwardThrust(h,_throatR.transform.position))
                {Hints.Show(IsXR?"Нужны две руки и укол вперёд ≥2 м/с":"Финал: свободная левая рука, Shift + F",HintPri.Error);return;}
            }
            else if(h.Speed<1.5f || !h.Edge){Hints.Show("Нужен удар лезвием ≥1,5 м/с",HintPri.Error);return;}
            _throatHits++;
            Hints.Show($"Золотая чешуя: {TotalThroatHits}/9",HintPri.Window);
            if(_throatHits<3)return;
            if(_phase==3)
            {
                ClearRain();ClearShadow();_flame.gameObject.SetActive(false);
                _stoneRenderers=GetComponentsInChildren<Renderer>(true);_stoneColors=new Color[_stoneRenderers.Length];
                var block=new MaterialPropertyBlock();
                for(int j=0;j<_stoneRenderers.Length;j++){_stoneRenderers[j].GetPropertyBlock(block);_stoneColors[j]=block.GetColor("_BaseColor");}
                SetState("Каменеет");return;
            }
            _throatHits=0;_phase++;_deck.Clear();SetState("Рёв");
            Hints.Show(_phase==2?"Фаза 2 — дракон взлетит на насест":"Фаза 3 — дракон быстрее",HintPri.Danger);
        }
        protected override void CancelAttack()
        {
            ClearRain();ClearShadow();if(_flame!=null)_flame.gameObject.SetActive(false);
            _head.localRotation=Quaternion.identity;
            if(Airborne){transform.position=DungeonManager.Origin+DragonLair.GroundPoints[0];SetAirborne(false);}
            base.CancelAttack();
        }
        protected override void OnPlayerDied()
        {
            CancelAttack();_phase=1;_throatHits=0;_deck.Clear();_lastAttack=null;
            transform.position=DungeonManager.Origin+DragonLair.GroundPoints[0];SetState("Ждёт");
        }
        protected override void OnCleanup(){ClearRain();ClearShadow();}
    }
}
