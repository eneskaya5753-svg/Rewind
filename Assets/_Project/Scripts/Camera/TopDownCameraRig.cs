using EcoGame.Data;
using UnityEngine;

namespace EcoGame.Cameras
{
    public class TopDownCameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private PlayerSettings settings;

        private Vector3 Offset =>
            Quaternion.Euler(0f, settings.topDownYaw, 0f) * new Vector3(0f, settings.topDownHeight, -settings.topDownDistance);

        private void OnEnable()
        {
            if (target != null) Snap();
        }

        private void LateUpdate()
        {
            if (target == null) return;
            float t = 1f - Mathf.Exp(-settings.topDownFollowSpeed * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, target.position + Offset, t);
            transform.rotation = Quaternion.LookRotation(-Offset);
        }

        private void Snap()
        {
            transform.position = target.position + Offset;
            transform.rotation = Quaternion.LookRotation(-Offset);
        }
    }
}
