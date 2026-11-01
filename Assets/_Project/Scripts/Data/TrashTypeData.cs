using UnityEngine;

namespace EcoGame.Data
{
    [CreateAssetMenu(menuName = "EcoGame/Trash Type")]
    public class TrashTypeData : ScriptableObject
    {
        public TrashType type;
        public string displayName;
        public Sprite icon;
        public GameObject prefab;
        public bool goesToMachine = true;
    }
}
