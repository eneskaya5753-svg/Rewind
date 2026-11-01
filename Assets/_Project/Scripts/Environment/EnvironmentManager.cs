using System;
using System.Collections.Generic;
using EcoGame.Data;
using UnityEngine;

namespace EcoGame.Environment
{
    /// <summary>Owns Pollution / Temperature / WaterPollution (0-100) and sums registered sources.</summary>
    public class EnvironmentManager : MonoBehaviour
    {
        private const float MAX_VALUE = 100f;

        private static readonly List<IPollutionSource> Sources = new();

        public static EnvironmentManager Instance { get; private set; }

        [SerializeField] private EnvironmentSettings settings;

        private bool _limitFired;
        private float _pendingPollutionDrop;
        private float _pendingTemperatureDrop;

        public EnvironmentSettings Settings => settings;
        public float Pollution { get; private set; }
        public float Temperature { get; private set; }
        public float WaterPollution { get; private set; }
        public bool IsRecovering { get; private set; }
        public int PollutionLevel { get; private set; }
        public int TemperatureLevel { get; private set; }

        public bool IsFullyRecovered => IsRecovering
            && Pollution <= settings.recoveredValue
            && Temperature <= settings.recoveredValue
            && WaterPollution <= settings.recoveredValue;

        public event Action<int> OnPollutionLevelChanged;
        public event Action<int> OnTemperatureLevelChanged;
        public event Action OnLimitReached;

        public static void Register(IPollutionSource source)
        {
            if (!Sources.Contains(source)) Sources.Add(source);
        }

        public static void Unregister(IPollutionSource source) => Sources.Remove(source);

        private void Awake()
        {
            Instance = this;
            Pollution = settings.startPollution;
            Temperature = settings.startTemperature;
            WaterPollution = settings.startWaterPollution;
            PollutionLevel = LevelOf(Pollution);
            TemperatureLevel = LevelOf(Temperature);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // Drops from buildings and watering ease in over time instead of snapping.
            float dropStep = settings.dropSpeed * dt;
            float pollutionDrop = Mathf.Min(_pendingPollutionDrop, dropStep);
            float temperatureDrop = Mathf.Min(_pendingTemperatureDrop, dropStep);
            _pendingPollutionDrop -= pollutionDrop;
            _pendingTemperatureDrop -= temperatureDrop;
            Pollution = Clamp(Pollution - pollutionDrop);
            Temperature = Clamp(Temperature - temperatureDrop);

            if (IsRecovering)
            {
                float step = settings.recoveryRate * dt;
                Pollution = Mathf.MoveTowards(Pollution, 0f, step);
                Temperature = Mathf.MoveTowards(Temperature, 0f, step);
                WaterPollution = Mathf.MoveTowards(WaterPollution, 0f, step);
            }
            else
            {
                float pollution = 0f, temperature = 0f, water = 0f;
                foreach (var source in Sources)
                {
                    if (!source.IsPolluting) continue;
                    pollution += source.PollutionRate;
                    temperature += source.TemperatureRate;
                    water += source.WaterPollutionRate;
                }

                Pollution = Clamp(Pollution + pollution * dt);
                Temperature = Clamp(Temperature + temperature * dt);
                WaterPollution = water > 0f
                    ? Clamp(WaterPollution + water * dt)
                    : Mathf.MoveTowards(WaterPollution, 0f, settings.waterRecoveryRate * dt);

                if (!_limitFired && (Pollution >= settings.loseValue || Temperature >= settings.loseValue))
                {
                    _limitFired = true;
                    OnLimitReached?.Invoke();
                }
            }

            UpdateLevels();
        }

        /// <summary>Queues a gradual decrease; it is applied at dropSpeed per second.</summary>
        public void ApplyDrop(float pollution, float temperature)
        {
            _pendingPollutionDrop += pollution;
            _pendingTemperatureDrop += temperature;
        }

        public void AddWaterPollution(float amount) => WaterPollution = Clamp(WaterPollution + amount);

        public void StartRecovery() => IsRecovering = true;

        private int LevelOf(float value)
        {
            int level = 0;
            foreach (var threshold in settings.thresholds)
                if (value >= threshold) level++;
            return level;
        }

        private void UpdateLevels()
        {
            int pollutionLevel = LevelOf(Pollution);
            if (pollutionLevel != PollutionLevel)
            {
                PollutionLevel = pollutionLevel;
                OnPollutionLevelChanged?.Invoke(pollutionLevel);
            }

            int temperatureLevel = LevelOf(Temperature);
            if (temperatureLevel != TemperatureLevel)
            {
                TemperatureLevel = temperatureLevel;
                OnTemperatureLevelChanged?.Invoke(temperatureLevel);
            }
        }

        private static float Clamp(float value) => Mathf.Clamp(value, 0f, MAX_VALUE);
    }
}
