using EcoGame.Data;
using EcoGame.Environment;
using EcoGame.Interaction;
using EcoGame.Player;
using EcoGame.Utils;
using UnityEngine;

namespace EcoGame.Trash
{
    /// <summary>Floating trash moved by RiverTrashFlow. Collectable only once caught by the net.</summary>
    public class RiverTrash : Interactable, IPollutionSource
    {
        public float Distance { get; set; }
        public float Phase { get; private set; }
        public bool IsCaught { get; private set; }

        public bool IsPolluting => !IsCaught;
        public float PollutionRate => 0f;
        public float TemperatureRate => 0f;
        public float WaterPollutionRate => EnvironmentManager.Instance.Settings.riverTrashWaterRate;

        private void Awake()
        {
            VisualVariants.PickRandom(transform);
            Phase = Random.Range(0f, 100f);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnvironmentManager.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EnvironmentManager.Unregister(this);
        }

        public void Catch() => IsCaught = true;

        public override string Title => "Nehir çöpü (Plastik)";

        public override string GetStatus() => IsCaught
            ? "Fileye takıldı. Topla!"
            : "Akıntıyla denize gidiyor ve suyu kirletiyor. Yakala ya da nehir filesi kur!";

        public override string GetPrompt(PlayerInteractor interactor) => "Topla";

        public override bool CanInteract(PlayerInteractor interactor) => true;

        public override void Interact(PlayerInteractor interactor)
        {
            string model = VisualVariants.ActiveModel(transform);
            if (!interactor.Inventory.TryAddToBag(TrashType.Plastic, model))
            {
                Notifier.Show("Çanta dolu!");
                return;
            }

            Notifier.Feed($"+1 {TrashNames.Display(model, TrashType.Plastic)}");
            Audio.AudioManager.Play(Audio.Sfx.Loot);
            Catch(); // stops the river flow from moving it while it flies to the player
            foreach (var hitCollider in GetComponentsInChildren<Collider>()) hitCollider.enabled = false;
            enabled = false;
            ItemMover.FlyTo(gameObject, interactor.transform, EnvironmentManager.Instance.Settings.itemFlySpeed);
        }
    }
}
