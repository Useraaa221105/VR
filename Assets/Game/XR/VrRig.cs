using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Podz
{
    /// <summary>
    /// VR-обвязка игрока на XR Interaction Toolkit 3: XR Origin, менеджер взаимодействий, Near-Far Interactor в каждой руке
    /// (grip — взять, trigger — действие) и мост «кнопка игры ↔ XRI»: любую кнопку (Pressable) можно нажать лучом издалека
    /// или рукой вблизи. Удары и хват инструментов считаются своим HitDetector/HandController (ТЗ 8.3), а XRI отвечает за
    /// трекинг-обвязку и взаимодействие с кнопками/меню.
    /// </summary>
    public class VrRig : MonoBehaviour
    {
        public static VrRig I { get; private set; }
        public static bool On => I != null && I.enabled;
        public XROrigin Origin { get; private set; }
        public XRInteractionManager Manager { get; private set; }
        public NearFarInteractor LeftInteractor { get; private set; }
        public NearFarInteractor RightInteractor { get; private set; }
        /// <summary>Сколько раз кнопка игры нажата через XRI (для автотеста).</summary>
        public static int XriPresses;

        static readonly List<Pressable> _pending = new List<Pressable>();
        readonly List<InputAction> _actions = new List<InputAction>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { I = null; XriPresses = 0; _pending.Clear(); }

        /// <summary>Включить VR-обвязку на игроке (вызывается PlayerRig при обнаружении шлема или симулятора).</summary>
        public static VrRig Attach(PlayerRig rig)
        {
            var v = rig.GetComponent<VrRig>();
            if (v == null) v = rig.gameObject.AddComponent<VrRig>();
            v.enabled = true;
            return v;
        }

        public static void Detach(PlayerRig rig)
        {
            var v = rig != null ? rig.GetComponent<VrRig>() : null;
            if (v != null) v.enabled = false;
        }

        void Awake()
        {
            I = this;
            var rig = GetComponent<PlayerRig>();
            // XR Origin: корень = игрок, смещение = CameraOffset, камера = голова
            Origin = gameObject.AddComponent<XROrigin>();
            Origin.Origin = gameObject;
            Origin.CameraFloorOffsetObject = rig.Offset.gameObject;
            Origin.Camera = rig.Cam;
            Origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            Origin.CameraYOffset = 0f;

            Manager = FindAnyObjectByType<XRInteractionManager>();
            if (Manager == null) Manager = new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();

            LeftInteractor = MakeInteractor(rig.Left, "{LeftHand}");
            RightInteractor = MakeInteractor(rig.Right, "{RightHand}");

            foreach (var p in FindObjectsByType<Pressable>()) Bridge(p);
        }

        NearFarInteractor MakeInteractor(HandController hand, string usage)
        {
            var go = new GameObject("NearFarInteractor");
            go.SetActive(false); // настроить ввод до OnEnable
            go.transform.SetParent(hand.transform, false);
            var it = go.AddComponent<NearFarInteractor>();
            it.interactionManager = Manager;
            it.selectInput.inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction;
            it.selectInput.inputActionPerformed = Button($"<XRController>{usage}/{{GripButton}}", $"<XRController>{usage}/{{Grip}}");
            it.selectInput.inputActionValue = Value($"<XRController>{usage}/{{Grip}}");
            it.activateInput.inputSourceMode = XRInputButtonReader.InputSourceMode.InputAction;
            it.activateInput.inputActionPerformed = Button($"<XRController>{usage}/{{TriggerButton}}", $"<XRController>{usage}/{{Trigger}}");
            it.activateInput.inputActionValue = Value($"<XRController>{usage}/{{Trigger}}");
            go.SetActive(true);
            return it;
        }

        InputAction Button(params string[] paths)
        {
            var a = new InputAction(type: InputActionType.Button);
            foreach (var p in paths) a.AddBinding(p);
            a.Enable(); _actions.Add(a);
            return a;
        }

        InputAction Value(string path)
        {
            var a = new InputAction(type: InputActionType.Value, binding: path, expectedControlType: "Axis");
            a.Enable(); _actions.Add(a);
            return a;
        }

        void OnEnable()
        {
            I = this;
            if (LeftInteractor != null) LeftInteractor.gameObject.SetActive(true);
            if (RightInteractor != null) RightInteractor.gameObject.SetActive(true);
            foreach (var a in _actions) a.Enable();
        }

        void OnDisable()
        {
            if (LeftInteractor != null) LeftInteractor.gameObject.SetActive(false);
            if (RightInteractor != null) RightInteractor.gameObject.SetActive(false);
            foreach (var a in _actions) a.Disable();
        }

        void OnDestroy()
        {
            foreach (var a in _actions) a.Dispose();
            _actions.Clear();
            if (I == this) I = null;
        }

        void Update()
        {
            // кнопки, созданные после включения VR (новые этажи, магазин), подключаем к XRI порциями — без поиска по сцене
            for (int i = _pending.Count - 1; i >= 0; i--) { Bridge(_pending[i]); _pending.RemoveAt(i); }
            // trigger по кнопке, на которую указывает луч/рука Near-Far Interactor, — нажатие (ТЗ: «trigger — действие»)
            _trigL.Update(XRIn.Trigger(UnityEngine.XR.XRNode.LeftHand) > 0.6f);
            _trigR.Update(XRIn.Trigger(UnityEngine.XR.XRNode.RightHand) > 0.6f);
            if (_trigL.Down) PressHovered(LeftInteractor);
            if (_trigR.Down) PressHovered(RightInteractor);
        }

        Edge _trigL, _trigR;

        static void PressHovered(NearFarInteractor it)
        {
            if (it == null) return;
            var hovered = it.interactablesHovered;
            for (int i = 0; i < hovered.Count; i++)
            {
                var c = hovered[i] as Component;
                var p = c != null ? c.GetComponent<Pressable>() : null;
                if (p == null) continue;
                XriPresses++;
                p.Press();
                return;
            }
        }

        /// <summary>Вызывается из Pressable.Start: зарегистрировать кнопку для XRI.</summary>
        public static void Register(Pressable p) { if (On) _pending.Add(p); }

        static void Bridge(Pressable p)
        {
            if (p == null || p.GetComponent<XRSimpleInteractable>() != null) return;
            var col = p.GetComponent<Collider>();
            if (col == null) return;
            var si = p.gameObject.AddComponent<XRSimpleInteractable>(); // коллайдер кнопки XRI находит сам
            // нажатие: trigger (activate) по наведённой кнопке или grip (select) вплотную
            si.activated.AddListener(_ => { XriPresses++; p.Press(); });
            si.selectEntered.AddListener(_ => { XriPresses++; p.Press(); });
        }

        /// <summary>Автотест: нажать кнопку через XRI так же, как это сделает контроллер (activate/select).</summary>
        public bool DebugPressViaXri(Pressable p, bool left = false)
        {
            if (p == null) return false;
            Bridge(p);
            var si = p.GetComponent<XRSimpleInteractable>();
            var it = left ? LeftInteractor : RightInteractor;
            if (si == null || it == null || Manager == null) return false;
            int before = XriPresses;
            Manager.SelectEnter((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)it, si);
            Manager.SelectExit((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)it, si);
            return XriPresses > before;
        }
    }
}
