using EcoGame.Building;
using UnityEngine;

namespace EcoGame.Environment
{
    /// <summary>Chimney smoke that stops once every clean-energy building is done.</summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class HouseSmoke : MonoBehaviour
    {
        private ParticleSystem _smoke;
        private bool _stopped;

        private void Awake() => _smoke = GetComponent<ParticleSystem>();

        private void Update()
        {
            if (_stopped || BuildSite.Sites.Count == 0) return;
            foreach (var site in BuildSite.Sites)
                if (!site.IsCompleted) return;

            _stopped = true;
            _smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
