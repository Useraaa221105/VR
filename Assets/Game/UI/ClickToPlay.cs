using UnityEngine;

namespace Podz
{
    /// <summary>
    /// Исправление 6: если курсор свободен (Esc, Alt+Tab, окно без фокуса) — по центру экрана крупно:
    /// «Щёлкни по игре, чтобы управлять». Щелчок захватывает мышь, надпись исчезает. Только ПК (в VR курсора нет).
    /// </summary>
    public class ClickToPlay : MonoBehaviour
    {
        public static ClickToPlay I;
        public const string Text = "Щёлкни по игре, чтобы управлять";
        Transform _root;
        Renderer _bg, _frame;
        TextMesh _title, _sub;

        public static bool Visible => I != null && I._root != null && I._root.gameObject.activeSelf;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { I = null; }

        public void AttachTo(Transform cam)
        {
            I = this;
            transform.SetParent(cam, false);
            transform.localPosition = new Vector3(0, 0.12f, 1.0f);
            transform.localRotation = Quaternion.identity;
            _root = new GameObject("ClickToPlayRoot").transform;
            _root.SetParent(transform, false);
            _frame = Prim.Hud(Prim.Quad("Frame", _root, new Vector3(0, 0, 0.012f), new Vector2(1.02f, 0.2f), new Color(1f, 0.85f, 0.35f, 1f)));
            _bg = Prim.Hud(Prim.Quad("Bg", _root, new Vector3(0, 0, 0.01f), new Vector2(1.0f, 0.18f), new Color(0.06f, 0.05f, 0.04f, 0.94f)));
            _frame.sortingOrder = 90; _bg.sortingOrder = 91;
            // значок «мышь»: корпус + колёсико
            var mouse = Prim.Hud(Prim.Quad("MouseBody", _root, new Vector3(-0.43f, 0.0f, 0.008f), new Vector2(0.05f, 0.08f), new Color(1f, 0.85f, 0.35f, 1f)));
            var wheel = Prim.Hud(Prim.Quad("MouseWheel", _root, new Vector3(-0.43f, 0.02f, 0.006f), new Vector2(0.01f, 0.025f), new Color(0.06f, 0.05f, 0.04f, 1f)));
            mouse.sortingOrder = 92; wheel.sortingOrder = 93;
            _title = Prim.Text(_root, new Vector3(0.035f, 0.025f, 0), Text, 0.042f, new Color(1f, 0.9f, 0.5f));
            _title.fontStyle = FontStyle.Bold;
            _sub = Prim.Text(_root, new Vector3(0.035f, -0.045f, 0), "мышь — осмотреться · Esc — отпустить курсор", 0.03f, new Color(0.9f, 0.9f, 0.9f));
            _title.GetComponent<MeshRenderer>().sortingOrder = 94; _sub.GetComponent<MeshRenderer>().sortingOrder = 94;
            _root.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (_root == null) return;
            var P = PlayerRig.I;
            bool show = P != null && !P.IsXR && !P.MouseCaptured && !BestiaryCard.Open;
            if (_root.gameObject.activeSelf != show) _root.gameObject.SetActive(show);
            if (show) _frame.transform.localScale = new Vector3(1.02f, 0.2f, 1) * (1f + 0.015f * Mathf.Sin(Time.unscaledTime * 4f)); // мягко «дышит»
        }

        void OnDestroy() { if (I == this) I = null; }
    }
}
