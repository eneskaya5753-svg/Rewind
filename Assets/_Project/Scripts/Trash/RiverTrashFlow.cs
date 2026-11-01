using System.Collections.Generic;
using EcoGame.Building;
using EcoGame.Environment;
using UnityEngine;

namespace EcoGame.Trash
{
    /// <summary>
    /// Moves river trash along waypoints without physics. The completed net stops it.
    /// The dirtier the water, the more trash floats down the river.
    /// </summary>
    public class RiverTrashFlow : MonoBehaviour
    {
        private const int NET_ROW_SIZE = 4;

        [SerializeField] private Transform[] waypoints;
        [SerializeField] private BuildSite riverNet;
        [SerializeField] private GameObject riverTrashPrefab;

        private readonly List<RiverTrash> _items = new();
        private float[] _cumulative;
        private float _length;
        private float _netDistance = float.MaxValue;
        private float _respawnTimer;

        /// <summary>Half the base count in clean water, up to double when the water is fully polluted.</summary>
        private static int TargetCount(Data.EnvironmentSettings settings) =>
            Mathf.Max(1, Mathf.RoundToInt(settings.riverTrashCount * Mathf.Lerp(0.5f, 2f, EnvironmentManager.Instance.WaterPollution / 100f)));

        private void Start()
        {
            _cumulative = new float[waypoints.Length];
            for (int i = 1; i < waypoints.Length; i++)
                _cumulative[i] = _cumulative[i - 1] + Vector3.Distance(waypoints[i - 1].position, waypoints[i].position);
            _length = _cumulative[^1];
            if (riverNet != null) _netDistance = ProjectDistance(riverNet.transform.position);

            var settings = EnvironmentManager.Instance.Settings;
            int count = TargetCount(settings);
            for (int i = 0; i < count; i++)
            {
                var item = Instantiate(riverTrashPrefab, transform).GetComponent<RiverTrash>();
                item.Distance = Mathf.Repeat(i * (_length / count), _length);
                item.transform.position = Evaluate(item.Distance);
                _items.Add(item);
            }
        }

        private void Update()
        {
            var settings = EnvironmentManager.Instance.Settings;
            bool netReady = riverNet != null && riverNet.IsCompleted;
            float step = settings.riverTrashSpeed * Time.deltaTime;

            // Pieces still afloat keep drifting until they reach the net; nothing is "caught" in mid-river.
            // Until the net is built, new trash keeps entering at the source (collected pieces get replaced).
            int target = TargetCount(settings);
            if (!netReady && _items.Count < target)
            {
                _respawnTimer -= Time.deltaTime;
                if (_respawnTimer <= 0f)
                {
                    _respawnTimer = Mathf.Min(settings.riverTrashSpacing, _length / target) / Mathf.Max(settings.riverTrashSpeed, 0.01f);
                    var fresh = Instantiate(riverTrashPrefab, transform).GetComponent<RiverTrash>();
                    fresh.Distance = 0f;
                    fresh.transform.position = Evaluate(0f);
                    _items.Add(fresh);
                }
            }

            for (int i = _items.Count - 1; i >= 0; i--)
            {
                var item = _items[i];
                if (item == null)
                {
                    _items.RemoveAt(i);
                    continue;
                }

                if (item.IsCaught) continue;

                float previous = item.Distance;
                item.Distance += step;
                // Caught pieces pile up against the net in short rows across the river, not in a long line upstream.
                int atNet = CaughtAtNet();
                int row = atNet / NET_ROW_SIZE, column = atNet % NET_ROW_SIZE;
                float stopPoint = _netDistance - row * settings.riverTrashStackSpacing;

                if (netReady && previous <= _netDistance && item.Distance >= stopPoint)
                {
                    item.Distance = stopPoint;
                    item.Catch();
                    float across = (column - (NET_ROW_SIZE - 1) * 0.5f) * settings.riverTrashSway * 0.8f;
                    PlaceCaught(item, across);
                    continue;
                }
                else if (item.Distance >= _length)
                {
                    if (!netReady) EnvironmentManager.Instance.AddWaterPollution(settings.riverTrashReachSeaWater);
                    item.Distance = 0f;
                }

                Place(item, settings);
            }
        }

        /// <summary>Drifts side to side across the current and tumbles slowly while floating.</summary>
        private void Place(RiverTrash item, Data.EnvironmentSettings settings)
        {
            Vector3 position = Evaluate(item.Distance);
            Vector3 ahead = Evaluate(item.Distance + 0.5f) - position;
            Vector3 side = Vector3.Cross(Vector3.up, ahead.sqrMagnitude > 0.0001f ? ahead.normalized : Vector3.right);
            float t = Time.time + item.Phase;
            position += side * (Mathf.Sin(item.Distance * 0.6f + item.Phase) * settings.riverTrashSway);
            item.transform.SetPositionAndRotation(position,
                Quaternion.Euler(Mathf.Sin(t * 1.7f) * 12f, item.Phase * 3.6f + item.Distance * 25f, Mathf.Cos(t * 1.3f) * 12f));
        }

        /// <summary>Pieces lying at the net that the player has not picked up yet (picked ones are disabled).</summary>
        private int CaughtAtNet()
        {
            int count = 0;
            foreach (var item in _items)
                if (item != null && item.IsCaught && item.enabled) count++;
            return count;
        }

        private void PlaceCaught(RiverTrash item, float across)
        {
            Vector3 position = Evaluate(item.Distance);
            Vector3 ahead = Evaluate(item.Distance + 0.5f) - position;
            Vector3 side = Vector3.Cross(Vector3.up, ahead.sqrMagnitude > 0.0001f ? ahead.normalized : Vector3.right);
            item.transform.SetPositionAndRotation(position + side * across, Quaternion.Euler(0f, item.Phase * 3.6f, 0f));
        }

        private Vector3 Evaluate(float distance)
        {
            for (int i = 1; i < waypoints.Length; i++)
            {
                if (distance > _cumulative[i]) continue;
                float segment = _cumulative[i] - _cumulative[i - 1];
                float t = segment > 0f ? (distance - _cumulative[i - 1]) / segment : 0f;
                return Vector3.Lerp(waypoints[i - 1].position, waypoints[i].position, t);
            }

            return waypoints[^1].position;
        }

        private float ProjectDistance(Vector3 point)
        {
            float best = float.MaxValue, result = 0f;
            for (int i = 1; i < waypoints.Length; i++)
            {
                Vector3 a = waypoints[i - 1].position, b = waypoints[i].position;
                Vector3 ab = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f));
                float distance = Vector3.Distance(point, a + ab * t);
                if (distance >= best) continue;
                best = distance;
                result = _cumulative[i - 1] + ab.magnitude * t;
            }

            return result;
        }
    }
}
