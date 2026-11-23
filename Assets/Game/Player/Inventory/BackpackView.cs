using UnityEngine;
using UnityEngine.InputSystem;
namespace Podz
{
    /// <summary>Представление существующего Inventory.Backpack, не второе хранилище.</summary>
    public class BackpackView : GrabItem
    {
        public static BackpackView I;
        static Transform _pcPanel;
        Transform _home, _view;
        TextMesh _title;
        int _shown = -1; string _signature = "";
        public static bool Open => _pcPanel != null;
        public Vector3 Opening => transform.TransformPoint(new Vector3(0,.13f,0));
        public static BackpackView Create(PlayerRig rig)
        {
            var home = new GameObject("BackpackShoulder").transform;
            home.SetParent(rig.Body,false); home.localPosition = new Vector3(.3f,1.4f,-.28f);
            var go = new GameObject("Рюкзак"); go.transform.SetParent(home,false);
            var bag = go.AddComponent<BackpackView>(); I=bag; bag._home=home;
            Prim.Cube("BagBody",go.transform,Vector3.zero,new Vector3(.28f,.3f,.18f),new Color(.38f,.25f,.12f),false);
            Prim.Cube("BagOpening",go.transform,new Vector3(0,.15f,0),new Vector3(.25f,.03f,.15f),new Color(.08f,.06f,.03f),false);
            var c=go.AddComponent<BoxCollider>(); c.size=new Vector3(.3f,.33f,.2f); c.isTrigger=true;
            bag.SetupPhysics(); bag.Rb.isKinematic=true;
            bag._view=new GameObject("BagContents").transform; bag._view.SetParent(go.transform,false); bag._view.localPosition=new Vector3(0,.35f,-.12f);
            return bag;
        }
        public static void ClosePanel() { if (_pcPanel != null) Object.Destroy(_pcPanel.gameObject); _pcPanel=null; }
        public override bool CanGrab(HandController h) => Tools.InDungeon && PlayerRig.I != null && PlayerRig.I.IsXR && !IsHeld;
        public override void Release(HandController h, Vector3 v)
        {
            // Не бросаем постоянный рюкзак в мир, возвращаем в крепление.
            base.Release(h,Vector3.zero); Rb.isKinematic=true;
            transform.SetParent(_home,false); transform.localPosition=Vector3.zero; transform.localRotation=Quaternion.identity;
            _view.gameObject.SetActive(false);
        }
        void Update()
        {
            var p=PlayerRig.I; if (p == null) return;
            GetComponent<Collider>().enabled = p.IsXR && Tools.InDungeon;
            if (!Tools.InDungeon) { ClosePanel(); _view.gameObject.SetActive(false); }
            if (!p.IsXR && Tools.InDungeon && !BestiaryCard.Open && !DungeonSettings.Open && !HuntIntro.Open && DIn.Down(Key.I))
            {
                if (Open) ClosePanel();
                else
                {
                    _pcPanel=new GameObject("InventoryPanel").transform; _pcPanel.SetParent(p.Cam.transform,false);
                    _pcPanel.localPosition=new Vector3(0,0,1.2f); BuildPanel(_pcPanel);
                }
            }
            foreach (var r in GetComponentsInChildren<Renderer>())
                if (!r.transform.IsChildOf(_view)) r.enabled=p.IsXR && Tools.InDungeon;
            _view.gameObject.SetActive(p.IsXR && Tools.InDungeon && IsHeld);
            string sig=""; foreach (var kv in Inventory.Backpack.All) sig+=kv.Key+":"+kv.Value+";";
            if (sig != _signature || _shown != (IsHeld ? 1:0))
            {
                _signature=sig; _shown=IsHeld?1:0;
                Rebuild(_view); if (Open) Rebuild(_pcPanel);
            }
        }
        void BuildPanel(Transform panel)
        {
            Prim.Cube("InventoryBg",panel,new Vector3(0,0,.03f),new Vector3(1.18f,.95f,.02f),new Color(.1f,.08f,.06f),false);
            Rebuild(panel);
        }
        void Rebuild(Transform panel)
        {
            var old=panel.Find("Rows"); if(old != null) Destroy(old.gameObject);
            var rows=new GameObject("Rows").transform; rows.SetParent(panel,false);
            Prim.Text(rows,new Vector3(0,.4f,0),"РЮКЗАК  " + Inventory.Backpack.Total + "/100",.055f,Color.white);
            int i=0;
            foreach(var id in Items.MonsterItems)
            {
                int n=Inventory.Backpack.Count(id); if(n<=0)continue;
                float y=.28f-i*.09f;
                var icon=FoodModels.Build(id,rows,.065f); icon.localPosition=new Vector3(-.47f,y-.02f,-.015f);
                Prim.Text(rows,new Vector3(-.36f,y,0),Items.Name(id)+" ×"+n,.04f,Color.white,TextAnchor.MiddleLeft); i++;
            }
            if(i==0)Prim.Text(rows,Vector3.zero,"Пусто",.05f,Color.white);
            Prim.Text(rows,new Vector3(0,-.4f,0),PlayerRig.I.IsXR ? "Положи добычу в горловину рюкзака" : "I — закрыть",.032f,Color.white);
        }
        void OnDestroy() { if(I==this) { I=null; ClosePanel(); } }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset() { I=null; _pcPanel=null; }
    }
}
