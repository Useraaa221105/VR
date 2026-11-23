using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using InputDevice = UnityEngine.XR.InputDevice;
using CommonUsages = UnityEngine.XR.CommonUsages;

namespace Podz
{
    /// <summary>
    /// Игрок: XR (Quest 3 через OpenXR) или ПК-режим (WASD + мышь) для проверки без шлема.
    /// Камера никогда не двигается сама — только от головы игрока, стика или телепорта под затемнением.
    /// </summary>
    public class PlayerRig : MonoBehaviour
    {
        public static PlayerRig I;

        public Camera Cam { get; private set; }
        public Transform Offset { get; private set; }
        public Transform Body { get; private set; }
        public HandController Left { get; private set; }
        public HandController Right { get; private set; }
        public PlayerHealth Health { get; private set; }
        public bool IsXR { get; private set; }
        public bool Crouched { get; private set; }
        public float Speed { get; private set; }
        public readonly Dictionary<ToolType, Tool> Tools = new Dictionary<ToolType, Tool>();

        public Vector3 HeadPos => Cam.transform.position;
        public Vector3 HeadForward => Cam.transform.forward;
        public Vector3 Feet => new Vector3(Cam.transform.position.x, transform.position.y, Cam.transform.position.z);

        public float MoveSpeed = 3f;
        CharacterController _cc;
        float _yaw, _pitch, _vy;
        bool _deskCrouch, _modeSet;
        Edge _snapL, _snapR;
        Vector3 _lastHead;

        public static PlayerRig Create(Vector3 pos, float yaw)
        {
            var go = new GameObject("PlayerRig");
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var r = go.AddComponent<PlayerRig>();
            r._yaw = yaw;
            r.Build();
            return r;
        }

        void Build()
        {
            I = this;
            _cc = gameObject.AddComponent<CharacterController>();
            _cc.height = 1.7f; _cc.radius = 0.25f; _cc.center = new Vector3(0, 0.85f, 0);
            _cc.stepOffset = 0.3f; _cc.skinWidth = 0.02f;

            Offset = new GameObject("CameraOffset").transform;
            Offset.SetParent(transform, false);
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(Offset, false);
            camGo.transform.localPosition = new Vector3(0, 1.6f, 0);
            Cam = camGo.AddComponent<Camera>();
            Cam.nearClipPlane = 0.03f;
            Cam.farClipPlane = 80f;
            camGo.AddComponent<AudioListener>();

            Body = new GameObject("Body").transform;
            Body.SetParent(transform, false);

            Health = gameObject.AddComponent<PlayerHealth>();
            Health.Init(this);
            Left = HandController.Create(this, true);
            Right = HandController.Create(this, false);
            Application.onBeforeRender += OnBeforeRender;
        }

        void OnDestroy()
        {
            Application.onBeforeRender -= OnBeforeRender;
            if (I == this) I = null;
        }

        // ---------- инструменты и кобуры ----------
        public Tool GiveTool(ToolType t)
        {
            if (Tools.TryGetValue(t, out var existing)) return existing;
            var holster = new GameObject("Holster_" + t).transform;
            holster.SetParent(Body, false);
            switch (t)
            {
                case ToolType.Spatula: holster.localPosition = new Vector3(-0.22f, 0.95f, 0.05f); break; // левое бедро
                case ToolType.Cleaver: holster.localPosition = new Vector3(0.22f, 0.95f, 0.05f); break;  // правое бедро
                case ToolType.Net: holster.localPosition = new Vector3(-0.32f, 1.5f, -0.3f); break;      // за спиной
                case ToolType.Hammer: holster.localPosition = new Vector3(0f, 0.9f, -0.2f); break;       // пояс сзади
                default: holster.localPosition = new Vector3(-0.28f, 1.15f, -0.12f); break;              // щит — левая рука/спина
            }
            holster.localRotation = Quaternion.Euler(90, 0, 0); // висит вниз
            Prim.Cube("Mount", holster, new Vector3(0,0,.06f), new Vector3(.08f,.06f,.04f), Podz.Tools.ColorOf(t)*.5f, false);
            var tool = Tool.Create(t, holster);
            Tools[t] = tool;
            return tool;
        }

        public bool Has(ToolType t) => Tools.ContainsKey(t);

        public bool ShieldRaisedTowards(Vector3 source)
        {
            if (!Tools.TryGetValue(ToolType.Shield, out var s) || s.Holder == null) return false;
            var toSrc = (source - s.transform.position).normalized;
            return Vector3.Dot(s.transform.forward, toSrc) > 0.55f;
        }

        // ---------- цикл ----------
        void Update()
        {
            bool xr = XRIn.Active;
            if (xr != IsXR || !_modeSet)
            {
                IsXR = xr; _modeSet = true;
                Left.SetMode(xr); Right.SetMode(xr);
                if (xr) { XRIn.SetFloorOrigin(); VrRig.Attach(this); Capture(false); }
                else { VrRig.Detach(this); Capture(true); } // исправление 6: на ПК мышь захватывается сразу
            }

            if (IsXR) UpdateXRHead(); else UpdateDesktopLook();
            Crouched = IsXR ? Cam.transform.localPosition.y < 1.2f : _deskCrouch;
            Move();
            Body.position = new Vector3(HeadPos.x, transform.position.y, HeadPos.z);
            Body.rotation = Quaternion.Euler(0, Cam.transform.eulerAngles.y, 0);

            var h = HeadPos; h.y = 0;
            float inst = Time.deltaTime > 0 ? (h - _lastHead).magnitude / Time.deltaTime : 0;
            Speed = Mathf.Lerp(Speed, Mathf.Min(inst, 10f), 0.2f);
            _lastHead = h;
        }

        void OnBeforeRender()
        {
            if (!IsXR) return;
            UpdateXRHead();
            Left.UpdatePose();
            Right.UpdatePose();
        }

        void UpdateXRHead()
        {
            if (XRIn.HeadPose(out var p, out var r))
            {
                Cam.transform.localPosition = p;
                Cam.transform.localRotation = r;
            }
        }

        void UpdateDesktopLook()
        {
            if (DIn.Down(Key.Escape)) Capture(false);
            else if (!MouseCaptured && (DIn.LMB || DIn.RMB)) Capture(true); // щелчок по игре — снова управляем мышью
            if (MouseCaptured && Health.MoveMultiplier > 0)
            {
                var d = DIn.MouseDelta * 0.12f;
                _yaw += d.x;
                _pitch = Mathf.Clamp(_pitch - d.y, -85f, 85f);
            }
            if (DIn.Down(Key.C) || DIn.Down(Key.LeftCtrl)) { _deskCrouch = !_deskCrouch; CrouchToggles++; Hints.Show(_deskCrouch ? $"Присел — тише и ниже ({Podz.Tools.CrouchKey} — встать)" : "Встал", HintPri.Action, 1.5f); } // пункт 6: присесть — переключатель на C (основная, без конфликта с Ctrl+S в редакторе) или левый Ctrl (запасная)
            transform.rotation = Quaternion.Euler(0, _yaw, 0);
            Cam.transform.localPosition = new Vector3(0, _deskCrouch ? 1.0f : 1.6f, 0);
            Cam.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }

        void Move()
        {
            Vector2 input;
            if (IsXR) input = XRIn.Stick(XRNode.LeftHand);
            else
            {
                input.x = (DIn.Held(Key.D) ? 1 : 0) - (DIn.Held(Key.A) ? 1 : 0);
                input.y = (DIn.Held(Key.W) ? 1 : 0) - (DIn.Held(Key.S) ? 1 : 0);
            }
            if (input.magnitude < 0.15f) input = Vector2.zero;

            var fwd = Cam.transform.forward; fwd.y = 0; fwd.Normalize();
            var right = Cam.transform.right; right.y = 0; right.Normalize();
            var v = fwd * input.y + right * input.x;
            if (v.sqrMagnitude > 1) v.Normalize();
            v *= MoveSpeed * Health.MoveMultiplier * (Crouched ? 0.6f : 1f);

            if (_cc.isGrounded && _vy < 0) _vy = -1f;
            _vy -= 9.81f * Time.deltaTime;

            if (IsXR)
            {
                var hl = Cam.transform.localPosition;
                float hgt = Mathf.Clamp(hl.y, 0.9f, 1.85f);
                _cc.height = hgt;
                _cc.center = new Vector3(hl.x, hgt * 0.5f + _cc.skinWidth, hl.z);
                var rs = XRIn.Stick(XRNode.RightHand).x;
                _snapR.Update(rs > 0.7f);
                _snapL.Update(rs < -0.7f);
                if (_snapR.Down) transform.RotateAround(HeadPos, Vector3.up, 45f);
                if (_snapL.Down) transform.RotateAround(HeadPos, Vector3.up, -45f);
            }
            if (_cc.enabled) _cc.Move((v + Vector3.up * _vy) * Time.deltaTime);
        }

        /// <summary>Перенос игрока (вызывать только в темноте Fader).</summary>
        /// <summary>Повернуть взгляд (ПК): yaw — тело, pitch — камера (вниз положительный).</summary>
        public void SetLook(float yaw, float pitch)
        {
            _yaw = yaw; _pitch = Mathf.Clamp(pitch, -85f, 85f);
            transform.rotation = Quaternion.Euler(0, _yaw, 0);
            if (!IsXR) Cam.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }

        /// <summary>Посмотреть на точку.</summary>
        public void LookAt(Vector3 point)
        {
            var d = point - HeadPos;
            var flat = new Vector3(d.x, 0, d.z);
            float yaw = flat.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(flat).eulerAngles.y : _yaw;
            float pitch = -Mathf.Atan2(d.y, flat.magnitude) * Mathf.Rad2Deg;
            SetLook(yaw, pitch);
        }

        /// <summary>Встать в точку и посмотреть на цель (руки не отпускаются, если keepHands).</summary>
        public void StandLook(Vector3 feet, Vector3 lookAt, bool keepHands = true)
        {
            var l = Left.Held; var r = Right.Held;
            _cc.enabled = false;
            transform.position = feet;
            _vy = 0;
            _cc.enabled = true;
            Physics.SyncTransforms();
            _lastHead = HeadPos;
            LookAt(lookAt);
        }

        /// <summary>Шаг персонажем с учётом стен (для автотеста — как WASD).</summary>
        public void DebugWalk(Vector3 delta) { if (_cc.enabled) _cc.Move(delta); }

        public void SetCrouch(bool on) { _deskCrouch = on; }

        // ---------- захват мыши (исправление 6) ----------
        bool _wantLock;
        /// <summary>Сколько раз игра захватывала мышь и сколько раз переключался присед (для автотеста).</summary>
        public static int LockRequests, CrouchToggles;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetMouseStatics() { LockRequests = 0; CrouchToggles = 0; }

        /// <summary>Мышь управляет камерой (курсор захвачен). В пакетном режиме (автотест) курсора нет — берём запрошенное состояние.</summary>
        public bool MouseCaptured => !IsXR && (Application.isBatchMode ? _wantLock : Cursor.lockState == CursorLockMode.Locked);

        public void Capture(bool on)
        {
            _wantLock = on;
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
            if (on) LockRequests++;
        }

        // окно потеряло фокус (Alt+Tab) — ОС отпускает курсор; вернуться — щелчком (появится подсказка)
        void OnApplicationFocus(bool focus) { if (!focus) _wantLock = false; }

        public void Teleport(Vector3 feet, float yaw)
        {
            BackpackView.ClosePanel(); DungeonSettings.ClosePanel();
            _cc.enabled = false;
            Left.ForceRelease(); Right.ForceRelease();
            _yaw = yaw; _pitch = 0;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            var headLocal = Cam.transform.localPosition; headLocal.y = 0;
            var offsetWorld = IsXR ? transform.rotation * headLocal : Vector3.zero;
            transform.position = feet - offsetWorld;
            _vy = 0;
            _cc.enabled = true;
            Physics.SyncTransforms();
            _lastHead = HeadPos;
        }
    }
}
