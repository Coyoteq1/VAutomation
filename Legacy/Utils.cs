using System;

namespace CrowbaneArena.Utils
{
    /// <summary>
    /// Shared utility functions for the mod.
    /// </summary>
    public static class Utils
    {
        public static string FormatMessage(string message)
        {
            return $"[CrowbaneArena] {message}";
        }

        public static int GenerateRandomGUID()
        {
            return new Random().Next();
        }

        public static bool IsArenaActive()
        {
            // Placeholder
            return false;
        }
    }
}
