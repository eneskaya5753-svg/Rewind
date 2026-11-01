using System.Collections.Generic;
using EcoGame.Environment;
using EcoGame.Utils;
using UnityEngine;

namespace EcoGame.Animals
{
    /// <summary>
    /// Walks (or flies) from the forest along a route (e.g. over the bridge) to the trough,
    /// drinks, walks the same route back and despawns.
    /// </summary>
    public class AnimalVisitor : MonoBehaviour
    {
        private const float ARRIVE_DISTANCE = 0.3f;

        private enum Phase { ToTarget, Drinking, Returning }

        [SerializeField] private bool flies;

        private readonly List<Vector3> _route = new();
        private int _index;
        private Phase _phase;
        private float _timer;
        private Vector3 _segmentStart;

        /// <summary>Route goes home -> waypoints -> target.</summary>
        public void Init(Vector3 home, IReadOnlyList<Vector3> waypoints, Vector3 target)
        {
            _route.Clear();
            _route.Add(home);
            _route.AddRange(waypoints);
            _route.Add(target);
            _index = 1;
            _segmentStart = home;
            transform.position = home;
        }

        private void Update()
        {
            var environment = EnvironmentManager.Instance;
            var settings = environment.Settings;
            float yaw = transform.eulerAngles.y;

            if (environment.PollutionLevel >= settings.thresholds.Length)
            {
                transform.rotation = Quaternion.Euler(0f, yaw, 90f);
                return;
            }

            switch (_phase)
            {
                case Phase.ToTarget:
                    if (!MoveTo(_route[_index], settings)) break;
                    _segmentStart = _route[_index];
                    if (_index < _route.Count - 1) _index++;
                    else
                    {
                        _phase = Phase.Drinking;
                        _timer = settings.visitorDrinkTime;
                    }
                    break;
                case Phase.Drinking:
                    transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    _timer -= Time.deltaTime;
                    if (_timer > 0f) break;
                    _phase = Phase.Returning;
                    _index = _route.Count - 2;
                    break;
                case Phase.Returning:
                    if (!MoveTo(_route[_index], settings)) break;
                    _segmentStart = _route[_index];
                    if (_index > 0) _index--;
                    else Destroy(gameObject);
                    break;
            }
        }

        private bool MoveTo(Vector3 to, Data.EnvironmentSettings settings)
        {
            Vector3 position = transform.position;
            Vector3 flat = Vector3.MoveTowards(new Vector3(position.x, to.y, position.z), to, settings.visitorSpeed * Time.deltaTime);
            float total = Mathf.Max(MathUtil.FlatDistance(_segmentStart, to), 0.01f);
            float progress = 1f - MathUtil.FlatDistance(flat, to) / total;
            flat.y = to.y + (flies ? Mathf.Sin(progress * Mathf.PI) * settings.visitorFlyHeight : 0f);

            Vector3 direction = to - _segmentStart;
            direction.y = 0f;
            transform.SetPositionAndRotation(flat,
                direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : transform.rotation);
            return MathUtil.FlatDistance(flat, to) < ARRIVE_DISTANCE;
        }
    }
}
