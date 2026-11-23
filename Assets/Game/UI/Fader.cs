using System;
using System.Collections;
using UnityEngine;

namespace Podz
{
    /// <summary>Затемнение экрана для телепортов (0,35 с туда, 0,35 с обратно). Камера не двигается — только чёрный экран.</summary>
    public class Fader : MonoBehaviour
    {
        public static Fader I;
        Renderer _quad;
        public bool Busy { get; private set; }

        public void AttachTo(Transform cam)
        {
            transform.SetParent(cam, false);
            transform.localPosition = new Vector3(0, 0, 0.08f);
            _quad = Prim.Quad("Fade", transform, Vector3.zero, new Vector2(1f, 1f), new Color(0, 0, 0, 0));
            _quad.gameObject.SetActive(false);
        }

        public void Run(Action inDark)
        {
            if (Busy) return;
            StartCoroutine(Co(inDark));
        }

        IEnumerator Co(Action inDark)
        {
            Busy = true;
            Sfx.Play(SfxId.Whoosh);
            _quad.gameObject.SetActive(true);
            for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime) { Set(t / 0.35f); yield return null; }
            Set(1);
            yield return null;
            try { inDark?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
            yield return null;
            for (float t = 0; t < 0.35f; t += Time.unscaledDeltaTime) { Set(1 - t / 0.35f); yield return null; }
            Set(0);
            _quad.gameObject.SetActive(false);
            Busy = false;
        }

        void Set(float a) => Prim.SetOverlayColor(_quad, new Color(0, 0, 0, a));
    }
}
