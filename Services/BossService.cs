using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using ProjectM;
using ProjectM.Network;
using CrowbaneArena.Data;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service for managing boss encounters and related functionality
    /// </summary>
    public static class BossService
    {
        private static bool _initialized = false;
        private static readonly Dictionary<string, BossInfo> _bosses = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<ulong, HashSet<string>> _defeatedBosses = new();

        /// <summary>
        /// Boss information structure
        /// </summary>
        public class BossInfo
        {
            public string Name { get; set; } = string.Empty;
            public int VBloodId { get; set; }
            public int Level { get; set; }
            public string Region { get; set; } = "Unknown";
            public List<string> Rewards { get; set; } = new();
        }

        /// <summary>
        /// Boss data structure for configuration
        /// </summary>
        public class BossData
        {
            public string Name { get; set; } = string.Empty;
            public int VBloodId { get; set; }
            public int Level { get; set; }
            public string Region { get; set; } = "Unknown";
            public List<string> Rewards { get; set; } = new();
        }

        /// <summary>
        /// Initialize the boss service
        /// </summary>
        public static bool Initialize()
        {
            try
            {
                if (_initialized)
                {
                    Plugin.Logger?.LogInfo("BossService already initialized");
                    return true;
                }

                Plugin.Logger?.LogInfo("Initializing BossService...");
                InitializeBossData();

                _initialized = true;
                Plugin.Logger?.LogInfo($"BossService initialized with {_bosses.Count} bosses");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to initialize BossService: {ex.Message}");
                Plugin.Logger?.LogError(ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Initialize boss data
        /// </summary>
        private static void InitializeBossData()
        {
            try
            {
                _bosses.Clear();

                // Add all V Blood bosses
                var vbloods = CrowbaneArena.Data.VBloodGUIDs.GetAllVBloods();
                foreach (var kvp in vbloods)
                {
                    _bosses[kvp.Key] = new BossInfo
                    {
                        Name = kvp.Key,
                        VBloodId = kvp.Value,
                        Level = GetBossLevel(kvp.Key)
                    };
                }

                Plugin.Logger?.LogInfo($"Initialized {_bosses.Count} bosses");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error initializing boss data: {ex.Message}");
                _bosses.Clear();
            }
        }

        private static int GetBossLevel(string bossName)
        {
            // This is a simplified version - you might want to expand this
            // with actual boss levels from game data
            return bossName.ToLower() switch
            {
                "alphawolf" => 16,
                "keely" => 20,
                "rufus" => 27,
                "errol" => 30,
                "lidia" => 34,
                "jade" => 37,
                "putridrat" => 42,
                "goreswine" => 47,
                "clive" => 52,
                "polora" => 57,
                "bear" => 60,
                "nicholaus" => 63,
                "quincey" => 66,
                "vincent" => 68,
                "christina" => 70,
                "tristan" => 72,
                "wingedhorror" => 75,
                "ungora" => 78,
                "terrorclaw" => 80,
                "willfred" => 82,
                "octavian" => 85,
                "solarus" => 90,
                _ => 1
            };
        }

        /// <summary>
        /// Mark a boss as defeated for a player
        /// </summary>
        public static bool DefeatBoss(ulong steamId, string bossName)
        {
            if (string.IsNullOrWhiteSpace(bossName))
            {
                Plugin.Logger?.LogWarning("Boss name cannot be empty");
                return false;
            }

            // Do not persist boss defeats while inside arena. Arena should never
            // modify permanent progression.
            if (GameSystems.IsPlayerInArena(steamId))
            {
                Plugin.Logger?.LogInfo($"[BossService] Ignoring boss defeat '{bossName}' for {steamId} while in arena");
                return false;
            }

            if (!_bosses.ContainsKey(bossName))
            {
                Plugin.Logger?.LogWarning($"Unknown boss: {bossName}");
                return false;
            }

            if (!_defeatedBosses.TryGetValue(steamId, out var defeatedSet))
            {
                defeatedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _defeatedBosses[steamId] = defeatedSet;
            }

            if (defeatedSet.Add(bossName))
            {
                Plugin.Logger?.LogInfo($"Player {steamId} defeated boss: {bossName}");
                return true;
            }

            Plugin.Logger?.LogInfo($"Player {steamId} already defeated boss: {bossName}");
            return false;
        }
        /// <summary>
        /// Check if player has defeated a boss
        /// </summary>
        public static bool HasDefeatedBoss(ulong steamId, string bossName)
        {
            if (string.IsNullOrWhiteSpace(bossName)) return false;
            return _defeatedBosses.TryGetValue(steamId, out var set) && set.Contains(bossName);
        }

        /// <summary>
        /// Get boss info by name
        /// </summary>
        public static BossInfo GetBossInfo(string bossName)
        {
            if (string.IsNullOrWhiteSpace(bossName)) return null;
            return _bosses.TryGetValue(bossName, out var info) ? info : null;
        }

        /// <summary>
        /// Get all bosses
        /// </summary>
        public static IReadOnlyCollection<BossInfo> GetAllBosses()
        {
            return _bosses.Values.ToList().AsReadOnly();
        }
    }
}
