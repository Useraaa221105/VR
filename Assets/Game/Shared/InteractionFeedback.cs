using UnityEngine;
using UnityEngine.XR;
namespace Podz
{
    public static class InteractionFeedback
    {
        public static void Emit(Vector3 pos, HandController hand = null, bool success = false)
        {
            Sfx.Play(success ? SfxId.Success : SfxId.Click, pos, .35f);
            var flash=new GameObject("InteractionFlash"); flash.transform.position=pos;
            for(int i=0;i<4;i++)Prim.Sphere("Spark",flash.transform,Quaternion.Euler(0,i*90,0)*Vector3.forward*.025f,Vector3.one*.018f,success?new Color(1f,.8f,.3f):Color.white);
            Object.Destroy(flash,.18f);
            if(hand==null && PlayerRig.I!=null && PlayerRig.I.IsXR)
            {
                var p=PlayerRig.I;
                hand=Vector3.Distance(p.Left.transform.position,pos)<Vector3.Distance(p.Right.transform.position,pos)?p.Left:p.Right;
            }
            if (hand == null || PlayerRig.I == null || !PlayerRig.I.IsXR) return;
            var device = InputDevices.GetDeviceAtXRNode(hand.Node);
            if (device.TryGetHapticCapabilities(out var caps) && caps.supportsImpulse) device.SendHapticImpulse(0,.25f,.07f);
        }
    }
}
