using System;
using System.Collections.Generic;
using EcoGame.Data;
using EcoGame.Environment;
using EcoGame.Interaction;
using EcoGame.Machines;
using EcoGame.Player;
using EcoGame.Utils;
using UnityEngine;

namespace EcoGame.Building
{
    /// <summary>
    /// Shows its hologram until built and accepts material bags (partial delivery allowed).
    /// When complete it swaps the hologram for the built model and reports to its generator.
    /// </summary>
    public class BuildSite : Interactable
    {
        public static readonly List<BuildSite> Sites = new();

        [SerializeField] private BuildingData data;
        [SerializeField] private Generator linkedGenerator;
        [SerializeField] private GameObject ghostVisual;
        [SerializeField] private GameObject builtVisual;

        private readonly int[] _delivered = new int[TrashTypeExtensions.Count];

        public BuildingData Data => data;
        public bool IsCompleted { get; private set; }
        public event Action<BuildSite> OnCompleted;

        protected override void OnEnable()
        {
            base.OnEnable();
            Sites.Add(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Sites.Remove(this);
        }

        /// <summary>G hides / shows all holograms (e.g. for screenshots).</summary>
        public static bool GhostsHidden { get; private set; }
        private static int _toggleFrame = -1;

        private void Start()
        {
            builtVisual.SetActive(false);
            ghostVisual.SetActive(!GhostsHidden);
        }

        private void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.gKey.wasPressedThisFrame && _toggleFrame != Time.frameCount)
            {
                _toggleFrame = Time.frameCount; // every site sees the press; flip only once
                GhostsHidden = !GhostsHidden;
            }

            bool showGhost = !IsCompleted && !GhostsHidden;
            if (ghostVisual.activeSelf != showGhost) ghostVisual.SetActive(showGhost);
        }

        public int GetDelivered(TrashType type) => _delivered[(int)type];

        public int GetRequired(TrashType type)
        {
            int total = 0;
            foreach (var requirement in data.requirements)
                if (requirement.type == type) total += requirement.amount;
            return EnvironmentManager.Instance.Settings.testMode ? Mathf.Min(total, 1) : total;
        }

        public override string Title => data.displayName;

        public override float Progress
        {
            get
            {
                int required = 0, delivered = 0;
                foreach (var type in TrashTypeExtensions.All)
                {
                    required += GetRequired(type);
                    delivered += GetDelivered(type);
                }
                return IsCompleted || required == 0 ? 1f : delivered / (float)required;
            }
        }

        public override string GetStatus()
        {
            if (IsCompleted) return "Tamamlandı!";
            var builder = new System.Text.StringBuilder();
            foreach (var type in TrashTypeExtensions.All)
            {
                int required = GetRequired(type);
                if (required == 0) continue;
                builder.Append("<color=").Append(type.TextColorHex()).Append('>').Append(type.DisplayName()).Append("</color> ")
                    .Append(GetDelivered(type)).Append('/').Append(required).Append("    ");
            }
            builder.Append("\nGerekli torbaları getir ve yerleştir.");
            return builder.ToString();
        }

        public override string GetPrompt(PlayerInteractor interactor) => "Torbaları yerleştir";

        public override bool CanInteract(PlayerInteractor interactor)
        {
            if (IsCompleted) return false;
            foreach (var type in TrashTypeExtensions.All)
                if (GetDelivered(type) < GetRequired(type) && interactor.Inventory.GetRaw(type) > 0) return true;
            return false;
        }

        public override void Interact(PlayerInteractor interactor)
        {
            bool done = true;
            foreach (var type in TrashTypeExtensions.All)
            {
                int need = GetRequired(type) - GetDelivered(type);
                if (need <= 0) continue;
                int taken = interactor.Inventory.TakeRaw(type, need);
                _delivered[(int)type] += taken;
                if (taken > 0) Notifier.Feed($"<color={type.TextColorHex()}>-{taken} {type.DisplayName()} torbası</color>");
                if (GetDelivered(type) < GetRequired(type)) done = false;
            }

            if (done) Complete();
        }

        private void Complete()
        {
            IsCompleted = true;
            ghostVisual.SetActive(false);
            builtVisual.SetActive(true);
            if (linkedGenerator != null) linkedGenerator.NotifySiteCompleted();
            EnvironmentManager.Instance.ApplyDrop(data.pollutionDrop, data.temperatureDrop);
            Notifier.Show($"{data.displayName} tamamlandı!");
            Audio.AudioManager.Play(Audio.Sfx.Build);
            OnCompleted?.Invoke(this);
        }
    }
}
