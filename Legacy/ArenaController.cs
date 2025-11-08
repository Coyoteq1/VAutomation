using System;
using System.Linq;
using ProjectM;
using Unity.Entities;
using Stunlock.Core;
using ProjectM.Network;
using CrowbaneArena.Services;
using static CrowbaneArena.MathUtils;
using float3 = Unity.Mathematics.float3;

namespace CrowbaneArena
{
    /// <summary>
    /// Main controller for CrowbaneArena mod with enhanced progression management.
    /// </summary>
    public class ArenaController
    {
        private static float3 entryPoint = Zero;
        private static float3 exitPoint = Zero;
        private static float3 spawnPoint = Zero;
        
        // Initialize the controller
        static ArenaController()
        {
            // Initialize the snapshot manager
            SnapshotService.Initialize();
        }

        /// <summary>
        /// Gets the singleton instance of the ArenaController.
        /// </summary>
        public static ArenaController Instance { get; private set; } = new ArenaController();

        /// <summary>
        /// Gets the EntityManager instance from the current world.
        /// </summary>
        private static EntityManager EntityManager => World.DefaultGameObjectInjectionWorld.EntityManager;

        /// <summary>
        /// Handles player entering the arena with enhanced progression capture.
        /// </summary>
        public void OnPlayerEnterArena(Entity userEntity, Entity characterEntity)
        {
            try
            {
                Plugin.Logger?.LogInfo("=== ARENA ENTRY STARTED ===");

                // Get arena spawn location from configuration
                var arenaLocation = GetDefaultArenaLocation();

                // Get player's Steam ID and name
                var user = EntityManager.GetComponentData<User>(userEntity);
                
                // Use SnapshotService to handle arena entry with proper state management
                bool success = SnapshotService.EnterArena(
                    userEntity: userEntity,
                    characterEntity: characterEntity,
                    location: arenaLocation,
                    loadoutName: "default"
                );

                if (success)
                {
                    Plugin.Logger?.LogInfo($"=== ARENA ENTRY COMPLETED FOR {user.CharacterName} ===");
                    ArenaHook.MarkPlayerEnteredArena();
                }
                else
                {
                    Plugin.Logger?.LogError("=== ARENA ENTRY FAILED ===");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"Critical error in OnPlayerEnterArena: {ex.Message}");
                Plugin.Logger?.LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Handles player exiting the arena with enhanced progression restoration.
        /// </summary>
        public void OnPlayerExitArena(Entity userEntity, Entity characterEntity)
        {
            try
            {
                Plugin.Logger?.LogInfo("=== ARENA EXIT STARTED ===");

                // Get player's Steam ID and name
                var user = EntityManager.GetComponentData<User>(userEntity);
                
                // Use SnapshotService to handle arena exit with proper state restoration
                bool success = SnapshotService.ExitArena(userEntity, characterEntity);

                if (success)
                {
                    Plugin.Logger?.LogInfo($"=== ARENA EXIT COMPLETED FOR {user.CharacterName} ===");
                    ArenaHook.MarkPlayerExitedArena();
                }
                else
                {
                    Plugin.Logger?.LogError("=== ARENA EXIT FAILED ===");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"Critical error in OnPlayerExitArena: {ex.Message}");
                Plugin.Logger?.LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Get the default arena spawn location from configuration.
        /// </summary>
        private float3 GetDefaultArenaLocation()
        {
            if (ArenaConfigurationService.ArenaSettings?.Zones != null && ArenaConfigurationService.ArenaSettings.Zones.Count > 0)
            {
                var defaultZone = ArenaConfigurationService.ArenaSettings.Zones.FirstOrDefault(z => z.Enabled);
                if (defaultZone != null)
                {
                    return new float3(defaultZone.SpawnX, defaultZone.SpawnY, defaultZone.SpawnZ);
                }
            }

            // Fallback to hardcoded location if config not available
            return new float3(-1000f, 0f, -500f);
        }

        /// <summary>
        /// Check if a player is currently in the arena.
        /// </summary>
        public bool IsPlayerInArena(ulong platformId)
        {
            return SnapshotService.IsInArena(platformId);
        }

        /// <summary>
        /// Get the number of active arena snapshots.
        /// </summary>
        public int GetActiveSnapshotCount()
        {
            // This is a placeholder - SnapshotService doesn't expose a direct count
            // of active arena players. We could add this to SnapshotService if needed.
            // For now, we'll return 0 to indicate we don't have this information.
            return 0;
        }

        /// <summary>
        /// Force clear all arena snapshots (for admin use).
        /// </summary>
        public void ClearAllSnapshots()
        {
            SnapshotService.ClearAllSnapshots();
            Plugin.Logger?.LogInfo("All arena snapshots have been cleared.");
        }

        // Removed SimulateArenaEntry and SimulateArenaExit as they are no longer needed
        // The functionality is now handled by SnapshotService

        /// <summary>
        /// Set arena zone radius (static method for commands)
        /// </summary>
        public static void SetZoneRadius(float radius)
        {
            Plugin.Logger?.LogInfo($"Arena zone radius set to: {radius}");
            // TODO: Implement actual zone radius setting
        }

        /// <summary>
        /// Set entry point location and radius (static method for commands)
        /// </summary>
        public static void SetEntryPoint(float3 position, float radius)
        {
            entryPoint = position;
            Plugin.Logger?.LogInfo($"Entry point set at {position} with radius {radius}");
            // TODO: Implement actual entry point setting
        }

        /// <summary>
        /// Set exit point location and radius (static method for commands)
        /// </summary>
        public static void SetExitPoint(float3 position, float radius)
        {
            exitPoint = position;
            Plugin.Logger?.LogInfo($"Exit point set at {position} with radius {radius}");
            // TODO: Implement actual exit point setting
        }

        /// <summary>
        /// Set arena spawn point (static method for commands)
        /// </summary>
        public static void SetSpawnPoint(float3 position)
        {
            ZoneManager.SetSpawnPoint(position);
            spawnPoint = position; // Keep local copy for backward compatibility if needed
            Plugin.Logger?.LogInfo($"Arena spawn point set at: {position}");
        }

        /// <summary>
        /// Get the spawn point position.
        /// </summary>
        public static float3 GetSpawnPoint() => ZoneManager.SpawnPoint;



        /// <summary>
        /// Check if player is in arena (static method for commands)
        /// </summary>
        public static bool IsPlayerInArena(Entity player)
        {
            return PlayerManager.GetPlayerState(player).IsInArena;
        }

        /// <summary>
        /// Get the entry point position.
        /// </summary>
        public static float3 GetEntryPoint() => entryPoint;

        /// <summary>
        /// Get the exit point position.
        /// </summary>
        public static float3 GetExitPoint() => exitPoint;

        /// <summary>
        /// Get current arena status (static method for commands)
        /// </summary>
        public static string GetArenaStatus()
        {
            return "Arena Available";
        }
    }
}
