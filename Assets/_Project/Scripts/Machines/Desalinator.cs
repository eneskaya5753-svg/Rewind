using EcoGame.Interaction;
using EcoGame.Player;
using EcoGame.Utils;

namespace EcoGame.Machines
{
    public class Desalinator : Interactable
    {
        public override string Title => "Su Arıtma Cihazı";

        public override string GetStatus() => "Deniz suyunu arıtır. Su kabını burada doldur.";

        public override string GetPrompt(PlayerInteractor interactor) => "Su doldur";

        public override Audio.Sfx? GetHoldSound(PlayerInteractor interactor) => Audio.Sfx.FillWater;

        public override bool CanInteract(PlayerInteractor interactor) =>
            interactor.Inventory.Water < interactor.Settings.waterCapacity;

        public override void Interact(PlayerInteractor interactor)
        {
            if (!interactor.Inventory.FillWater()) return;
            Notifier.Feed($"Su kabı doldu ({interactor.Inventory.Water}/{interactor.Settings.waterCapacity})");
        }
    }
}
