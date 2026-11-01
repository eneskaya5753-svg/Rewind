using System;

namespace EcoGame.Utils
{
    /// <summary>On-screen messages; UIManager listens.</summary>
    public static class Notifier
    {
        /// <summary>Center-screen message for important events.</summary>
        public static event Action<string> OnMessage;

        /// <summary>Small side feed for pickups and deliveries ("+1 Cam").</summary>
        public static event Action<string> OnFeed;

        public static void Show(string message) => OnMessage?.Invoke(message);

        public static void Feed(string line) => OnFeed?.Invoke(line);
    }
}
