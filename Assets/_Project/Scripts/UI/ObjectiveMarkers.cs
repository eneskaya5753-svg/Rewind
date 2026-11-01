using System.Collections.Generic;
using EcoGame.Building;
using EcoGame.Cameras;
using EcoGame.Interaction;
using EcoGame.Water;
using UnityEngine;
using UnityEngine.UI;

namespace EcoGame.UI
{
    /// <summary>
    /// Small downward arrows over interactable places: yellow for unfinished build sites and the unfertilized garden,
    /// blue for trees, the trough and the garden (things to water), plus fixed stations such as the desalinator
    /// and the recycling machine. Only drawn while on screen.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ObjectiveMarkers : MonoBehaviour
    {
        [SerializeField] private CameraModeManager cameraModeManager;
        [SerializeField] private Garden garden;
        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private Color color = new(1f, 0.85f, 0.2f);
        [SerializeField] private Color waterColor = new(0.35f, 0.7f, 1f);
        [SerializeField] private Transform[] stations;
        [SerializeField] private Color[] stationColors;
        [SerializeField] private float markerSize = 22f;
        [SerializeField] private float heightOffset = 4f;

        private readonly List<(Vector3 position, Color color)> _targets = new();
        private readonly List<Image> _markers = new();
        private RectTransform _rect;

        private void Awake() => _rect = (RectTransform)transform;

        private void LateUpdate()
        {
            _targets.Clear();
            foreach (var site in BuildSite.Sites)
                if (!site.IsCompleted) _targets.Add((site.transform.position, color));
            if (garden != null && !garden.IsFertilized) _targets.Add((garden.transform.position, color));
            foreach (var interactable in Interactable.All)
                if (interactable is Waterable waterable && !(waterable == garden && !garden.IsFertilized))
                    _targets.Add((waterable.transform.position, waterColor));
            for (int i = 0; i < stations.Length; i++)
                if (stations[i] != null) _targets.Add((stations[i].position, i < stationColors.Length ? stationColors[i] : color));

            var cam = cameraModeManager.ActiveCamera;
            for (int i = 0; i < _markers.Count || i < _targets.Count; i++)
            {
                var marker = GetMarker(i);
                bool show = cam != null && i < _targets.Count && Place(marker, cam, _targets[i].position + Vector3.up * heightOffset);
                if (show) marker.color = _targets[i].color;
                if (marker.gameObject.activeSelf != show) marker.gameObject.SetActive(show);
            }
        }

        /// <summary>Returns false when the point is behind the camera or outside the screen.</summary>
        private bool Place(Image marker, Camera cam, Vector3 world)
        {
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z < 0f || screen.x < 0f || screen.x > Screen.width || screen.y < 0f || screen.y > Screen.height)
                return false;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, screen, null, out var local);
            marker.rectTransform.anchoredPosition = local;
            return true;
        }

        private Image GetMarker(int index)
        {
            while (_markers.Count <= index)
            {
                var go = new GameObject("Img_ObjectiveMarker", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var image = go.AddComponent<Image>();
                image.sprite = arrowSprite;
                image.raycastTarget = false;
                image.rectTransform.sizeDelta = Vector2.one * markerSize;
                image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f); // sprite points right
                _markers.Add(image);
            }
            return _markers[index];
        }
    }
}
