using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using ProjectM;
using ProjectM.Network;
using CrowbaneArena.Models;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service that provides snapshot functionality for arena management
    /// This is a wrapper that delegates to SnapshotManagerService and other services
    /// </summary>
    public static class SnapshotService
    {
        private static readonly HashSet<ulong> _playersInArena = new HashSet<ulong>();
        private static int _snapshotCount = 0;

        /// <summary>
        /// Initialize the snapshot service
        /// </summary>
        public static void Initialize()
        {
            try
            {
                Plugin.Logger?.LogInfo("SnapshotService initialized");
                // Initialize the underlying SnapshotManagerService
                var service = new SnapshotManagerService();
                service.Initialize();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error initializing SnapshotService: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if a player is currently in the arena
        /// </summary>
        public static bool IsInArena(ulong steamId)
        {
            return _playersInArena.Contains(steamId);
        }

        /// <summary>
        /// Enter arena and create a snapshot
        /// </summary>
        public static bool EnterArena(Entity userEntity, Entity characterEntity, Unity.Mathematics.float3 location, string loadoutName = null)
        {
            try
            {
                if (!VRisingCore.EntityManager.TryGetComponentData(userEntity, out User user))
                {
                    Plugin.Logger?.LogError("Failed to get User component in EnterArena");
                    return false;
                }

                var snapshotManager = new SnapshotManagerService();
                var result = snapshotManager.CreateSnapshotAsync(userEntity, characterEntity).Result;
                
                if (result)
                {
                    _playersInArena.Add(user.PlatformId);
                    _snapshotCount++;
                    
                    // Apply arena loadout if specified
                    if (!string.IsNullOrEmpty(loadoutName))
                    {
                        try
                        {
                            LoadoutService.ApplyLoadout(characterEntity, loadoutName);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Logger?.LogWarning($"Failed to apply loadout {loadoutName}: {ex.Message}");
                        }
                    }
                    
                    Plugin.Logger?.LogInfo($"Player {user.CharacterName} entered arena with snapshot");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in EnterArena: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Exit arena and restore from snapshot
        /// </summary>
        public static bool ExitArena(Entity userEntity, Entity characterEntity)
        {
            try
            {
                if (!VRisingCore.EntityManager.TryGetComponentData(userEntity, out User user))
                {
                    Plugin.Logger?.LogError("Failed to get User component in ExitArena");
                    return false;
                }

                var snapshotManager = new SnapshotManagerService();
                var result = snapshotManager.RestoreSnapshotAsync(userEntity, characterEntity).Result;
                
                if (result)
                {
                    _playersInArena.Remove(user.PlatformId);
                    _snapshotCount = Math.Max(0, _snapshotCount - 1);
                    
                    Plugin.Logger?.LogInfo($"Player {user.CharacterName} exited arena and snapshot restored");
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ExitArena: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clear all snapshots
        /// </summary>
        public static void ClearAllSnapshots()
        {
            try
            {
                _playersInArena.Clear();
                _snapshotCount = 0;
                
                var snapshotManager = new SnapshotManagerService();
                snapshotManager.DeleteAllSnapshots();
                
                Plugin.Logger?.LogInfo("All snapshots cleared");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ClearAllSnapshots: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the number of active snapshots
        /// </summary>
        public static int GetSnapshotCount()
        {
            return _snapshotCount;
        }

        /// <summary>
        /// Check if a player has a snapshot on disk
        /// </summary>
        public static bool HasSnapshotOnDisk(ulong steamId)
        {
            try
            {
                var snapshotManager = new SnapshotManagerService();
                return snapshotManager.HasSnapshot(steamId);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error checking snapshot on disk: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get snapshot information for a player
        /// </summary>
        public static SnapshotInfo GetSnapshotInfo(ulong steamId)
        {
            try
            {
                var snapshotManager = new SnapshotManagerService();
                var snapshot = snapshotManager.GetSnapshot(steamId);
                
                if (snapshot != null)
                {
                    return new SnapshotInfo
                    {
                        SteamId = steamId,
                        CharacterName = snapshot.CharacterName,
                        IsInArena = IsInArena(steamId),
                        CreatedAt = DateTime.UtcNow, // Would need to be stored in snapshot
                        HasDiskSnapshot = HasSnapshotOnDisk(steamId)
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error getting snapshot info: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Force cleanup all snapshots
        /// </summary>
        public static void ForceCleanupAllSnapshots()
        {
            try
            {
                ClearAllSnapshots();
                Plugin.Logger?.LogInfo("Force cleanup completed");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in force cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply arena loadout to a character
        /// </summary>
        public static void ApplyArenaLoadout(Entity characterEntity, string loadoutName)
        {
            try
            {
                LoadoutService.ApplyLoadout(characterEntity, loadoutName);
                Plugin.Logger?.LogInfo($"Applied loadout {loadoutName} to character");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error applying loadout: {ex.Message}");
            }
        }

        /// <summary>
        /// Try to apply weapon data to character
        /// </summary>
        public static bool TryApplyWeaponFromData(Entity characterEntity, WeaponData weaponData)
        {
            try
            {
                // Implementation would depend on WeaponData structure
                Plugin.Logger?.LogInfo("TryApplyWeaponFromData called");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error applying weapon: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try to apply armor data to character
        /// </summary>
        public static bool TryApplyArmorFromData(Entity characterEntity, Armors armorData)
        {
            try
            {
                // Implementation would depend on Armors structure
                Plugin.Logger?.LogInfo("TryApplyArmorFromData called");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error applying armor: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Try to apply consumable data to character
        /// </summary>
        public static bool TryApplyConsumableFromData(Entity characterEntity, BuildItemData consumableData)
        {
            try
            {
                // Implementation would depend on BuildItemData structure
                Plugin.Logger?.LogInfo("TryApplyConsumableFromData called");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error applying consumable: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Information about a player snapshot
    /// </summary>
    public class SnapshotInfo
    {
        public ulong SteamId { get; set; }
        public string CharacterName { get; set; }
        public bool IsInArena { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasDiskSnapshot { get; set; }
    }
}