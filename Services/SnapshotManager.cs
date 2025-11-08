using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrowbaneArena.Models;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Manages snapshot persistence to disk for crash-safe restore
    /// </summary>
    public static class SnapshotManager
    {
        private static readonly string SnapshotDirectory = Path.Combine("BepInEx", "config", "CrowbaneArena", "Snapshots");
        
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true
        };

        /// <summary>
        /// Initialize snapshot manager (create directory if needed)
        /// </summary>
        public static void Initialize()
        {
            try
            {
                if (!Directory.Exists(SnapshotDirectory))
                {
                    Directory.CreateDirectory(SnapshotDirectory);
                    Plugin.Logger?.LogInfo($"Created snapshot directory: {SnapshotDirectory}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to initialize SnapshotManager: {ex.Message}");
            }
        }

        /// <summary>
        /// Save a snapshot to disk (specific PlayerSnapshot overload kept for compatibility)
        /// </summary>
        public static void SaveSnapshot(string path, PlayerSnapshot snapshot)
        {
            SaveSnapshot<PlayerSnapshot>(path, snapshot);
        }

        /// <summary>
        /// Generic save helper for any serializable object. Stores under the SnapshotDirectory base path.
        /// </summary>
        public static void SaveSnapshot<T>(string path, T data) where T : class
        {
            try
            {
                var fullPath = Path.Combine(SnapshotDirectory, path);
                var directory = Path.GetDirectoryName(fullPath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(fullPath, json);

                Plugin.Logger?.LogInfo($"Saved snapshot to: {fullPath}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to save snapshot to {path}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Load a snapshot from disk
        /// </summary>
        public static T LoadSnapshot<T>(string path) where T : class
        {
            try
            {
                var fullPath = Path.Combine(SnapshotDirectory, path);
                
                if (!File.Exists(fullPath))
                {
                    Plugin.Logger?.LogWarning($"Snapshot file not found: {fullPath}");
                    return null;
                }

                var json = File.ReadAllText(fullPath);
                var snapshot = JsonSerializer.Deserialize<T>(json, JsonOptions);
                
                Plugin.Logger?.LogInfo($"Loaded snapshot from: {fullPath}");
                return snapshot;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to load snapshot from {path}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Delete a snapshot from disk
        /// </summary>
        public static bool DeleteSnapshot(string path)
        {
            try
            {
                var fullPath = Path.Combine(SnapshotDirectory, path);
                
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    Plugin.Logger?.LogInfo($"Deleted snapshot: {fullPath}");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to delete snapshot {path}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a snapshot exists
        /// </summary>
        public static bool SnapshotExists(string path)
        {
            var fullPath = Path.Combine(SnapshotDirectory, path);
            return File.Exists(fullPath);
        }

        /// <summary>
        /// Clear all snapshots
        /// </summary>
        public static void ClearAllSnapshots()
        {
            try
            {
                if (Directory.Exists(SnapshotDirectory))
                {
                    var files = Directory.GetFiles(SnapshotDirectory, "*.json", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }
                    Plugin.Logger?.LogInfo($"Cleared {files.Length} snapshot files");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to clear snapshots: {ex.Message}");
            }
        }

        /// <summary>
        /// Get snapshot file path for a player
        /// </summary>
        public static string GetSnapshotPath(ulong steamId)
        {
            return $"{steamId}.json";
        }
    }
}
