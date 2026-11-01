using System.Collections.Generic;
using EcoGame.Environment;
using EcoGame.Water;
using UnityEngine;

namespace EcoGame.Animals
{
    /// <summary>Bees circle a blooming garden while pollution stays below the first threshold.</summary>
    public class BeeSwarm : MonoBehaviour
    {
        private const float BOB_SPEED = 3f;
        private const float BOB_AMOUNT = 0.3f;

        [SerializeField] private Garden garden;
        [SerializeField] private GameObject beePrefab;

        private readonly List<Transform> _bees = new();
        private readonly List<Vector3> _params = new(); // radius, phase, direction

        private void Start()
        {
            var settings = EnvironmentManager.Instance.Settings;
            for (int i = 0; i < settings.beeCount; i++)
            {
                var bee = Instantiate(beePrefab, transform).transform;
                bee.gameObject.SetActive(false);
                _bees.Add(bee);
                _params.Add(new Vector3(
                    settings.beeOrbitRadius * Random.Range(0.4f, 1f),
                    Random.Range(0f, Mathf.PI * 2f),
                    Random.value < 0.5f ? -1f : 1f));
            }
        }

        private void Update()
        {
            var environment = EnvironmentManager.Instance;
            var settings = environment.Settings;
            bool active = garden.IsBlooming && environment.PollutionLevel == 0;
            Vector3 center = garden.transform.position;
            float time = Time.time;

            for (int i = 0; i < _bees.Count; i++)
            {
                var bee = _bees[i];
                if (bee.gameObject.activeSelf != active) bee.gameObject.SetActive(active);
                if (!active) continue;

                Vector3 p = _params[i];
                float angle = p.y + p.z * time * settings.beeSpeed / p.x;
                var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * p.x;
                float height = settings.beeHeight + Mathf.Sin(time * BOB_SPEED + p.y) * BOB_AMOUNT;
                var tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * p.z;
                bee.SetPositionAndRotation(center + offset + Vector3.up * height, Quaternion.LookRotation(tangent));
            }
        }
    }
}
