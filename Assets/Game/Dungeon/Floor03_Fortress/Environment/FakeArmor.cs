using UnityEngine;

namespace Podz
{
    public class FakeArmor : MonoBehaviour, IHittable
    {
        public static FakeArmor Create(Transform parent, Vector3 pos, float yaw)
        {
            var go = new GameObject("FakeArmor");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            LivingArmor.BuildArmorShape(go.transform, false, null, out _, out _);
            FortressCreatures.Armor(go.transform, false);
            return go.AddComponent<FakeArmor>();
        }

        public void OnToolHit(HitInfo h)
        {
            if (h.Speed > 0.5f) Hints.Show("Пустой доспех — в щелях шлема нет слизи", HintPri.Action);
        }
    }
}
