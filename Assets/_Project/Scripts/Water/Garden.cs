using EcoGame.Data;
using EcoGame.Environment;
using EcoGame.Player;
using EcoGame.Utils;
using UnityEngine;

namespace EcoGame.Water
{
    /// <summary>Needs organic bags (fertilizer) from the recycling machine and water; while blooming it slowly cools the planet.</summary>
    public class Garden : Waterable
    {
        [SerializeField] private GameObject flowers;

        private int _fertilizer;
        private bool _bloomApplied;

        public int Fertilizer => _fertilizer;
        private int FertilizerNeeded => Settings.testMode ? 1 : Settings.gardenFertilizerNeeded;
        public bool IsFertilized => _fertilizer >= FertilizerNeeded;
        public bool IsBlooming => IsFertilized && !IsDry;

        public override bool IsPolluting => IsBlooming;
        public override float PollutionRate => 0f;
        public override float TemperatureRate => Settings.gardenTemperatureRate;

        private bool WantsFertilizer(PlayerInventory inventory) =>
            !IsFertilized && inventory.GetRaw(TrashType.Organic) > 0;

        public override string Title => "Bahçe";

        public override string GetStatus()
        {
            string fertilizer = IsFertilized
                ? "<color=#3a8a3a>Gübrelendi.</color>"
                : $"Gübre {_fertilizer}/{FertilizerNeeded} (organik torba gerekli)";
            string bloom = IsBlooming ? "\n<color=#3a8a3a>Çiçek açtı, arılar geliyor!</color>" : "";
            return $"{fertilizer}\n{WaterStatus()}{bloom}";
        }

        public override string GetPrompt(PlayerInteractor interactor) =>
            WantsFertilizer(interactor.Inventory) ? "Gübre torbası koy" : base.GetPrompt(interactor);

        public override Audio.Sfx? GetHoldSound(PlayerInteractor interactor) =>
            WantsFertilizer(interactor.Inventory) ? null : base.GetHoldSound(interactor);

        public override bool CanInteract(PlayerInteractor interactor) =>
            WantsFertilizer(interactor.Inventory) || base.CanInteract(interactor);

        public override void Interact(PlayerInteractor interactor)
        {
            if (!WantsFertilizer(interactor.Inventory))
            {
                base.Interact(interactor);
                return;
            }

            int need = FertilizerNeeded - _fertilizer;
            int taken = interactor.Inventory.TakeRaw(TrashType.Organic, need);
            _fertilizer += taken;
            Notifier.Feed($"<color={TrashType.Organic.TextColorHex()}>-{taken} Organik torbası</color>");
            if (IsFertilized) Notifier.Show("Bahçe gübrelendi! Sulamayı unutma.");
        }

        protected override void Update()
        {
            base.Update();
            if (flowers != null && flowers.activeSelf != IsBlooming) flowers.SetActive(IsBlooming);
            if (!IsBlooming || _bloomApplied) return;

            _bloomApplied = true;
            EnvironmentManager.Instance.ApplyDrop(0f, Settings.gardenTemperatureDrop);
            Notifier.Show("Bahçe çiçek açtı!");
        }
    }
}
