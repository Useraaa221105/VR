using System.Collections.Generic;
using UnityEngine;
namespace Podz
{
    public class DragonReferenceMotion : MonoBehaviour
    {
        public Dragon Owner; public Transform HeadPivot,Jaw; public Mesh Neck;
        public List<Transform> Wings=new List<Transform>();
        readonly Vector3[] _vertices=new Vector3[62];
        readonly Vector3[] _path=new Vector3[5];
        readonly float[] _radii={.85f,.73f,.55f,.42f,.35f};
        // Only baked prefab instances need a private deformation mesh. Legacy generated dragons keep their old path.
        [SerializeField, HideInInspector] bool _savedNeck;
        public bool Animate = true;
        Mesh _ownedNeck;
#if UNITY_EDITOR
        public void MarkSavedNeck() { _savedNeck = true; }
#endif
        void Start()
        {
            if (!_savedNeck || Neck == null) return;
            var source = Neck; _ownedNeck = Instantiate(source); _ownedNeck.name = source.name + "_Instance";
            foreach (var filter in GetComponentsInChildren<MeshFilter>(true))
                if (filter.sharedMesh == source) filter.sharedMesh = _ownedNeck;
            Neck = _ownedNeck;
        }
        void OnDestroy() { if (_ownedNeck != null) Destroy(_ownedNeck); }
        void LateUpdate()
        {
            if(!Animate || Owner==null || !Owner.Initialized || Owner.Dead || Neck==null || DTime.Delta<=0)return;
            var start=new Vector3(0,2,1.5f);var end=transform.InverseTransformPoint(HeadPivot.position);
            for(int k=0;k<5;k++){float t=k/4f;_path[k]=Vector3.Lerp(start,end,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.3f);}
            for(int k=0;k<5;k++)
            {
                var dir=k==4?_path[4]-_path[3]:_path[k+1]-_path[k];
                var q=Quaternion.FromToRotation(Vector3.up,dir.normalized);
                for(int j=0;j<12;j++){float a=j*Mathf.PI/6;_vertices[k*12+j]=_path[k]+q*new Vector3(Mathf.Cos(a)*_radii[k],0,Mathf.Sin(a)*_radii[k]);}
            }
            _vertices[60]=_path[0];_vertices[61]=_path[4];Neck.vertices=_vertices;Neck.RecalculateNormals();Neck.RecalculateBounds();
            bool bite=Owner.State=="Укус: замах" || Owner.State=="Укус";
            Jaw.localRotation=Quaternion.Slerp(Jaw.localRotation,Quaternion.Euler(bite?24:0,0,0),DTime.Delta*10);
            float phase=DTime.Now*(Owner.Airborne?8:1.7f),amount=Owner.Airborne?38:5;
            for(int j=0;j<Wings.Count;j++)Wings[j].localRotation=Quaternion.Euler(0,0,(j==0?-1:1)*Mathf.Sin(phase)*amount);
        }
    }
}
