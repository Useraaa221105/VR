using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.InputSystem.XR;
using UnityEngine.InputSystem.Controls;
using InputDevice = UnityEngine.InputSystem.InputDevice;

namespace Podz
{
    /// <summary>Клавиатура/мышь (Input System) — режим без шлема для проверки в редакторе.</summary>
    public static class DIn
    {
        public static bool Held(Key k) => Keyboard.current != null && Keyboard.current[k].isPressed;
        public static bool Down(Key k) => (Keyboard.current != null && Keyboard.current[k].wasPressedThisFrame) || (_fakeKey == k && _fakeFrame == Time.frameCount);
        public static bool LMB => (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || (_fakeKey == Key.None && _fakeClick && _fakeFrame == Time.frameCount);
        // автотест: «нажать» клавишу/щелчок в следующем кадре (в пакетном режиме у окна нет фокуса, настоящие события не доходят)
        static Key _fakeKey; static bool _fakeClick; static int _fakeFrame = -1;
        public static void DebugTap(Key k) { _fakeKey = k; _fakeClick = false; _fakeFrame = Time.frameCount + 1; }
        public static void DebugClick() { _fakeKey = Key.None; _fakeClick = true; _fakeFrame = Time.frameCount + 1; }
        public static bool RMB => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        public static bool MMB => Mouse.current != null && Mouse.current.middleButton.wasPressedThisFrame;
        public static Vector2 MouseDelta => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
    }

    /// <summary>
    /// Шлем и контроллеры через Input System (OpenXR на Quest 3 и XR Device Simulator из XR Interaction Toolkit
    /// дают одинаковые устройства XRHMD/XRController). Один вход для всей игры: позы, grip, trigger, кнопки, стики.
    /// </summary>
    public static class XRIn
    {
        /// <summary>Контролы одной руки, найденные по стандартным usage (одинаковы у OpenXR Touch и симулятора XRI).</summary>
        class Hand
        {
            public XRController Dev;
            public Vector3Control Pos; public QuaternionControl Rot;
            public AxisControl Grip, Trig; public ButtonControl Prim, Sec; public Vector2Control Stick;
        }
        static readonly Hand[] _h = { new Hand(), new Hand() };
        static XRHMD _hmdDev;
        static Vector3Control _hp; static QuaternionControl _hr;

        static T ByUsage<T>(InputDevice d, string usage) where T : InputControl
        {
            foreach (var c in d.allControls) if (c is T t) foreach (var u in c.usages) if (u == usage) return t;
            return null;
        }

        static T ByName<T>(InputDevice d, string name) where T : InputControl => d.TryGetChildControl<T>(name);

        static Hand GetHand(int i)
        {
            var dev = i == 0 ? XRController.leftHand : XRController.rightHand;
            var h = _h[i];
            if (h.Dev == dev) return h;
            h.Dev = dev;
            if (dev == null) { h.Pos = null; h.Rot = null; h.Grip = h.Trig = null; h.Prim = h.Sec = null; h.Stick = null; return h; }
            h.Pos = ByName<Vector3Control>(dev, "devicePosition") ?? ByUsage<Vector3Control>(dev, "DevicePosition");
            h.Rot = ByName<QuaternionControl>(dev, "deviceRotation") ?? ByUsage<QuaternionControl>(dev, "DeviceRotation");
            h.Grip = ByUsage<AxisControl>(dev, "Grip");
            h.Trig = ByUsage<AxisControl>(dev, "Trigger");
            h.Prim = ByUsage<ButtonControl>(dev, "PrimaryButton");
            h.Sec = ByUsage<ButtonControl>(dev, "SecondaryButton");
            h.Stick = ByUsage<Vector2Control>(dev, "Primary2DAxis");
            return h;
        }

        static XRHMD Hmd()
        {
            var d = InputSystem.GetDevice<XRHMD>();
            if (d != _hmdDev)
            {
                _hmdDev = d;
                _hp = d == null ? null : (ByName<Vector3Control>(d, "centerEyePosition") ?? ByName<Vector3Control>(d, "devicePosition"));
                _hr = d == null ? null : (ByName<QuaternionControl>(d, "centerEyeRotation") ?? ByName<QuaternionControl>(d, "deviceRotation"));
            }
            return d;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { ForceActive = false; _hmdDev = null; _h[0] = new Hand(); _h[1] = new Hand(); }

        /// <summary>Автотест/симулятор: считать, что шлем надет, даже если XR-подсистема не запущена.</summary>
        public static bool ForceActive;

        /// <summary>Шлем подключён: настоящий (OpenXR) или симулятор XRI (XRSimulatedHMD — наследник XRHMD).</summary>
        public static bool Active => XRSettings.isDeviceActive || ForceActive || InputSystem.GetDevice<XRHMD>() != null;

        static int H(XRNode n) => n == XRNode.LeftHand ? 0 : 1;
        static Quaternion Safe(Quaternion r) => (r.x == 0 && r.y == 0 && r.z == 0 && r.w == 0) ? Quaternion.identity : r;

        public static bool HeadPose(out Vector3 p, out Quaternion r)
        {
            p = Vector3.zero; r = Quaternion.identity;
            if (Hmd() == null || _hp == null || _hr == null) return false;
            p = _hp.ReadValue(); r = Safe(_hr.ReadValue());
            return true;
        }

        public static bool Pose(XRNode n, out Vector3 p, out Quaternion r)
        {
            var h = GetHand(H(n));
            p = Vector3.zero; r = Quaternion.identity;
            if (h.Pos == null || h.Rot == null) return false;
            p = h.Pos.ReadValue(); r = Safe(h.Rot.ReadValue());
            return true;
        }

        public static float Grip(XRNode n) { var h = GetHand(H(n)); return h.Grip != null ? h.Grip.ReadValue() : 0f; }
        public static float Trigger(XRNode n) { var h = GetHand(H(n)); return h.Trig != null ? h.Trig.ReadValue() : 0f; }
        public static bool Primary(XRNode n) { var h = GetHand(H(n)); return h.Prim != null && h.Prim.isPressed; }
        public static bool Secondary(XRNode n) { var h = GetHand(H(n)); return h.Sec != null && h.Sec.isPressed; }
        public static Vector2 Stick(XRNode n) { var h = GetHand(H(n)); return h.Stick != null ? h.Stick.ReadValue() : Vector2.zero; }

        static readonly List<XRInputSubsystem> _subs = new List<XRInputSubsystem>();

        public static void SetFloorOrigin()
        {
            _subs.Clear();
            SubsystemManager.GetSubsystems(_subs);
            foreach (var s in _subs) s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        }
    }

    /// <summary>Детектор нажатия (переход false→true) для кнопок/осей.</summary>
    public struct Edge
    {
        bool _prev;
        public bool Down { get; private set; }
        public bool Up { get; private set; }
        public bool Held { get; private set; }
        public void Update(bool now) { Down = now && !_prev; Up = !now && _prev; Held = now; _prev = now; }
    }
}
