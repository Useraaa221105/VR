using UnityEngine;
using UnityEngine.InputSystem;

namespace Podz
{
    /// <summary>Boss-only supporting grip. Reads existing XR controls; does not change XR configuration.</summary>
    [DefaultExecutionOrder(-20)]
    public class DragonFinisher : MonoBehaviour
    {
        public bool DesktopStroke { get; private set; }
        Tool _tool;
        float _until;
        public static DragonFinisher BeginDesktop(Tool tool)
        {
            var p=PlayerRig.I;
            if(p==null || p.IsXR || tool==null || tool.Type!=ToolType.Cleaver || tool.Holder!=p.Right ||
                p.Left.Held!=null || p.Left.ElixirOut || !DIn.Held(Key.LeftShift) || DungeonManager.I==null || DungeonManager.I.Level!=4) return null;
            var f=tool.GetComponent<DragonFinisher>();
            if(f==null)f=tool.gameObject.AddComponent<DragonFinisher>();
            f._tool=tool;f.DesktopStroke=true;f._until=Time.time+.8f;return f;
        }
        public void EndDesktop(){DesktopStroke=false;}
        void LateUpdate()
        {
            if(!DesktopStroke)return;
            var p=PlayerRig.I;
            if(p==null || p.IsXR || _tool==null || _tool.Holder!=p.Right || Time.time>_until){DesktopStroke=false;return;}
            if(p.Left.Held==null)
            {
                p.Left.transform.position=_tool.transform.TransformPoint(new Vector3(-.055f,0,-.035f));
                p.Left.transform.rotation=_tool.transform.rotation;
            }
        }
        public static bool TwoHands(HitInfo hit)
        {
            var p=PlayerRig.I;var tool=hit.Tool;
            if(p==null || tool==null || tool.Holder==null)return false;
            var other=tool.Holder==p.Right?p.Left:p.Right;
            if(other==null || other.Held!=null || other.ElixirOut)return false;
            if(!p.IsXR)
            {
                var f=tool.GetComponent<DragonFinisher>();
                return f!=null && f.DesktopStroke && DIn.Held(Key.LeftShift);
            }
            // Empty supporting hand closes Grip at the cleaver handle, not simply anywhere nearby.
            var handle=tool.transform.TransformPoint(new Vector3(0,0,-.035f));
            return XRIn.Grip(other.Node)>.6f && Vector3.Distance(other.transform.position,handle)<.16f;
        }
        public static bool ForwardThrust(HitInfo hit,Vector3 target)
        {
            if(hit.Tool==null || hit.Speed<2 || !hit.Edge)return false;
            var blade=hit.Tool.transform.forward;
            var toTarget=(target-hit.Tool.transform.position).normalized;
            return Vector3.Dot(hit.Dir,blade)>.75f && Vector3.Dot(hit.Dir,toTarget)>.65f;
        }
    }
}