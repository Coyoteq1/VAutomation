using HarmonyLib;
using Unity.Entities;
using Unity.Transforms;
using ProjectM;
using ProjectM.Gameplay.Systems;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using static CrowbaneArena.MathUtils;
using float3 = Unity.Mathematics.float3;

namespace CrowbaneArena
{
    /// <summary>
    /// Tracks player positions and handles zone transitions
    /// </summary>
    [HarmonyPatch]
    public static class PlayerTracker
    {
        private static EntityManager _entityManager;
        private static bool _isInitialized = false;
        private static readonly Dictionary<Entity, float3> _lastPositions = new();
        private const float POSITION_UPDATE_THRESHOLD = 0.1f; // Minimum distance to trigger position update

        /// <summary>
        /// Gets whether PlayerTracker is initialized
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the EntityManager used by PlayerTracker
        /// </summary>
        public static EntityManager EntityManager => _entityManager;

        /// <summary>
        /// Initializes the PlayerTracker with the EntityManager from the current world
        /// </summary>
        public static void Initialize()
        {
            try
            {
                World serverWorld = null;

                // Try to find the server world specifically
                foreach (var world in World.All)
                {
                    if (world.Name == "Server" && world.IsCreated)
                    {
                        serverWorld = world;
                        Plugin.Logger?.LogInfo($"Found V Rising Server World: {world.Name}");
                        break;
                    }
                }

                // Fallback to DefaultGameObjectInjectionWorld
                if (serverWorld == null)
                {
                    serverWorld = World.DefaultGameObjectInjectionWorld;
                    if (serverWorld != null && serverWorld.IsCreated)
                    {
                        Plugin.Logger?.LogWarning($"Using DefaultGameObjectInjectionWorld as fallback: {serverWorld.Name}");
                    }
                }

                if (serverWorld == null || !serverWorld.IsCreated)
                {
                    Plugin.Logger?.LogWarning("No valid server world found, cannot initialize PlayerTracker");
                    return;
                }

                _entityManager = serverWorld.EntityManager;
                _isInitialized = true;
                Plugin.Logger?.LogInfo("PlayerTracker initialized successfully");
            }
            catch (Exception ex)
            {
                _isInitialized = false;
                Plugin.Logger?.LogError($"Error initializing PlayerTracker: {ex.Message}");
                Plugin.Logger?.LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Called on each server update to track player positions
        /// </summary>
        [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUpdate))]
        [HarmonyPostfix]
        public static void OnServerUpdate(ServerBootstrapSystem __instance)
        {
            try
            {
                // Try to initialize if not already done
                if (!_isInitialized)
                {
                    Initialize();
                    if (!_isInitialized)
                    {
                        // Don't spam the logs with warnings if we're still initializing
                        return;
                    }
                    // Now initialize PlayerService since world is ready
                    try
                    {
                        CrowbaneArena.Services.PlayerService.Initialize();
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger?.LogError($"Failed to initialize PlayerService in tracker: {ex.Message}");
                    }
                }

                // Skip if the entity manager is not ready
                try
                {
                    // Check if we can safely use the EntityManager
                    if (!_entityManager.World.IsCreated)
                    {
                        return;
                    }

                    // Get all player characters
                    var playerQuery = _entityManager.CreateEntityQuery(new EntityQueryDesc
                    {
                        All = new[] { ComponentType.ReadOnly<PlayerCharacter>(), ComponentType.ReadOnly<LocalToWorld>() }
                    });

                    var playerEntities = playerQuery.ToEntityArray(Allocator.Temp);
                    try
                    {
                        foreach (var entity in playerEntities)
                        {
                            try
                            {
                                if (!_entityManager.Exists(entity)) continue;

                                var localToWorld = _entityManager.GetComponentData<LocalToWorld>(entity);
                                var currentPos = (float3)localToWorld.Position;

                                // Check if player has moved significantly
                                if (_lastPositions.TryGetValue(entity, out var lastPos))
                                {
                                    if (DistanceSquared(currentPos, lastPos) < POSITION_UPDATE_THRESHOLD * POSITION_UPDATE_THRESHOLD)
                                        continue; // Skip if not moved enough
                                }

                                // Update last position
                                _lastPositions[entity] = currentPos;

                                // Check zone triggers
                                ZoneManager.CheckPlayerZones(entity, currentPos);
                            }
                            catch (Exception ex)
                            {
                                Plugin.Logger?.LogError($"Error in PlayerTracker for entity {entity.Index}: {ex.Message}");
                            }
                        }
                    }
                    finally
                    {
                        playerEntities.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"Error in PlayerTracker update: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in PlayerTracker.OnServerUpdate: {ex.Message}");
            }
        }
    }
}
