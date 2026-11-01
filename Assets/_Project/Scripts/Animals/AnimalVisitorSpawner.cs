using System.Collections.Generic;
using EcoGame.Environment;
using EcoGame.Water;
using UnityEngine;

namespace EcoGame.Animals
{
    /// <summary>Sends visitors to a watered trough; fewer as pollution rises.</summary>
    public class AnimalVisitorSpawner : MonoBehaviour
    {
        private const float DRINK_RING = 1.3f;

        [SerializeField] private GameObject[] visitorPrefabs;
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Waterable trough;
        [Tooltip("Route from the spawn point to the trough, e.g. both ends of the bridge.")]
        [SerializeField] private Transform[] waypoints;

        private readonly List<AnimalVisitor> _active = new();
        private readonly List<Vector3> _route = new();
        private float _timer;

        private void Update()
        {
            _active.RemoveAll(visitor => visitor == null);

            var environment = EnvironmentManager.Instance;
            var settings = environment.Settings;
            int level = Mathf.Min(environment.PollutionLevel, settings.visitorsByLevel.Length - 1);
            int allowed = trough.IsDry ? 0 : settings.visitorsByLevel[level];

            _timer -= Time.deltaTime;
            if (_active.Count >= allowed || _timer > 0f) return;

            _timer = settings.visitorSpawnInterval;
            var prefab = visitorPrefabs[Random.Range(0, visitorPrefabs.Length)];
            var visitor = Instantiate(prefab, transform).GetComponent<AnimalVisitor>();
            Vector2 ring = Random.insideUnitCircle.normalized * DRINK_RING;
            Vector3 drinkPoint = trough.transform.position + new Vector3(ring.x, 0f, ring.y);
            _route.Clear();
            foreach (var point in waypoints)
                if (point != null) _route.Add(point.position);
            visitor.Init(spawnPoint.position, _route, drinkPoint);
            _active.Add(visitor);
        }
    }
}
