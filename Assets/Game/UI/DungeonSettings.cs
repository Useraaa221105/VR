using UnityEngine;
namespace Podz
{
    // World-space panel: ПК E; VR trigger, registered by existing Pressable/XRI.
    public class DungeonSettings : MonoBehaviour
    {
        static DungeonSettings _i;
        public static bool Open => _i != null;
        [SerializeField] Pressable _toggle, _close;
        public static void Toggle()
        {
            if (_i != null) { Object.Destroy(_i.gameObject); return; }
            if (PlayerRig.I == null || BestiaryCard.Open || HuntIntro.Open) return;
            var cam = PlayerRig.I.Cam.transform;
            var template = SavedDungeonAssets.Active?.Settings;
            _i = template != null && template._toggle != null && template._close != null
                ? Instantiate(template) : BuildVisual();
            SavedDungeonAssets.RestoreText(_i.gameObject);
            _i.transform.position = cam.position + cam.forward * 1.2f;
            _i.transform.rotation = Quaternion.LookRotation(cam.forward, Vector3.up);
            var panel = _i;
            panel._toggle.OnPress = () => { HintsToggle.Toggle(); panel.Refresh(); };
            panel._close.OnPress = Toggle;
            panel.Refresh();
            DTime.Paused = true;
        }

        public static DungeonSettings BuildVisual()
        {
            var go = SavedUi.CreateRoot("Настройки подземелья");
            var panel = go.AddComponent<DungeonSettings>();
            Prim.Cube("Panel", go.transform, Vector3.zero, new Vector3(1.2f, .65f, .03f), new Color(.12f,.1f,.08f), false);
            Prim.Text(go.transform, new Vector3(0,.24f,-.025f), "НАСТРОЙКИ ПОДЗЕМЕЛЬЯ", .06f, Color.white);
            panel._toggle = Pressable.Create("Помощь подземелья",go.transform,new Vector3(0,.05f,-.05f),new Color(.25f,.45f,.35f), null,new Vector3(.95f,.12f,.03f), .035f);
            Prim.Text(go.transform,new Vector3(0,-.09f,-.03f),"Кухонная помощь всегда включена",.035f,Color.white);
            panel._close = Pressable.Create("Закрыть (F2)",go.transform,new Vector3(0,-.23f,-.05f),Color.gray,null,new Vector3(.6f,.1f,.03f));
            return panel;
        }
        public static void ClosePanel() { if(_i != null) { var old=_i;_i=null; DTime.Paused=false; Object.Destroy(old.gameObject); } }
        void Refresh() { _toggle.SetLabel("Помощь подземелья: " + (HintsToggle.On ? "ВКЛ" : "ВЫКЛ")); }
        void OnDestroy() { if (_i == this) { _i = null; DTime.Paused = false; } }
    }
}
