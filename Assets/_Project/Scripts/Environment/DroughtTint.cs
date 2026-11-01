using System.Collections.Generic;
using UnityEngine;

namespace EcoGame.Environment
{
    /// <summary>
    /// Tints every matching material under this object toward a dry color as Temperature rises
    /// (ground tiles, grass, flowers). Refreshes on an interval because it can cover hundreds of renderers.
    /// </summary>
    public class DroughtTint : MonoBehaviour
    {
        [Tooltip("Only materials whose name contains one of these are tinted. Empty = all materials.")]
        [SerializeField] private string[] materialKeywords = { };
        [SerializeField] private Color dryColor = new(0.85f, 0.62f, 0.25f);
        [SerializeField] private Vector2 temperatureRange = new(35f, 90f);
        [SerializeField] private float refreshInterval = 0.3f;
        [SerializeField] private string colorProperty = "_BaseColor";

        private struct Slot
        {
            public Renderer Renderer;
            public int Index;
            public Color Original;
        }

        private readonly List<Slot> _slots = new();
        private MaterialPropertyBlock _block;
        private int _colorId;
        private float _timer;
        private float _applied = -1f;

        private void Start()
        {
            _block = new MaterialPropertyBlock();
            _colorId = Shader.PropertyToID(colorProperty);
            foreach (var meshRenderer in GetComponentsInChildren<Renderer>(true))
            {
                var materials = meshRenderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || !material.HasProperty(_colorId) || !Matches(material.name)) continue;
                    _slots.Add(new Slot { Renderer = meshRenderer, Index = i, Original = material.GetColor(_colorId) });
                }
            }
        }

        private void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = refreshInterval;

            float t = Mathf.InverseLerp(temperatureRange.x, temperatureRange.y, EnvironmentManager.Instance.Temperature);
            if (Mathf.Abs(t - _applied) < 0.01f) return;
            _applied = t;

            foreach (var slot in _slots)
            {
                if (slot.Renderer == null) continue;
                slot.Renderer.GetPropertyBlock(_block, slot.Index);
                _block.SetColor(_colorId, Color.Lerp(slot.Original, dryColor * slot.Original.maxColorComponent, t));
                slot.Renderer.SetPropertyBlock(_block, slot.Index);
            }
        }

        private bool Matches(string materialName)
        {
            if (materialKeywords.Length == 0) return true;
            string lower = materialName.ToLowerInvariant();
            foreach (var keyword in materialKeywords)
                if (lower.Contains(keyword)) return true;
            return false;
        }
    }
}
