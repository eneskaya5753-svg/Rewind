using System.Collections.Generic;
using EcoGame.Water;
using UnityEngine;

namespace EcoGame.Environment
{
    /// <summary>
    /// Tints leaf materials under "Visual" toward orange with Temperature and toward brown when dry.
    /// Materials whose name contains an exclude keyword (trunk) keep their color.
    /// </summary>
    public class TreeColorController : MonoBehaviour
    {
        [SerializeField] private List<Renderer> excludeRenderers = new();
        [SerializeField] private string[] excludeMaterialKeywords = { "wood", "bark", "trunk" };
        [SerializeField] private string colorProperty = "_BaseColor";

        private struct Slot
        {
            public Renderer Renderer;
            public int Index;
            public Color Original;
        }

        private readonly List<Slot> _slots = new();
        private MaterialPropertyBlock _block;
        private Waterable _waterable;
        private int _colorId;
        private float _hot;
        private float _dry;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _colorId = Shader.PropertyToID(colorProperty);
            _waterable = GetComponent<Waterable>();

            var visual = transform.Find("Visual");
            foreach (var meshRenderer in (visual != null ? visual : transform).GetComponentsInChildren<Renderer>(true))
            {
                if (excludeRenderers.Contains(meshRenderer)) continue;
                var materials = meshRenderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null || !material.HasProperty(_colorId) || IsExcluded(material.name)) continue;
                    _slots.Add(new Slot { Renderer = meshRenderer, Index = i, Original = material.GetColor(_colorId) });
                }
            }
        }

        private void Update()
        {
            var environment = EnvironmentManager.Instance;
            var settings = environment.Settings;
            float speed = settings.visualLerpSpeed * Time.deltaTime;
            float hotTarget = Mathf.InverseLerp(settings.treeHotRange.x, settings.treeHotRange.y, environment.Temperature);
            // Waterable trees dry out when not watered; the rest of the forest dries out in extreme heat.
            float dryTarget = _waterable != null
                ? (_waterable.IsDry ? 1f : 0f)
                : Mathf.InverseLerp(settings.treeHotRange.y - 10f, settings.loseValue, environment.Temperature);
            _hot = Mathf.Lerp(_hot, hotTarget, speed);
            _dry = Mathf.Lerp(_dry, dryTarget, speed * 2f);

            foreach (var slot in _slots)
            {
                Color color = Color.Lerp(Color.Lerp(slot.Original, settings.treeHotColor, _hot), settings.treeDryColor, _dry);
                slot.Renderer.GetPropertyBlock(_block, slot.Index);
                _block.SetColor(_colorId, color);
                slot.Renderer.SetPropertyBlock(_block, slot.Index);
            }
        }

        private bool IsExcluded(string materialName)
        {
            string lower = materialName.ToLowerInvariant();
            foreach (var keyword in excludeMaterialKeywords)
                if (lower.Contains(keyword)) return true;
            return false;
        }
    }
}
