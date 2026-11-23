using System;
using UnityEngine;
namespace Podz
{
    public class FaceViewer : MonoBehaviour
    {
        void LateUpdate() { if (PlayerRig.I != null) Prim.FaceTo(transform, PlayerRig.I.HeadPos); }
    }
}
