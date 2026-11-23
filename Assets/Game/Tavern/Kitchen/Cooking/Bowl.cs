using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>Миска супа. Ставится на окно раздачи.</summary>
    public class Bowl : GrabItem
    {
        public static readonly List<Bowl> All = new List<Bowl>();
        public Recipe Recipe { get; private set; }
        public int Stars { get; private set; }
        public bool HalfPrice { get; private set; }
        public float OnWindowSince = -1;
        [SerializeField] TextMesh _label;
        public Toby Courier { get; private set; }
        public Guest ServedTo { get; private set; }
        public bool Returned { get; private set; }
        readonly HashSet<int> _wrongFor = new HashSet<int>();
        public override bool CanGrab(HandController hand) => Courier == null && ServedTo == null && base.CanGrab(hand);
        public override void Grab(HandController hand) { Returned = false; base.Grab(hand); }
        public override void Release(HandController hand, Vector3 v)
        {
            base.Release(hand, v);
            // Рука может наклонить миску: ограничения физики не должны закрепить этот наклон.
            var upright = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            Rb.rotation = upright; transform.rotation = upright; Rb.angularVelocity = Vector3.zero;
        }
        public bool MarkWrongOrder(Guest g) { Returned = true; return g != null && _wrongFor.Add(g.GetHashCode()); }
        public bool Reserve(Toby courier)
        {
            if (courier == null || IsHeld || Courier != null || ServedTo != null || Recipe == null) return false;
            Courier = courier; SetVelocity(Rb, Vector3.zero); Rb.isKinematic = true; return true;
        }
        public bool BeginCarry(Transform carry)
        {
            if (Courier == null || IsHeld || carry == null) return false;
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
            Rb.interpolation = RigidbodyInterpolation.None;
            transform.SetParent(carry, false); transform.localPosition = Vector3.zero; transform.localRotation = Quaternion.identity;
            return true;
        }
        public void ReleaseDelivery(Vector3 point, bool returned)
        {
            transform.SetParent(null, true); Courier = null; ServedTo = null; Returned = returned;
            foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = true;
            Rb.interpolation = RigidbodyInterpolation.Interpolate; PlaceAt(point);
        }
        public void ServeTo(Guest g, Vector3 point)
        {
            if (SoupVisual.IsStarter(Recipe)) point.y -= .032f;
            ReleaseDelivery(point, false); ServedTo = g;
            Rb.isKinematic = true; // миска остаётся видимой на столе до окончания еды
        }

        public static Bowl Spawn(Recipe r, int stars, bool halfPrice, Vector3 pos)
        {
            if (r == null) { Hints.Show("Нет готового супа для миски", HintPri.Error); return null; }
            if (All.Count >= 12) { Hints.Show("Слишком много мисок — отнеси на окно", HintPri.Error); return null; }
            var prefab = SavedDungeonAssets.Active?.FindBowl(r.Index);
            if (prefab != null && prefab._label != null)
            {
                var saved = Instantiate(prefab, pos, Quaternion.identity);
                saved.Recipe = r;
                saved.Stars = stars;
                saved.HalfPrice = halfPrice;
                saved.Rb = saved.GetComponent<Rigidbody>();
                saved._label.text = $"{r.Name} {new string('★', stars)}";
                SavedDungeonAssets.RestoreText(saved.gameObject);
                All.Add(saved);
                return saved;
            }
            var go = new GameObject("Bowl_"+r.Name); go.transform.position=pos;
            if (SoupVisual.IsStarter(r)) SoupVisual.StarterBowl(go.transform);
            else
            {
            Prim.Shape(PrimitiveType.Cylinder,"BowlBody",go.transform,Vector3.zero,new Vector3(.28f,.018f,.28f),new Color(.85f,.8f,.7f),false);
            for(int rim=0;rim<24;rim++){float angle=rim*Mathf.PI*2/24;Prim.Shape(PrimitiveType.Sphere,"BowlRim",go.transform,new Vector3(Mathf.Sin(angle)*.132f,.06f,Mathf.Cos(angle)*.132f),new Vector3(.035f,.04f,.035f),new Color(.85f,.8f,.7f),false);}
            }
            var bc=go.AddComponent<BoxCollider>();bc.size=SoupVisual.IsStarter(r) ? new Vector3(.29f,.095f,.29f) : new Vector3(.28f,.11f,.28f);bc.center=SoupVisual.IsStarter(r) ? new Vector3(0,.0295f,0) : new Vector3(0,.015f,0);
            var liquid=SoupVisual.Disc(go.transform,new Vector3(0,.064f,0),.115f,r.SoupColor);
            SoupVisual.Chunks(go.transform,r.Items,new Vector3(0,.067f,0),.09f);
            var b = go.AddComponent<Bowl>();
            b.Recipe = r; b.Stars = stars; b.HalfPrice = halfPrice;
            b.SetupPhysics(0.4f);
            // Только миски: остаются динамическими/захватываемыми, но не опрокидываются
            // при небольшом контакте на узкой стойке. Продукты и инструменты не затронуты.
            b.Rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            b._label = Prim.Text(go.transform, new Vector3(0, .2f, 0), $"{r.Name} {new string('★', stars)}", .035f, Color.white);
            All.Add(b);
            return b;
        }

        void Update()
        {
            if (_label != null && PlayerRig.I != null) Prim.FaceTo(_label.transform, PlayerRig.I.HeadPos);
            if (transform.position.y < -10f) Destroy(gameObject);
        }

        void OnDestroy() { All.Remove(this); }
    }
}
