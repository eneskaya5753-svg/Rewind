using EcoGame.Environment;
using UnityEngine;

namespace EcoGame.Animals
{
    /// <summary>
    /// Fish that leap out of the river now and then. Only live in clean water: the dirtier the river,
    /// the fewer fish jump, and none at all above <see cref="noFishPollution"/>.
    /// </summary>
    public class RiverFish : MonoBehaviour
    {
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private GameObject[] fishModels;
        [SerializeField] private int maxFish = 8;
        [Tooltip("Water pollution where fish start to disappear, and where they are all gone.")]
        [SerializeField] private float allFishPollution = 15f;
        [SerializeField] private float noFishPollution = 40f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float jumpLength = 2.2f;
        [SerializeField] private float jumpTime = 0.9f;
        [SerializeField] private Vector2 waitRange = new(1.5f, 5f);
        [SerializeField] private float sideSpread = 1.8f;

        private Fish[] _fish;
        private float[] _cumulative;
        private float _length;

        private class Fish
        {
            public Transform Body;
            public Vector3 Start, End;
            public float Timer; // > 0 waiting under water, <= 0 jumping
            public float JumpT;
        }

        private void Start()
        {
            _cumulative = new float[waypoints.Length];
            for (int i = 1; i < waypoints.Length; i++)
                _cumulative[i] = _cumulative[i - 1] + Vector3.Distance(waypoints[i - 1].position, waypoints[i].position);
            _length = _cumulative[^1];

            _fish = new Fish[maxFish];
            for (int i = 0; i < maxFish; i++)
            {
                var body = Instantiate(fishModels[i % fishModels.Length], transform).transform;
                body.gameObject.SetActive(false);
                _fish[i] = new Fish { Body = body, Timer = Random.Range(0f, waitRange.y) };
            }
        }

        private void Update()
        {
            float pollution = EnvironmentManager.Instance.WaterPollution;
            float clean = 1f - Mathf.InverseLerp(allFishPollution, noFishPollution, pollution);
            int allowed = Mathf.RoundToInt(maxFish * clean);

            for (int i = 0; i < _fish.Length; i++)
            {
                var fish = _fish[i];
                if (fish.Timer > 0f)
                {
                    fish.Timer -= Time.deltaTime;
                    if (fish.Timer <= 0f)
                    {
                        if (i >= allowed) { fish.Timer = waitRange.x; continue; } // too dirty for this one
                        BeginJump(fish);
                    }
                    continue;
                }

                fish.JumpT += Time.deltaTime / jumpTime;
                if (fish.JumpT >= 1f)
                {
                    fish.Body.gameObject.SetActive(false);
                    fish.Timer = Random.Range(waitRange.x, waitRange.y);
                    continue;
                }

                // Arc from under the surface, over and back in; nose follows the path.
                float t = fish.JumpT;
                Vector3 position = Vector3.Lerp(fish.Start, fish.End, t) + Vector3.up * (4f * jumpHeight * t * (1f - t));
                Vector3 velocity = (fish.End - fish.Start) + Vector3.up * (4f * jumpHeight * (1f - 2f * t));
                fish.Body.SetPositionAndRotation(position, Quaternion.LookRotation(velocity));
            }
        }

        private void BeginJump(Fish fish)
        {
            float distance = Random.Range(0f, _length);
            Vector3 center = Evaluate(distance);
            Vector3 along = (Evaluate(distance + 0.5f) - center).normalized;
            if (along.sqrMagnitude < 0.01f) along = Vector3.right;
            Vector3 side = Vector3.Cross(Vector3.up, along);
            center += side * Random.Range(-sideSpread, sideSpread) + Vector3.down * 0.3f;
            // Jump either way along the river, slightly across it.
            Vector3 direction = Quaternion.Euler(0f, Random.Range(-30f, 30f), 0f) * (Random.value < 0.5f ? along : -along);
            fish.Start = center - direction * (jumpLength * 0.5f);
            fish.End = center + direction * (jumpLength * 0.5f);
            fish.JumpT = 0f;
            fish.Body.gameObject.SetActive(true);
        }

        private Vector3 Evaluate(float distance)
        {
            distance = Mathf.Clamp(distance, 0f, _length);
            for (int i = 1; i < waypoints.Length; i++)
            {
                if (distance > _cumulative[i]) continue;
                float segment = _cumulative[i] - _cumulative[i - 1];
                float t = segment > 0f ? (distance - _cumulative[i - 1]) / segment : 0f;
                return Vector3.Lerp(waypoints[i - 1].position, waypoints[i].position, t);
            }
            return waypoints[^1].position;
        }
    }
}
