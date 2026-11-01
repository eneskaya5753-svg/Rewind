using EcoGame.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoGame.Cameras
{
    /// <summary>Pitch only; yaw is applied to the player body by PlayerController.</summary>
    public class FirstPersonCameraRig : MonoBehaviour
    {
        [SerializeField] private PlayerSettings settings;

        private float _pitch;

        private void Update()
        {
            var mouse = Mouse.current;
            if (Time.timeScale == 0f || mouse == null || Cursor.lockState != CursorLockMode.Locked) return;
            _pitch = Mathf.Clamp(_pitch - mouse.delta.ReadValue().y * settings.mouseSensitivity,
                -settings.pitchLimit, settings.pitchLimit);
            transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }
    }
}
