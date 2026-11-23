using UnityEngine;

namespace Podz
{
    /// <summary>Часть монстра, которую берут рукой (листья мандрагоры, шлем доспеха, нога мимика).</summary>
    public class MonsterGrabPoint : MonoBehaviour, IGrabbable
    {
        public Monster Owner;
        public bool Active = true;
        HandController _hand;
        Vector3 _startHand;

        public static MonsterGrabPoint Add(GameObject go, Monster owner, bool active = true)
        {
            var g = go.AddComponent<MonsterGrabPoint>();
            g.Owner = owner; g.Active = active;
            return g;
        }

        public bool CanGrab(HandController hand) => Active && _hand == null && Owner != null && !Owner.Dead;
        public void Grab(HandController hand) { _hand = hand; _startHand = hand.transform.position; Owner.OnGrab(this, hand); }

        public void Release(HandController hand, Vector3 v)
        {
            var h = _hand;
            _hand = null;
            if (Owner == null || Owner.Dead) return;
            bool pulled = v.y > 1.5f || (h != null && (h.transform.position - _startHand).magnitude > 0.22f);
            Owner.OnGrabRelease(this, hand, pulled);
        }

        public bool Held => _hand != null;
        // Read-only visual target; does not move the grab point or alter pulling thresholds.
        public Vector3 HoldPosition => _hand != null ? _hand.transform.position : transform.position;
        public float PullDistance => _hand == null ? 0 : (_hand.transform.position - _startHand).magnitude;
        public float PullUp => _hand == null ? 0 : _hand.transform.position.y - _startHand.y;

        public void ForceDrop()
        {
            if (_hand != null) { var h = _hand; _hand = null; h.ClearIfHolding(this); }
        }
    }
}
