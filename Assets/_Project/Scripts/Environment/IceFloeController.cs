using UnityEngine;

namespace EcoGame.Environment
{
    /// <summary>Scales the ice floe from 1 to iceMinScale with Temperature.</summary>
    public class IceFloeController : MonoBehaviour
    {
        [SerializeField] private EnvironmentManager environmentManager;
        [SerializeField] private Transform surfaceAnchor;
        [SerializeField] private float baseRadius = 3f;

        private float _scale = 1f;

        public float Radius => baseRadius * _scale;
        public Vector3 Center => surfaceAnchor != null ? surfaceAnchor.position : transform.position;

        private void Start()
        {
            _scale = TargetScale();
            Apply();
        }

        private void Update()
        {
            _scale = Mathf.Lerp(_scale, TargetScale(), environmentManager.Settings.visualLerpSpeed * Time.deltaTime);
            Apply();
        }

        private float TargetScale() =>
            Mathf.Lerp(1f, environmentManager.Settings.iceMinScale, environmentManager.Temperature / 100f);

        private void Apply() => transform.localScale = Vector3.one * _scale;
    }
}
