using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service for handling auto-enter and auto-equip functionality
    /// </summary>
    public static class AutoEnterService
    {
        private static EntityManager EM => CrowbaneArenaCore.EntityManager;
        private static readonly Dictionary<ulong, bool> _autoEnterEnabled = new();
        private static readonly Dictionary<ulong, string> _autoEquipLoadouts = new();

        /// <summary>
        /// Initialize the AutoEnterService
        /// </summary>
        public static void Initialize()
        {
            LoadPreferences();
            
            // Wire into EventFramework if available
            try
            {
                // Uncomment and modify these lines based on your event system
                // EventFrameworkApi.OnUserConnected += (userEntity) => TryAutoEnter(userEntity, reason: "Connected");
                // EventFrameworkApi.OnCharacterSpawned += (userEntity, characterEntity) => TryAutoEnter(userEntity, characterEntity, "Spawned");
                // EventFrameworkApi.OnUserDisconnected += (userEntity) => TryAutoExit(userEntity, reason: "Disconnected");
                
                Plugin.Logger?.LogInfo("AutoEnterService initialized with event hooks");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"AutoEnterService initialized without event hooks: {ex.Message}");
            }
        }

        /// <summary>
        /// Enable or disable auto-enter for a player
        /// </summary>
        public static void SetAutoEnter(ulong steamId, bool enabled, string loadoutName = null)
        {
            _autoEnterEnabled[steamId] = enabled;
            
            if (!string.IsNullOrEmpty(loadoutName))
            {
                _autoEquipLoadouts[steamId] = loadoutName;
            }
            else if (enabled)
            {
                _autoEquipLoadouts.Remove(steamId);
            }

            SavePreferences();
            Plugin.Logger?.LogInfo($"Auto-enter {(enabled ? "enabled" : "disabled")} for {steamId}" +
                                 (enabled && !string.IsNullOrEmpty(loadoutName) ? $" with loadout: {loadoutName}" : ""));
        }

        /// <summary>
        /// Check if auto-enter is enabled for a player
        /// </summary>
        public static bool IsAutoEnterEnabled(ulong steamId) => 
            _autoEnterEnabled.GetValueOrDefault(steamId, false);

        /// <summary>
        /// Get the auto-equip loadout for a player
        /// </summary>
        public static string GetAutoEquipLoadout(ulong steamId)
        {
            _autoEquipLoadouts.TryGetValue(steamId, out var loadout);
            return loadout;
        }

        public static bool TryAutoEnter(Entity userEntity, Entity characterEntity = default, string reason = null)
        {
            try
            {
                if (!EM.TryGetComponentData(userEntity, out User user)) 
                {
                    Plugin.Logger?.LogWarning("TryAutoEnter: Invalid user entity");
                    return false;
                }

                if (!IsAutoEnterEnabled(user.PlatformId)) 
                {
                    Plugin.Logger?.LogDebug($"Auto-enter not enabled for {user.CharacterName}");
                    return false;
                }

                if (characterEntity == Entity.Null)
                {
                    characterEntity = user.LocalCharacter.GetEntityOnServer();
                    if (characterEntity == Entity.Null) 
                    {
                        Plugin.Logger?.LogWarning($"TryAutoEnter: Could not find character entity for {user.CharacterName}");
                        return false;
                    }
                }

                // Check if already in arena
                if (SnapshotService.IsInArena(user.PlatformId))
                {
                    Plugin.Logger?.LogInfo($"[AutoEnter] {user.CharacterName} already in arena");
                    return true;
                }

                Plugin.Logger?.LogInfo($"[AutoEnter] Processing auto-enter for {user.CharacterName}, reason: {reason ?? "unknown"}");

                // Step 1: Snapshot boss unlock state BEFORE any mutations
                try
                {
                    BossManager.SnapshotBossUnlockState(characterEntity);
                    Plugin.Logger?.LogDebug($"[AutoEnter] Boss unlock state snapshotted for {user.CharacterName}");
                }
                catch (Exception bex)
                {
                    Plugin.Logger?.LogWarning($"[AutoEnter] Failed to snapshot boss unlocks for {user.CharacterName}: {bex.Message}");
                }

                // Step 2: Create player snapshot and enter arena
                var loadout = GetAutoEquipLoadout(user.PlatformId) ?? "default";
                var spawn = ZoneManager.SpawnPoint;

                bool entered = SnapshotService.EnterArena(userEntity, characterEntity, spawn, loadout);
                if (!entered) 
                {
                    Plugin.Logger?.LogError($"[AutoEnter] SnapshotService.EnterArena failed for {user.CharacterName}");
                    return false;
                }

                // Step 3: Apply zone manager arena logic
                try 
                { 
                    ZoneManager.ManualEnterArena(characterEntity);
                    Plugin.Logger?.LogInfo($"[AutoEnter] Successfully entered arena: {user.CharacterName} (reason: {reason ?? "unknown"})");
                } 
                catch (Exception zex)
                {
                    Plugin.Logger?.LogError($"[AutoEnter] ZoneManager.ManualEnterArena failed for {user.CharacterName}: {zex.Message}");
                    // Try to clean up the snapshot if zone entry failed
                    try { SnapshotService.ExitArena(userEntity, characterEntity); } catch { }
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"TryAutoEnter error for user {userEntity}: {ex.Message}");
                return false;
            }
        }

        public static bool TryAutoExit(Entity userEntity, Entity characterEntity = default, string reason = null)
        {
            try
            {
                if (!EM.TryGetComponentData(userEntity, out User user)) 
                {
                    Plugin.Logger?.LogWarning("TryAutoExit: Invalid user entity");
                    return false;
                }

                if (!SnapshotService.IsInArena(user.PlatformId)) 
                {
                    Plugin.Logger?.LogDebug($"[AutoExit] {user.CharacterName} not in arena");
                    return false;
                }

                if (characterEntity == Entity.Null)
                {
                    characterEntity = user.LocalCharacter.GetEntityOnServer();
                    if (characterEntity == Entity.Null) 
                    {
                        Plugin.Logger?.LogWarning($"TryAutoExit: Could not find character entity for {user.CharacterName}");
                        return false;
                    }
                }

                Plugin.Logger?.LogInfo($"[AutoExit] Processing auto-exit for {user.CharacterName}, reason: {reason ?? "unknown"}");

                // Step 1: Restore player state from snapshot
                bool restored = SnapshotService.ExitArena(userEntity, characterEntity);
                if (!restored)
                {
                    Plugin.Logger?.LogError($"[AutoExit] SnapshotService.ExitArena failed for {user.CharacterName}");
                    return false;
                }

                // Step 2: Apply zone manager exit logic
                try 
                { 
                    ZoneManager.ManualExitArena(characterEntity);
                    Plugin.Logger?.LogInfo($"[AutoExit] Successfully exited arena: {user.CharacterName} (reason: {reason ?? "unknown"})");
                } 
                catch (Exception zex)
                {
                    Plugin.Logger?.LogWarning($"[AutoExit] ZoneManager.ManualExitArena failed for {user.CharacterName}: {zex.Message}");
                    // Continue anyway since snapshot restoration succeeded
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"TryAutoExit error for user {userEntity}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save auto-enter preferences
        /// </summary>
        private static void SavePreferences()
        {
            try
            {
                var payload = new AutoEnterPrefs
                {
                    Enabled = new Dictionary<ulong, bool>(_autoEnterEnabled),
                    Loadouts = new Dictionary<ulong, string>(_autoEquipLoadouts)
                };
                SnapshotManager.SaveSnapshot("AutoEnterPrefs.json", payload);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error saving auto-enter preferences: {ex.Message}");
            }
        }

        /// <summary>
        /// Load auto-enter preferences
        /// </summary>
        private static void LoadPreferences()
        {
            try
            {
                var prefs = SnapshotManager.LoadSnapshot<AutoEnterPrefs>("AutoEnterPrefs.json");
                if (prefs != null)
                {
                    _autoEnterEnabled.Clear();
                    _autoEquipLoadouts.Clear();
                    foreach (var kv in prefs.Enabled) _autoEnterEnabled[kv.Key] = kv.Value;
                    foreach (var kv in prefs.Loadouts) _autoEquipLoadouts[kv.Key] = kv.Value;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error loading auto-enter preferences: {ex.Message}");
            }
        }

        /// <summary>
        /// Get count of players with auto-enter enabled
        /// </summary>
        public static int GetAutoEnterCount()
        {
            return _autoEnterEnabled.Count(kvp => kvp.Value);
        }

        /// <summary>
        /// Get all players with auto-enter enabled
        /// </summary>
        public static Dictionary<ulong, string> GetAutoEnterPlayers()
        {
            var result = new Dictionary<ulong, string>();
            foreach (var kvp in _autoEnterEnabled)
            {
                if (kvp.Value)
                {
                    var loadout = GetAutoEquipLoadout(kvp.Key) ?? "default";
                    result[kvp.Key] = loadout;
                }
            }
            return result;
        }

        /// <summary>
        /// Clear all auto-enter settings
        /// </summary>
        public static void ClearAllAutoEnter()
        {
            _autoEnterEnabled.Clear();
            _autoEquipLoadouts.Clear();
            SavePreferences();
            Plugin.Logger?.LogInfo("All auto-enter settings cleared");
        }

        /// <summary>
        /// Event handlers for connection/disconnection (call these from your event system)
        /// </summary>
        public static void OnUserConnected(Entity userEntity)
        {
            TryAutoEnter(userEntity, reason: "Connected");
        }

        public static void OnCharacterSpawned(Entity userEntity, Entity characterEntity)
        {
            TryAutoEnter(userEntity, characterEntity, "Spawned");
        }

        public static void OnUserDisconnected(Entity userEntity)
        {
            // Create leave snapshot before exiting
            if (CrowbaneArenaCore.EntityManager.TryGetComponentData(userEntity, out User user))
            {
                var characterEntity = user.LocalCharacter.GetEntityOnServer();
                if (characterEntity != Entity.Null)
                {
                    // AutoSnapshotService.CreateLeaveSnapshot(user, characterEntity);
                    // Delete snapshot after creating leave snapshot
                    // AutoSnapshotService.DeletePlayerSnapshotOnExit(user.PlatformId);
                }
            }

            TryAutoExit(userEntity, reason: "Disconnected");
        }

        private class AutoEnterPrefs
        {
            public Dictionary<ulong, bool> Enabled { get; set; } = new();
            public Dictionary<ulong, string> Loadouts { get; set; } = new();
        }
    }
}
