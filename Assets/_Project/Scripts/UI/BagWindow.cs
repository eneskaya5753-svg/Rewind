using System;
using System.Collections.Generic;
using EcoGame.Cameras;
using EcoGame.Data;
using EcoGame.Player;
using UnityEngine;
using UnityEngine.UI;

namespace EcoGame.UI
{
    /// <summary>
    /// Inventory list on the left (Tab): one row per trash piece with a small picture and its type.
    /// Dragging a row outside the panel drops that piece on the ground. Material bags are summarized below.
    /// </summary>
    public class BagWindow : MonoBehaviour
    {
        [Serializable]
        public struct Icon
        {
            public string model;
            public Sprite sprite;
        }

        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private CameraModeManager cameraModeManager;
        [SerializeField] private RectTransform listRoot;
        [SerializeField] private BagItemRow rowTemplate;
        [SerializeField] private Text txtBags;
        [SerializeField] private Icon[] icons;
        [SerializeField] private Sprite bagIcon;
        [Tooltip("HUD parts hidden while the bag is open (metrics, bag/water bars).")]
        [SerializeField] private GameObject[] hideWhileOpen;
        [SerializeField] private float rowHeight = 46f;

        private readonly List<BagItemRow> _rows = new();
        private string _signature;
        private Image _dragGhost;

        public RectTransform PanelRect => (RectTransform)transform;

        private void Awake()
        {
            // Full height on the left edge: top to bottom with a small margin.
            const float MARGIN = 20f;
            var panel = PanelRect;
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 1f);
            panel.anchoredPosition = new Vector2(MARGIN, -MARGIN);
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, -2f * MARGIN);

            // "Çanta boş." sits right under the title instead of over the rows.
            var bagsRect = txtBags.rectTransform;
            bagsRect.anchorMin = new Vector2(0f, 1f);
            bagsRect.anchorMax = new Vector2(1f, 1f);
            bagsRect.pivot = new Vector2(0.5f, 1f);
            bagsRect.anchoredPosition = new Vector2(0f, -52f);
            bagsRect.sizeDelta = new Vector2(-30f, 30f);
        }

        private void OnEnable()
        {
            _signature = null;
            cameraModeManager.SetCursorFree(true);
            SetOthersVisible(false);
        }

        private void OnDisable()
        {
            EndDrag();
            if (cameraModeManager != null) cameraModeManager.SetCursorFree(Time.timeScale == 0f);
            SetOthersVisible(true);
        }

        private void SetOthersVisible(bool visible)
        {
            foreach (var part in hideWhileOpen)
                if (part != null) part.SetActive(visible);
        }

        private void Update()
        {
            string signature = Signature();
            if (signature != _signature)
            {
                _signature = signature;
                Rebuild();
            }
        }

        private string Signature()
        {
            var builder = new System.Text.StringBuilder();
            foreach (var item in inventory.BagItems) builder.Append(item.model).Append('|');
            foreach (var type in TrashTypeExtensions.All) builder.Append(inventory.GetRaw(type)).Append(',');
            return builder.ToString();
        }

        private void Rebuild()
        {
            var items = inventory.BagItems;
            var bagTypes = new List<TrashType>();
            foreach (var type in TrashTypeExtensions.All)
                if (inventory.GetRaw(type) > 0) bagTypes.Add(type);

            int total = items.Count + bagTypes.Count;
            // Rows shrink when the bag is very full, so the list never runs past the hint at the bottom.
            const float LIST_TOP = 52f, HINT_SPACE = 44f;
            float available = PanelRect.rect.height - LIST_TOP - HINT_SPACE;
            float step = total > 0 ? Mathf.Min(rowHeight, available / total) : rowHeight;
            while (_rows.Count < total)
            {
                var row = Instantiate(rowTemplate, listRoot);
                row.gameObject.SetActive(true);
                _rows.Add(row);
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < total;
                _rows[i].gameObject.SetActive(used);
                if (!used) continue;
                if (i < items.Count) _rows[i].Bind(this, i, FindIcon(items[i].model), items[i].type);
                else
                {
                    var type = bagTypes[i - items.Count];
                    _rows[i].BindBag(this, bagIcon, type, inventory.GetRaw(type));
                }
                var rowRect = (RectTransform)_rows[i].transform;
                rowRect.anchoredPosition = new Vector2(0f, -i * step);
                rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, step - 4f);
            }
            listRoot.sizeDelta = new Vector2(listRoot.sizeDelta.x, total * step);
            txtBags.text = total == 0 ? "Çanta boş." : "";
        }

        private Sprite FindIcon(string model)
        {
            foreach (var icon in icons)
                if (icon.model == model) return icon.sprite;
            return null;
        }

        public void BeginDrag(BagItemRow row)
        {
            EndDrag();
            var canvas = GetComponentInParent<Canvas>().rootCanvas;
            _dragGhost = new GameObject("Img_DragGhost", typeof(RectTransform)).AddComponent<Image>();
            _dragGhost.transform.SetParent(canvas.transform, false);
            _dragGhost.sprite = row.IconSprite;
            _dragGhost.color = row.IconSprite != null ? Color.white : row.IconColor;
            _dragGhost.raycastTarget = false;
            _dragGhost.rectTransform.sizeDelta = new Vector2(56f, 56f);
        }

        public void Drag(Vector2 screenPosition)
        {
            if (_dragGhost != null) _dragGhost.transform.position = screenPosition;
        }

        public void EndDrag(BagItemRow row, Vector2 screenPosition)
        {
            EndDrag();
            if (!RectTransformUtility.RectangleContainsScreenPoint(PanelRect, screenPosition, null))
                interactor.DropBagItem(row.Index);
        }

        private void EndDrag()
        {
            if (_dragGhost != null) Destroy(_dragGhost.gameObject);
            _dragGhost = null;
        }
    }
}
