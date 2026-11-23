using UnityEngine;

namespace Podz
{
    public class ArchitectureMeshOwner : MonoBehaviour {
        public Mesh Mesh;
        [SerializeField, HideInInspector] bool _savedResource;
#if UNITY_EDITOR
        public void MarkSavedResource() { _savedResource = true; }
#endif
        void OnDestroy(){
            if(_savedResource || Mesh==null)return;
#if UNITY_EDITOR
            if(!Application.isPlaying){if(!UnityEditor.EditorUtility.IsPersistent(Mesh))Object.DestroyImmediate(Mesh);return;}
#endif
            Object.Destroy(Mesh);
        }
    }
}
