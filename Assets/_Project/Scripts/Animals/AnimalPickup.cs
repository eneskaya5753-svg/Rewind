using EcoGame.Cameras;
using EcoGame.Player;
using UnityEngine;

namespace EcoGame.Animals
{
    /// <summary>
    /// F (petting) picks the animal up and holds it in front of the camera, facing us; F again puts it down in front of the player.
    /// Its movement scripts are paused while held.
    /// </summary>
    public class AnimalPickup : MonoBehaviour
    {
        // Held size follows the animal's real size (a deer looks bigger than a chick), kept within this range.
        private const float HOLD_SIZE_MIN = 0.6f;
        private const float HOLD_SIZE_MAX = 0.9f;
        private const float HOLD_DISTANCE = 0.8f;
        private const float HOLD_DROP = 0f;

        [SerializeField] private string displayName = "Hayvan";

        public static AnimalPickup Held { get; private set; }

        private static CameraModeManager _cameraModeManager;
        private Transform _player;
        private Vector3 _groundScale;
        private Vector3 _localCenter; // the models pivot at their feet; this is the body center
        private MonoBehaviour[] _movers;

        public bool IsHeld => Held == this;
        public static readonly System.Collections.Generic.List<AnimalPickup> All = new();

        /// <summary>Our own pet: no key hint is shown for it.</summary>
        public bool IsPet => GetComponent<PetFollower>() != null;

        private void Awake()
        {
            _movers = new MonoBehaviour[]
            {
                GetComponent<AnimalWanderer>(), GetComponent<AnimalVisitor>(), GetComponent<PetFollower>()
            };
        }

        public void Toggle(PlayerInteractor interactor)
        {
            if (IsHeld) PutDown();
            else PickUp(interactor.transform);
        }

        private void PickUp(Transform player)
        {
            _player = player;
            Held = this;
            _groundScale = transform.localScale;
            SetMovers(false);

            // Shrink so the whole animal fits nicely in front of the camera.
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                _localCenter = transform.InverseTransformPoint(bounds.center);
                float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                if (size > 0.01f)
                    transform.localScale = _groundScale * (Mathf.Clamp(size, HOLD_SIZE_MIN, HOLD_SIZE_MAX) / size);
            }
        }

        private void PutDown()
        {
            Held = null;
            transform.localScale = _groundScale;
            Vector3 forward = _player.forward;
            forward.y = 0f;
            transform.SetPositionAndRotation(_player.position + forward.normalized * 1.2f,
                Quaternion.LookRotation(forward.sqrMagnitude > 0.001f ? forward : Vector3.forward));
            SetMovers(true);
        }

        private void SetMovers(bool on)
        {
            foreach (var mover in _movers)
                if (mover != null) mover.enabled = on;
        }

        private void LateUpdate()
        {
            if (!IsHeld) return;
            if (_cameraModeManager == null) _cameraModeManager = FindFirstObjectByType<CameraModeManager>();
            var cam = _cameraModeManager != null ? _cameraModeManager.ActiveCamera : Camera.main;
            if (cam == null) return;

            var view = cam.transform;
            transform.rotation = Quaternion.LookRotation(-view.forward, view.up);
            transform.position = view.position + view.forward * HOLD_DISTANCE - view.up * HOLD_DROP
                - transform.TransformVector(_localCenter);
        }

        private void OnEnable() => All.Add(this);

        private void OnDisable()
        {
            All.Remove(this);
            if (IsHeld) Held = null;
        }
    }
}
