using System;
using UnityEngine;
namespace Podz
{
    public class Pressable : MonoBehaviour
    {
        public Action OnPress;
        public bool Interactable = true;
        [SerializeField] Renderer _r;
        [SerializeField] Color _color;
        [SerializeField] TextMesh _label;
        float _last;

        public static Pressable Create(string label, Transform parent, Vector3 localPos, Color color, Action onPress, Vector3? size = null, float textSize = 0.035f)
        {
            var go = Prim.Cube("Btn_" + label, parent, localPos, size ?? new Vector3(0.16f, 0.08f, 0.05f), color);
            var p = go.AddComponent<Pressable>();
            p._r = go.GetComponent<Renderer>();
            p._color = color;
            p.OnPress = onPress;
            p._label = Prim.Text(parent, localPos + new Vector3(0, 0, -0.035f), label, textSize, Color.white);
            p._label.transform.localRotation = Quaternion.identity;
            return p;
        }

        void Start() { VrRig.Register(this); } // VR: кнопку можно нажать лучом/рукой через XR Interaction Toolkit

        public TextMesh Label => _label;
        public void SetLabel(string s) { if (_label != null && _label.text != s) _label.text = s; }

        internal void SetBaseColor(Color color) => _color = color;

        public void SetInteractable(bool v)
        {
            if (Interactable == v) return;
            Interactable = v;
            Prim.SetColor(_r, v ? _color : _color * 0.35f);
        }

        public void Press()
        {
            if (!Interactable || Time.unscaledTime - _last < 0.3f) return;
            _last = Time.unscaledTime;
            Prim.SetColor(_r, _color, Color.white * 0.6f);
            Invoke(nameof(Unflash), 0.15f);
            Sfx.Play(SfxId.Click, transform.position, 0.5f);
            InteractionFeedback.Emit(transform.position);
            OnPress?.Invoke();
        }

        void Unflash() { if (_r != null) Prim.SetColor(_r, Interactable ? _color : _color * 0.35f); }

        void OnDestroy() { OnPress = null; if (Application.isPlaying && _label != null) Destroy(_label.gameObject); _label = null; }
    }
}
