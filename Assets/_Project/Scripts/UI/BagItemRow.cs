using EcoGame.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EcoGame.UI
{
    /// <summary>One draggable row in the bag window: a trash piece or a material bag. Dragging it out drops it.</summary>
    public class BagItemRow : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text label;

        private BagWindow _owner;

        public int Index { get; private set; }
        public bool IsBag { get; private set; }
        public TrashType Type { get; private set; }
        public Sprite IconSprite => icon.sprite;
        public Color IconColor => icon.color;

        public void Bind(BagWindow owner, int index, Sprite sprite, TrashType type)
        {
            _owner = owner;
            Index = index;
            IsBag = false;
            Type = type;
            ColorUtility.TryParseHtmlString(type.BagColorHex(), out var typeColor);
            icon.sprite = sprite;
            icon.color = sprite != null ? Color.white : typeColor;
            label.text = $"<color={type.TextColorHex()}>●</color> {type.DisplayName()}";
        }

        /// <summary>A material bag from the recycler, shown with its amount. Dragging it out drops one bag.</summary>
        public void BindBag(BagWindow owner, Sprite bagSprite, TrashType type, int amount)
        {
            _owner = owner;
            Index = -1;
            IsBag = true;
            Type = type;
            ColorUtility.TryParseHtmlString(type.BagColorHex(), out var typeColor);
            icon.sprite = bagSprite;
            icon.color = typeColor;
            label.text = $"{type.DisplayName()} torbası  <b>x{amount}</b>";
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _owner.BeginDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _owner.Drag(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _owner.EndDrag(this, eventData.position);
        }
    }
}
