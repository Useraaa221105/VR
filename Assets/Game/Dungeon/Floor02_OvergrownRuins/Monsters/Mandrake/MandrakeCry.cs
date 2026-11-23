using System.Collections.Generic;
using UnityEngine;
namespace Podz {
    /// <summary>Крик переживает испорченную мандрагору, но не смену этажа. Вред проходит через общий жетон, не теряется при занятой очереди.</summary>
    public class MandrakeCry : MonoBehaviour {
        static readonly List<MandrakeCry> Pending = new List<MandrakeCry>();
        public static int PendingCount => Pending.Count;
        float _damage; int _ownerId; bool _soundOnly;
        public static bool Contains(int id) {foreach(var c in Pending)if(c!=null && c._ownerId==id)return true;return false;}
        public static void Enqueue(Vector3 pos,float damage,Transform parent,int ownerId=0,bool soundOnly=false) {
            // Ошибка извлечения не теряется за уже ожидающим предупреждением того же корня.
            if(ownerId!=0) foreach(var pending in Pending) if(pending!=null && pending._ownerId==ownerId) {
                if(pending._soundOnly && !soundOnly) { pending._soundOnly=false;pending._damage=damage; }
                return;
            }
            var go=new GameObject("MandrakeCryPending");go.transform.SetParent(parent,true);go.transform.position=pos;
            var c=go.AddComponent<MandrakeCry>();c._damage=damage;c._ownerId=ownerId;c._soundOnly=soundOnly;Pending.Add(c);
        }
        void Update() {
            if(DTime.Paused || Pending.Count==0 || Pending[0]!=this || !MandrakeVoice.CanStart) return;
            if(!_soundOnly && !AttackGate.TryWorldAttack()) return; // предупреждение не занимает общий жетон урона
            var p=PlayerRig.I;
            // Не удалять событие до принятого запуска голоса.
            if(!Sfx.TryMandrakeScream(transform.position)) return;
            if(!_soundOnly) FX.Sparkle(transform.position,new Color(1f,.8f,.4f));
            if(!_soundOnly && p!=null && Vector3.Distance(p.Feet,transform.position)<4f && !Rays.Blocked(transform.position,p.HeadPos,transform)) {
                p.Health.Damage(_damage,"крик мандрагоры");p.Health.Apply(Status.Stunned,2f);
            }
            Debug.Log($"[MandrakeCry] started owner={_ownerId}; soundOnly={_soundOnly}; damage={_damage}; voiceAlive={MandrakeVoice.Source!=null}");
            Destroy(gameObject);Pending.Remove(this);
        }
        void OnDestroy(){Pending.Remove(this);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){Pending.Clear();}
    }
}
