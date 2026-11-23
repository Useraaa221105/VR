using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    public enum MonsterKind { Slime, Mushroom, Mandrake, Basilisk, Armor, Mimic, Dragon }
    public enum Zone { Body, Head, Neck, Back, Front, Shield, Leaves, Lid, Throat, Tail }

    /// <summary>
    /// Базовый монстр (greybox: кубы, без анимаций). Конечный автомат состояний в наследниках.
    /// Жизненный цикл: Init → Register в реестре → ... → Die(причина) → отписка от событий, удаление из реестра,
    /// обнуление ссылок, Destroy(gameObject). Повторная смерть игнорируется.
    /// </summary>
    /// <summary>Шаг охоты для крупной подсказки по центру. Step/Total — вместе с шагом 1 «возьми инструмент» (его текст добавляет HuntCoach).</summary>
    public struct HuntStep
    {
        public int Step, Total;
        public string Text;
        public Transform Target;   // куда бить/хватать — подсвечивается контуром, когда Strike
        public bool Strike;        // сейчас можно бить/хватать
        public bool NoTool;        // шаг не про инструмент (схватить рукой, присесть, щит) — «возьми инструмент» не подменяет его
        public static HuntStep Of(int step, int total, string text, Transform target = null, bool strike = false, bool noTool = false)
            => new HuntStep { Step = step, Total = total, Text = text, Target = target, Strike = strike, NoTool = noTool };
    }

    public struct CardPart { public Mesh Mesh; public Material Material; public Matrix4x4 Mat; public Color Col; }

    public abstract class Monster : MonoBehaviour
    {
        public MonsterKind Kind { get; private set; }
        public int Id { get; private set; }
        public bool Initialized => _registered;
        public string State { get; private set; } = "Покой";
        public VanishEvidence CaptureVanish { get; private set; }
        public bool Dead { get; private set; }
        public bool Asleep { get; private set; }
        public Vector3 Center => transform.position + Vector3.up * Height * 0.5f;

        protected float StateTime;
        protected float Height = 0.5f;
        protected float Radius = 0.25f;
        protected DungeonManager Dungeon;
        protected Bounds Room;
        public Bounds RoomBounds => Room;
        protected Rigidbody Body;

        MonsterCache.Entry _cache; // рендеры/цвета/коллайдеры — живут только пока жив монстр
        List<Renderer> _rends => _cache.Renderers;
        List<Color> _colors => _cache.Colors;
        Color _glow;
        bool _registered;
        TextMesh _stateLabel;
        Vector3 _wanderTarget;
        float _wanderUntil;

        protected PlayerRig P => PlayerRig.I;
        protected float DistToPlayer => P == null ? 999f : Flat(P.Feet - transform.position).magnitude;
        protected Vector3 ToPlayerFlat => P == null ? Vector3.zero : Flat(P.Feet - transform.position).normalized;

        public void Init(MonsterKind kind, DungeonManager dungeon, Bounds room)
        {
            if (_registered) return; // сохранённый экземпляр не регистрируется повторно
            Kind = kind;
            Dungeon = dungeon;
            Room = room;
            bool savedModel = BindSavedModel();
            Body = savedModel ? GetComponent<Rigidbody>() : null;
            if (Body == null) Body = gameObject.AddComponent<Rigidbody>();
            Body.isKinematic = true;
            if (!savedModel)
            {
                Build();
                MonsterLooks.Decorate(this); // прежний путь основной игры остаётся по умолчанию
            }
            _stateLabel = Prim.Text(transform, new Vector3(0, Height + 0.25f, 0), "", 0.06f, new Color(1, 1, 1, 0.8f));
            Id = MonsterRegistry.Register(this);
            _cache = MonsterCache.Build(this, Id);
            NoGlow(); // сразу зелёный свет «бить нельзя»
            // исправление 5: кольцо на полу цвета нужного инструмента (мимик — золотое, как у сундука: не выдаём его)
            if (Kind == MonsterKind.Mimic) GlowRing.Create(transform, transform.position, 0.75f, GlowRing.Gold, RingKind.Loot);
            else GlowRing.Create(transform, transform.position, Mathf.Max(0.45f, Radius * 1.5f), Tools.ColorOf(Bestiary.Get(kind).Tool), RingKind.Monster);
            _registered = true;
            GameEvents.PlayerDied += OnPlayerDied; // подписка → обязательно отписка в Cleanup
            GameEvents.RaiseMonsterSpawned(this);
            name = $"{kind}#{Id}";
        }

        // По умолчанию всё как раньше. Только явно привязанный prefab использует сохранённый визуал.
        [SerializeField, HideInInspector] bool _savedGeometry;
        [SerializeField, HideInInspector] float _savedHeight, _savedRadius;
        public bool HasSavedGeometry => _savedGeometry;

        protected virtual bool BindSavedModel()
        {
            if (!_savedGeometry) return false;
            // Full rig scale (visual + colliders), not independent cosmetic stretching.
            var scale = transform.lossyScale;
            Height = _savedHeight * Mathf.Abs(scale.y);
            Radius = _savedRadius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            OnSavedModelBound();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var material = renderer.sharedMaterial;
                if (material == null) continue;
                var color = material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") :
                    material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
                var emission = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;
                Prim.SetColor(renderer, color, emission);
            }
            return true;
        }
        protected virtual void OnSavedModelBound() { }

#if UNITY_EDITOR
        // Editor tool only: uses the SAME Build, never Init/registry/events or a second AI.
        public void BuildGeometryForEditor(MonsterKind kind)
        {
            if (Application.isPlaying || Initialized || transform.childCount != 0)
                throw new System.InvalidOperationException("Строить можно только новый пустой Editor-объект.");
            if (!((this is Mushroom && kind == MonsterKind.Mushroom) ||
                  (this is Mandrake && kind == MonsterKind.Mandrake) ||
                  (this is Basilisk && kind == MonsterKind.Basilisk) ||
                  (this is LivingArmor && kind == MonsterKind.Armor) ||
                  (this is Mimic && kind == MonsterKind.Mimic) ||
                  (this is Dragon && kind == MonsterKind.Dragon)))
                throw new System.InvalidOperationException("Этот тип ещё не включён в группу сохранённых моделей.");
            Kind = kind;
            Build();
            _savedHeight = Height; _savedRadius = Radius; _savedGeometry = true;
        }
#endif
        protected abstract void Build();
        protected abstract void Tick(float dt);
        public abstract void OnHit(HitInfo h, Zone zone);
        public virtual void OnGrab(MonsterGrabPoint p, HandController hand) { }
        public virtual void OnGrabRelease(MonsterGrabPoint p, HandController hand, bool pulled) { }
        protected virtual void OnPlayerDied() { CancelAttack(); }
        protected virtual void CancelAttack()
        {
            AttackGate.Release(this);
            string idle = Kind == MonsterKind.Dragon ? "Ждёт" : Kind == MonsterKind.Slime ? "Покой" : Kind == MonsterKind.Mimic ? "Раскрыт" : "Патруль";
            SetState(idle);
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCounters() { CapturedTotal = 0; RuinedTotal = 0; LastCapturedName = null; }

        void Update()
        {
            if (!_registered || Dead || P == null) return;
            // «сон» дальних монстров — экономия CPU на Quest
            Asleep = DistToPlayer > (Kind == MonsterKind.Dragon ? 60f : 25f);
            if (Asleep) { AttackGate.Release(this); return; }
            float dt = DTime.Delta;
            if (_stateLabel != null)
            {
                bool near = HintsToggle.On && DistToPlayer < 9f && !BestiaryCard.Open; // над карточкой бестиария подписи не видны
                if (_stateLabel.gameObject.activeSelf != near) _stateLabel.gameObject.SetActive(near);
                if (_stateLabel.text != State) _stateLabel.text = State;
                Prim.FaceTo(_stateLabel.transform, P.HeadPos);
            }
            if (dt <= 0f) return;
            if (IsAttackState(State))
            {
                // Дракон завершает ограниченную атаку за укрытием; сам урон всё равно проверяет LOS.
                if (!CanSeePlayer(16f) && State != "На лице" && Kind != MonsterKind.Dragon) { CancelAttack(); return; }
                if (!AttackGate.Request(this)) { CancelAttack(); return; }
            }
            // Ждущие не являются физической стеной; попадания/ручной захват работают и по trigger.
            if (_cache != null && Kind != MonsterKind.Dragon)
                foreach (var c in _cache.Colliders) if (c != null) c.isTrigger = !AttackGate.Holds(this);
            StateTime += dt;
            Tick(dt);
            if (!Dead && Hunt().Strike) Window();
            if (_bar != null && _barFrame != Time.frameCount && _bar.gameObject.activeSelf) { _bar.gameObject.SetActive(false); WindowLeft = 0; }
            if (!Dead && !Bestiary.Seen(Kind) && DistToPlayer < CardDistance && CanSeePlayer(CardDistance)) Bestiary.MarkSeen(Kind);
        }

        /// <summary>С какого расстояния открывается карточка бестиария при первой встрече (большие монстры — дальше).</summary>
        protected virtual float CardDistance => 9f; // пункт 8: карточка первой встречи — когда монстр виден с 8–10 м

        /// <summary>Только для автотеста/отладки: принудительно выставить состояние.</summary>
        public void DebugSetState(string s) => SetState(s, true);
        bool _waitingTurn;
        /// <summary>Хотел напасть, но ждёт очереди (другой монстр атакует или идёт общая пауза).</summary>
        public bool WaitingTurn => _waitingTurn && !Dead;
        public float StateAge => StateTime;
        /// <summary>Сколько монстров было добыто/испорчено за сессию (для подсказок и тестов).</summary>
        public static int CapturedTotal, RuinedTotal;
        public static Monster LastCaptured; public static string LastCapturedName; public static Vector3 LastCapturedPos;

        /// <summary>Пункт 7: состояния нападения — на них нужен «жетон» AttackGate (ровно один атакующий, общий перерыв после атаки).</summary>
        public static bool IsAttackState(string s) => s == "Готовится" || s == "Прыжок" || s == "На лице" || s == "Замах" || s == "Клевок" || s == "Вдох" || s == "Огонь" || s == "Лапа: замах" || s == "Хвост: замах" || s == "Дождь" || Dragon.AttackState(s);

        protected void SetState(string s) => SetState(s, false);
        void SetState(string s, bool force)
        {
            if (State == s) return;
            bool atk = IsAttackState(s);
            if (atk && !force && !AttackGate.Request(this)) { _waitingTurn = true; return; } // очередь: кружит рядом
            if (!atk) AttackGate.Release(this);
            _waitingTurn = false;
            var from = State;
            State = s;
            StateTime = 0f;
            OnStateChanged(from, s);
        }

        /// <summary>Вызывается при каждой смене состояния (наследник может вернуть то, что включал/выключал).</summary>
        protected virtual void OnStateChanged(string from, string to) { }

        // ---------- живая подсказка по шагам (пункт 3) ----------
        /// <summary>Какой шаг охоты сейчас (шаг 1 «возьми инструмент» добавляет HuntCoach сам). Total = 0 — не подсказывать (напр. мимик в маскировке).</summary>
        public virtual HuntStep Hunt() => default;
        protected bool IsXR => P != null && P.IsXR;
        /// <summary>«ЖМИ E» на ПК, «ХВАТАЙ рукой (Grip)» в VR.</summary>
        protected string GrabCmd => IsXR ? "ХВАТАЙ рукой (Grip)" : "ЖМИ E";
        protected string Lmb => IsXR ? "сильный замах сверху" : "ЛКМ";
        protected string Rmb => IsXR ? "горизонтальный замах" : "ПКМ";
        protected string CrouchCmd => IsXR ? "пригнись" : $"присядь ({Tools.CrouchKey})";

        // ---------- таймер-полоска окна над монстром ----------
        Transform _bar, _barFill; TextMesh _barText; int _barFrame = -1;
        /// <summary>Сколько секунд осталось в текущем окне (для подсказок и автотеста); 0 — окна нет.</summary>
        public float WindowLeft { get; private set; }
        public float WindowTotal { get; private set; }
        public bool WindowBarVisible => _bar != null && _bar.gameObject.activeSelf;
        /// <summary>Показать над монстром полоску «сколько осталось» (вызывать каждый кадр, пока идёт окно).</summary>
        protected void WindowTimer(float left, float total)
        {
            // Внутреннее окно механики сохранено; таймеры/секунды над монстрами запрещены.
            WindowLeft = Mathf.Max(0, left); WindowTotal = total; _barFrame = Time.frameCount;
            if (_bar != null) _bar.gameObject.SetActive(false);
        }

        /// <summary>Пункт 8: детали тела для портрета на карточке (меш, положение относительно монстра, цвет).</summary>
        public void PortraitParts(List<CardPart> list)
        {
            if (_cache == null) return;
            var inv = transform.worldToLocalMatrix;
            for (int i = 0; i < _rends.Count; i++)
            {
                var r = _rends[i] as MeshRenderer;
                if (r == null) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                list.Add(new CardPart { Mesh = mf.sharedMesh, Material = r.sharedMaterial, Mat = inv * r.transform.localToWorldMatrix, Col = i < _colors.Count ? _colors[i] : Color.gray });
            }
        }

        // ---------- визуальные сигналы ----------
        /// <summary>Пункт 9: «светофор» монстра. ЗЕЛЁНЫЙ — бить нельзя (лут испортится), ЖЁЛТЫЙ — можно (оглушён, устал, клюв/меч застрял,
        /// треснул, перегрев), КРАСНЫЙ — сейчас ударит. Мимик в маскировке не светится (иначе выдаст себя).</summary>
        public static readonly Color SafeGreen = new Color(0.1f, 0.85f, 0.25f) * 0.55f;
        public static readonly Color StrikeYellow = new Color(1f, 0.85f, 0.05f) * 1.1f;
        public static readonly Color DangerRed = new Color(1f, 0.1f, 0.05f) * 1.2f;
        public enum Light3 { None, Green, Yellow, Red }
        /// <summary>Какой свет сейчас у монстра (для автотеста и карточки).</summary>
        public Light3 LightNow { get; private set; }
        protected virtual bool Disguised => false;
        Light _lamp;
        Color _prevGlow;

        protected void Glow(Color c)
        {
            if (c == Color.black && !Disguised) c = SafeGreen;
            _glow = c;
            LightNow = c == SafeGreen ? Light3.Green : c == StrikeYellow ? Light3.Yellow : c == DangerRed ? Light3.Red : c == Color.black ? Light3.None : LightNow;
            if (_cache == null) return;
            for (int i = 0; i < _rends.Count; i++) if (_rends[i] != null) Prim.SetEmission(_rends[i], c * (LightNow==Light3.Yellow ? .28f : LightNow==Light3.Red ? .18f : .045f));
            if (_lamp == null)
            {
                _lamp = Prim.PointLight(transform, new Vector3(0, Height + 0.3f, 0), Color.green, Mathf.Min(1.1f, Mathf.Max(.55f,Height*.65f)), .12f);
                _lamp.shadows = LightShadows.None;
            }
            bool on = LightNow==Light3.Yellow || LightNow==Light3.Red;
            if (_lamp.enabled != on) _lamp.enabled = on;
            if (on) { var lc = (c / c.maxColorComponent); if (_lamp.color != lc) _lamp.color = lc; }
        }
        protected void Telegraph() => Glow(DangerRed);   // красное: сейчас ударит
        protected void Window() => Glow(StrikeYellow);   // жёлтое: «Сейчас!» — можно бить нужным инструментом
        protected void NoGlow() => Glow(Color.black);    // = зелёное «бить нельзя» (мимик в маскировке — без света)
        public void Highlight(bool on) { if (on) { _prevGlow = _glow; Glow(Color.white * 0.5f); } else Glow(_prevGlow); }

        protected void Tint(float k)
        {
            if (_cache == null) return;
            for (int i = 0; i < _rends.Count; i++) if (_rends[i] != null) Prim.SetColor(_rends[i], _colors[i] * k, _glow * (LightNow==Light3.Yellow ? .28f : LightNow==Light3.Red ? .18f : .045f));
        }

        // ---------- движение (без NavMesh: прямое управление + проверка стен SphereCast) ----------
        static readonly RaycastHit[] _hits = new RaycastHit[12];

        protected bool Move(Vector3 dir, float speed, float dt)
        {
            dir = Flat(dir);
            if (dir.sqrMagnitude < 1e-4f) return true;
            dir.Normalize();
            float dist = speed * dt;
            var origin = transform.position + Vector3.up * Mathf.Min(0.3f, Height * 0.5f);
            int n = Physics.SphereCastNonAlloc(origin, Radius * 0.9f, dir, _hits, dist + 0.08f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var t = _hits[i].collider.transform;
                if (t.IsChildOf(transform)) continue;
                if (P != null && t.IsChildOf(P.transform)) continue;
                if (_hits[i].distance <= 0f) continue;
                return false;
            }
            transform.position += dir * dist;
            return true;
        }

        protected void Face(Vector3 dir, float dt, float degPerSec = 360f)
        {
            dir = Flat(dir);
            if (dir.sqrMagnitude < 1e-4f) return;
            var target = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, degPerSec * dt);
        }

        protected void Wander(float speed, float dt)
        {
            if (DTime.Now > _wanderUntil || Flat(_wanderTarget - transform.position).magnitude < 0.4f)
            {
                _wanderTarget = new Vector3(Random.Range(Room.min.x + 1f, Room.max.x - 1f), transform.position.y, Random.Range(Room.min.z + 1f, Room.max.z - 1f));
                _wanderUntil = DTime.Now + Random.Range(3f, 6f);
            }
            var d = _wanderTarget - transform.position;
            Face(d, dt, 180f);
            if (!Move(d, speed, dt)) _wanderUntil = 0;
        }

        protected bool CanSeePlayer(float range)
        {
            if (P == null || DistToPlayer > range) return false;
            var eye = transform.position + Vector3.up * (Height + 0.05f);
            return !Rays.Blocked(eye, P.HeadPos, transform);
        }

        protected bool PlayerInFront(float cosAngle)
        {
            return Vector3.Dot(transform.forward, ToPlayerFlat) > cosAngle;
        }

        protected static Vector3 Flat(Vector3 v) { v.y = 0; return v; }

        protected void DamagePlayer(float dmg, string src)
        {
            if (P != null && !Rays.Blocked(Center, P.HeadPos, transform) && AttackGate.MayHurt(this)) P.Health.Damage(dmg, src);
        }

        // ---------- результат охоты ----------
        protected void Capture(ItemId loot, int count = 1)
        {
            if (Dead) return;
            DungeonLoot.Create(loot, count, Dungeon != null ? Dungeon.WorldRoot : null, new Vector3(Center.x, Mathf.Max(transform.position.y, DungeonManager.Origin.y) + .12f, Center.z));
            Banner.Captured(Kind, loot, count);
            FX.Sparkle(Center, new Color(.9f,.8f,.3f));
            CaptureVanish = FX.Vanish(Center, Mathf.Max(0.5f, Height), _colors.Count > 0 ? _colors[0] : Color.white); // монстр исчезает в облачке
            Sfx.Play(SfxId.Capture, Center, 0.4f);
            CapturedTotal++; LastCapturedName = name; LastCapturedPos = Center;
            Die(DeathReason.Captured);
        }

        protected void Ruin(string why)
        {
            if (Dead) return;
            Banner.Ruined(Kind, why);
            Sfx.Play(SfxId.Ruin, transform.position, 0.5f);
            FX.Splat(transform.position, _colors.Count > 0 ? _colors[0] : Color.gray);
            FX.Vanish(Center, Mathf.Max(0.5f, Height), Color.gray);
            RuinedTotal++;
            Die(DeathReason.Ruined);
        }

        /// <summary>Единственная точка смерти монстра. Чистит всё, что на него ссылается.</summary>
        public void Die(DeathReason reason)
        {
            if (Dead) return;
            Dead = true;
            StopAllCoroutines();
            GameEvents.RaiseMonsterDied(this, reason); // DungeonManager / BestiaryCard убирают его из своих списков
            Cleanup();
            Debug.Log($"[Monster] {name} удалён ({reason}). Живых в реестре: {MonsterRegistry.AliveCount}, в кеше: {MonsterRegistry.CacheCount}");
            Destroy(gameObject);
        }

        void Cleanup()
        {
            GameEvents.PlayerDied -= OnPlayerDied;
            AttackGate.Release(this);
            if (_registered) { MonsterRegistry.Unregister(this, Id); _registered = false; }
            // руки игрока не должны держать части мёртвого монстра
            foreach (var gp in GetComponentsInChildren<MonsterGrabPoint>())
            {
                if (P != null) { P.Left.ClearIfHolding(gp); P.Right.ClearIfHolding(gp); }
            }
            OnCleanup();
            MonsterCache.Release(Id, name); // ← очистка кеша убитого монстра
            _cache = null;
            Dungeon = null;
            Body = null;
            _stateLabel = null;
        }

        protected virtual void OnCleanup() { }

        /// <summary>Страховка: объект уничтожен вместе с этажом, а Die не вызывался.</summary>
        protected virtual void OnDestroy()
        {
            // Editor-генерация/Prefab Mode не должны посылать игровые события и чистить живой реестр.
            if (!_registered) return; // у Editor-предпросмотра Init не вызывался; runtime cleanup при выходе из Play сохраняется
            if (!Dead)
            {
                Dead = true;
                GameEvents.RaiseMonsterDied(this, DeathReason.Despawned);
                Cleanup();
            }
        }
    }

}
