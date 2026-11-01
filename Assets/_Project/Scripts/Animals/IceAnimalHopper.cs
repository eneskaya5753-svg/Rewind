using EcoGame.Environment;
using EcoGame.Utils;
using UnityEngine;

namespace EcoGame.Animals
{
    /// <summary>Hops randomly inside the current ice radius. At the top pollution level it slows down or lies down.</summary>
    public class IceAnimalHopper : MonoBehaviour
    {
        private const float ARRIVE_DISTANCE = 0.2f;
        private const float WANDER_FACTOR = 0.8f;

        [SerializeField] private IceFloeController ice;

        private Vector3 _target;
        private float _phase;
        private bool _sickDecided;
        private bool _lying;

        private void Start()
        {
            _phase = Random.value * 10f;
            PickTarget();
            transform.position = _target;
        }

        private void Update()
        {
            var environment = EnvironmentManager.Instance;
            var settings = environment.Settings;
            bool sick = environment.PollutionLevel >= settings.thresholds.Length;

            if (sick && !_sickDecided)
            {
                _sickDecided = true;
                _lying = Random.value < settings.lieDownChance;
            }
            else if (!sick)
            {
                _sickDecided = false;
                _lying = false;
            }

            Vector3 center = ice.Center;
            Vector3 position = transform.position;
            Vector3 offset = position - center;
            offset.y = 0f;
            if (offset.magnitude > ice.Radius) position = center + offset.normalized * ice.Radius;

            float yaw = transform.eulerAngles.y;
            if (_lying)
            {
                transform.SetPositionAndRotation(new Vector3(position.x, center.y, position.z), Quaternion.Euler(0f, yaw, 90f));
                return;
            }

            if (MathUtil.FlatDistance(position, _target) < ARRIVE_DISTANCE
                || MathUtil.FlatDistance(_target, center) > ice.Radius)
                PickTarget();

            float speed = settings.hopSpeed * (sick ? settings.sickSpeedMultiplier : 1f);
            Vector3 flatTarget = new(_target.x, position.y, _target.z);
            position = Vector3.MoveTowards(position, flatTarget, speed * Time.deltaTime);
            _phase += Time.deltaTime * settings.hopFrequency * (sick ? settings.sickSpeedMultiplier : 1f);
            position.y = center.y + Mathf.Abs(Mathf.Sin(_phase)) * settings.hopHeight;

            Vector3 direction = flatTarget - transform.position;
            direction.y = 0f;
            Quaternion rotation = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.Euler(0f, yaw, 0f);
            transform.SetPositionAndRotation(position, rotation);
        }

        private void PickTarget()
        {
            Vector3 center = ice.Center;
            Vector2 offset = Random.insideUnitCircle * ice.Radius * WANDER_FACTOR;
            _target = new Vector3(center.x + offset.x, center.y, center.z + offset.y);
        }
    }
}
