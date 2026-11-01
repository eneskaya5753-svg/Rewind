using System;
using System.Collections.Generic;
using EcoGame.Building;
using EcoGame.Environment;
using EcoGame.Trash;
using EcoGame.Utils;
using EcoGame.Water;
using UnityEngine;

namespace EcoGame.Core
{
    /// <summary>
    /// Once every building is up, nature starts healing on its own. The game is won when air pollution,
    /// temperature and water pollution have all dropped to <see cref="winThreshold"/> or below.
    /// </summary>
    public class ObjectiveManager : MonoBehaviour
    {
        [SerializeField] private List<BuildSite> buildSites = new();
        [SerializeField] private Garden garden;
        [SerializeField] private TrashSpawner trashSpawner;
        [SerializeField] private EnvironmentManager environmentManager;
        [Tooltip("All three metrics must be at or below this (%) to win.")]
        [SerializeField] private float winThreshold = 15f;

        private bool _finished;

        public bool RecoveryStarted { get; private set; }
        public int BuildingCount => buildSites.Count;
        public bool GardenDone => garden != null && garden.IsFertilized;
        public int TrashRemaining => trashSpawner != null ? trashSpawner.Remaining : 0;

        public int CompletedBuildings
        {
            get
            {
                int count = 0;
                foreach (var site in buildSites)
                    if (site != null && site.IsCompleted) count++;
                return count;
            }
        }

        public event Action OnRecoveryStarted;
        public event Action OnRecoveryFinished;

        private void Update()
        {
            if (_finished) return;

            if (!RecoveryStarted)
            {
                if (CompletedBuildings < BuildingCount) return;
                RecoveryStarted = true;
                environmentManager.StartRecovery();
                Notifier.Show("Tüm yapılar tamam! Doğa iyileşiyor...");
                OnRecoveryStarted?.Invoke();
            }
            else if (environmentManager.Pollution <= winThreshold
                     && environmentManager.Temperature <= winThreshold
                     && environmentManager.WaterPollution <= winThreshold)
            {
                _finished = true;
                OnRecoveryFinished?.Invoke();
            }
        }
    }
}
