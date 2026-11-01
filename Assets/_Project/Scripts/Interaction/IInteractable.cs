using EcoGame.Player;
using UnityEngine;

namespace EcoGame.Interaction
{
    public interface IInteractable
    {
        Vector3 Position { get; }
        string GetPrompt(PlayerInteractor interactor);
        bool CanInteract(PlayerInteractor interactor);
        void Interact(PlayerInteractor interactor);
    }
}
