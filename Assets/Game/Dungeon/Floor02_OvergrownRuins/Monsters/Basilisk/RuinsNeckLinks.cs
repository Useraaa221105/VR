using System.Collections.Generic;
using UnityEngine;
namespace Podz {
    /// <summary>Только зрительные соединители шеи василиска −2; не управляет ударами, состояниями или звуком.</summary>
    [DefaultExecutionOrder(50)]
    public class RuinsNeckLinks : MonoBehaviour {
        public Transform Neck,Head;public List<Transform> Links=new List<Transform>();
        public void Refresh(){if(Neck==null || Head==null || Links.Count!=2)return;
            var points=new[]{transform.TransformPoint(new Vector3(0,1.15f,.30f)),Neck.position,Head.position-transform.up*.08f};
            for(int j=0;j<2;j++){var delta=points[j+1]-points[j];var t=Links[j];t.position=(points[j+1]+points[j])*.5f;t.rotation=Quaternion.FromToRotation(Vector3.up,delta.normalized);t.localScale=new Vector3(.20f,delta.magnitude*.5f+.08f,.20f);}
        }
        void LateUpdate(){Refresh();}
    }
}
