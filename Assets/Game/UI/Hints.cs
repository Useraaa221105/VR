using System.Collections.Generic;
using UnityEngine;

namespace Podz
{
    public enum HintPri { Tutorial = 0, Action = 1, Window = 2, Error = 3, Danger = 4 }

    /// <summary>Одна подсказка за раз, по приоритету. Одинаковая — не чаще раза в 8 с.</summary>
    public class Hints : MonoBehaviour
    {
        public static Hints I;
        TextMesh _text;
        Renderer _bg;
        float _hideAt;
        HintPri _cur;
        readonly Dictionary<string, float> _lastShown = new Dictionary<string, float>();

        /// <summary>Последняя подсказка (для отчёта автотеста).</summary>
        public static string Last = "";
        /// <summary>Сколько подсказок не показано, потому что подсказки выключены (H).</summary>
        public static int Suppressed;

        public static void Show(string msg, HintPri pri = HintPri.Action, float duration = 3f)
        {
            Last = msg;
            if (I != null) I.ShowInternal(msg, pri, duration, false);
            if (I == null) Debug.Log("[Hint] " + msg);
        }

        /// <summary>Короткая ЗЕЛЁНАЯ надпись об успехе («Вода налита!», «Желе добавлено!»).</summary>
        public static void Good(string msg, float duration = 1.6f)
        {
            Last = msg;
            Banner.Show(true, msg, "", "");
            Last = msg;
            if (I == null) Debug.Log("[Hint] " + msg);
        }

        /// <summary>Пункт 6: убрать текущую подсказку (смерть, возврат в таверну) — чтобы «Не смотри ему в глаза!» не висела после.</summary>
        public static int Cleared;
        public static void Clear()
        {
            Cleared++;
            if (I == null) return;
            I._hideAt = 0; I._cur = HintPri.Tutorial;
            I._lastShown.Clear();
            I.SetVisible(false);
        }

        public void AttachTo(Transform cam)
        {
            transform.SetParent(cam, false);
            transform.localPosition = new Vector3(0, -0.36f, 1.2f); // чуть ниже центра; верх — задача/HP/инструменты, низ экрана свободен
            transform.localRotation = Quaternion.identity;
            _bg = Prim.Quad("HintBg", transform, new Vector3(0, 0, 0.01f), new Vector2(1.0f, 0.09f), new Color(0, 0, 0, 0.72f));
            _bg.sortingOrder = 1; Prim.Hud(_bg); Prim.SetOverlayColor(_bg, new Color(0, 0, 0, 0.95f));
            _text = Prim.Text(transform, Vector3.zero, "", 0.05f, new Color(1f, 0.93f, 0.75f));
            _text.GetComponent<MeshRenderer>().sortingOrder = 6;
            SetVisible(false);
        }

        void ShowInternal(string msg, HintPri pri, float duration, bool good)
        {
            if (string.IsNullOrEmpty(msg) || _text == null) return;
            if (!good && Tools.InDungeon && !HintsToggle.On) { Suppressed++; return; } // пункт 10: подсказки выключены (H); ошибки, опасность и зелёные «готово» — остаются
            bool visible = Time.unscaledTime < _hideAt;
            if (visible && pri < _cur) return;
            if (_lastShown.TryGetValue(msg, out var t) && Time.unscaledTime - t < 8f && pri < HintPri.Danger) return;
            _lastShown[msg] = Time.unscaledTime;
            if (_lastShown.Count > 64) _lastShown.Clear(); // кеш подсказок не растёт бесконечно
            _cur = pri;
            string shown = msg;
            if (msg.Length > 60)
            {   // длинная подсказка — в две строки по ближайшему пробелу к середине
                int mid = msg.Length / 2, cut = msg.LastIndexOf(' ', mid);
                int cut2 = msg.IndexOf(' ', mid);
                if (cut2 > 0 && (cut < 0 || cut2 - mid < mid - cut)) cut = cut2;
                if (cut > 0) shown = msg.Substring(0, cut) + "\n" + msg.Substring(cut + 1);
            }
            _text.text = shown;
            _text.color = good ? new Color(0.45f, 1f, 0.45f) : pri >= HintPri.Error ? new Color(1f, 0.55f, 0.5f) : pri == HintPri.Window ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.93f, 0.75f);
            int lines = shown.Split('\n').Length, longest = 0;
            foreach (var l in shown.Split('\n')) longest = Mathf.Max(longest, l.Length);
            _bg.transform.localScale = new Vector3(Mathf.Clamp(HudPanel.TextWidth(_text) + 0.06f, 0.3f, 2f * HudPanel.HalfW - 0.06f), 0.062f * lines + 0.03f, 1f);
            _hideAt = Time.unscaledTime + duration;
            SetVisible(true);
        }

        /// <summary>Где сейчас подсказка на экране (координаты HUD, для проверки «не налезает»).</summary>
        public static Rect Area => I == null || I._bg == null || !I._bg.gameObject.activeSelf ? Rect.zero :
            new Rect(-I._bg.transform.localScale.x * 0.5f, I.transform.localPosition.y + I._bg.transform.localScale.y * 0.5f, I._bg.transform.localScale.x, I._bg.transform.localScale.y);
        public static bool Visible => I != null && I._bg != null && I._bg.gameObject.activeSelf;

        void Update()
        {
            if (_text != null && _text.gameObject.activeSelf && (Time.unscaledTime >= _hideAt || HudPanel.ModalOpen)) SetVisible(false);
        }

        void SetVisible(bool v)
        {
            if (_text != null) _text.gameObject.SetActive(v);
            if (_bg != null) _bg.gameObject.SetActive(v);
        }
    }
}
