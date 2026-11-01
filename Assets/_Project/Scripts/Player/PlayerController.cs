using EcoGame.Cameras;
using EcoGame.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoGame.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int JumpId = Animator.StringToHash("Jump");

        [SerializeField] private CameraModeManager cameraModeManager;
        [SerializeField] private PlayerSettings settings;

        private CharacterController _controller;
        private Animator _animator;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (Time.timeScale == 0f || keyboard == null) return;

            var input = new Vector2(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 forward = cameraModeManager.MoveForward;
            var right = new Vector3(forward.z, 0f, -forward.x);
            Vector3 move = forward * input.y + right * input.x;

            if (cameraModeManager.Mode == CameraMode.FirstPerson) RotateWithMouse();
            else FaceMovement(move);

            if (_controller.isGrounded)
            {
                bool jump = keyboard.spaceKey.wasPressedThisFrame;
                _verticalVelocity = jump ? Mathf.Sqrt(2f * settings.jumpHeight * -settings.gravity) : -1f;
                if (jump && _animator != null) _animator.SetTrigger(JumpId);
            }
            else _verticalVelocity += settings.gravity * Time.deltaTime;
            float speed = settings.moveSpeed * (keyboard.leftShiftKey.isPressed ? settings.sprintMultiplier : 1f);
            _controller.Move((move * speed + Vector3.up * _verticalVelocity) * Time.deltaTime);

            if (_animator != null) _animator.SetFloat(SpeedId, move.magnitude);
        }

        private void RotateWithMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null || Cursor.lockState != CursorLockMode.Locked) return; // free cursor = a window is open
            transform.Rotate(0f, mouse.delta.ReadValue().x * settings.mouseSensitivity, 0f);
        }

        /// <summary>Top-down: the character turns toward where it walks (A left, D right, S back).</summary>
        private void FaceMovement(Vector3 move)
        {
            if (move.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation,
                Quaternion.LookRotation(move), settings.turnSpeed * Time.deltaTime);
        }
    }
}
