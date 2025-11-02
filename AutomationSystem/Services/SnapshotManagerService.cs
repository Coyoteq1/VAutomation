using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using Unity.Entities;
using Unity.Mathematics;
using CrowbaneArena.Data;

namespace CrowbaneArena.Services
{
    public static class SnapshotManagerService
    {
        private static Dictionary<Guid, PlayerSnapshot> _snapshots = new();
        private static Dictionary<ulong, Guid> _playerCurrentSnapshot = new();
        private static readonly string SnapshotsPath = Path.Combine(Paths.ConfigPath, "CrowbaneArena_Snapshots.json");

        public static void Initialize() => PlayerSnapshotService.Initialize();

        public static bool IsInArena(ulong platformId) => PlayerSnapshotService.IsInArena(platformId);

        public static bool EnterArena(Entity userEntity, Entity characterEntity, float3 arenaLocation, int presetIndex = 0) =>
            PlayerSnapshotService.EnterArena(userEntity, characterEntity, arenaLocation);

        public static bool LeaveArena(ulong platformId, Entity userEntity, Entity characterEntity) =>
            PlayerSnapshotService.LeaveArena(platformId, userEntity, characterEntity);

        public static int GetSnapshotCount() => PlayerSnapshotService.GetSnapshotCount();

        public static void ClearAllSnapshots()
        {
            _snapshots.Clear();
            _playerCurrentSnapshot.Clear();
            if (File.Exists(SnapshotsPath))
            {
                File.Delete(SnapshotsPath);
            }
            Plugin.LogSource?.LogInfo("All snapshots cleared");
        }

        public static Dictionary<Guid, PlayerSnapshot> LoadSnapshot<T>(string path) where T : Dictionary<Guid, PlayerSnapshot>
        {
            try
            {
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    return JsonConvert.DeserializeObject<T>(json) ?? new Dictionary<Guid, PlayerSnapshot>();
                }
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogError($"Error loading snapshot from {path}: {ex.Message}");
            }
            return new Dictionary<Guid, PlayerSnapshot>();
        }

        public static void SaveSnapshot(string path, Dictionary<Guid, PlayerSnapshot> snapshots)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(snapshots, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Plugin.LogSource?.LogError($"Error saving snapshot to {path}: {ex.Message}");
            }
        }
    }
}
