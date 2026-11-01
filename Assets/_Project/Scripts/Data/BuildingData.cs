using System;
using UnityEngine;

namespace EcoGame.Data
{
    [Serializable]
    public struct ResourceAmount
    {
        public TrashType type;
        public int amount;
    }

    [CreateAssetMenu(menuName = "EcoGame/Building")]
    public class BuildingData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;
        public ResourceAmount[] requirements;
        public float pollutionDrop;
        public float temperatureDrop;
        public GameObject ghostPrefab;
        public GameObject builtPrefab;
    }
}
