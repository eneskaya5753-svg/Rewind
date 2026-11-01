using System.Collections.Generic;
using UnityEngine;

namespace EcoGame.Environment
{
    /// <summary>
    /// Lerps water between clean and dirty colors by WaterPollution.
    /// Only materials whose name contains materialKeyword are tinted (river tiles also carry grass).
    /// </summary>
    public class WaterColorController : MonoBehaviour
    {
        [SerializeField] private EnvironmentManager environmentManager;
        [SerializeField] private Renderer[] waterRenderers;
        [SerializeField] private string materialKeyword = "water";
        [SerializeField] private string colorProperty = "_BaseColor";

        private readonly List<(Renderer renderer, int index)> _slots = new();
        private MaterialPropertyBlock _block;
        private int _colorId;
        private float _t;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _colorId = Shader.PropertyToID(colorProperty);

            foreach (var waterRenderer in waterRenderers)
            {
                if (waterRenderer == null) continue;
                var materials = waterRenderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] != null && materials[i].name.ToLowerInvariant().Contains(materialKeyword))
                        _slots.Add((waterRenderer, i));
            }
        }

        private void Start() => _t = environmentManager.WaterPollution / 100f;

        private void Update()
        {
            var settings = environmentManager.Settings;
            _t = Mathf.Lerp(_t, environmentManager.WaterPollution / 100f, settings.visualLerpSpeed * Time.deltaTime);
            Color color = Color.Lerp(settings.waterCleanColor, settings.waterDirtyColor, _t);

            foreach (var (waterRenderer, index) in _slots)
            {
                waterRenderer.GetPropertyBlock(_block, index);
                _block.SetColor(_colorId, color);
                waterRenderer.SetPropertyBlock(_block, index);
            }
        }
    }
}
