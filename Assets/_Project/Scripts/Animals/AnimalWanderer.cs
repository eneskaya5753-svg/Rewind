using EcoGame.Environment;
using EcoGame.Utils;
using UnityEngine;

namespace EcoGame.Animals
{
    /// <summary>
    /// Roams inside a box in the play area. Bad air slows it down; at high pollution some animals collapse,
    /// and at the top level all of them do. They get back up once the air clears.
    /// </summary>
    public class AnimalWanderer : MonoBehaviour
    {
        private const float ARRIVE_DISTANCE = 0.3f;
        private const int SLOW_LEVEL = 1;
        private const int FRAGILE_LEVEL = 2;

        [SerializeField] private Vector3 areaCenter;
        [SerializeField] private Vector3 areaSize = new(30f, 0f, 30f);
        [SerializeField] private Vector2 pauseRange = new(1f, 4f);
        [SerializeField] private float speedMultiplier = 0.6f;

        private Vector3 _target;
        private float _pause;
        private bool _fragile;

        public void SetArea(Vector3 center, Vector3 size)
        {
            areaCenter = center;
            areaSize = size;
        }

        private void Start()
        {
            _fragile = Random.value < 0.5f;
            PickTarget();
        }

        private void Update()
        {
            var environment = EnvironmentManager.Instance;
            var settings = environment.Settings;
            int level = environment.PollutionLevel;
            float yaw = transform.eulerAngles.y;

            bool dead = level >= settings.thresholds.Length || (level >= FRAGILE_LEVEL && _fragile);
            if (dead)
            {
                transform.rotation = Quaternion.Euler(0f, yaw, 90f);
                return;
            }

            if (_pause > 0f)
            {
                transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                _pause -= Time.deltaTime;
                return;
            }

            float speed = settings.visitorSpeed * speedMultiplier * (level >= SLOW_LEVEL ? settings.sickSpeedMultiplier * 2f : 1f);
            Vector3 position = Vector3.MoveTowards(transform.position, _target, speed * Time.deltaTime);
            Vector3 direction = _target - transform.position;
            direction.y = 0f;
            transform.SetPositionAndRotation(position,
                direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.Euler(0f, yaw, 0f));

            if (MathUtil.FlatDistance(position, _target) < ARRIVE_DISTANCE)
            {
                _pause = Random.Range(pauseRange.x, pauseRange.y);
                PickTarget();
            }
        }

        /// <summary>Random point in the area with a clear straight path (no fences, buildings or trees in between).</summary>
        private void PickTarget()
        {
            const int TRIES = 10;
            Vector3 from = transform.position + Vector3.up * 0.4f;
            for (int i = 0; i < TRIES; i++)
            {
                _target = areaCenter + new Vector3(
                    Random.Range(-0.5f, 0.5f) * areaSize.x, 0f, Random.Range(-0.5f, 0.5f) * areaSize.z);
                _target.y = transform.position.y;
                if (!Physics.Linecast(from, _target + Vector3.up * 0.4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    return;
            }
            _target = transform.position; // boxed in: wait and try again after the pause
        }
    }
}
