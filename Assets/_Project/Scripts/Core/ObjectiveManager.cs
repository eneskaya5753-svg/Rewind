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
    /// <summary>Watches win conditions, then runs the recovery phase.</summary>
    public class ObjectiveManager : MonoBehaviour
    {
        [SerializeField] private List<BuildSite> buildSites = new();
        [SerializeField] private Garden garden;
        [SerializeField] private TrashSpawner trashSpawner;
        [SerializeField] private EnvironmentManager environmentManager;

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
                if (CompletedBuildings < BuildingCount || !GardenDone || TrashRemaining > 0) return;
                RecoveryStarted = true;
                environmentManager.StartRecovery();
                Notifier.Show("Tüm görevler tamam! Doğa iyileşiyor...");
                OnRecoveryStarted?.Invoke();
            }
            else if (environmentManager.IsFullyRecovered)
            {
                _finished = true;
                OnRecoveryFinished?.Invoke();
            }
        }
    }
}
