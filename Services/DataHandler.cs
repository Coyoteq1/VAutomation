using System.Collections.Generic;
using Unity.Entities;

namespace CrowbaneArena
{
    /// <summary>
    /// Utility for handling V Blood data and progression.
    /// </summary>
    public static class DataHandler
    {
        public static Dictionary<string, int> GetBossGUIDs()
        {
            return new Dictionary<string, int>
            {
                {"Dracula", -123456789},
                {"Simon Belmont", -987654321},
                // Add more mappings
            };
        }

        public static bool IsBossUnlocked(Entity playerEntity, string bossName)
        {
            // Placeholder for actual check
            return false;
        }
    }
}
