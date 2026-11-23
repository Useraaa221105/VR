using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    public class Toby : MonoBehaviour
    {
        class Job { public Guest G; public Bowl B; public Vector3 Pickup, Service; public int Step, Waypoint; public float Travel; }
        readonly List<Job> _jobs = new List<Job>();
        Vector3 _home;
        [SerializeField] Transform _carry;
        [SerializeField, Min(.01f)] float Speed = 2.5f, DeliveryTime = 3f;

        public static Toby Create(Vector3 home)
        {
            var prefab = SavedDungeonAssets.Active?.Toby;
            if (prefab != null && prefab._carry != null)
            {
                var saved = Instantiate(prefab, home, Quaternion.identity);
                saved._home = home;
                SavedDungeonAssets.RestoreText(saved.gameObject);
                return saved;
            }
            return NewVisual(home);
        }

#if UNITY_EDITOR
        public static Toby BuildForEditor() => NewVisual(Vector3.zero);
#endif

        static Toby NewVisual(Vector3 home)
        {
            var go = new GameObject("Toby"); go.transform.position = home;
            var t = go.AddComponent<Toby>(); t._home = home;
            Prim.Cube("Body", go.transform, new Vector3(0, 0.45f, 0), new Vector3(0.35f, 0.9f, 0.3f), new Color(0.3f, 0.5f, 0.3f), false);
            Prim.Cube("Head", go.transform, new Vector3(0, 1.05f, 0), new Vector3(0.28f, 0.28f, 0.28f), new Color(0.95f, 0.8f, 0.65f), false);
            Prim.Text(go.transform, new Vector3(0, 1.45f, 0), "Тоби", 0.08f, new Color(0.8f, 1f, 0.8f)).gameObject.AddComponent<FaceCam>();
            t._carry = new GameObject("CarryPoint").transform;
            t._carry.SetParent(go.transform, false); t._carry.localPosition = new Vector3(0, .95f, .3f);
            return t;
        }

        public bool Deliver(Guest g, Bowl b)
        {
            if (g == null || b == null || !g.Waiting || g.Assigned || g.Order != b.Recipe || _jobs.Count >= 2 || !b.Reserve(this)) return false;
            g.Assigned = true;
            Debug.Log($"[TavernDelivery] reserved table={g.Table + 1} bowl={b.GetHashCode()}");
            _jobs.Add(new Job { G = g, B = b, Pickup = b.transform.position,
                Service = g.transform.position + new Vector3(0, 0, g.transform.position.z > 0 ? -.8f : .8f) });
            return true;
        }

        public string StatusFor(Guest g)
        {
            var j = _jobs.Find(x => x.G == g);
            return j == null ? "ждёт" : j.Step == 0 ? "Тоби идёт к стойке" : j.Step == 1 ? "несут" : "возврат";
        }

        public void ResetAll()
        {
            foreach (var j in _jobs)
            {
                if (j.G != null) { j.G.Assigned = false; j.G.DishInTransit = false; }
                if (j.B != null) j.B.ReleaseDelivery(j.Pickup, true);
            }
            _jobs.Clear(); transform.position = _home;
        }

        bool Walk(Vector3 target, float dt)
        {
            target.y = transform.position.y;
            var delta = target - transform.position;
            if (delta.magnitude < .08f) return true;
            var dir = delta.normalized;
            // Нет телепорта через стену и нет физического толкания игрока. При занятом проходе ждём/обходим.
            foreach (float turn in new[] { 0f, 45f, -45f, 90f, -90f })
            {
                var stepDir = Quaternion.Euler(0, turn, 0) * dir;
                var next = transform.position + stepDir * Mathf.Min(Speed * dt, delta.magnitude);
                var player = PlayerRig.I;
                if (player != null) { var near = next - player.Feet; near.y = 0; if (near.sqrMagnitude < .45f * .45f) continue; }
                if (Physics.CheckCapsule(next + Vector3.up * .22f, next + Vector3.up * 1.15f, .18f, ~(1 << 2), QueryTriggerInteraction.Ignore)) continue;
                transform.position = next; transform.rotation = Quaternion.LookRotation(stepDir); break;
            }
            return false;
        }

        void Finish(Job j)
        {
            if (j.G != null) { j.G.Assigned = false; j.G.DishInTransit = false; }
            _jobs.Remove(j);
        }

        void Update()
        {
            float dt = DTime.Delta;
            if (dt <= 0 || (DungeonManager.I != null && DungeonManager.I.InDungeon)) return;
            if (_jobs.Count == 0) { Walk(_home, dt); return; }
            var j = _jobs[0];
            if (j.B == null) { Finish(j); return; }
            if (j.Step < 2 && (j.G == null || !j.G.Waiting))
            {
                if (j.Step == 0) { j.B.ReleaseDelivery(j.Pickup, true); Finish(j); return; }
                j.Step = 2; j.Waypoint = 0;
                if (j.G != null) { j.G.Assigned = false; j.G.DishInTransit = false; }
            }
            if (j.Step == 0)
            {
                var counter = new Vector3(4.65f, 0, j.Pickup.z);
                if (!Walk(counter, dt)) return;
                if (!Kitchen.I.InWindow(j.B.transform.position) || !j.B.BeginCarry(_carry))
                { j.B.ReleaseDelivery(j.Pickup, true); Finish(j); return; }
                Debug.Log($"[TavernDelivery] picked table={j.G.Table + 1} bowl={j.B.GetHashCode()}");
                j.G.DishInTransit = true; j.Step = 1; j.Waypoint = 0; j.Travel = 0;
                return;
            }
            if (j.Step == 1)
            {
                j.Travel += dt;
                Vector3 target = j.Waypoint == 0 ? new Vector3(5.15f, 0, 0) : j.Waypoint == 1 ? new Vector3(j.Service.x, 0, 0) : j.Service;
                if (!Walk(target, dt)) return;
                if (j.Waypoint < 2) { j.Waypoint++; return; }
                if (j.Travel < DeliveryTime) return;
                bool accepted = j.G.Receive(j.B.Recipe, j.B.Stars, j.B.HalfPrice);
                j.G.Assigned = false; j.G.DishInTransit = false;
                if (accepted)
                {
                    j.B.ServeTo(j.G, Kitchen.I.Tables[j.G.Table].position + Vector3.up * .84f);
                    j.G.SetDish(j.B);
                    Debug.Log($"[TavernDelivery] accepted table={j.G.Table + 1} bowl={j.B.GetHashCode()}");
                    Finish(j); return;
                }
                Hints.Show($"Стол {j.G.Table + 1}: блюдо отклонено — Тоби возвращает ту же миску", HintPri.Error, 4f);
                j.Step = 2; j.Waypoint = 0;
                return;
            }
            Vector3 homeward = j.Waypoint == 0 ? new Vector3(j.Service.x, 0, 0) : j.Waypoint == 1 ? new Vector3(5.15f, 0, 0) : new Vector3(4.65f, 0, j.Pickup.z);
            if (!Walk(homeward, dt)) return;
            if (j.Waypoint < 2) { j.Waypoint++; return; }
            if (!Kitchen.I.TryReturnSlot(j.B, out var point)) return; // заполненная стойка: держим предмет, не теряем
            Debug.Log($"[TavernDelivery] returned bowl={j.B.GetHashCode()}");
            j.B.ReleaseDelivery(point, true); Finish(j);
        }
    }
}
