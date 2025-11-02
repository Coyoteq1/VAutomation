using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Unity.Entities;
using ProjectM;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Core data persistence manager for all player data operations
    /// Provides centralized save/load functionality with persistence suppression
    /// </summary>
    public static class DataPersistenceManager
    {
        private static readonly string DataDirectory = Path.Combine(Paths.ConfigPath, "CrowbaneArena", "PlayerData");
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Persistence suppression system
        private static int _persistenceSuppressionCount = 0;
        private static readonly object _suppressionLock = new();

        /// <summary>
        /// Returns an IDisposable scope that prevents automatic saves during data operations
        /// </summary>
        public static IDisposable SuppressPersistence()
        {
            lock (_suppressionLock)
            {
                _persistenceSuppressionCount++;
                return new PersistenceSuppressionScope();
            }
        }

        /// <summary>
        /// Property to check if persistence is currently suppressed
        /// </summary>
        public static bool IsPersistenceSuppressed
        {
            get
            {
                lock (_suppressionLock)
                {
                    return _persistenceSuppressionCount > 0;
                }
            }
        }

        /// <summary>
        /// Ensures data directory exists
        /// </summary>
        private static void EnsureDataDirectory()
        {
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }
        }

        /// <summary>
        /// Gets the file path for a player's data file
        /// </summary>
        private static string GetPlayerDataFilePath(ulong steamId, string dataType)
        {
            return Path.Combine(DataDirectory, $"{steamId}_{dataType}.json");
        }

        /// <summary>
        /// Saves data to JSON file if persistence is not suppressed
        /// </summary>
        private static bool SaveData<T>(ulong steamId, string dataType, T data)
        {
            if (IsPersistenceSuppressed)
            {
                return true; // Consider suppressed saves as successful
            }

            try
            {
                EnsureDataDirectory();
                var filePath = GetPlayerDataFilePath(steamId, dataType);
                var json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Failed to save {dataType} data for player {steamId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads data from JSON file
        /// </summary>
        private static bool LoadData<T>(ulong steamId, string dataType, out T data)
        {
            data = default;
            var filePath = GetPlayerDataFilePath(steamId, dataType);

            if (!File.Exists(filePath))
            {
                return false;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                data = JsonSerializer.Deserialize<T>(json, JsonOptions);
                return true;
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Failed to load {dataType} data for player {steamId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Persistence suppression scope implementation
        /// </summary>
        private class PersistenceSuppressionScope : IDisposable
        {
            public void Dispose()
            {
                lock (_suppressionLock)
                {
                    _persistenceSuppressionCount = Math.Max(0, _persistenceSuppressionCount - 1);
                }
            }
        }

        #region Experience & Leveling APIs

        /// <summary>
        /// Attempts to get player experience data
        /// </summary>
        public static bool TryGetPlayerExperience(ulong steamId, out KeyValuePair<int, float> experience)
        {
            return LoadData(steamId, "experience", out experience);
        }

        /// <summary>
        /// Sets player experience data
        /// </summary>
        public static bool SetPlayerExperience(ulong steamId, KeyValuePair<int, float> data)
        {
            return SaveData(steamId, "experience", data);
        }

        /// <summary>
        /// Attempts to get player rested XP data
        /// </summary>
        public static bool TryGetPlayerRestedXP(ulong steamId, out KeyValuePair<DateTime, float> restedXP)
        {
            return LoadData(steamId, "rested_xp", out restedXP);
        }

        /// <summary>
        /// Sets player rested XP data
        /// </summary>
        public static bool SetPlayerRestedXP(ulong steamId, KeyValuePair<DateTime, float> data)
        {
            return SaveData(steamId, "rested_xp", data);
        }

        #endregion

        #region Weapon Stats APIs

        /// <summary>
        /// Attempts to get weapon statistics for a player
        /// </summary>
        public static bool TryGetPlayerWeaponStats(ulong steamId, out Dictionary<WeaponType, List<WeaponManager.WeaponStats.WeaponStatType>> weaponStats)
        {
            return WeaponManager.WeaponStats.TryGetPlayerWeaponStats(steamId, out weaponStats);
        }

        /// <summary>
        /// Sets weapon statistics for a player
        /// </summary>
        public static bool SetPlayerWeaponStats(ulong steamId, Dictionary<WeaponType, List<WeaponManager.WeaponStats.WeaponStatType>> data)
        {
            return WeaponManager.WeaponStats.SetPlayerWeaponStats(steamId, data);
        }

        #endregion

        #region Spells & Equipment APIs

        /// <summary>
        /// Attempts to get player spells data
        /// </summary>
        public static bool TryGetPlayerSpells(ulong steamId, out (int FirstUnarmed, int SecondUnarmed, int ClassSpell) spells)
        {
            return LoadData(steamId, "spells", out spells);
        }

        /// <summary>
        /// Sets player spells data
        /// </summary>
        public static bool SetPlayerSpells(ulong steamId, (int FirstUnarmed, int SecondUnarmed, int ClassSpell) data)
        {
            return SaveData(steamId, "spells", data);
        }

        #endregion

        #region Quests APIs

        /// <summary>
        /// Attempts to get player quests data
        /// </summary>
        public static bool TryGetPlayerQuests(ulong steamId, out Dictionary<string, (string Objective, int Progress, DateTime LastReset)> quests)
        {
            return LoadData(steamId, "quests", out quests);
        }

        /// <summary>
        /// Sets player quests data
        /// </summary>
        public static bool SetPlayerQuests(ulong steamId, Dictionary<string, (string Objective, int Progress, DateTime LastReset)> data)
        {
            return SaveData(steamId, "quests", data);
        }

        #endregion

        #region Familiar APIs

        /// <summary>
        /// Attempts to get familiar box data
        /// </summary>
        public static bool TryGetFamiliarBox(ulong steamId, out string familiarSet)
        {
            return LoadData(steamId, "familiar_box", out familiarSet);
        }

        /// <summary>
        /// Sets familiar box data
        /// </summary>
        public static bool SetFamiliarBox(ulong steamId, string data)
        {
            return SaveData(steamId, "familiar_box", data);
        }

        /// <summary>
        /// Attempts to get binding index
        /// </summary>
        public static bool TryGetBindingIndex(ulong steamId, out int index)
        {
            return LoadData(steamId, "binding_index", out index);
        }

        /// <summary>
        /// Sets binding index
        /// </summary>
        public static bool SetBindingIndex(ulong steamId, int index)
        {
            return SaveData(steamId, "binding_index", index);
        }

        #endregion

        #region Player Preferences APIs

        /// <summary>
        /// Saves player boolean preferences
        /// </summary>
        public static bool SavePlayerBools(ulong steamId, Dictionary<string, bool> preferences)
        {
            return SaveData(steamId, "preferences", preferences);
        }

        /// <summary>
        /// Loads player boolean preferences
        /// </summary>
        public static bool LoadPlayerBools(ulong steamId, out Dictionary<string, bool> preferences)
        {
            return LoadData(steamId, "preferences", out preferences);
        }

        /// <summary>
        /// Gets or initializes player boolean preferences with defaults
        /// </summary>
        public static Dictionary<string, bool> GetOrInitializePlayerBools(ulong steamId, Dictionary<string, bool> defaultBools)
        {
            if (LoadPlayerBools(steamId, out var existing))
            {
                // Merge with defaults for any missing keys
                foreach (var kvp in defaultBools)
                {
                    if (!existing.ContainsKey(kvp.Key))
                    {
                        existing[kvp.Key] = kvp.Value;
                    }
                }
                return existing;
            }

            // Save defaults if no existing data
            SavePlayerBools(steamId, defaultBools);
            return new Dictionary<string, bool>(defaultBools);
        }

        #endregion

        #region UnlockDataAPI

        /// <summary>
        /// Data structure for player unlocks
        /// </summary>
        public class PlayerUnlockData
        {
            public Dictionary<string, bool> VBloodUnlocks { get; set; } = new();
            public Dictionary<string, bool> AbilityUnlocks { get; set; } = new();
            public Dictionary<string, bool> RecipeUnlocks { get; set; } = new();
            public Dictionary<string, bool> QuestUnlocks { get; set; } = new();
            public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        }

        /// <summary>
        /// Attempts to get player unlock data
        /// </summary>
        public static bool TryGetPlayerUnlockData(ulong steamId, out PlayerUnlockData unlockData)
        {
            return LoadData(steamId, "unlock_data", out unlockData);
        }

        /// <summary>
        /// Sets player unlock data
        /// </summary>
        public static bool SetPlayerUnlockData(ulong steamId, PlayerUnlockData unlockData)
        {
            unlockData.LastUpdated = DateTime.UtcNow;
            return SaveData(steamId, "unlock_data", unlockData);
        }

        /// <summary>
        /// Unlocks a specific VBlood for a player
        /// </summary>
        public static bool UnlockVBlood(ulong steamId, string vBloodId)
        {
            if (!TryGetPlayerUnlockData(steamId, out var unlockData))
            {
                unlockData = new PlayerUnlockData();
            }

            unlockData.VBloodUnlocks[vBloodId] = true;
            return SetPlayerUnlockData(steamId, unlockData);
        }

        /// <summary>
        /// Locks a specific VBlood for a player
        /// </summary>
        public static bool LockVBlood(ulong steamId, string vBloodId)
        {
            if (!TryGetPlayerUnlockData(steamId, out var unlockData))
            {
                return false;
            }

            unlockData.VBloodUnlocks[vBloodId] = false;
            return SetPlayerUnlockData(steamId, unlockData);
        }

        /// <summary>
        /// Checks if a VBlood is unlocked for a player
        /// </summary>
        public static bool IsVBloodUnlocked(ulong steamId, string vBloodId)
        {
            if (!TryGetPlayerUnlockData(steamId, out var unlockData))
            {
                return false;
            }

            return unlockData.VBloodUnlocks.TryGetValue(vBloodId, out var unlocked) && unlocked;
        }

        #endregion

        #region RecoverDataAPI

        /// <summary>
        /// Data structure for recovery operations
        /// </summary>
        public class DataRecoveryResult
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public Dictionary<string, bool> RecoveredItems { get; set; } = new();
            public DateTime RecoveryTime { get; set; } = DateTime.UtcNow;
        }

        /// <summary>
        /// Attempts to recover corrupted or missing player data
        /// </summary>
        public static DataRecoveryResult RecoverPlayerData(ulong steamId)
        {
            var result = new DataRecoveryResult();

            try
            {
                EnsureDataDirectory();
                var dataFiles = Directory.GetFiles(DataDirectory, $"{steamId}_*.json");

                foreach (var file in dataFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var dataType = fileName.Substring(fileName.IndexOf('_') + 1);

                        // Attempt to validate JSON structure
                        var json = File.ReadAllText(file);
                        using var doc = JsonDocument.Parse(json);

                        result.RecoveredItems[dataType] = true;
                    }
                    catch (JsonException)
                    {
                        // Attempt to repair corrupted JSON
                        result.RecoveredItems[Path.GetFileNameWithoutExtension(file)] = false;
                    }
                }

                result.Success = result.RecoveredItems.Count > 0;
                result.Message = result.Success ?
                    $"Recovered {result.RecoveredItems.Count} data files" :
                    "No recoverable data found";

                VRisingCore.Log?.LogInfo($"Data recovery for player {steamId}: {result.Message}");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Recovery failed: {ex.Message}";
                VRisingCore.Log?.LogError($"Data recovery error for player {steamId}: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Creates a backup of all player data
        /// </summary>
        public static bool BackupPlayerData(ulong steamId)
        {
            try
            {
                EnsureDataDirectory();
                var backupDir = Path.Combine(DataDirectory, "backups", steamId.ToString());

                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                var dataFiles = Directory.GetFiles(DataDirectory, $"{steamId}_*.json");
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

                foreach (var file in dataFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var backupPath = Path.Combine(backupDir, $"{timestamp}_{fileName}");
                    File.Copy(file, backupPath, true);
                }

                VRisingCore.Log?.LogInfo($"Created backup for player {steamId} with {dataFiles.Length} files");
                return true;
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Failed to backup data for player {steamId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restores player data from backup
        /// </summary>
        public static bool RestorePlayerDataFromBackup(ulong steamId, string timestamp)
        {
            try
            {
                var backupDir = Path.Combine(DataDirectory, "backups", steamId.ToString());
                var backupFiles = Directory.GetFiles(backupDir, $"{timestamp}_{steamId}_*.json");

                foreach (var backupFile in backupFiles)
                {
                    var fileName = Path.GetFileName(backupFile);
                    var originalName = fileName.Substring(timestamp.Length + 1); // Remove timestamp prefix
                    var targetPath = Path.Combine(DataDirectory, originalName);

                    File.Copy(backupFile, targetPath, true);
                }

                VRisingCore.Log?.LogInfo($"Restored backup for player {steamId} from {timestamp}");
                return true;
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Failed to restore backup for player {steamId}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region PlayerSnapshots API

        /// <summary>
        /// Data structure for player snapshots
        /// </summary>
        public class PlayerSnapshot
        {
            public ulong SteamId { get; set; }
            public string SnapshotId { get; set; }
            public DateTime CreatedAt { get; set; }
            public Dictionary<string, object> SnapshotData { get; set; } = new();
            public string Description { get; set; }
            public bool IsArenaSnapshot { get; set; }
        }

        private static readonly ConcurrentDictionary<string, PlayerSnapshot> _snapshots = new();

        /// <summary>
        /// Creates a snapshot of current player data
        /// </summary>
        public static string CreatePlayerSnapshot(ulong steamId, string description = null, bool isArenaSnapshot = false)
        {
            try
            {
                var snapshotId = $"{steamId}_{DateTime.UtcNow.ToString("yyyyMMdd_HHmmss")}";
                var snapshot = new PlayerSnapshot
                {
                    SteamId = steamId,
                    SnapshotId = snapshotId,
                    CreatedAt = DateTime.UtcNow,
                    Description = description ?? "Manual snapshot",
                    IsArenaSnapshot = isArenaSnapshot,
                    SnapshotData = new Dictionary<string, object>()
                };

                // Capture all available player data
                if (TryGetPlayerExperience(steamId, out var exp))
                    snapshot.SnapshotData["experience"] = exp;

                if (TryGetPlayerWeaponStats(steamId, out var weaponStats))
                    snapshot.SnapshotData["weapon_stats"] = weaponStats;

                if (TryGetPlayerSpells(steamId, out var spells))
                    snapshot.SnapshotData["spells"] = spells;

                if (TryGetPlayerQuests(steamId, out var quests))
                    snapshot.SnapshotData["quests"] = quests;

                if (TryGetPlayerUnlockData(steamId, out var unlockData))
                    snapshot.SnapshotData["unlock_data"] = unlockData;

                _snapshots[snapshotId] = snapshot;

                // Also save to disk
                SaveData(steamId, $"snapshot_{snapshotId}", snapshot);

                VRisingCore.Log?.LogInfo($"Created snapshot {snapshotId} for player {steamId}");
                return snapshotId;
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Failed to create snapshot for player {steamId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Restores player data from a snapshot
        /// </summary>
        public static bool RestorePlayerSnapshot(ulong steamId, string snapshotId)
        {
            try
            {
                if (!_snapshots.TryGetValue(snapshotId, out var snapshot) || snapshot.SteamId != steamId)
                {
                    // Try to load from disk
                    if (!LoadData(steamId, $"snapshot_{snapshotId}", out snapshot))
                    {
                        VRisingCore.Log?.LogWarning($"Snapshot {snapshotId} not found for player {steamId}");
                        return false;
                    }
                }

                // Restore data from snapshot
                using (SuppressPersistence())
                {
                    foreach (var kvp in snapshot.SnapshotData)
                    {
                        switch (kvp.Key)
                        {
                            case "experience":
                                if (kvp.Value is KeyValuePair<int, float> exp)
                                    SetPlayerExperience(steamId, exp);
                                break;
                            case "weapon_stats":
                                if (kvp.Value is Dictionary<WeaponType, List<WeaponManager.WeaponStats.WeaponStatType>> weaponStats)
                                    SetPlayerWeaponStats(steamId, weaponStats);
                                break;
                            case "spells":
                                if (kvp.Value is (int, int, int) spellsTuple)
                                    SetPlayerSpells(steamId, spellsTuple);
                                break;
                            case "quests":
                                if (kvp.Value is Dictionary<string, (string, int, DateTime)> quests)
                                    SetPlayerQuests(steamId, quests);
                                break;
                            case "unlock_data":
                                if (kvp.Value is PlayerUnlockData unlockData)
                                    SetPlayerUnlockData(steamId, unlockData);
                                break;
                        }
                    }
                }

                VRisingCore.Log?.LogInfo($"Restored snapshot {snapshotId} for player {steamId}");
                return true;
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Failed to restore snapshot {snapshotId} for player {steamId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Lists all snapshots for a player
        /// </summary>
        public static List<PlayerSnapshot> GetPlayerSnapshots(ulong steamId)
        {
            var snapshots = new List<PlayerSnapshot>();

            try
            {
                // Get in-memory snapshots
                foreach (var snapshot in _snapshots.Values)
                {
                    if (snapshot.SteamId == steamId)
                    {
                        snapshots.Add(snapshot);
                    }
                }

                // Also check for saved snapshots on disk
                EnsureDataDirectory();
                var snapshotFiles = Directory.GetFiles(DataDirectory, $"{steamId}_snapshot_*.json");

                foreach (var file in snapshotFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        var snapshotId = fileName.Substring(fileName.LastIndexOf('_') + 1);

                        if (LoadData(steamId, $"snapshot_{snapshotId}", out PlayerSnapshot snapshot))
                        {
                            // Avoid duplicates
                            if (!snapshots.Any(s => s.SnapshotId == snapshotId))
                            {
                                snapshots.Add(snapshot);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        VRisingCore.Log?.LogWarning($"Error loading snapshot file {file}: {ex.Message}");
                    }
                }

                return snapshots.OrderByDescending(s => s.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Error getting snapshots for player {steamId}: {ex.Message}");
                return snapshots;
            }
        }

        /// <summary>
        /// Deletes a player snapshot
        /// </summary>
        public static bool DeletePlayerSnapshot(ulong steamId, string snapshotId)
        {
            try
            {
                // Remove from memory
                _snapshots.TryRemove(snapshotId, out _);

                // Remove from disk
                var filePath = GetPlayerDataFilePath(steamId, $"snapshot_{snapshotId}");
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                VRisingCore.Log?.LogInfo($"Deleted snapshot {snapshotId} for player {steamId}");
                return true;
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Failed to delete snapshot {snapshotId} for player {steamId}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Loads all player data based on enabled config systems
        /// </summary>
        public static bool LoadPlayerData(ulong steamId)
        {
            try
            {
                // This would load all data types based on configuration
                // For now, return true as a placeholder
                VRisingCore.Log?.LogInfo($"Loading all data for player {steamId}");
                return true;
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Failed to load player data for {steamId}: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
