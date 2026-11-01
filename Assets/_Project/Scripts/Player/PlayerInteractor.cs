using EcoGame.Animals;
using EcoGame.Audio;
using EcoGame.Cameras;
using EcoGame.Data;
using EcoGame.Interaction;
using EcoGame.Trash;
using EcoGame.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoGame.Player
{
    /// <summary>
    /// Proximity based: the nearest interactable that accepts the player is the target.
    /// Inspected is what the info panel describes: the crosshair target in first person, the E target in top-down.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public static PlayerInteractor Instance { get; private set; }

        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private CameraModeManager cameraModeManager;
        [Tooltip("Where emptied trash goes back to (Q).")]
        [SerializeField] private TrashSpawner trashSpawner;
        [SerializeField] private float dropRadius = 1.4f;
        [Tooltip("Bag model used when a material bag is dropped on the ground.")]
        [SerializeField] private GameObject bagPrefab;

        public PlayerInventory Inventory => inventory;
        public PlayerSettings Settings => inventory.Settings;
        public Interactable Current { get; private set; }
        public IInspectable Inspected { get; private set; }
        /// <summary>0..1 while E is held on a hold action, -1 otherwise.</summary>
        public float HoldProgress { get; private set; } = -1f;
        /// <summary>Animal in reach for petting (F), or the one being held.</summary>
        public AnimalPickup NearestAnimal { get; private set; }

        private Interactable _holdTarget;
        private float _holdDuration;
        private bool _waitForRelease;

        private void Awake() => Instance = this;

        private void Update()
        {
            if (Time.timeScale == 0f)
            {
                Current = null;
                Inspected = null;
                CancelHold();
                return;
            }

            // Holding an animal: hands are full, only F (put it down) works.
            bool holdingAnimal = AnimalPickup.Held != null;
            Current = holdingAnimal ? null : FindNearest();
            NearestAnimal = holdingAnimal ? AnimalPickup.Held : FindNearestAnimal();
            // First person: what the crosshair is on. Top-down: whatever E would act on, else the nearest thing around.
            Inspected = holdingAnimal ? null : cameraModeManager.Mode == CameraMode.FirstPerson ? FindLookedAt()
                : Current != null ? Current : FindNearestInspectable();
            if (Inspected is Interactable { ShowsInfo: false }) Inspected = null;

            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (NearestAnimal != null && keyboard.fKey.wasPressedThisFrame) NearestAnimal.Toggle(this);
            var holdSound = Current != null ? Current.GetHoldSound(this) : null;
            if (holdSound.HasValue) UpdateHold(keyboard, holdSound.Value);
            else
            {
                CancelHold();
                if (Current != null && keyboard.eKey.wasPressedThisFrame && Time.frameCount != UI.QuizPanel.ClosedFrame)
                    Current.Interact(this);
            }
            if (!keyboard.eKey.isPressed) _waitForRelease = false;
            if (keyboard.qKey.wasPressedThisFrame) EmptyBag();
        }

        /// <summary>Filling and watering: hold E while the sound plays; letting go early cancels and cuts the sound.</summary>
        private void UpdateHold(Keyboard keyboard, Sfx sound)
        {
            bool holding = keyboard.eKey.isPressed && !_waitForRelease;
            if (!holding || _holdTarget != Current) CancelHold();
            if (!holding) return;

            if (_holdTarget == null)
            {
                _holdTarget = Current;
                _holdDuration = AudioManager.StartHold(sound);
                HoldProgress = 0f;
            }

            HoldProgress += Time.deltaTime / _holdDuration;
            if (HoldProgress < 1f) return;

            var target = _holdTarget;
            CancelHold();
            _waitForRelease = true; // one action per press
            target.Interact(this);
        }

        private void CancelHold()
        {
            if (_holdTarget != null) AudioManager.StopHold();
            _holdTarget = null;
            HoldProgress = -1f;
        }

        /// <summary>Drops every piece of trash in a ring around the player.</summary>
        private void EmptyBag()
        {
            var items = inventory.TakeAllTrash();
            if (items.Count == 0 || trashSpawner == null) return;
            for (int i = 0; i < items.Count; i++)
            {
                float angle = i * Mathf.PI * 2f / items.Count;
                var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dropRadius;
                trashSpawner.Drop(items[i].type, items[i].model, new Vector3(transform.position.x, 0f, transform.position.z) + offset);
            }
            Notifier.Feed($"{items.Count} çöp yere bırakıldı");
        }

        /// <summary>Drops one bag item in front of the player (dragged out of the bag window).</summary>
        public void DropBagItem(int index)
        {
            if (trashSpawner == null || index < 0 || index >= inventory.BagItems.Count) return;
            var (type, model) = inventory.RemoveBagItem(index);
            Vector3 front = transform.position + transform.forward * dropRadius + Random.insideUnitSphere * 0.3f;
            trashSpawner.Drop(type, model, new Vector3(front.x, 0f, front.z));
            Notifier.Feed($"{type.DisplayName()} çöp yere bırakıldı");
        }

        /// <summary>Puts one material bag on the ground in front of the player (dragged out of the bag window).</summary>
        public void DropMaterialBag(TrashType type)
        {
            if (bagPrefab == null || inventory.TakeRaw(type, 1) == 0) return;
            Vector3 front = transform.position + transform.forward * dropRadius + Random.insideUnitSphere * 0.3f;
            DroppedBag.Spawn(bagPrefab, type, new Vector3(front.x, transform.position.y, front.z));
            Notifier.Feed($"<color={type.TextColorHex()}>{type.DisplayName()} torbası</color> yere bırakıldı");
        }

        private Interactable FindNearest()
        {
            float best = Settings.interactRadius;
            Interactable result = null;
            Vector3 position = transform.position;

            foreach (var interactable in Interactable.All)
            {
                if (interactable == null) continue;
                float distance = MathUtil.FlatDistance(position, interactable.Position);
                if (distance > best || !interactable.CanInteract(this)) continue;
                best = distance;
                result = interactable;
            }

            return result;
        }

        /// <summary>Nearest inspectable within reach, even when there is nothing to do with it.</summary>
        private AnimalPickup FindNearestAnimal()
        {
            float best = Settings.interactRadius;
            AnimalPickup result = null;
            foreach (var animal in AnimalPickup.All)
            {
                if (!animal.CanPet) continue;
                float distance = MathUtil.FlatDistance(transform.position, animal.transform.position);
                if (distance > best) continue;
                best = distance;
                result = animal;
            }
            return result;
        }

        private IInspectable FindNearestInspectable()
        {
            float best = Settings.interactRadius; // info shows at the same distance as the E prompt
            IInspectable result = null;
            foreach (var inspectable in Inspectables.All)
            {
                if (inspectable is Object unityObject && unityObject == null) continue;
                float distance = MathUtil.FlatDistance(transform.position, inspectable.Position);
                if (distance > best) continue;
                best = distance;
                result = inspectable;
            }
            return result;
        }

        private IInspectable FindLookedAt()
        {
            var cam = cameraModeManager.ActiveCamera;
            if (cam == null) return null;

            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // The player and invisible blockers live on the Ignore Raycast layer.
            if (!Physics.Raycast(ray, out var hit, Settings.inspectDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                return null;
            if (MathUtil.FlatDistance(transform.position, hit.point) > Settings.interactRadius) return null;
            return hit.collider.GetComponentInParent<IInspectable>();
        }
    }
}
