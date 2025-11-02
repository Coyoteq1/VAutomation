using System;
using System.Collections.Generic;
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
            // TODO: Re-enable when EventManager is properly implemented
            // EventManager.OnUserConnected += OnPlayerConnected;
            Plugin.Logger?.LogInfo("AutoEnterService initialized (EventManager integration disabled)");
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

        /// <summary>
        /// Handle player connection event
        /// </summary>
        private static void OnPlayerConnected(Entity userEntity)
        {
            var user = EM.GetComponentData<User>(userEntity);
            if (!IsAutoEnterEnabled(user.PlatformId)) return;

            var characterEntity = PlayerService.GetPlayerCharacter(userEntity);
            if (characterEntity == Entity.Null) return;

            // Auto-equip loadout if specified
            if (_autoEquipLoadouts.TryGetValue(user.PlatformId, out var loadoutName))
            {
                // Replace with your loadout equipping logic
                // Example: LoadoutManager.EquipLoadout(characterEntity, loadoutName);
                Plugin.Logger?.LogInfo($"Auto-equipped loadout '{loadoutName}' for {user.CharacterName}");
            }

            // Teleport to arena
            if (TeleportService.TeleportToArena(characterEntity))
            {
                Plugin.Logger?.LogInfo($"Auto-entered {user.CharacterName} to arena");
            }
        }

        /// <summary>
        /// Save auto-enter preferences
        /// </summary>
        private static void SavePreferences()
        {
            try
            {
                // Save to database or file
                // Example: Database.Save("AutoEnterPrefs", new { _autoEnterEnabled, _autoEquipLoadouts });
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
                // Load from database or file
                // Example: var data = Database.Load<...>("AutoEnterPrefs");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error loading auto-enter preferences: {ex.Message}");
            }
        }
    }
}
