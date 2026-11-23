using System.Collections.Generic;
using UnityEngine;
namespace Podz
{
    /// <summary>Один жетон для всех атак и эффектов. FIFO, пауза после освобождения, короткая аренда для побочных атак.</summary>
    public static class AttackGate
    {
        public const int MaxNormal = 1, MaxLowHp = 1;
        public const float LowHp = 30f, LowHpGap = 3f, NormalGap = 2f;
        static Monster _owner;
        static float _leaseUntil, _nextAt, _lastStart = -99f, _worldUntil;
        static readonly List<Monster> _queue = new List<Monster>();
        static readonly Dictionary<Monster,float> _requests = new Dictionary<Monster,float>();
        public static int MaxSeen, Denied;
        public static float LastStart => _lastStart;
        public static float NextAllowed => _nextAt;
        public static int Limit => 1;
        static bool LowHpNow => PlayerRig.I != null && PlayerRig.I.Health.HP < LowHp;
        static bool Valid(Monster m) => m != null && !m.Dead && !m.Asleep && m.State != "Оглушён" && PlayerRig.I != null && PlayerRig.I.Health.HP > 0 && !PlayerRig.I.Health.EntryProtected &&
            Vector3.Distance(m.transform.position, PlayerRig.I.Feet) < (m.Kind == MonsterKind.Dragon ? 60f : 16f);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { _owner = null; _queue.Clear(); _requests.Clear(); _nextAt = 0; _worldUntil = 0; _lastStart = -99; MaxSeen = Denied = 0; }
        static void Prune()
        {
            if(_worldUntil > 0 && DTime.Now >= _worldUntil) { _worldUntil = 0; _nextAt = DTime.Now + (LowHpNow ? LowHpGap : NormalGap); }
            if (!ReferenceEquals(_owner,null) && (!Valid(_owner) || DTime.Now > _leaseUntil)) Release(_owner);
            for (int i = _queue.Count-1; i >= 0; i--)
            {
                var m = _queue[i];
                if (!Valid(m) || !_requests.TryGetValue(m,out float t) || DTime.Now-t > .8f) { _queue.RemoveAt(i); _requests.Remove(m); }
            }
        }
        public static int Count { get { Prune(); return _owner != null || _worldUntil > 0 ? 1 : 0; } }
        public static bool Holds(Monster m) { Prune(); return _owner != null && _owner == m; }
        public static bool Request(Monster m)
        {
            Prune(); if (!Valid(m)) return false;
            if (_owner == m) { _leaseUntil = DTime.Now + .6f; return true; }
            if (!_queue.Contains(m)) _queue.Add(m);
            _requests[m] = DTime.Now;
            if (_owner != null || _worldUntil > 0 || MandrakeCry.PendingCount > 0 || DTime.Now < _nextAt || _queue[0] != m) { Denied++; return false; }
            _queue.RemoveAt(0); _requests.Remove(m);
            _owner = m; _leaseUntil = DTime.Now + .6f; _lastStart = DTime.Now; MaxSeen = 1; return true;
        }
        public static bool MayHurt(Monster m) => Request(m);
        public static bool TryWorldAttack() {
            Prune(); var p=PlayerRig.I;
            if(p==null || p.Health.HP<=0 || p.Health.EntryProtected || _owner!=null || _worldUntil>0 || DTime.Now<_nextAt) return false;
            _worldUntil=DTime.Now+.6f; _lastStart=DTime.Now; MaxSeen=1; return true;
        }
        public static void Release(Monster m)
        {
            if (ReferenceEquals(m,null)) return;
            _queue.Remove(m); _requests.Remove(m);
            if (_owner != m) return;
            _owner = null; _nextAt = DTime.Now + (LowHpNow ? LowHpGap : NormalGap);
        }
        public static void Clear() { _worldUntil = 0; _owner = null; _queue.Clear(); _requests.Clear(); _nextAt = DTime.Now + NormalGap; }
    }
}
