using System.Collections.Generic;

namespace EcoGame.Data
{
    /// <summary>Display names for trash model variants (keys are the model names under a trash prefab's Visual).</summary>
    public static class TrashNames
    {
        private static readonly Dictionary<string, string> Names = new()
        {
            ["wine-red"] = "Kırmızı şarap şişesi",
            ["wine-white"] = "Beyaz şarap şişesi",
            ["glass-wine"] = "Şarap kadehi",
            ["soda-bottle"] = "Gazoz şişesi",
            ["can"] = "Konserve kutusu",
            ["can-small"] = "Küçük konserve",
            ["can-open"] = "Açık konserve",
            ["soda-can"] = "Kola kutusu",
            ["soda-can-crushed"] = "Ezik kola kutusu",
            ["bottle-ketchup"] = "Ketçap şişesi",
            ["bottle-musterd"] = "Hardal şişesi",
            ["soda"] = "Plastik bardak",
            ["cup-coffee"] = "Kahve bardağı",
            ["candy-bar-wrapper"] = "Çikolata ambalajı",
            ["apple-half"] = "Elma artığı",
            ["banana"] = "Muz",
            ["fish-bones"] = "Balık kılçığı",
            ["pizza"] = "Pizza artığı",
            ["pear-half"] = "Armut artığı",
            ["tomato"] = "Domates",
            ["watermelon"] = "Karpuz",
        };

        public static string Display(string modelKey, TrashType fallback) =>
            modelKey != null && Names.TryGetValue(modelKey, out var name) ? name : fallback.DisplayName();
    }
}
