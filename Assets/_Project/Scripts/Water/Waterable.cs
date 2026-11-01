using EcoGame.Data;
using EcoGame.Environment;
using EcoGame.Interaction;
using EcoGame.Player;
using EcoGame.Utils;
using UnityEngine;

namespace EcoGame.Water
{
    /// <summary>Thirst 0-100 that drains faster when hot. No permanent death: watering restores it.</summary>
    public class Waterable : Interactable, IPollutionSource
    {
        private const float FULL = 100f;

        [SerializeField] private string displayName = "Ağaç";
        [SerializeField] private bool pollutesWhenDry = true;

        public float Thirst { get; protected set; } = FULL;
        public bool IsDry => Thirst <= 0f;
        protected EnvironmentSettings Settings => EnvironmentManager.Instance.Settings;
        public bool NeedsWater => Thirst < Settings.waterableUseThreshold;

        public virtual bool IsPolluting => pollutesWhenDry && IsDry;
        public virtual float PollutionRate => Settings.dryTreePollutionRate;
        public virtual float TemperatureRate => 0f;
        public float WaterPollutionRate => 0f;

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

        protected virtual void Start()
        {
            Vector2 range = Settings.waterableStartThirst;
            Thirst = Random.Range(range.x, range.y);
        }

        protected virtual void Update()
        {
            var environment = EnvironmentManager.Instance;
            if (environment.IsRecovering)
            {
                Thirst = FULL;
                return;
            }

            float rate = Settings.thirstDecayPerSecond * (1f + environment.Temperature * Settings.thirstTemperatureMultiplier);
            Thirst = Mathf.Max(0f, Thirst - rate * Time.deltaTime);
        }

        public override string Title => displayName;
        public override float Progress => Thirst / FULL;

        public override string GetStatus() => WaterStatus();

        protected string WaterStatus()
        {
            string state = IsDry ? "<color=#e05040>Kurumuş! Hemen sula.</color>"
                : NeedsWater ? "Susuz kalıyor, sulayabilirsin."
                : "<color=#3a8a3a>Sulanmış.</color>";
            return $"Su: %{Mathf.RoundToInt(Thirst)}   {state}";
        }

        public override string GetPrompt(PlayerInteractor interactor) => "Sula";

        public override bool CanInteract(PlayerInteractor interactor) => NeedsWater;

        public override Audio.Sfx? GetHoldSound(PlayerInteractor interactor) =>
            interactor.Inventory.Water > 0 ? Audio.Sfx.Water : null;

        public override void Interact(PlayerInteractor interactor)
        {
            if (!interactor.Inventory.UseWater())
            {
                Notifier.Show("Su kabın boş! Arıtma cihazından doldur.");
                return;
            }

            Thirst = FULL;
            EnvironmentManager.Instance.ApplyDrop(0f, Settings.waterTemperatureDrop);
            Notifier.Feed($"{displayName} sulandı (su kabı {interactor.Inventory.Water}/{interactor.Settings.waterCapacity})");
        }
    }
}
