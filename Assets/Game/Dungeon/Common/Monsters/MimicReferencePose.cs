using UnityEngine;

namespace Podz
{
    public class MimicReferencePose : MonoBehaviour
    {
        public Monster Owner; public Transform Interior, Lid;
        void LateUpdate()
        {
            if (Owner == null || Owner.Dead) return;
            bool open = Owner.State != "Маскировка";
            Interior.gameObject.SetActive(open);
            if (DTime.Delta > 0) Lid.localRotation = Quaternion.Slerp(Lid.localRotation, Quaternion.Euler(open ? -38f : 0, 0, 0), DTime.Delta * 6);
        }
    }
}