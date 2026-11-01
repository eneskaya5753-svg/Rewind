using UnityEngine;

namespace EcoGame.Environment
{
    public class FogController : MonoBehaviour
    {
        [SerializeField] private EnvironmentManager environmentManager;

        private float _density;

        private void Start()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            _density = TargetDensity();
        }

        private void Update()
        {
            var settings = environmentManager.Settings;
            RenderSettings.fogColor = settings.fogColor;
            _density = Mathf.Lerp(_density, TargetDensity(), settings.visualLerpSpeed * Time.deltaTime);
            RenderSettings.fogDensity = _density;
        }

        private float TargetDensity()
        {
            var settings = environmentManager.Settings;
            float t = Mathf.InverseLerp(settings.fogStartPollution, settings.loseValue, environmentManager.Pollution);
            return Mathf.Lerp(settings.fogDensityClean, settings.fogDensityMax, t);
        }
    }
}
