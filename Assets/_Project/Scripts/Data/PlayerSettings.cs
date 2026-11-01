using UnityEngine;

namespace EcoGame.Data
{
    [CreateAssetMenu(menuName = "EcoGame/Player Settings")]
    public class PlayerSettings : ScriptableObject
    {
        [Header("Movement")]
        public float moveSpeed = 7f;
        public float sprintMultiplier = 1.6f;
        public float turnSpeed = 720f;
        public float gravity = -20f;
        public float jumpHeight = 1.2f;

        [Header("Inventory")]
        public int bagCapacity = 20;
        public int waterCapacity = 5;

        [Header("Interaction")]
        public float interactRadius = 2.2f;
        public float buildPanelRadius = 6f;
        public float inspectDistance = 40f;
        [Tooltip("Info shows only when the looked-at point is this close to the player.")]
        public float inspectRange = 3.5f;

        [Header("Camera")]
        public float mouseSensitivity = 0.12f;
        public float pitchLimit = 80f;
        public float topDownHeight = 16f;
        public float topDownDistance = 10f;
        [Tooltip("World direction the top-down camera faces (0 = north/river, -90 = west/sea).")]
        public float topDownYaw = -90f;
        public float topDownFollowSpeed = 8f;
    }
}
