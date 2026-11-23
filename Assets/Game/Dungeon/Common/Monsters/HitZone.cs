using UnityEngine;

namespace Podz
{
    /// <summary>Коллайдер-зона попадания (голова, шея, спина...).</summary>
    public class HitZone : MonoBehaviour, IHittable
    {
        public Monster Owner;
        public Zone Zone;
        public void OnToolHit(HitInfo h) { if (Owner != null && !Owner.Dead) Owner.OnHit(h, Zone); }

        public static HitZone Add(GameObject go, Monster owner, Zone zone)
        {
            var z = go.AddComponent<HitZone>();
            z.Owner = owner; z.Zone = zone;
            return z;
        }
    }

}
