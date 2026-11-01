using System.Collections.Generic;
using EcoGame.Player;
using UnityEngine;

namespace EcoGame.Interaction
{
    /// <summary>Base for proximity interaction. Instances register themselves; PlayerInteractor picks the nearest.</summary>
    public abstract class Interactable : MonoBehaviour, IInteractable, IInspectable
    {
        public static readonly List<Interactable> All = new();

        public virtual Vector3 Position => transform.position;
        public abstract string Title { get; }
        public virtual string GetStatus() => "";
        public virtual float Progress => -1f;

        /// <summary>When set, E must be held while this sound plays; the action happens once the hold bar fills.</summary>
        public virtual Audio.Sfx? GetHoldSound(PlayerInteractor interactor) => null;

        /// <summary>False hides the info panel for this target (only the E prompt shows).</summary>
        public virtual bool ShowsInfo => true;

        protected virtual void OnEnable()
        {
            All.Add(this);
            Inspectables.All.Add(this);
        }

        protected virtual void OnDisable()
        {
            All.Remove(this);
            Inspectables.All.Remove(this);
        }

        public abstract string GetPrompt(PlayerInteractor interactor);
        public abstract bool CanInteract(PlayerInteractor interactor);
        public abstract void Interact(PlayerInteractor interactor);
    }
}
