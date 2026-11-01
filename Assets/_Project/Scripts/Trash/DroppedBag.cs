using EcoGame.Data;
using EcoGame.Interaction;
using EcoGame.Player;
using EcoGame.Utils;
using UnityEngine;

namespace EcoGame.Trash
{
    /// <summary>A material bag thrown out of the bag window. Lies on the ground until picked up again with E.</summary>
    public class DroppedBag : Interactable
    {
        private TrashType _type;

        /// <summary>Spawns a bag of the given type, tinted in its bag color like the recycler's output.</summary>
        public static DroppedBag Spawn(GameObject bagPrefab, TrashType type, Vector3 position, string colorProperty = "_BaseColor")
        {
            var bag = Instantiate(bagPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)).AddComponent<DroppedBag>();
            bag._type = type;
            ColorUtility.TryParseHtmlString(type.BagColorHex(), out var color);
            var block = new MaterialPropertyBlock();
            block.SetColor(colorProperty, color);
            foreach (var bagRenderer in bag.GetComponentsInChildren<Renderer>()) bagRenderer.SetPropertyBlock(block);
            return bag;
        }

        public override string Title => $"{_type.DisplayName()} torbası";
        public override string GetStatus() => "Yere bırakılmış malzeme torbası. İnşaat için geri alabilirsin.";
        public override string GetPrompt(PlayerInteractor interactor) => "Topla";
        public override bool CanInteract(PlayerInteractor interactor) => true;

        public override void Interact(PlayerInteractor interactor)
        {
            if (interactor.Inventory.FreeSpace <= 0)
            {
                Notifier.Show("Çanta dolu!");
                return;
            }

            interactor.Inventory.AddRaw(_type, 1);
            Notifier.Feed($"<color={_type.TextColorHex()}>+1 {_type.DisplayName()} torbası</color>");
            Audio.AudioManager.Play(Audio.Sfx.Loot);
            Destroy(gameObject);
        }
    }
}
