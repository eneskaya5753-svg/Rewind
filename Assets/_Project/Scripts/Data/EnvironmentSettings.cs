using UnityEngine;

namespace EcoGame.Data
{
    [CreateAssetMenu(menuName = "EcoGame/Environment Settings")]
    public class EnvironmentSettings : ScriptableObject
    {
        [Header("Test")]
        [Tooltip("Buildings and the garden ask for 1 of each material, and only a few trash items spawn.")]
        public bool testMode = true;
        public int testTrashPerType = 2;

        [Header("Start")]
        public float startPollution = 20f;
        public float startTemperature = 20f;
        public float startWaterPollution = 10f;

        [Header("Sources (per second)")]
        public float generatorPollutionRate = 0.03f;
        public float generatorTemperatureRate = 0.015f;
        public float trashTemperatureRate = 0.001f;
        public float dryTreePollutionRate = 0.01f;
        public float riverTrashWaterRate = 0.02f;
        public float riverTrashReachSeaWater = 2f;
        public float waterRecoveryRate = 0.5f;
        [Tooltip("How fast building/watering drops are applied, in points per second.")]
        public float dropSpeed = 1.5f;

        [Header("Thresholds")]
        public float[] thresholds = { 50f, 70f, 90f };
        public float loseValue = 100f;

        [Header("Watering")]
        public float thirstDecayPerSecond = 0.4f;
        public float thirstTemperatureMultiplier = 0.02f;
        public float waterTemperatureDrop = 0.5f;
        public float waterableUseThreshold = 90f;
        public Vector2 waterableStartThirst = new(50f, 100f);

        [Header("Garden")]
        public int gardenFertilizerNeeded = 5;
        public float gardenTemperatureDrop = 5f;
        public float gardenTemperatureRate = -0.01f;

        [Header("Recycling")]
        public float itemFlySpeed = 12f;
        public float beltSpeed = 3f;
        public float itemSpawnInterval = 0.12f;
        public float machineRunTime = 1.2f;

        [Header("Recovery")]
        public float recoveryRate = 2f;
        public float recoveredValue = 0.5f;

        [Header("River")]
        public int riverTrashCount = 6;
        public float riverTrashSpeed = 1.5f;
        public float riverTrashSpacing = 8f;
        public float riverTrashStackSpacing = 0.7f;
        public float riverTrashSway = 0.8f;

        [Header("Visuals")]
        public float visualLerpSpeed = 0.5f;
        public float fogStartPollution = 30f;
        public float fogDensityClean = 0.002f;
        public float fogDensityMax = 0.035f;
        public Color fogColor = new(0.55f, 0.52f, 0.45f);
        public float iceMinScale = 0.3f;
        public Color waterCleanColor = new(0.2f, 0.55f, 0.9f, 1f);
        public Color waterDirtyColor = new(0.4f, 0.3f, 0.15f, 1f);
        public Color treeHotColor = new(1f, 0.55f, 0.1f);
        public Color treeDryColor = new(0.45f, 0.3f, 0.15f);
        public Vector2 treeHotRange = new(50f, 90f);

        [Header("Animals")]
        public int[] visitorsByLevel = { 3, 2, 1, 0 };
        public float visitorSpawnInterval = 6f;
        public float visitorSpeed = 2f;
        public float visitorDrinkTime = 4f;
        public float visitorFlyHeight = 2f;
        public float hopSpeed = 0.8f;
        public float hopHeight = 0.25f;
        public float hopFrequency = 6f;
        public float sickSpeedMultiplier = 0.3f;
        public float lieDownChance = 0.5f;
        public int beeCount = 6;
        public float beeOrbitRadius = 2.5f;
        public float beeHeight = 1f;
        public float beeSpeed = 1.5f;
    }
}
