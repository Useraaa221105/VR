using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>Визуальный стиль этажа: в greybox биомы различаются только светом, цветом и туманом.</summary>
    public struct Biome
    {
        public string Name;
        public Color Floor, Wall, Light, Accent;
        public float LightIntensity;
        public int Level;              // 1..3, 4 — логово
        public LightPreset Preset;
        public MonsterKind A, B;
    }

    /// <summary>
    /// Подземелье: строит этаж, спавнит монстров, ведёт свой список живых (чистится по событию MonsterDied),
    /// при выходе — DespawnAll, уничтожение этажа, Resources.UnloadUnusedAssets + GC.Collect и отчёт в Console.
    /// </summary>
    public class DungeonManager : MonoBehaviour
    {
        public static DungeonManager I;
        public static readonly Vector3 Origin = new Vector3(1000, 0, 0);

        public SavedMonsterPrefabs SavedMonsters { get; set; }
        public bool InDungeon { get; private set; }
        public int Level { get; private set; }
        public int LocalCount => _mine.Count;
        public event Action<bool> Returned;            // true — вернулся после смерти
        public Func<Vector3> TavernSpawn;              // задаёт GameFlow
        public float TavernYaw;

        Transform _root;
        FloorLayout _layout;
        readonly List<Monster> _mine = new List<Monster>();
        // ---- пункт 4: в каждой комнате ≥ 3 монстров, недостающие появляются через 10–15 с из дальнего угла ----
        public const int PerRoom = 3;            // сколько монстров держим в каждой комнате (кроме стартовой)
        public const int FloorLimit = 45;        // всего созданных монстров за одну вылазку на этаж (21 сразу + до 24 новых)
        public const float RefillMin = 10f, RefillMax = 15f;
        readonly Dictionary<Monster, int> _roomOf = new Dictionary<Monster, int>();
        readonly Dictionary<int, float> _refillDue = new Dictionary<int, float>();
        float _nextRefillCheck;
        bool _refillOff;
        public int CreatedThisVisit { get; private set; }
        public int Refills { get; private set; }
        public Monster LastRefill { get; private set; }
        public int LastRefillRoom { get; private set; } = -1;
        /// <summary>Автотест: включить/выключить пополнение комнат (DebugClearMonsters выключает его до следующего входа).</summary>
        public bool RefillEnabled { get => !_refillOff; set { _refillOff = !value; _refillDue.Clear(); } }
        public int AliveInRoom(int ri) { int n = 0; foreach (var kv in _roomOf) if (kv.Value == ri && kv.Key != null && !kv.Key.Dead) n++; return n; }
        /// <summary>Через сколько секунд в комнате появится новый монстр (−1 — не ждём).</summary>
        public float RefillIn(int ri) => _refillDue.TryGetValue(ri, out var t) ? t - DTime.Now : -1f;
        public int RoomOf(Monster m) => m != null && _roomOf.TryGetValue(m, out var r) ? r : -1;
        public int TrackedCount => _roomOf.Count;
        /// <summary>Автотест: сделать вид, что за вылазку уже создано n монстров (проверка лимита этажа).</summary>
        public void DebugSetCreated(int n) => CreatedThisVisit = n;
        readonly List<string> _roomNames = new List<string>();
        readonly List<int> _roomTypes = new List<int>();
        public FloorLayout Layout => _layout;
        public Transform WorldRoot => _root;
        public Transform Root => _root;
        public IReadOnlyList<string> RoomNames => _roomNames;
        public IReadOnlyList<int> RoomTypeList => _roomTypes;

        /// <summary>Название комнаты, в которой точка (или «Коридор»).</summary>
        public string RoomNameAt(Vector3 p)
        {
            if (!InDungeon) return "Таверна";
            if (_layout == null) return "Логово дракона";
            int i = RoomIndexAt(p);
            return i >= 0 && i < _roomNames.Count ? _roomNames[i] : "Коридор";
        }

        public int RoomIndexAt(Vector3 p)
        {
            if (_layout == null) return -1;
            for (int i = 0; i < _layout.Rooms.Count; i++)
            {
                var r = _layout.Rooms[i];
                if (p.x >= r.min.x && p.x <= r.max.x && p.z >= r.min.z && p.z <= r.max.z) return i;
            }
            return -1;
        }

        public Vector3 RoomCenter(int i) { var r = _layout.Rooms[i]; return new Vector3(r.center.x, Origin.y, r.center.z); }
        public int RoomOfType(int type) { for (int i = 0; i < _roomTypes.Count; i++) if (_roomTypes[i] == type) return i; return -1; }
        public Bounds RoomFlatBounds(int i) { var r = _layout.Rooms[i]; var b = new Bounds(new Vector3(r.center.x, Origin.y, r.center.z), new Vector3(r.size.x, 0.01f, r.size.z)); b.min = new Vector3(b.min.x, Origin.y, b.min.z); return b; }

        /// <summary>Автотест/отладка: создать монстра в точке.</summary>
        public Monster DebugSpawn(MonsterKind kind, Vector3 pos, float yaw, Bounds? room = null)
        {
            var rb = room ?? new Bounds(pos, new Vector3(7f, 0.01f, 7f));
            return Spawn(kind, pos, rb, yaw);
        }

        /// <summary>Автотест: убрать всех монстров (через Die → кеш чистится).</summary>
        public void DebugClearMonsters()
        {
            var tmp = new List<Monster>(_mine);
            foreach (var m in tmp) if (m != null) m.Die(DeathReason.Despawned);
            RefillEnabled = false; // автотест расставляет своих монстров — комнаты не пополняем до следующего входа
        }
        bool _subscribed;

        public static Biome GetBiome(int level)
        {
            switch (level)
            {
                case 1: return new Biome { Name = "−1 Верхние пещеры", Floor = new Color(0.22f, 0.3f, 0.32f), Wall = new Color(0.25f, 0.38f, 0.4f), Light = new Color(0.3f, 0.9f, 1f), Accent = new Color(0.2f, 1f, 0.9f), LightIntensity = 1.6f, A = MonsterKind.Slime, B = MonsterKind.Mushroom,
                    Level = 1, // исправление 5: светлее, туман бирюзовый
                    Preset = new LightPreset { Ambient = new Color(0.33f, 0.39f, 0.39f), Fog = new Color(0.15f, 0.27f, 0.29f), Background = new Color(0.06f, 0.18f, 0.21f), FogStart = 9, FogEnd = 34 } };
                case 2: return new Biome { Name = "−2 Заросшие руины", Floor = new Color(.31f,.245f,.15f), Wall = new Color(.63f,.59f,.46f), Light = new Color(.92f,.78f,.41f), Accent = new Color(.76f,.62f,.29f), LightIntensity = 1.5f, A = MonsterKind.Mandrake, B = MonsterKind.Basilisk,
                    Level = 2, // туман травянисто-зелёный
                    Preset = new LightPreset { Ambient = new Color(.40f,.38f,.29f), Fog = new Color(.24f,.28f,.15f), Background = new Color(.14f,.16f,.09f), FogStart = 12, FogEnd = 45 } };
                case 3: return new Biome { Name = "−3 Крепость", Floor = new Color(0.28f, 0.26f, 0.25f), Wall = new Color(0.4f, 0.37f, 0.35f), Light = new Color(1f, 0.55f, 0.2f), Accent = new Color(1f, 0.45f, 0.1f), LightIntensity = 1.8f, A = MonsterKind.Armor, B = MonsterKind.Mimic,
                    Level = 3, // туман янтарный (факелы)
                    Preset = new LightPreset { Ambient = new Color(0.40f, 0.31f, 0.22f), Fog = new Color(0.28f, 0.17f, 0.08f), Background = new Color(0.16f, 0.09f, 0.04f), FogStart = 9, FogEnd = 32 } };
                default: return new Biome { Name = "Логово дракона", Floor = new Color(0.2f, 0.1f, 0.08f), Wall = new Color(0.3f, 0.14f, 0.1f), Light = new Color(1f, 0.25f, 0.1f), Accent = new Color(1f, 0.3f, 0f), LightIntensity = 2.2f, A = MonsterKind.Dragon, B = MonsterKind.Dragon,
                    Level = 4, // нейтральный дальний туман: силуэт дракона читается
                    Preset = new LightPreset { Ambient = new Color(.39f, .38f, .34f), Fog = new Color(.23f, .23f, .21f), Background = new Color(.12f, .12f, .105f), FogStart = 28, FogEnd = 65 } };
            }
        }

        /// <summary>Какой инструмент нужен, чтобы спуститься на уровень.</summary>
        public static ToolType? Requirement(int level)
        {
            switch (level) { case 2: return ToolType.Cleaver; case 3: return ToolType.Hammer; case 4: return ToolType.Shield; default: return null; }
        }

        public static bool CanEnter(int level, bool hint)
        {
            if (level < 1 || level > 4) return false;
            var req = Requirement(level);
            if (req == null || (PlayerRig.I != null && PlayerRig.I.Has(req.Value))) return true;
            if (hint) Hints.Show($"Нужен {Tools.Name(req.Value)} — купи в лавке", HintPri.Error);
            return false;
        }

        void Awake()
        {
            I = this;
            GameEvents.PlayerDied += OnPlayerDied;
        }

        void OnDestroy()
        {
            GameEvents.PlayerDied -= OnPlayerDied;
            Unsubscribe();
            if (I == this) I = null;
        }

        void Subscribe() { if (!_subscribed) { GameEvents.MonsterDied += OnMonsterDied; _subscribed = true; } }
        void Unsubscribe() { if (_subscribed) { GameEvents.MonsterDied -= OnMonsterDied; _subscribed = false; } }

        void OnMonsterDied(Monster m, DeathReason r)
        {
            _mine.Remove(m); _roomOf.Remove(m);
            // пункт 5: пойманный монстр иногда оставляет эликсир (25 %)
            if (r == DeathReason.Captured && InDungeon && _root != null && m != null && UnityEngine.Random.value < (ForceElixirDrop ? 1f : Elixirs.DropChance))
            {
                var pos = Level == 1 ? UpperCaves.SafeDrop(m.transform.position) : Level == 2 ? OvergrownRuins.SafeDrop(m.transform.position) : m.transform.position;
                ElixirPickup.Create(_root, pos);
                ElixirDrops++;
            }
        }
        /// <summary>Пункт 5: сколько эликсиров разложено на этаже при входе; сколько выпало из монстров; автотест — выпадение всегда.</summary>
        public int ElixirsOnFloor { get; private set; }
        public int ElixirDrops { get; private set; }
        public bool ForceElixirDrop;

        // ======================= вход =======================
        public void Enter(int level)
        {
            if (!CanEnter(level, true) || Fader.I.Busy) return;
            Fader.I.Run(() =>
            {
                if (InDungeon) Teardown();
                Build(level);
            });
        }

        void Build(int level)
        {
            Level = level;
            _refillOff = false; _refillDue.Clear(); _roomOf.Clear(); _nextRefillCheck = 0f;
            ElixirDrops = 0;
            InDungeon = true;
            var b = GetBiome(level);
            _root = new GameObject($"Dungeon_L{level}").transform;
            _root.position = Origin;
            WorldLighting.Apply(b.Preset);
            Subscribe();
            Vector3 startPos;
            if (level >= 4) startPos = BuildLair(b);
            else startPos = BuildFloor(level, b);
            BackpackView.ClosePanel();
            PlayerRig.I.Teleport(startPos, 0f);
            PlayerRig.I.Health.ProtectEntry(3f);
            CreatedThisVisit = _mine.Count; Refills = 0; LastRefill = null; LastRefillRoom = -1;
            Hints.Show(b.Name, HintPri.Window, 4f);
            Debug.Log($"[Dungeon] вход на {b.Name}: монстров {_mine.Count}, в реестре {MonsterRegistry.AliveCount}");
        }

        Vector3 BuildFloor(int level, Biome b)
        {
            if (level == 1) return BuildUpperCaves(b);
            if (level == 2) return BuildOvergrownRuins(b);
            _layout = FloorGenerator.Build(_root, Origin, b);
            var startRoom = _layout.Rooms[_layout.Start];
            var floorC = new Vector3(startRoom.center.x, Origin.y, startRoom.center.z);
            Portal.Create(_root, floorC + new Vector3(0, 0, -2.5f), "В таверну", new Color(1f, 0.8f, 0.4f), () => Exit(false));
            var stairsRoom = _layout.Rooms[_layout.Stairs];
            var stairsC = new Vector3(stairsRoom.center.x, Origin.y, stairsRoom.center.z);
            int next = level + 1;
            var door=TransitionDoor.Create(_root,_layout.TransitionEntrance,next,b);
            door.transform.rotation=Quaternion.LookRotation(_layout.TransitionForward);
            var landing=new GameObject("ThreeStepsAndPlatform").transform;landing.SetParent(_root,true);landing.position=stairsC;landing.rotation=door.transform.rotation;
            for(int step=0;step<3;step++) {
                float height=.2f*(step+1);
                Prim.Cube("Step_"+(step+1),landing,new Vector3(0,height*.5f,-1.6f+step*.6f),new Vector3(2.6f,height,.6f),new Color(.4f,.38f,.33f));
            }
            Prim.Cube("FlatUpperPlatform",landing,new Vector3(0,.3f,.9f),new Vector3(3.2f,.6f,2f),new Color(.4f,.38f,.33f));
            Portal.Create(_root,landing.TransformPoint(new Vector3(0,.6f,1.15f)),next==4?"В логово":"На −"+next,new Color(.48f,.6f,.85f),()=>Enter(next),()=>CanEnter(next,true));


            // типы комнат (8 на этаж), таблички, декор и ловушки по биому
            _roomNames.Clear(); _roomTypes.Clear();
            int order = 0;
            for (int i = 0; i < _layout.Rooms.Count; i++)
            {
                var r = _layout.Rooms[i];
                var c = new Vector3(r.center.x, Origin.y, r.center.z);
                int type = RoomTypes.Assign(i, _layout.Start, _layout.Stairs, order);
                if (i != _layout.Start && i != _layout.Stairs) order++;
                _roomTypes.Add(type);
                _roomNames.Add(RoomTypes.Names[level][type]);
                RoomDecor.Build(_root, level, type, c, b, r.size);
                Decorate(level, b, c, i == _layout.Start, i == _layout.Stairs, type);
            }

            // пункт 5: 2–3 целебных эликсира на этаж в случайных комнатах (не в стартовой)
            var rooms = new List<int>();
            for (int i = 0; i < _layout.Rooms.Count; i++) if (i != _layout.Start) rooms.Add(i);
            ElixirsOnFloor = 0;
            int want = UnityEngine.Random.Range(2, 4);
            for (int k = 0; k < want && rooms.Count > 0; k++)
            {
                int pick = rooms[UnityEngine.Random.Range(0, rooms.Count)];
                rooms.Remove(pick);
                var rc = _layout.Rooms[pick].center;
                ElixirPickup.Create(_root, new Vector3(rc.x + UnityEngine.Random.Range(-2.5f, 2.5f), Origin.y, rc.z + UnityEngine.Random.Range(-2.5f, 2.5f)));
                ElixirsOnFloor++;
            }

            // монстры (пункт 4): в каждой комнате, кроме стартовой, по 3; виды 60/40 между двумя монстрами этажа (+ «любимый» вид комнаты)
            for (int ri = 0; ri < _layout.Rooms.Count; ri++)
            {
                if (ri == _layout.Start) continue;
                for (int k = 0; k < PerRoom; k++) SpawnInRoom(b, ri, false);
            }
            DungeonArchitecture.SurfaceAll(_root,level);
            return floorC + new Vector3(0, 0, -1f);
        }

        // Только −2: отдельная городская архитектура. Механики портала/двери/монстров прежние.
        Vector3 BuildOvergrownRuins(Biome b)
        {
            _layout=FloorGenerator.Build(_root,Origin,b);
            _roomNames.Clear();_roomTypes.Clear();_roomNames.AddRange(OvergrownRuins.Names);_roomTypes.AddRange(OvergrownRuins.Types);
            var start=RoomCenter(0);var back=start+Vector3.back*2.8f;OvergrownRuins.Reserve(back);
            Portal.Create(_root,back,"В таверну",new Color(.65f,.53f,.32f),()=>Exit(false));
            var door=TransitionDoor.Create(_root,_layout.TransitionEntrance,3,b);door.transform.rotation=Quaternion.LookRotation(_layout.TransitionForward);
            if (!door.UsesSavedAsset) foreach(var n in new[]{"ArchL","ArchR","ArchTop"}){var r=door.transform.Find(n).GetComponent<Renderer>();r.sharedMaterial=OvergrownRuins.Stone;r.SetPropertyBlock(null);}
            var landing=new GameObject("RuinsThreeStepsAndWallLanding").transform;landing.SetParent(_root,true);landing.position=RoomCenter(7)+_layout.TransitionForward*(_layout.Rooms[7].extents.z-2.1f);landing.rotation=door.transform.rotation;
            for(int step=0;step<3;step++){float h=.2f*(step+1);OvergrownRuins.Box("Step_"+(step+1),landing,new Vector3(0,h*.5f,-1.6f+step*.6f),new Vector3(2.6f,h,.6f),OvergrownRuins.Stone);}
            OvergrownRuins.Box("FlatWallLanding",landing,new Vector3(0,.3f,.9f),new Vector3(3.2f,.6f,2),OvergrownRuins.Stone);
            var down=landing.TransformPoint(new Vector3(0,.6f,1.15f));OvergrownRuins.Reserve(down);
            Portal.Create(_root,down,"На −3",new Color(.61f,.47f,.26f),()=>Enter(3),()=>CanEnter(3,true));
            var wellPos=RoomCenter(6)+new Vector3(0,0,-1.8f);OvergrownRuins.Reserve(wellPos);
            var well=HealPickup.Create(_root,wellPos,25,HealKind.Well);
            foreach(var n in new[]{"WellRing","Roof"})well.transform.Find(n).GetComponent<Renderer>().enabled=false;
            foreach(var r in well.GetComponentsInChildren<Renderer>())if(r.name=="Post")r.enabled=false;
            for(int j=0;j<14;j++){float a=j*Mathf.PI/7;var g=OvergrownRuins.Box("OldWellStone",well.transform,new Vector3(Mathf.Cos(a)*.47f,.34f,Mathf.Sin(a)*.47f),new Vector3(.22f,.68f,.20f),OvergrownRuins.Stone);g.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}
            var water=well.transform.Find("Water");water.localPosition=new Vector3(0,.35f,0);water.GetComponent<Renderer>().sharedMaterial=OvergrownRuins.Water;water.GetComponent<Renderer>().SetPropertyBlock(null);
            var vines=Hazard.Create(_root,RoomCenter(1)+new Vector3(-2.7f,0,2.0f),"лозы",new Color(.38f,.47f,.19f),1,Dmg.TrapVines,Status.Slowed);vines.RuinsVisual(0);
            var collapsePos=RoomCenter(5)+new Vector3(2.3f,0,-1.9f);var collapse=Hazard.Create(_root,collapsePos,"камнепад",new Color(.65f,.58f,.40f),1.1f,Dmg.TrapRockfall);
            if(Physics.Raycast(collapsePos+Vector3.up,Vector3.up,out var roof,8,~0,QueryTriggerInteraction.Ignore))collapse.RuinsVisual(roof.point.y-collapsePos.y);
            var platePos=RoomCenter(2)+new Vector3(-6.4f,0,-1.8f);var darts=Hazard.Create(_root,platePos,"дротики",new Color(.62f,.51f,.30f),.8f,Dmg.TrapDarts);darts.RuinsVisual(0);
            if (!darts.UsesSavedVisual) OvergrownRuins.DartWallVisual(_root,RoomCenter(2)+new Vector3(-7.87f,0,-1.8f));
            ElixirsOnFloor=UnityEngine.Random.Range(2,4);for(int k=0;k<ElixirsOnFloor;k++){var p=OvergrownRuins.Point(new[]{1,4,6}[k]);ElixirPickup.Create(_root,p);OvergrownRuins.Reserve(p);}
            Physics.SyncTransforms();for(int ri=1;ri<8;ri++)for(int k=0;k<PerRoom;k++)SpawnInRoom(b,ri,false);
            return start+Vector3.back*.8f;
        }

        // Только −1: штатные портал, дверь, лечение, ловушки и спавн; без старого greybox-декора.
        Vector3 BuildUpperCaves(Biome b)
        {
            _layout = FloorGenerator.Build(_root, Origin, b);
            _roomNames.Clear(); _roomTypes.Clear();
            _roomNames.AddRange(UpperCaves.Names); _roomTypes.AddRange(UpperCaves.Types);
            var start = RoomCenter(0);
            var back = start + new Vector3(0,0,-2.6f);
            UpperCaves.Reserve(back);
            Portal.Create(_root,back,"В таверну",new Color(.65f,.53f,.32f),()=>Exit(false));
            var door = TransitionDoor.Create(_root,_layout.TransitionEntrance,2,b);
            door.transform.rotation = Quaternion.LookRotation(_layout.TransitionForward);
            if (!door.UsesSavedAsset) foreach(var n in new[]{"ArchL","ArchR","ArchTop"}){var r=door.transform.Find(n).GetComponent<Renderer>();r.sharedMaterial=CavePrototype.Rock;r.SetPropertyBlock(null);}
            var landing = new GameObject("ThreeStepsAndPlatform").transform;
            landing.SetParent(_root,true); landing.position=RoomCenter(7)+_layout.TransitionForward*(_layout.Rooms[7].extents.z-2.1f); landing.rotation=door.transform.rotation;
            for(int k=0;k<3;k++){float h=.2f*(k+1);UpperStone("Step_"+(k+1),landing,new Vector3(0,h*.5f,-1.6f+k*.6f),new Vector3(2.6f,h,.6f));}
            UpperStone("FlatUpperPlatform",landing,new Vector3(0,.3f,.9f),new Vector3(3.2f,.6f,2));
            var down=landing.TransformPoint(new Vector3(0,.6f,1.15f)); UpperCaves.Reserve(down);
            Portal.Create(_root,down,"На −2",new Color(.35f,.47f,.58f),()=>Enter(2),()=>CanEnter(2,true));
            // Колодец — прежнее лечение, мох исключительно декоративный.
            var wellPos=RoomCenter(3)+new Vector3(0,0,-2.8f);UpperCaves.Reserve(wellPos);
            var well=HealPickup.Create(_root,wellPos,25,HealKind.Well);
            well.transform.Find("WellRing").GetComponent<Renderer>().enabled=false;
            for(int j=0;j<12;j++){float a=j*Mathf.PI/6;var stone=UpperStone("WellStone",well.transform,new Vector3(Mathf.Cos(a)*.43f,.35f,Mathf.Sin(a)*.43f),new Vector3(.24f,.65f,.19f));stone.transform.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}
            var water=well.transform.Find("Water");water.localPosition=new Vector3(0,.24f,0);water.GetComponent<Renderer>().sharedMaterial=UpperCaves.Water;water.GetComponent<Renderer>().SetPropertyBlock(null);
            foreach(var spec in new[]{new Vector3(1,-2.9f,.3f),new Vector3(1,2.8f,-1.6f),new Vector3(4,0,0)}){
                var puddle=Puddle.Create(_root,RoomCenter((int)spec.x)+new Vector3(spec.y,0,spec.z),spec.x==4?2.8f:1.15f,new Color(.18f,.34f,.36f));foreach(var r in puddle.GetComponentsInChildren<Renderer>())r.enabled=false;
            }
            var hp=RoomCenter(5)+new Vector3(0,0,1.8f);
            var trap=Hazard.Create(_root,hp,"сталактит",new Color(.48f,.62f,.65f),.9f,Dmg.TrapStalactite);
            if(Physics.Raycast(hp+Vector3.up,Vector3.up,out var roofHit,8,~0,QueryTriggerInteraction.Ignore))trap.UpperCaveStoneVisual(roofHit.point.y-hp.y);
            Hazard.Create(_root,RoomCenter(2)+new Vector3(-1.5f,0,2.4f),"споры",new Color(.5f,.58f,.3f),1,Dmg.TrapSpores,Status.Spores);
            ElixirsOnFloor=UnityEngine.Random.Range(2,4);
            for(int k=0;k<ElixirsOnFloor;k++){var p=UpperCaves.Point(new[]{1,2,4}[k]);ElixirPickup.Create(_root,p);UpperCaves.Reserve(p);}
            Physics.SyncTransforms();
            for(int ri=1;ri<8;ri++)for(int k=0;k<PerRoom;k++)SpawnInRoom(b,ri,false);
            return start+Vector3.back*.8f;
        }
        static GameObject UpperStone(string name,Transform parent,Vector3 pos,Vector3 size){var g=Prim.Cube(name,parent,pos,size,Color.white);var r=g.GetComponent<Renderer>();r.sharedMaterial=CavePrototype.Rock;r.SetPropertyBlock(null);return g;}

        void Decorate(int level, Biome b, Vector3 c, bool start, bool stairs, int type)
        {
            Vector3 Rnd() => c + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0, UnityEngine.Random.Range(-3f, 3f));
            // светящиеся акценты по углам — главный «биомный» сигнал
            for (int k = 0; k < (level == 3 ? 0 : 2); k++)
            {
                var corner = c + new Vector3(k == 0 ? -3.6f : 3.6f, 0, UnityEngine.Random.Range(-3.4f, 3.4f));
                var deco = Prim.Cube(level == 3 ? "Torch" : level == 2 ? "Moss" : "Crystal", _root, Vector3.zero,
                    level == 3 ? new Vector3(0.12f, 0.5f, 0.12f) : level == 2 ? new Vector3(0.5f, 0.15f, 0.5f) : new Vector3(0.25f, 0.8f, 0.25f), b.Accent, false);
                deco.transform.position = corner + Vector3.up * (level == 3 ? 1.8f : level == 2 ? 0.08f : 0.4f);
                if (level == 1) deco.transform.rotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-20f, 20f));
                Prim.SetEmission(deco.GetComponent<Renderer>(), b.Accent * 1.5f);
                if (level == 3) deco.AddComponent<Flicker>().Init(Prim.PointLight(deco.transform, Vector3.up * 0.4f, b.Light, 5f, 1.2f));
            }
            // исправление 5: светящиеся грибы и кристаллы у стен (только эмиссия, без лишних источников света)
            if (level <= 2)
            {
                var capA = level == 1 ? new Color(0.3f, 1f, 0.95f) : new Color(0.85f, 1f, 0.3f);
                var capB = level == 1 ? new Color(0.75f, 0.45f, 1f) : new Color(1f, 0.5f, 0.75f);
                for (int k = 0; k < 3; k++)
                {
                    float side = k == 1 ? 1f : -1f;
                    var p = c + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0, side * 3.5f);
                    if (k == 2) p = c + new Vector3(UnityEngine.Random.value < 0.5f ? -3.5f : 3.5f, 0, UnityEngine.Random.Range(-2.5f, 2.5f));
                    if (level == 1 && k == 2) GlowDecor.Crystals(_root, p, new Color(0.45f, 0.75f, 1f), 4);
                    else GlowDecor.Mushrooms(_root, p, k == 0 ? capA : capB, UnityEngine.Random.Range(3, 6));
                }
            }
            if (start) return;
            switch (level)
            {
                case 1:
                    if (type == 1 || type == 5) Puddle.Create(_root, Rnd(), 0.8f, new Color(0.3f, 0.8f, 0.9f, 0.7f));
                    if (type == 4) Hazard.Create(_root, Rnd(), "сталактит", new Color(0.6f, 0.8f, 1f), 0.9f, Dmg.TrapStalactite);
                    else if (type == 3) Hazard.Create(_root, Rnd(), "споры", new Color(1f, 0.9f, 0.3f), 1.0f, Dmg.TrapSpores, Status.Spores);
                    else if (UnityEngine.Random.value < 0.4f) Hazard.Create(_root, Rnd(), "кислотная капля", new Color(0.4f, 1f, 0.6f), 0.9f, Dmg.TrapAcid);
                    if (type == 2) HealPickup.Create(_root, c + new Vector3(0, 0, 1.5f), 25f, HealKind.Well); // пункт 11: вместо мха — колодец
                    break;
                case 2:
                    if (type == 3) Hazard.Create(_root, Rnd(), "камнепад", new Color(0.8f, 0.7f, 0.5f), 1.1f, Dmg.TrapRockfall);
                    else if (type == 1 || type == 5) Hazard.Create(_root, Rnd(), "лозы", new Color(0.3f, 0.9f, 0.3f), 1.0f, Dmg.TrapVines, Status.Slowed);
                    else if (UnityEngine.Random.value < 0.5f) Hazard.Create(_root, Rnd(), "споровый гриб", new Color(1f, 0.9f, 0.3f), 1.0f, Dmg.TrapSpores, Status.Spores);
                    if (type == 4) HealPickup.Create(_root, c + new Vector3(0, 0, 1.5f), 25f, HealKind.Well);
                    break;
                case 3:
                    if (type == 1 || type == 2) FakeArmor.Create(_root, Vector3.zero, 0).transform.position = c + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0, 3.4f);
                    if (type == 4 || type == 5) Chest.Create(_root, Vector3.zero, 180f).transform.position = c + new Vector3(UnityEngine.Random.Range(-3f, 3f), 0, -3.3f);
                    if (type == 3) Hazard.Create(_root, Rnd(), "маятник", new Color(0.9f, 0.9f, 1f), 1.0f, Dmg.TrapPendulum);
                    else if (type == 2) Hazard.Create(_root, Rnd(), "дротики", new Color(1f, 0.3f, 0.2f), 0.9f, Dmg.TrapDarts);
                    else if (type == 1 || UnityEngine.Random.value < 0.4f) Hazard.Create(_root, Rnd(), "огненная решётка", new Color(1f, 0.4f, 0.1f), 0.9f, Dmg.TrapFireGrate);
                    if (type == 6) HealPickup.Create(_root, c + new Vector3(0, 0, 2.5f), 30f, HealKind.Well); // пункт 11: вместо свечи — колодец
                    break;
            }
            // пункт 11: случайного сухпайка больше нет — лечат только колодец и эликсир
        }

        Vector3 BuildLair(Biome b)
        {
            _layout = null;
            var c = Origin;
            if (!SavedDungeonAssets.TryBuildLair(_root)) DragonLair.Build(_root);
            HealPickup.Create(_root, c + new Vector3(-13, 0, -13), 30f, HealKind.Well);
            ElixirPickup.Create(_root, c + new Vector3(-13, 0, -7));
            ElixirPickup.Create(_root, c + new Vector3(13, 0, -7));
            ElixirsOnFloor = 2;
            // Return portal is created by OnDragonDefeated, not available before victory.
            var room = new Bounds(c + Vector3.up * 6, new Vector3(DragonLair.InteriorSize, 12, DragonLair.InteriorSize));
            Spawn(MonsterKind.Dragon, c + DragonLair.GroundPoints[0], room, 180);
            Physics.SyncTransforms();
            return c + new Vector3(0, 0, -14);
        }

        // ======================= спавн =======================
        Monster CreateMonster(MonsterKind kind, Vector3 pos, float yaw)
        {
            var rotation = Quaternion.Euler(0, yaw, 0);
            var prefab = SavedMonsters?.Get(kind);
            if (prefab != null)
                return Instantiate(prefab, pos, rotation, _root);

            var go = new GameObject(kind.ToString());
            go.transform.SetParent(_root, false);
            go.transform.SetPositionAndRotation(pos, rotation);
            switch (kind)
            {
                case MonsterKind.Slime: return go.AddComponent<Slime>();
                case MonsterKind.Mushroom: return go.AddComponent<Mushroom>();
                case MonsterKind.Mandrake: return go.AddComponent<Mandrake>();
                case MonsterKind.Basilisk: return go.AddComponent<Basilisk>();
                case MonsterKind.Armor: return go.AddComponent<LivingArmor>();
                case MonsterKind.Mimic: return go.AddComponent<Mimic>();
                default: return go.AddComponent<Dragon>();
            }
        }

        Monster Spawn(MonsterKind kind, Vector3 pos, Bounds room, float yaw)
        {
            var m = CreateMonster(kind, pos, yaw);
            m.Init(kind, this, room);
            _mine.Add(m);
            if (_layout != null) _roomOf[m] = RoomIndexAt(pos);
            CreatedThisVisit++;
            return m;
        }

        /// <summary>Отладка (F6): монстр этажа в 3 м перед игроком — проверить «убил → кеш очищен».</summary>
        public void DebugSpawnInFront()
        {
            if (!InDungeon || PlayerRig.I == null) { Hints.Show("F6 работает только в подземелье", HintPri.Error); return; }
            var b = GetBiome(Level);
            var f = PlayerRig.I.HeadForward; f.y = 0; f.Normalize();
            var pos = PlayerRig.I.Feet + f * 3f;
            var room = new Bounds(pos, new Vector3(6f, 0.01f, 6f));
            room.min = new Vector3(room.min.x, Origin.y, room.min.z);
            var kind = Level >= 4 ? MonsterKind.Dragon : (UnityEngine.Random.value < 0.5f ? b.A : b.B);
            if (kind == MonsterKind.Dragon) { Hints.Show("В логове дракон уже есть", HintPri.Error); return; }
            var m = Spawn(kind, new Vector3(pos.x, Origin.y, pos.z), room, Quaternion.LookRotation(-f).eulerAngles.y);
            Hints.Show($"Отладка: создан {m.name}. Реестр: {MonsterRegistry.AliveCount}", HintPri.Action);
        }

        MonsterKind PickKind(Biome b, int ri)
        {
            var kind = UnityEngine.Random.value < 0.6f ? b.A : b.B;
            var fav = ri < _roomTypes.Count ? RoomTypes.Favorite(Level, _roomTypes[ri]) : null;
            if (fav.HasValue && UnityEngine.Random.value < 0.75f) kind = fav.Value;
            return kind;
        }

        /// <summary>Монстр в комнате ri. refill = пополнение: в дальнем от игрока углу, с эффектом «выполз из тени».</summary>
        Monster SpawnInRoom(Biome b, int ri, bool refill)
        {
            if (_layout == null || ri < 0 || ri >= _layout.Rooms.Count) return null;
            var r = _layout.Rooms[ri];
            var room = new Bounds(new Vector3(r.center.x, Origin.y, r.center.z), new Vector3(r.size.x, 0.01f, r.size.z));
            room.min = new Vector3(room.min.x, Origin.y, room.min.z);
            var cands = new List<Vector3>();
            if (refill)
            {   // 4 угла комнаты, самый дальний от игрока — первым
                const float inset = 1.3f;
                foreach (var sx in new[] { -1f, 1f }) foreach (var sz in new[] { -1f, 1f })
                    cands.Add(new Vector3(r.center.x + sx * (r.extents.x - inset), Origin.y, r.center.z + sz * (r.extents.z - inset)));
                var feet = PlayerRig.I != null ? PlayerRig.I.Feet : r.center;
                cands.Sort((p, q) => (q - feet).sqrMagnitude.CompareTo((p - feet).sqrMagnitude));
                for (int k = 0; k < 6; k++) cands.Add(new Vector3(UnityEngine.Random.Range(r.min.x + 1.2f, r.max.x - 1.2f), Origin.y, UnityEngine.Random.Range(r.min.z + 1.2f, r.max.z - 1.2f))); // углы заняты — любое свободное место подальше
            }
            else for (int k = 0; k < (Level == 1 ? 40 : Level == 2 ? 40 : 12); k++) cands.Add(new Vector3(UnityEngine.Random.Range(r.min.x + 1.2f, r.max.x - 1.2f), Origin.y, UnityEngine.Random.Range(r.min.z + 1.2f, r.max.z - 1.2f)));
            foreach (var pos in cands)
            {
                if (Level == 1 && !UpperCaves.Free(pos, ri)) continue;
                if (Level == 2) {
                    if (!OvergrownRuins.Free(pos,ri)) continue;
                    bool occupied=false;foreach(var alive in _mine){if(alive==null || alive.Dead)continue;var d=alive.transform.position-pos;d.y=0;if(d.sqrMagnitude<1.44f){occupied=true;break;}}
                    if(occupied)continue;Physics.SyncTransforms();
                }
                if (refill && PlayerRig.I != null && (PlayerRig.I.Feet - pos).magnitude < 4f) continue; // не под носом у игрока
                if (Physics.CheckSphere(pos + Vector3.up * 0.6f, 0.45f, ~0, QueryTriggerInteraction.Ignore)) continue; // колонна/сундук/монстр
                var m = Spawn(PickKind(b, ri), pos, room, refill ? Quaternion.LookRotation(new Vector3(r.center.x, 0, r.center.z) - new Vector3(pos.x, 0, pos.z)).eulerAngles.y : UnityEngine.Random.Range(0f, 360f));
                _roomOf[m] = ri;
                if (refill)
                {
                    SpawnFx(pos);
                    if (PlayerRig.I != null && RoomIndexAt(PlayerRig.I.Feet) == ri) Hints.Show($"Из тёмного угла выполз новый монстр — {Bestiary.Get(m.Kind).Name}!", HintPri.Action, 2.5f);
                }
                return m;
            }
            return null;
        }

        /// <summary>Эффект появления: тёмно-фиолетовое облако + кольцо на полу + звук. Всё само удаляется.</summary>
        static void SpawnFx(Vector3 pos)
        {
            var purple = new Color(0.55f, 0.25f, 0.85f);
            FX.Vanish(pos + Vector3.up * 0.4f, 1.6f, purple);
            var ring = FX.Disc(null, pos + Vector3.up * 0.02f, 0.9f, purple * 0.6f);
            Prim.SetEmission(ring.GetComponent<Renderer>(), purple * 1.5f);
            UnityEngine.Object.Destroy(ring, 1.5f);
            Sfx.Play(SfxId.Whoosh, pos);
        }

        void Update()
        {
            if (!InDungeon || Level >= 4 || _layout == null || DTime.Delta <= 0 || _refillOff) return;
            if (DTime.Now < _nextRefillCheck) return;
            _nextRefillCheck = DTime.Now + 0.5f;
            for (int ri = 0; ri < _layout.Rooms.Count; ri++)
            {
                if (ri == _layout.Start) continue;
                if (AliveInRoom(ri) >= PerRoom) { _refillDue.Remove(ri); continue; }
                if (CreatedThisVisit >= FloorLimit) continue; // лимит этажа — больше не создаём
                if (!_refillDue.TryGetValue(ri, out var due)) { _refillDue[ri] = DTime.Now + UnityEngine.Random.Range(RefillMin, RefillMax); continue; }
                if (DTime.Now < due) continue;
                var m = SpawnInRoom(GetBiome(Level), ri, true);
                if (m != null)
                {
                    Refills++; LastRefill = m; LastRefillRoom = ri;
                    _refillDue.Remove(ri);
                    Debug.Log($"[Dungeon] пополнение: комната {ri} «{(ri < _roomNames.Count ? _roomNames[ri] : "?")}» — {m.Kind}; живых {_mine.Count}, создано за вылазку {CreatedThisVisit}/{FloorLimit}");
                }
                else _refillDue[ri] = DTime.Now + 3f; // все углы заняты или игрок рядом — попробуем позже
            }
        }

        // ======================= выход =======================
        public static string LastHomeLoot = "";
        public void Exit(bool died)
        {
            if (!InDungeon || Fader.I.Busy) return;
            Fader.I.Run(() =>
            {
                Teardown();
                // Единая атомарная операция переноса: из рюкзака убирается только принятое ледником.
                int moved = 0;
                var got = new List<ItemId>(); var parts = new List<string>();
                foreach(var row in Inventory.TransferToFridge())
                {
                    moved+=row.n;got.Add(row.id);parts.Add($"{Items.Name(row.id)} ×{row.n}");
                }
                int lost = Inventory.Backpack.Total; // не потеряно: это неперенесённый остаток
                if (Fridge.I != null) Fridge.I.MarkNew(got);
                // Удаляем только реально перенесённое; остаток не уничтожается.
                BackpackView.ClosePanel();
                var spawn = TavernSpawn != null ? TavernSpawn() : Vector3.zero;
                PlayerRig.I.Teleport(spawn, TavernYaw);
                if (died) PlayerRig.I.Health.Revive(); // пункт 11: таверна НЕ лечит; после обморока Тоби ставит на ноги (это не лечение, а «новая попытка»)
                WorldLighting.Apply(WorldLighting.Tavern);
                string msg = moved > 0 ? $"В ледник на кухне: {moved} шт. — {string.Join(", ", parts)}" : "Без добычи";
                if (lost > 0) msg += $" (осталось в рюкзаке: {lost})";
                LastHomeLoot = msg;
                Hints.Show(died ? "Тоби вытащил тебя в таверну. " + msg : msg, HintPri.Window, 5f);
                if (moved > 0) Banner.Show(true, "Добыча в леднике!", string.Join(" · ", parts), "Ледник — шкаф у стены кухни справа. Подойди и нажми E");
                Returned?.Invoke(died);
            });
        }

        /// <summary>Полная очистка этажа: монстры, их кеш, объекты, неиспользуемые ассеты.</summary>
        void Teardown()
        {
            int before = MonsterRegistry.AliveCount;
            BestiaryCard.ForceClose();
            Hints.Clear(); // пункт 6: подсказки этажа («Не смотри ему в глаза!») не переезжают в таверну
            PlayerRig.I.Left.ForceRelease();
            PlayerRig.I.Right.ForceRelease();
            MonsterRegistry.DespawnAll();   // каждый монстр: Die(Despawned) → отписки, удаление из реестра, Destroy
            _mine.Clear();
            _roomOf.Clear(); _refillDue.Clear(); // кеш комнат чистится вместе с этажом
            Unsubscribe();
            if (_root != null) Destroy(_root.gameObject);
            _root = null;
            _layout = null;
            _roomNames.Clear(); _roomTypes.Clear();
            InDungeon = false;
            StartCoroutine(UnloadAndReport(before));
        }

        IEnumerator UnloadAndReport(int before)
        {
            yield return null; // Destroy срабатывает в конце кадра
            long memBefore = GC.GetTotalMemory(false);
            yield return Resources.UnloadUnusedAssets();
            GC.Collect();
            long memAfter = GC.GetTotalMemory(true);
            Debug.Log($"[Cleanup] этаж выгружен: было монстров {before}, в реестре {MonsterRegistry.AliveCount}, в кеше {MonsterRegistry.CacheCount}, " +
                      $"подписчиков MonsterDied {GameEvents.MonsterDiedSubscribers}, PlayerDied {GameEvents.PlayerDiedSubscribers}, " +
                      $"память {memBefore / 1048576f:0.0} → {memAfter / 1048576f:0.0} МБ. {MonsterRegistry.Audit()}");
        }

        void OnPlayerDied()
        {
            if (!InDungeon) { PlayerRig.I.Health.Revive(); return; }
            Inventory.Backpack.LoseHalf();
            Hints.Clear(); // пункт 6: старые подсказки этажа не остаются после смерти
            Hints.Show("Ты потерял сознание… Тоби вытащил тебя. Половина рюкзака потеряна.", HintPri.Danger, 5f);
            Exit(true);
        }

        public void OnDragonDefeated()
        {
            Hints.Show("Дракон повержен! Возвращайся в таверну — сегодня придёт Король.", HintPri.Window, 6f);
            if (GameFlow.I != null) GameFlow.I.OnDragonDefeated();
            if (_root != null && PlayerRig.I != null)
                Portal.Create(_root, PlayerRig.I.Feet + PlayerRig.I.transform.forward * -1.5f, "Домой", new Color(1f, 0.85f, 0.3f), () => Exit(false));
        }
    }

    /// <summary>Мерцание факела (крепость).</summary>

}
