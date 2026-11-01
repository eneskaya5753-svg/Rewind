using System;
using EcoGame.Data;
using EcoGame.Environment;
using EcoGame.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EcoGame.Trash
{
    public class TrashSpawner : MonoBehaviour
    {
        private const int MAX_TRIES = 12;
        private const float CLEARANCE = 0.6f;

        [Serializable]
        public struct SpawnEntry
        {
            public TrashTypeData data;
            public int count;
        }

        [SerializeField] private SpawnEntry[] spawnTable;
        [SerializeField] private BoxCollider[] spawnAreas;

        private Transform _container;

        public int Remaining => _container != null ? _container.childCount : int.MaxValue;

        /// <summary>Puts a piece of trash back on the ground (bag emptied); it counts as ground trash again.</summary>
        public void Drop(TrashType type, string model, Vector3 position)
        {
            foreach (var entry in spawnTable)
            {
                if (entry.data.type != type) continue;
                var item = Instantiate(entry.data.prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), _container);
                var trash = item.GetComponent<TrashItem>();
                if (trash != null) trash.ShowModel(model);
                else VisualVariants.Show(item.transform, model);
                return;
            }
        }

        /// <summary>Random point in a random area that is not inside a building, machine or tree.</summary>
        private Vector3 PickFreePoint()
        {
            Vector3 position = transform.position;
            for (int attempt = 0; attempt < MAX_TRIES; attempt++)
            {
                var area = spawnAreas[Random.Range(0, spawnAreas.Length)];
                var local = area.center + Vector3.Scale(area.size,
                    new Vector3(Random.Range(-0.5f, 0.5f), -0.5f, Random.Range(-0.5f, 0.5f)));
                position = area.transform.TransformPoint(local);
                if (!Physics.CheckSphere(position + Vector3.up * (CLEARANCE + 0.05f), CLEARANCE,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    return position;
            }
            return position;
        }

        private void Start()
        {
            _container = new GameObject("Spawned").transform;
            _container.SetParent(transform, false);

            var settings = EnvironmentManager.Instance.Settings;
            foreach (var entry in spawnTable)
            {
                int count = settings.testMode ? Mathf.Min(entry.count, settings.testTrashPerType) : entry.count;
                for (int i = 0; i < count; i++)
                    Instantiate(entry.data.prefab, PickFreePoint(), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), _container);
            }
        }
    }
}
