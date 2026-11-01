using System;

namespace EcoGame.Data
{
    public enum TrashType { Glass, Metal, Plastic, Organic }

    public static class TrashTypeExtensions
    {
        public static readonly TrashType[] All = (TrashType[])Enum.GetValues(typeof(TrashType));
        public static int Count => All.Length;

        public static bool IsRecyclable(this TrashType type) => type != TrashType.Organic;

        /// <summary>Hex color of the material bag, used in rich text.</summary>
        public static string BagColorHex(this TrashType type) => type switch
        {
            TrashType.Glass => "#5fd38d",
            TrashType.Metal => "#b8c2cc",
            TrashType.Plastic => "#ffd84d",
            _ => "#c08850"
        };

        /// <summary>Darker shade of the bag color for text on the light UI panels.</summary>
        public static string TextColorHex(this TrashType type) => type switch
        {
            TrashType.Glass => "#1f8a4c",
            TrashType.Metal => "#4f5d6b",
            TrashType.Plastic => "#b07a00",
            _ => "#7a4a1e"
        };

        public static string DisplayName(this TrashType type) => type switch
        {
            TrashType.Glass => "Cam",
            TrashType.Metal => "Metal",
            TrashType.Plastic => "Plastik",
            _ => "Organik"
        };
    }
}
