using EcoGame.Interaction;
using EcoGame.Player;
using UnityEngine;

namespace EcoGame.Machines
{
    public enum RecyclerPortType { Input, Button, Output }

    /// <summary>One interaction point of the recycling station.</summary>
    public class RecyclerPort : Interactable
    {
        [SerializeField] private RecyclingMachine machine;
        [SerializeField] private RecyclerPortType port;

        public override string Title => port switch
        {
            RecyclerPortType.Input => "Giriş Bandı",
            RecyclerPortType.Button => "Çalıştırma Butonu",
            _ => "Çıkış Bandı"
        };

        public override string GetStatus() => machine.GetStatus();

        public override string GetPrompt(PlayerInteractor interactor) => port switch
        {
            RecyclerPortType.Input => "Çöpleri banda boşalt",
            RecyclerPortType.Button => "Makineyi çalıştır",
            _ => machine.OutputHasBags ? "Torbaları al" : "Çöpleri geri al"
        };

        public override bool CanInteract(PlayerInteractor interactor) => port switch
        {
            RecyclerPortType.Input => !machine.IsBusy && interactor.Inventory.TrashTotal > 0,
            RecyclerPortType.Button => !machine.IsBusy && machine.LoadedTotal > 0,
            _ => machine.OutputCount > 0 && !interactor.Inventory.BagFull
        };

        public override void Interact(PlayerInteractor interactor)
        {
            switch (port)
            {
                case RecyclerPortType.Input: machine.Load(interactor); break;
                case RecyclerPortType.Button: machine.Run(); break;
                default: machine.Collect(interactor); break;
            }
        }
    }
}
