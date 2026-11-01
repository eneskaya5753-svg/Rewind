using EcoGame.Environment;
using EcoGame.Player;
using EcoGame.Utils;
using UnityEngine;

namespace EcoGame.Animals
{
    /// <summary>
    /// Pet that trots after the player and sits down nearby when it catches up.
    /// Not immortal: at the worst pollution level it dies like the other animals, and stays dead.
    /// </summary>
    public class PetFollower : MonoBehaviour
    {
        [SerializeField] private float followDistance = 1.8f;
        [SerializeField] private float catchUpDistance = 3f;
        [SerializeField] private float teleportDistance = 25f;
        [SerializeField] private float speed = 5f;

        private Transform _player;
        private bool _moving;

        public bool IsDead { get; private set; }

        private void Update()
        {
            var environment = EnvironmentManager.Instance;
            if (!IsDead && environment != null && environment.PollutionLevel >= environment.Settings.thresholds.Length)
            {
                IsDead = true;
                Notifier.Show("Kedin kirli havaya dayanamadı...");
            }
            if (IsDead)
            {
                transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 90f);
                return;
            }

            if (_player == null)
            {
                if (PlayerInteractor.Instance == null) return;
                _player = PlayerInteractor.Instance.transform;
            }

            Vector3 target = _player.position;
            float distance = MathUtil.FlatDistance(transform.position, target);
            if (distance > teleportDistance)
            {
                transform.position = target - _player.forward * followDistance;
                return;
            }

            // Start walking once the player is a bit away, stop when close again.
            if (distance > catchUpDistance) _moving = true;
            else if (distance < followDistance) _moving = false;

            Vector3 direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 10f * Time.deltaTime);
            if (!_moving) return;

            float step = speed * Mathf.Clamp(distance / catchUpDistance, 1f, 2f) * Time.deltaTime;
            Vector3 position = Vector3.MoveTowards(transform.position, target, step);
            position.y = transform.position.y; // stays on the ground when the player jumps
            transform.position = position;
        }
    }
}
