using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Урна у стола подготовки (баг-лист, пункт 2): упавший на пол, сгоревший или испорченный продукт берут E и выбрасывают сюда
    /// (ПК: смотришь на урну с продуктом в руке → E; VR: просто бросить в урну). Мясо монстра без ударов возвращается в ледник.
    /// </summary>
    public class TrashBin : MonoBehaviour
    {
        public static TrashBin I;
        public const string Tip = "Выброси в урну и возьми новый из ледника";
        public static int Thrown;
        public static string LastThrown = "";
        public Vector3 Mouth => transform.position + Vector3.up * 0.62f;

        public static TrashBin Create(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("TrashBin");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var b = go.AddComponent<TrashBin>();
            I = b;
            b.Build();
            return b;
        }
        void OnDestroy() { if (I == this) I = null; }

        void Build()
        {
            var t = transform;
            var metal = new Color(0.32f, 0.34f, 0.36f);
            var body = Prim.Shape(PrimitiveType.Cylinder, "BinBody", t, new Vector3(0, 0.3f, 0), new Vector3(0.44f, 0.3f, 0.44f), metal);
            Prim.Shape(PrimitiveType.Cylinder, "BinRim", t, new Vector3(0, 0.6f, 0), new Vector3(0.48f, 0.02f, 0.48f), metal * 1.3f, false);
            Prim.Shape(PrimitiveType.Cylinder, "BinHole", t, new Vector3(0, 0.615f, 0), new Vector3(0.38f, 0.005f, 0.38f), new Color(0.05f, 0.05f, 0.05f), false);
            // полоски-обручи — узнаваемое ведро
            for (int i = 0; i < 2; i++) Prim.Shape(PrimitiveType.Cylinder, "Hoop", t, new Vector3(0, 0.15f + i * 0.3f, 0), new Vector3(0.455f, 0.012f, 0.455f), new Color(0.2f, 0.2f, 0.22f), false);
            var trig = new GameObject("BinMouth"); trig.transform.SetParent(t, false); trig.transform.localPosition = new Vector3(0, 0.62f, 0);
            var bc = trig.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(0.42f, 0.25f, 0.42f);
            trig.AddComponent<TrashMouth>().Owner = this;
            // табличка-картинка: урна + продукт со стрелкой вниз
            var sign = Prim.Text(t, new Vector3(0, 1.0f, 0), "УРНА\nупавшее — сюда (E)", 0.06f, new Color(1f, 0.9f, 0.6f));
            sign.fontStyle = FontStyle.Bold;
            sign.gameObject.AddComponent<FaceViewer>();
        }

        /// <summary>Выбросить предмет (продукт или миску).</summary>
        public bool Throw(GrabItem item)
        {
            if (item == null) return false;
            if (item is Ingredient ing)
            {
                LastThrown = Items.Name(ing.Id);
                bool back = ing.ReturnsToFridge;
                ing.Despawn(true);
                Thrown++;
                Sfx.Play(SfxId.Pour, transform.position, 0.6f);
                KitchenToast.Show(back ? $"Выброшено! {LastThrown} — возьми новый из ледника" : $"Выброшено! Возьми новый: {LastThrown}");
                return true;
            }
            if (item is Bowl bowl)
            {
                if (bowl.Courier != null || bowl.ServedTo != null) return false;
                LastThrown = "миска";
                if (bowl.IsHeld) bowl.Holder.ForceRelease();
                Destroy(bowl.gameObject);
                Thrown++;
                Sfx.Play(SfxId.Pour, transform.position, 0.6f);
                KitchenToast.Show("Миска выброшена");
                return true;
            }
            return false;
        }
    }

    public class TrashMouth : MonoBehaviour
    {
        public TrashBin Owner;
        void OnTriggerEnter(Collider c)
        {
            var g = c.GetComponentInParent<GrabItem>();
            if (g != null && !g.IsHeld && Owner != null && (g is Ingredient || g is Bowl)) Owner.Throw(g);
        }
    }

    /// <summary>
    /// Короткая зелёная надпись после действия на кухне («Вода налита!», «Желе добавлено!») — по центру экрана, 1.6 с.
    /// </summary>
    public static class KitchenToast
    {
        public static string Last = "";
        public static float LastAt = -99f;
        public static int Count;
        public static void Show(string text, float dur = 1.6f)
        {
            Last = text; LastAt = Time.unscaledTime; Count++;
            Hints.Good(text, dur);
        }
        public static bool Visible => Time.unscaledTime - LastAt < 1.6f;
    }
}
