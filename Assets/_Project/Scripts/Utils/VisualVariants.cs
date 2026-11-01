using UnityEngine;

namespace EcoGame.Utils
{
    public static class VisualVariants
    {
        /// <summary>Keeps one random child of "Visual" active.</summary>
        public static void PickRandom(Transform root) => PickRandomChild(root.Find("Visual"));

        /// <summary>Name of the active variant under "Visual" (the model key), or null.</summary>
        public static string ActiveModel(Transform root)
        {
            var visual = root.Find("Visual");
            if (visual == null) return null;
            foreach (Transform child in visual)
                if (child.gameObject.activeSelf) return child.name.Replace("(Clone)", "").Trim();
            return null;
        }

        /// <summary>Activates the variant with the given model name, if present.</summary>
        public static void Show(Transform root, string model)
        {
            var visual = root.Find("Visual");
            if (visual == null || model == null || visual.Find(model) == null) return;
            foreach (Transform child in visual) child.gameObject.SetActive(child.name == model);
        }

        /// <summary>Keeps one random child of the given transform active.</summary>
        public static void PickRandomChild(Transform parent)
        {
            if (parent == null || parent.childCount < 2) return;
            int pick = Random.Range(0, parent.childCount);
            for (int i = 0; i < parent.childCount; i++)
                parent.GetChild(i).gameObject.SetActive(i == pick);
        }
    }
}
