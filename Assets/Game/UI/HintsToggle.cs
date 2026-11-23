using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Podz
{
    /// <summary>Только добровольная помощь подземелья. По умолчанию выключена; кухня независима.</summary>
    public class HintsToggle : MonoBehaviour
    {
        public const string PrefKey = "podz_dungeon_hints_v2";
        static int _on = -1;
        public static bool On
        {
            get { if (_on < 0) _on = PlayerPrefs.GetInt(PrefKey, 0); return _on == 1; }
            set { _on = value ? 1 : 0; PlayerPrefs.SetInt(PrefKey, _on); PlayerPrefs.Save(); }
        }
        public static int Toggles;
        public static string ToastText => I != null && I._toast.gameObject.activeSelf ? I._toast.text : "";
        public static string CornerText => I != null ? I._corner.text : "";
        /// <summary>Прямоугольник надписи в углу (x, верх, ширина, высота) — для проверки «HUD не налезает».</summary>
        public static Rect CornerArea => Rect.zero;
        /// <summary>Сбросить кеш (автотест: проверка «запоминается после перезапуска»).</summary>
        public static void Reload() { _on = -1; }

        public static HintsToggle I;
        TextMesh _corner, _toast;
        Renderer _cornerBg, _toastBg;
        float _toastUntil, _cornerW = 0.4f;
        Edge _stick;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _on = -1; Toggles = 0; }

        public void AttachTo(Transform cam)
        {
            I = this;
            transform.SetParent(cam, false);
            transform.localPosition = new Vector3(0, 0, HudPanel.Z);
            transform.localRotation = Quaternion.identity;
            _cornerBg = Prim.Quad("HintsCornerBg", transform, Vector3.zero, new Vector2(0.4f, 0.05f), new Color(0, 0, 0, 0.8f));
            Prim.Hud(_cornerBg); _cornerBg.sortingOrder = 1; Prim.SetOverlayColor(_cornerBg, new Color(0.02f, 0.015f, 0.01f, 0.85f));
            _corner = Prim.Text(transform, Vector3.zero, "", 0.032f, Color.white, TextAnchor.MiddleLeft);
            _corner.GetComponent<MeshRenderer>().sortingOrder = 6;
            _toastBg = Prim.Quad("HintsToastBg", transform, new Vector3(0, 0.12f, 0.004f), new Vector2(0.8f, 0.12f), new Color(0, 0, 0, 0.9f));
            Prim.Hud(_toastBg); _toastBg.sortingOrder = 8;
            _toast = Prim.Text(transform, new Vector3(0, 0.12f, 0), "", 0.08f, Color.white);
            _toast.GetComponent<MeshRenderer>().sortingOrder = 9;
            _toast.gameObject.SetActive(false); _toastBg.gameObject.SetActive(false);
        }

        /// <summary>Нажатие H.</summary>
        public static void Toggle()
        {
            On = !On; Toggles++;
            if (I != null) { I._toastUntil = Time.unscaledTime + 2f; I.Refresh(true); }
            if (!On) Hints.Clear();
            Debug.Log("[Hints] Подсказки: " + (On ? "ВКЛ" : "ВЫКЛ"));
        }

        void Update()
        {
            var P = PlayerRig.I;
            _stick.Update(false); // VR: переключатель — в меню паузы (кнопки стиков недоступны во всех профилях)
            if (P != null && DIn.Down(Key.F2)) DungeonSettings.Toggle();
            Refresh(false);
            HuntIntro.Poll();
        }

        void Refresh(bool force)
        {
            _corner.text = "";
            _corner.gameObject.SetActive(false); _cornerBg.gameObject.SetActive(false);
            _toast.gameObject.SetActive(false); _toastBg.gameObject.SetActive(false);

        }
    }
}
