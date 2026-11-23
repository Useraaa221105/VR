using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace Podz
{
    /// <summary>
    /// Пункт 8: экран «Как охотиться» в начале подземелья (пауза): светофор монстра и клавиши. Кнопка «Понятно (Enter)».
    /// Открывается исключительно вручную по F1. Автоматического показа нет.
    /// </summary>
    public class HuntIntro : MonoBehaviour
    {
        static HuntIntro _i;
        public static bool Open => _i != null;
        public static int Shown;
        public static string LastText = "";
        /// <summary>В автотесте сам не открывается (его открывает этап теста).</summary>
        public static bool AutoShow = false;
        static bool _shownThisRun;
        [SerializeField, TextArea] string _summary;
        float _openedAt;
        Edge _a, _x;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _shownThisRun = false; Shown = 0; AutoShow = false; }

        public static void Poll()
        {
            var dm = DungeonManager.I; var P = PlayerRig.I;
            if (P == null) return;
            if (DIn.Down(UnityEngine.InputSystem.Key.F1) && !Open) { ShowNow(); return; }

        }

        public static void ShowNow()
        {
            var P = PlayerRig.I;
            if (P == null || _i != null) return;
            if (BestiaryCard.Open) BestiaryCard.ForceClose();
            _shownThisRun = true; Shown++;
            var cam = P.Cam.transform;
            var config = SavedDungeonAssets.Active;
            var template = P.IsXR ? config?.HuntIntroVr : config?.HuntIntroPc;
            _i = template != null ? Instantiate(template).GetComponent<HuntIntro>() : BuildVisual(P.IsXR);
            var go = _i.gameObject;
            var fwd = cam.forward; if (Mathf.Abs(fwd.y) > 0.6f) fwd.y = Mathf.Sign(fwd.y) * 0.6f; fwd.Normalize();
            go.transform.position = cam.position + fwd * 1.4f + Vector3.up * 0.05f;
            go.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            go.transform.localScale = Vector3.one * 0.88f;
            _i._openedAt = Time.unscaledTime;
            LastText = _i._summary;
            Prim.OnTop(go.transform);
            DTime.Paused = true;
            Sfx.Play(SfxId.Card);
        }

        public static HuntIntro BuildVisual(bool xr)
        {
            var screen = SavedUi.CreateRoot("HuntIntro").AddComponent<HuntIntro>();
            screen._summary = Build(screen.transform, xr);
            return screen;
        }

        static TextMesh T(Transform p, Vector3 pos, string s, float h, Color c, TextAnchor a = TextAnchor.MiddleLeft)
        { var t = Prim.Text(p, pos, s, h, c, a); t.GetComponent<MeshRenderer>().sortingOrder = 6; return t; }
        static void Q(Transform p, Vector3 pos, Vector2 size, Color c, int order = 3)
        { var q = Prim.Quad("Q", p, pos, size, c); q.sortingOrder = order; }
        static void Key(Transform p, Vector3 pos, string k, float w = 0.09f)
        {
            Q(p, pos + new Vector3(0, 0, 0.003f), new Vector2(w, 0.08f), new Color(0.75f, 0.72f, 0.68f), 3);
            Q(p, pos + new Vector3(0, 0.003f, 0.002f), new Vector2(w - 0.014f, 0.064f), new Color(0.95f, 0.93f, 0.9f), 4);
            var t = T(p, pos + new Vector3(0, 0.003f, 0), k, 0.045f, Color.black, TextAnchor.MiddleCenter); t.fontStyle = FontStyle.Bold;
        }

        static string Build(Transform root, bool xr)
        {
            var sb = new System.Text.StringBuilder();
            var gold = new Color(1f, 0.82f, 0.35f);
            Prim.Cube("Frame", root, new Vector3(0, 0, 0.03f), new Vector3(1.62f, 1.08f, 0.02f), gold * 0.7f, false);
            Prim.Cube("Panel", root, new Vector3(0, 0, 0.018f), new Vector3(1.58f, 1.04f, 0.02f), new Color(0.11f, 0.075f, 0.05f), false);
            T(root, new Vector3(0, 0.45f, 0), "КАК ОХОТИТЬСЯ", 0.08f, gold, TextAnchor.MiddleCenter);
            sb.AppendLine("КАК ОХОТИТЬСЯ");
            T(root, new Vector3(0, 0.37f, 0), "Смотри на свет монстра — он подсказывает, когда бить", 0.034f, Color.white, TextAnchor.MiddleCenter);
            float x = -0.7f;
            MonsterCard.Lamp(root, new Vector3(x, 0.27f, -0.004f), 0.08f, new Color(0.15f, 0.9f, 0.3f));
            T(root, new Vector3(x + 0.07f, 0.27f, 0), "ЗЕЛЁНЫЙ — бить НЕЛЬЗЯ: лут испортится", 0.042f, new Color(0.55f, 1f, 0.6f));
            MonsterCard.Lamp(root, new Vector3(x, 0.17f, -0.004f), 0.08f, new Color(1f, 0.85f, 0.1f));
            T(root, new Vector3(x + 0.07f, 0.17f, 0), "ЖЁЛТЫЙ — можно бить нужным инструментом", 0.042f, new Color(1f, 0.9f, 0.45f));
            T(root, new Vector3(x + 0.07f, 0.115f, 0), "(оглушён, устал, клюв или меч застрял, треснул, перегрев)", 0.028f, new Color(1f, 0.9f, 0.6f, 0.85f));
            MonsterCard.Lamp(root, new Vector3(x, 0.05f, -0.004f), 0.08f, new Color(1f, 0.15f, 0.1f));
            T(root, new Vector3(x + 0.07f, 0.05f, 0), "КРАСНЫЙ — сейчас ударит: шагни вбок", 0.042f, new Color(1f, 0.6f, 0.55f));
            sb.AppendLine("ЗЕЛЁНЫЙ — бить НЕЛЬЗЯ: лут испортится; ЖЁЛТЫЙ — можно бить нужным инструментом; КРАСНЫЙ — сейчас ударит");
            Q(root, new Vector3(0, -0.02f, 0.007f), new Vector2(1.5f, 0.004f), new Color(0.5f, 0.4f, 0.3f));
            // клавиши
            float y1 = -0.1f, y2 = -0.21f;
            if (!xr)
            {
                for (int i = 1; i <= 6; i++) Key(root, new Vector3(-0.72f + (i - 1) * 0.1f, y1, 0), i.ToString());
                T(root, new Vector3(-0.1f, y1, 0), "инструменты (лопатка, сачок, тесак, молоток, щит…)", 0.03f, Color.white);
                Key(root, new Vector3(-0.72f, y2, 0), "B"); T(root, new Vector3(-0.66f, y2, 0), "бестиарий", 0.032f, Color.white);
                Key(root, new Vector3(-0.38f, y2, 0), "F2"); T(root, new Vector3(-0.32f, y2, 0), "подсказки", 0.032f, Color.white);
                Key(root, new Vector3(0.0f, y2, 0), Tools.CrouchKey.Split(' ')[0]); T(root, new Vector3(0.06f, y2, 0), "присесть", 0.032f, Color.white);
                Key(root, new Vector3(0.28f, y2, 0), "E"); T(root, new Vector3(0.34f, y2, 0), "схватить", 0.032f, Color.white);
                Key(root, new Vector3(0.55f, y2, 0), "ЛКМ", 0.12f); T(root, new Vector3(0.625f, y2, 0), "бить", 0.032f, Color.white);
                sb.AppendLine("Клавиши: 1–6 — инструменты, B — бестиарий, F2 — настройки, C — присесть, E — схватить, ЛКМ — бить");
            }
            else
            {
                T(root, new Vector3(-0.72f, y1, 0), "Инструменты — на поясе (бери рукой), Y — бестиарий, F2 на клавиатуре — настройки", 0.03f, Color.white);
                T(root, new Vector3(-0.72f, y2, 0), "Пригнись — присесть, Grip — схватить, взмах — бить", 0.03f, Color.white);
                sb.AppendLine("VR: инструменты на поясе, Y — бестиарий, F2 — настройки, пригнись, Grip, взмах");
            }
            T(root, new Vector3(0, -0.3f, 0), "B — открыть бестиарий вручную; F2 — настройки помощи", 0.03f, new Color(0.8f, 0.9f, 1f), TextAnchor.MiddleCenter);
            Q(root, new Vector3(0, -0.4f, 0.004f), new Vector2(0.5f, 0.085f), new Color(0.2f, 0.55f, 0.25f), 3);
            T(root, new Vector3(0, -0.4f, 0), xr ? "Понятно (A)" : "Понятно (Enter)", 0.048f, Color.white, TextAnchor.MiddleCenter);
            T(root, new Vector3(0, -0.465f, 0), "F1 — открыть этот экран снова", 0.024f, new Color(1, 1, 1, 0.6f), TextAnchor.MiddleCenter);
            sb.AppendLine("Понятно (Enter)");
            return sb.ToString();
        }

        void Update()
        {
            _a.Update(XRIn.Primary(XRNode.RightHand)); _x.Update(XRIn.Primary(XRNode.LeftHand));
            if (Time.unscaledTime - _openedAt < 0.3f) return;
            if (DIn.Down(UnityEngine.InputSystem.Key.Enter) || DIn.Down(UnityEngine.InputSystem.Key.NumpadEnter) || DIn.Down(UnityEngine.InputSystem.Key.Space) || _a.Down || _x.Down) Close();
        }

        public static void ForceClose() { if (_i != null) _i.Close(); }
        void Close() { Destroy(gameObject); }
        void OnDestroy() { if (_i == this) { _i = null; DTime.Paused = false; } }
    }
}
