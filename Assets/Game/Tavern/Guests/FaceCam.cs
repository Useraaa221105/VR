using UnityEngine;

namespace Podz
{
    public class FaceCam : MonoBehaviour
    {
        void LateUpdate() { if (PlayerRig.I != null) Prim.FaceTo(transform, PlayerRig.I.HeadPos); }
    }
}
