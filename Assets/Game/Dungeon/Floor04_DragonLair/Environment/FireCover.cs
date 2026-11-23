using System.Collections.Generic;
using UnityEngine;
namespace Podz
{
    public class FireCover : MonoBehaviour
    {
        static readonly List<FireCover> Active = new List<FireCover>();
        public int Integrity { get; private set; } = 3;
        public bool Intact => Integrity > 0;
        [SerializeField] MeshCollider _collider; [SerializeField] Transform _visual;
        public static FireCover Create(Transform parent,Vector3 local)
        {
            var t=ReferenceMesh.Node("FireBlockingStoneCover",parent,local);
            var c=t.gameObject.AddComponent<FireCover>();
            var g=ReferenceMesh.Shell("RoughBrokenColumn",t,Vector3.zero,
                new[]{0f,.06f,.55f,2.7f,3.55f,DragonLair.CoverHeight},
                new[]{0f,1.6f,1.48f,1.25f,.85f,0f},
                new[]{0f,1.4f,1.3f,1.1f,.7f,0f},new Color(.49f,.47f,.41f),"stone",10);
            c._visual=g.transform;c._collider=g.AddComponent<MeshCollider>();
            c._collider.sharedMesh=g.GetComponent<MeshFilter>().sharedMesh;c._collider.convex=true;
            if(!Active.Contains(c))Active.Add(c);return c;
        }
        public void InterceptFireball()
        {
            if(!Intact)return;
            Integrity--;
            if(Integrity==0)
            {
                _collider.enabled=false;_visual.localScale=new Vector3(1,.08f,1);
                Hints.Show("Каменное укрытие разрушено — смени позицию",HintPri.Danger);
            }
        }
        public static FireCover Between(Vector3 from,Vector3 to,out Vector3 impact)
        {
            impact=to;FireCover closest=null;float distance=(to-from).magnitude,best=distance;
            if(distance<.001f)return null;var ray=new Ray(from,(to-from)/distance);
            foreach(var c in Active)
            {
                if(c==null || !c.Intact || !c._collider.enabled)continue;
                if(c._collider.Raycast(ray,out var hit,distance) && hit.distance<best){best=hit.distance;closest=c;impact=hit.point;}
            }
            return closest;
        }
        void OnEnable(){if(!Active.Contains(this))Active.Add(this);}
        void OnDisable(){Active.Remove(this);}
        void OnDestroy(){Active.Remove(this);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){Active.Clear();}
    }
}
