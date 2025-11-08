using System;
using System.Threading.Tasks;
using CrowbaneArena.Models;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;

namespace CrowbaneArena.Services
{
    public static class ProgressionService
    {
        private static ISnapshotManager _impl;

        public static void Initialize(ISnapshotManager impl)
        {
            try
            {
                _impl = impl;
                Plugin.Logger?.LogInfo("ProgressionService initialized");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error initializing ProgressionService: {ex.Message}");
            }
        }

        public static async Task<bool> CreateSnapshot(Entity userEntity, Entity characterEntity)
        {
            if (_impl == null) { Plugin.Logger?.LogError("ProgressionService not initialized"); return false; }
            return await _impl.CreateSnapshotAsync(userEntity, characterEntity);
        }

        public static async Task<bool> RestoreSnapshot(Entity userEntity, Entity characterEntity)
        {
            if (_impl == null) { Plugin.Logger?.LogError("ProgressionService not initialized"); return false; }
            return await _impl.RestoreSnapshotAsync(userEntity, characterEntity);
        }

        public static bool HasSnapshot(ulong platformId)
        {
            if (_impl == null) { return false; }
            return _impl.HasSnapshot(platformId);
        }

        public static void DeleteSnapshot(ulong platformId)
        {
            _impl?.DeleteSnapshot(platformId);
        }

        public static void DeleteAllSnapshots()
        {
            _impl?.DeleteAllSnapshots();
        }

        public static ProgressionSnapshot GetSnapshot(ulong platformId)
        {
            if (_impl == null) { return null; }
            return _impl.GetSnapshot(platformId);
        }

        /// <summary>
        /// Overwrite in-memory progression so other systems will see everything unlocked.
        /// This is the "hybrid" approach: it updates runtime systems (debug events, VBlood buffers,
        /// achievements, waypoints, etc.) but does not permanently change on-disk player files.
        /// The original state should still be restored by calling <see cref="RestoreSnapshot"/>.
        /// </summary>
        public static bool OverwriteAllInMemory(Entity userEntity, Entity characterEntity)
        {
            try
            {
                if (userEntity == Entity.Null || characterEntity == Entity.Null)
                {
                    Plugin.Logger?.LogWarning("ProgressionService.OverwriteAllInMemory called with null entities");
                    return false;
                }

                var fromCharacter = new FromCharacter
                {
                    User = userEntity,
                    Character = characterEntity
                };

                try
                {
                    var systemService = new SystemService(VRisingCore.ServerWorld);
                    var debugEventsSystem = systemService.DebugEventsSystem;
                    debugEventsSystem.UnlockAllResearch(fromCharacter);
                    debugEventsSystem.UnlockAllVBloods(fromCharacter);
                    debugEventsSystem.CompleteAllAchievements(fromCharacter);
                }
                catch (Exception sysEx)
                {
                    Plugin.Logger?.LogWarning($"Failed to access DebugEventsSystem for progression overwrite: {sysEx.Message}");
                }

                try
                {
                    BossManager.UnlockAllBosses(characterEntity);
                }
                catch (Exception bEx)
                {
                    Plugin.Logger?.LogWarning($"BossManager.UnlockAllBosses failed during overwrite: {bEx.Message}");
                }

                try
                {
                    Plugin.Logger?.LogWarning("Map/waypoint reveal not implemented in ProgressionService; skipping.");
                }
                catch (Exception mEx)
                {
                    Plugin.Logger?.LogWarning($"Map/waypoint reveal skipped during overwrite: {mEx.Message}");
                }

                Plugin.Logger?.LogInfo("ProgressionService: in-memory progression overwritten (hybrid unlock)");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in OverwriteAllInMemory: {ex.Message}");
                return false;
            }
        }
    }
}