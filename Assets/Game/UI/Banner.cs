using System.Collections.Generic;
using UnityEngine;
namespace Podz
{
    /// <summary>Компактные сообщения в левом нижнем углу. Одна очередь, короткое время, без перекрытия центра/HP.</summary>
    public class Banner : MonoBehaviour
    {
        public static Banner I;
        public static string LastTitle="", LastText="";
        public static bool LastGood;
        public static int ShownGood,ShownBad;
        public const float Duration=2f;
        struct Notice { public bool Good; public string Title,Sub,Reason; public float Created; }
        readonly Queue<Notice> _pending=new Queue<Notice>();
        Transform _root; Renderer _bg; TextMesh _text; float _until;
        public static bool Visible => I != null && I._root != null && I._root.gameObject.activeSelf;
        public static Rect Area => !Visible ? Rect.zero : new Rect(I.transform.localPosition.x-.46f,I.transform.localPosition.y+.08f,.92f,.16f);
        static readonly string[] _names={"Слайм","Ходячий гриб","Мандрагора","Василиск","Живые доспехи","Мимик","Красный дракон"};
        public static void Captured(MonsterKind k,ItemId loot,int count)
        {
            ShownGood++; Show(true,_names[(int)k]+" — добыто",Items.Name(loot)+" ×"+count+" → на земле","");
            Sfx.Play(SfxId.Success,null,.45f);
        }
        public static void Ruined(MonsterKind k,string why)
        {
            ShownBad++;Show(false,_names[(int)k]+" — добыча испорчена","",HintsToggle.On?why:"");Sfx.Play(SfxId.Fail,null,.4f);
        }
        public static void Show(bool good,string title,string sub,string reason)
        {
            LastGood=good;LastTitle=title;LastText=sub+(string.IsNullOrEmpty(reason)?"":" | "+reason);
            Debug.Log("[Banner] "+title+" "+LastText);
            if(I != null) I.Enqueue(new Notice {Good=good,Title=title,Sub=sub,Reason=reason});
        }
        public void AttachTo(Transform cam)
        {
            I=this;transform.SetParent(cam,false);transform.localPosition=new Vector3(0,0,HudPanel.Z);
            _root=new GameObject("CornerNotice").transform;_root.SetParent(transform,false);
            _bg=Prim.Hud(Prim.Quad("NoticeBg",_root,new Vector3(0,0,.004f),new Vector2(.92f,.16f),new Color(.05f,.05f,.04f,1f)));
            _bg.sortingOrder=20;
            _text=Prim.Text(_root,new Vector3(-.43f,.055f,0),"",.032f,Color.white,TextAnchor.UpperLeft);_text.GetComponent<MeshRenderer>().sortingOrder=21;
            _root.gameObject.SetActive(false);
        }
        void Enqueue(Notice n)
        {
            if(_pending.Count>=4)_pending.Dequeue();
            n.Created=Time.time;
            _pending.Enqueue(n);if(!Visible)Next();
        }
        void Next()
        {
            if(_pending.Count==0){_root.gameObject.SetActive(false);return;}
            while(_pending.Count>0 && Time.time-_pending.Peek().Created>4f)_pending.Dequeue();
            if(_pending.Count==0){_root.gameObject.SetActive(false);return;}
            var n=_pending.Dequeue();
            string message=n.Title+(string.IsNullOrEmpty(n.Sub)?"":"\n"+n.Sub);
            // Обоснование порчи хранится в бестиарии/добровольной помощи, не заполняет угол длинными инструкциями.
            _text.text=message;_text.color=n.Good?new Color(.65f,1f,.7f):new Color(1f,.65f,.55f);
            _until=Time.time+Duration;_root.gameObject.SetActive(true);
        }
        void Update()
        {
            float w=Mathf.Min(.92f,HudPanel.HalfW*1.1f);
            transform.localPosition=new Vector3(-HudPanel.HalfW+HudPanel.Margin+w*.5f,-HudPanel.HalfH+HudPanel.Margin+.09f,HudPanel.Z);
            _bg.transform.localScale=new Vector3(w,.16f,1);
            _text.transform.localPosition=new Vector3(-w*.5f+.02f,.055f,0);
            _text.characterSize=.032f*10f/64f;
            float tw=HudPanel.TextWidth(_text);if(tw>w-.04f)_text.characterSize*=Mathf.Max(.6f,(w-.04f)/tw);
            if(Visible && Time.time>=_until)Next();
            if(_root!=null)foreach(var r in _root.GetComponentsInChildren<Renderer>())r.enabled=!HudPanel.ModalOpen;
        }
        public static void DebugHold(){if(I!=null)I._until=Time.time+Duration;}
        void OnDestroy(){if(I==this)I=null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void Reset(){I=null;LastTitle=LastText="";ShownGood=ShownBad=0;}
    }
}
