using System.Collections.Generic;
using EcoGame.Data;
using UnityEngine;

namespace EcoGame.Player
{
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private PlayerSettings settings;

        private readonly int[] _bag = new int[TrashTypeExtensions.Count];
        private readonly int[] _raw = new int[TrashTypeExtensions.Count];
        private readonly List<(TrashType type, string model)> _bagItems = new();

        public PlayerSettings Settings => settings;
        public int Water { get; private set; }

        /// <summary>Trash plus material bags; bags take as much room as the material inside.</summary>
        public int BagTotal
        {
            get
            {
                int total = 0;
                foreach (var count in _bag) total += count;
                foreach (var count in _raw) total += count;
                return total;
            }
        }

        public int TrashTotal
        {
            get
            {
                int total = 0;
                foreach (var count in _bag) total += count;
                return total;
            }
        }

        public bool BagFull => BagTotal >= settings.bagCapacity;
        public int FreeSpace => Mathf.Max(0, settings.bagCapacity - BagTotal);

        public int GetBag(TrashType type) => _bag[(int)type];
        public int GetRaw(TrashType type) => _raw[(int)type];

        /// <summary>Individual trash pieces in the bag (model key, e.g. "cup-coffee"), shown by name in the bag window.</summary>
        public IReadOnlyList<(TrashType type, string model)> BagItems => _bagItems;

        public bool TryAddToBag(TrashType type, string model = null)
        {
            if (BagFull) return false;
            _bag[(int)type]++;
            _bagItems.Add((type, model));
            return true;
        }

        public int TakeFromBag(TrashType type, int max)
        {
            int taken = Mathf.Clamp(max, 0, _bag[(int)type]);
            _bag[(int)type] -= taken;
            for (int i = _bagItems.Count - 1, left = taken; i >= 0 && left > 0; i--)
            {
                if (_bagItems[i].type != type) continue;
                _bagItems.RemoveAt(i);
                left--;
            }
            return taken;
        }

        public void ReturnToBag(TrashType type, int amount)
        {
            _bag[(int)type] += amount;
            for (int i = 0; i < amount; i++) _bagItems.Add((type, null));
        }

        /// <summary>Takes one specific trash piece out of the bag.</summary>
        public (TrashType type, string model) RemoveBagItem(int index)
        {
            var item = _bagItems[index];
            _bagItems.RemoveAt(index);
            _bag[(int)item.type]--;
            return item;
        }

        /// <summary>Empties all trash (not material bags) out of the bag.</summary>
        public List<(TrashType type, string model)> TakeAllTrash()
        {
            var items = new List<(TrashType type, string model)>(_bagItems);
            _bagItems.Clear();
            for (int i = 0; i < _bag.Length; i++) _bag[i] = 0;
            return items;
        }

        public void AddRaw(TrashType type, int amount) => _raw[(int)type] += amount;

        public int TakeRaw(TrashType type, int max)
        {
            int taken = Mathf.Clamp(max, 0, _raw[(int)type]);
            _raw[(int)type] -= taken;
            return taken;
        }

        public bool FillWater()
        {
            if (Water >= settings.waterCapacity) return false;
            Water = settings.waterCapacity;
            return true;
        }

        public bool UseWater()
        {
            if (Water <= 0) return false;
            Water--;
            return true;
        }
    }
}
