using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoGame.Cameras
{
    public enum CameraMode { TopDown, FirstPerson }

    /// <summary>Switches FP / top-down (V) and gives the movement reference direction.</summary>
    public class CameraModeManager : MonoBehaviour
    {
        [SerializeField] private FirstPersonCameraRig firstPersonRig;
        [SerializeField] private TopDownCameraRig topDownRig;
        [SerializeField] private Transform player;
        [SerializeField] private CameraMode startMode = CameraMode.TopDown;

        private bool _cursorFree;

        public CameraMode Mode { get; private set; }
        public Camera ActiveCamera { get; private set; }
        public event Action<CameraMode> OnModeChanged;

        public Vector3 MoveForward
        {
            get
            {
                Vector3 forward = Mode == CameraMode.FirstPerson ? player.forward : topDownRig.transform.forward;
                forward.y = 0f;
                return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            }
        }

        private void Awake() => SetMode(startMode);

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (Time.timeScale == 0f || keyboard == null || !keyboard.vKey.wasPressedThisFrame) return;
            SetMode(Mode == CameraMode.TopDown ? CameraMode.FirstPerson : CameraMode.TopDown);
        }

        public void SetMode(CameraMode mode)
        {
            Mode = mode;
            firstPersonRig.gameObject.SetActive(mode == CameraMode.FirstPerson);
            topDownRig.gameObject.SetActive(mode == CameraMode.TopDown);
            ActiveCamera = mode == CameraMode.FirstPerson
                ? firstPersonRig.GetComponent<Camera>()
                : topDownRig.GetComponent<Camera>();

            // In first person the body would clip into view (chin, arms when jumping); keep only its shadow.
            foreach (var bodyRenderer in player.GetComponentsInChildren<SkinnedMeshRenderer>())
                bodyRenderer.shadowCastingMode = mode == CameraMode.FirstPerson
                    ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
                    : UnityEngine.Rendering.ShadowCastingMode.On;
            ApplyCursor();
            OnModeChanged?.Invoke(mode);
        }

        public void SetCursorFree(bool free)
        {
            _cursorFree = free;
            ApplyCursor();
        }

        private void ApplyCursor()
        {
            bool locked = !_cursorFree && Mode == CameraMode.FirstPerson;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
