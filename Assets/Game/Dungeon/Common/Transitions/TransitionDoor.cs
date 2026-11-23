using System.Collections.Generic;
using UnityEngine;
namespace Podz
{
    /// <summary>Дверь входа в комнату портала. Только физический проход: телепортации здесь нет.</summary>
    public class TransitionDoor : MonoBehaviour
    {
        public static readonly List<TransitionDoor> All = new List<TransitionDoor>();
        [field: SerializeField] public int NextLevel { get; private set; }
        public bool IsOpen { get; private set; }
        public bool UsesSavedAsset { get; private set; }
        public bool Transitioned { get; private set; }
        [field: SerializeField] public Collider LeafCollider { get; private set; }
        [SerializeField] Transform _hinge; float _angle, _createdAt; Vector3 _previous; bool _approached, _havePrevious;
        [SerializeField] Pressable _handle;
        [SerializeField, HideInInspector] bool _savedBindings;
#if UNITY_EDITOR
        public void PrepareSavedBindings() { _savedBindings=true; }
#endif
        void Start() {
            if(!_savedBindings)return;
            foreach(var handle in _hinge.GetComponentsInChildren<Pressable>(true))handle.OnPress=ToggleFromHandle;
            if(!All.Contains(this))All.Add(this);
        }
        public static TransitionDoor Create(Transform parent,Vector3 pos,int next,Biome b)
        {
            var template = SavedDungeonAssets.FindTemplate<TransitionDoor>(
                SavedDungeonAssets.Variant(SavedDungeonAssets.Active?.Doors, next - 2));
            if (template != null && template.NextLevel == next && template._hinge != null &&
                template._handle != null && template.LeafCollider != null)
            {
                var instance = Instantiate(template, pos, Quaternion.identity, parent);
                instance.name = "DoorToFloor_" + next;
                instance._savedBindings = false;
                instance.UsesSavedAsset = true;
                instance._createdAt = Time.time;
                foreach (var handle in instance._hinge.GetComponentsInChildren<Pressable>(true))
                    handle.OnPress = instance.ToggleFromHandle;
                SavedDungeonAssets.RestoreText(instance.gameObject);
                All.Add(instance);
                return instance;
            }
            var go=new GameObject("DoorToFloor_"+next); go.transform.SetParent(parent,true); go.transform.position=pos;
            var door=go.AddComponent<TransitionDoor>(); door.NextLevel=next; door._createdAt=Time.time;
            var t=go.transform;
            Prim.Cube("ArchL",t,new Vector3(-1.2f,1.3f,0),new Vector3(.3f,2.6f,.4f),b.Wall);
            Prim.Cube("ArchR",t,new Vector3(1.2f,1.3f,0),new Vector3(.3f,2.6f,.4f),b.Wall);
            Prim.Cube("ArchTop",t,new Vector3(0,2.55f,0),new Vector3(2.7f,.3f,.4f),b.Wall);
            Prim.Text(t,new Vector3(0,2.85f,-.1f),next==4?"ГЛУБЖЕ: ЛОГОВО":"ГЛУБЖЕ: −"+next,.1f,Color.white);
            Prim.PointLight(t,new Vector3(0,2,1.5f),new Color(1f,.65f,.25f),3f,.65f);
            door._hinge=new GameObject("Hinge").transform; door._hinge.SetParent(t,false); door._hinge.localPosition=new Vector3(-1.05f,0,0);
            var leaf=Prim.Cube("DoorLeaf",door._hinge,new Vector3(1.05f,1.15f,0),new Vector3(2.1f,2.3f,.16f),new Color(.28f,.18f,.09f));
            door.LeafCollider=leaf.GetComponent<Collider>();
            for(int i=0;i<5;i++)Prim.Cube("Board",door._hinge,new Vector3(.21f+i*.42f,1.15f,-.09f),new Vector3(.025f,2.2f,.02f),new Color(.12f,.08f,.04f),false);
            door._handle=Pressable.Create("",door._hinge,new Vector3(1.85f,1.1f,-.14f),new Color(.7f,.55f,.2f),door.ToggleFromHandle,new Vector3(.12f,.12f,.1f));
            Pressable.Create("",door._hinge,new Vector3(1.85f,1.1f,.14f),new Color(.7f,.55f,.2f),door.ToggleFromHandle,new Vector3(.12f,.12f,.1f));
            All.Add(door); return door;
        }
        public bool Toggle()
        {
            if(Transitioned || Fader.I != null && Fader.I.Busy)return false;
            if(IsOpen && PlayerRig.I != null)
            {
                var p=transform.InverseTransformPoint(PlayerRig.I.Feet);
                // Не начинаем закрытие, пока игрок в проёме/зоне движения створки.
                if(Mathf.Abs(p.x)<1.5f && p.z>-.6f && p.z<2f) { Banner.Show(false,"Дверь не закрыта","Проём занят","");return false; }
            }
            IsOpen=!IsOpen; _approached=false;
            InteractionFeedback.Emit(_handle.transform.position,NearestHand());
            return true;
        }
        void ToggleFromHandle() { Toggle(); }
        HandController NearestHand()
        {
            var p=PlayerRig.I;if(p==null)return null;
            return Vector3.Distance(p.Left.transform.position,_handle.transform.position)<Vector3.Distance(p.Right.transform.position,_handle.transform.position)?p.Left:p.Right;
        }
        // Compatibility for archived test harnesses. Door crossing must NEVER change floor.
        public bool TryTraverse(Vector3 before,Vector3 after) => false;
        public Vector3 FrontHandle => _handle.transform.position;
        void Update()
        {
            var p=PlayerRig.I;if(p==null)return;
            if(!IsOpen)
            {
                var local=transform.InverseTransformPoint(p.Feet);
                if(Mathf.Abs(local.x)<1.45f && local.z>-.5f && local.z<1.8f && _angle>1f) { IsOpen=true; }
            }
            _angle=Mathf.MoveTowards(_angle,IsOpen?105f:0f,150f*Time.deltaTime);
            _hinge.localRotation=Quaternion.Euler(0,-_angle,0);
            var feet=p.Feet;

            _previous=feet;_havePrevious=true;
        }
        void OnDestroy(){All.Remove(this);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){All.Clear();}
    }
}
