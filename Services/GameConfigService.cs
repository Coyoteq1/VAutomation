using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using CrowbaneArena.Models;
using ProjectM;
using Stunlock.Core;
using Unity.Mathematics;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Manages boss data and snapshots (complements ArenaConfigurationService)
    /// </summary>
    public static class GameConfigService
    {
        private static BossConfig _bossConfig;
        private static readonly string _dataDirectory = Path.Combine("BepInEx", "config", "CrowbaneArena", "Data");
            
        private static readonly string _bossConfigPath = Path.Combine(_dataDirectory, "boss_data.cfg");
        private static readonly string _snapshotsDir = Path.Combine("BepInEx", "config", "CrowbaneArena", "Snapshots");

        /// <summary>
        /// Gets the current boss configuration
        /// </summary>
        public static BossConfig Bosses => _bossConfig ??= LoadOrCreateBossConfig();

        /// <summary>
        /// Initialize the service
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // Ensure directories exist
                if (!Directory.Exists(_dataDirectory))
                    Directory.CreateDirectory(_dataDirectory);
                    
                if (!Directory.Exists(_snapshotsDir))
                    Directory.CreateDirectory(_snapshotsDir);
                
                // Load boss config
                _bossConfig = LoadOrCreateBossConfig();
                
                Plugin.Logger?.LogInfo($"GameConfigService initialized with {_bossConfig?.Bosses?.Count ?? 0} bosses");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to initialize GameConfigService: {ex}");
                _bossConfig = new BossConfig();
            }
        }
        
        /// <summary>
        /// Reload boss configuration from disk
        /// </summary>
        public static void ReloadBossConfig()
        {
            try
            {
                _bossConfig = LoadBossConfig();
                Plugin.Logger?.LogInfo("Boss configuration reloaded");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to reload boss config: {ex}");
                throw;
            }
        }

        #region Boss Management

        /// <summary>
        /// Get all bosses
        /// </summary>
        public static IReadOnlyDictionary<string, BossService.BossData> GetAllBosses() => Bosses.Bosses;

        /// <summary>
        /// Get a boss by name (case-insensitive)
        /// </summary>
        public static bool TryGetBoss(string bossName, out BossService.BossData boss)
        {
            return Bosses.Bosses.TryGetValue(bossName, out boss);
        }

        #endregion

        #region Snapshot Management

        /// <summary>
        /// Save a player snapshot to file (deprecated - use SnapshotManager instead)
        /// </summary>
        [Obsolete("Use SnapshotManager.SaveSnapshot instead")]
        public static void SaveSnapshot(ulong steamId, string snapshotData)
        {
            var snapshotPath = Path.Combine(_snapshotsDir, $"snapshot_{steamId}.txt");
            File.WriteAllText(snapshotPath, snapshotData);
        }

        /// <summary>
        /// Load a player snapshot from file (deprecated - use SnapshotManager instead)
        /// </summary>
        [Obsolete("Use SnapshotManager.LoadSnapshot instead")]
        public static string LoadSnapshot(ulong steamId)
        {
            var snapshotPath = Path.Combine(_snapshotsDir, $"snapshot_{steamId}.txt");
            return File.Exists(snapshotPath) ? File.ReadAllText(snapshotPath) : null;
        }

        /// <summary>
        /// Delete a player snapshot (deprecated - use SnapshotManager instead)
        /// </summary>
        [Obsolete("Use SnapshotManager.DeleteSnapshot instead")]
        public static bool DeleteSnapshot(ulong steamId)
        {
            var snapshotPath = Path.Combine(_snapshotsDir, $"snapshot_{steamId}.txt");
            if (!File.Exists(snapshotPath)) return false;

            File.Delete(snapshotPath);
            return true;
        }

        #endregion

        #region Private Methods

        private static BossConfig LoadOrCreateBossConfig()
        {
            if (File.Exists(_bossConfigPath))
                return LoadBossConfig();

            var defaultConfig = CreateDefaultBossConfig();
            SaveBossConfig(defaultConfig);
            return defaultConfig;
        }

        private static BossConfig LoadBossConfig()
        {
            if (!File.Exists(_bossConfigPath))
                throw new FileNotFoundException("Boss config file not found", _bossConfigPath);

            // Try to parse as JSON first, fall back to CFG if needed
            try
            {
                var json = File.ReadAllText(_bossConfigPath);
                return JsonSerializer.Deserialize<BossConfig>(json);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"Failed to parse boss config as JSON, trying CFG: {ex.Message}");
                return null;
            }
        }

        private static void SaveBossConfig(BossConfig config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_bossConfigPath, json);
        }

        private static BossConfig CreateDefaultBossConfig()
        {
            return new BossConfig
            {
                Bosses = new Dictionary<string, BossService.BossData>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Alphawolf"] = new BossService.BossData
                    {
                        Name = "Alphawolf",
                        VBloodId = -1905691330,
                        Level = 16,
                        Region = "Farbane Woods",
                        Rewards = new List<string> { "WolfVBlood", "Leather" }
                    },
                    ["Keely"] = new BossService.BossData
                    {
                        Name = "Keely",
                        VBloodId = -1342764880,
                        Level = 20,
                        Region = "Farbane Woods",
                        Rewards = new List<string> { "BanditVBlood", "Copper" }
                    }
                }
            };
        }

        #endregion
    }

    /// <summary>
    /// Boss configuration container
    /// </summary>
    public class BossConfig
    {
        public Dictionary<string, BossService.BossData> Bosses { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

}
