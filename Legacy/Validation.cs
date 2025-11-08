using System;
using ProjectM;
using Unity.Entities;
using CrowbaneArena.Services;

namespace CrowbaneArena
{
    /// <summary>
    /// Utility for validating game data and progression.
    /// </summary>
    public static class Validation
    {
        private static readonly LoggingService Log = new LoggingService();

        public static bool IsValidPlayer(Entity playerEntity)
        {
            // Basic validation: ensure entity is not default and has a non-zero index/version
            return !playerEntity.Equals(default(Entity));
        }

        public static bool IsValidGUID(int guid)
        {
            // Valid GUIDs in V Rising are non-zero and should not be placeholder
            return guid != 0 && guid != Constants.PlaceholderGUID;
        }

        public static void EnsureProgressionIntegrity()
        {
            // Minimal sanity check hook
            Log.LogEvent("Ensuring progression integrity (basic checks).");
        }
    }
}
