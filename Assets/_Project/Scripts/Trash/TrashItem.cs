using EcoGame.Data;
using EcoGame.Environment;
using EcoGame.Interaction;
using EcoGame.Player;
using EcoGame.Utils;
using UnityEngine;

namespace EcoGame.Trash
{
    /// <summary>Ground trash: warms the planet (methane) until collected.</summary>
    public class TrashItem : Interactable, IPollutionSource
    {
        [SerializeField] private TrashTypeData data;

        public TrashTypeData Data => data;
        public bool IsPolluting => true;
        public float PollutionRate => 0f;
        public float TemperatureRate => EnvironmentManager.Instance.Settings.trashTemperatureRate;
        public float WaterPollutionRate => 0f;

        private void Awake()
        {
            VisualVariants.PickRandom(transform);
            if (data.type != TrashType.Organic) LieDown();
        }

        /// <summary>Bottles, cans and wrappers lie tipped over on the ground instead of standing upright.</summary>
        private void LieDown()
        {
            var visual = transform.Find("Visual");
            if (visual == null) return;
            foreach (Transform model in visual)
            {
                if (!model.gameObject.activeSelf) continue;
                model.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Quaternion.Euler(90f, 0f, 0f);
                var renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) return;
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                model.position += new Vector3(transform.position.x - bounds.center.x, transform.position.y - bounds.min.y, transform.position.z - bounds.center.z);
                return;
            }
        }

        /// <summary>Called when this piece is put back on the ground with a chosen model (bag emptied).</summary>
        public void ShowModel(string model)
        {
            var visual = transform.Find("Visual");
            if (visual == null || model == null || visual.Find(model) == null) return;
            foreach (Transform child in visual) child.gameObject.SetActive(child.name == model);
            if (data.type != TrashType.Organic) LieDown();
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

        public override string Title => $"{data.displayName} çöp";

        public override string GetStatus() => data.type == TrashType.Organic
            ? "Çürüyor ve metan salıyor. Topla, makinede gübre torbasına çevir."
            : "Yerde kaldıkça dünyayı ısıtıyor. Topla!";

        public override string GetPrompt(PlayerInteractor interactor) => "Topla";

        public override bool CanInteract(PlayerInteractor interactor) => true;

        public override void Interact(PlayerInteractor interactor)
        {
            string model = VisualVariants.ActiveModel(transform);
            if (!interactor.Inventory.TryAddToBag(data.type, model))
            {
                Notifier.Show("Çanta dolu! Çöpleri makineye götür, torbaları inşaatta ve bahçede kullan.");
                return;
            }

            Notifier.Feed($"+1 {TrashNames.Display(model, data.type)}");
            Audio.AudioManager.Play(Audio.Sfx.Loot);
            foreach (var hitCollider in GetComponentsInChildren<Collider>()) hitCollider.enabled = false;
            enabled = false;
            ItemMover.FlyTo(gameObject, interactor.transform, EnvironmentManager.Instance.Settings.itemFlySpeed);
        }
    }
}
