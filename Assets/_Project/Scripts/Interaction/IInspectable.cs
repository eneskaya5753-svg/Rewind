using System.Collections.Generic;
using UnityEngine;

namespace EcoGame.Interaction
{
    /// <summary>Anything that shows a name, status and optional progress bar when the player is close.</summary>
    public interface IInspectable
    {
        Vector3 Position { get; }
        string Title { get; }
        string GetStatus();

        /// <summary>0-1 for a bar, negative for none.</summary>
        float Progress { get; }
    }

    public static class Inspectables
    {
        public static readonly List<IInspectable> All = new();
    }
}
