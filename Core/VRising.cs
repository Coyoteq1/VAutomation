using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System;
using Unity.Entities;

namespace CrowbaneArena
{
    /// <summary>
    /// Core utility class for accessing V Rising game systems and components.
    /// Provides centralized access to EntityManager, Server, and other game systems.
    /// Updated to match working ICB.core pattern for proper V Rising 1.1+ compatibility.
    /// </summary>
    public static class VRisingCore
    {
        private static World? _serverWorld;
        private static EntityManager _entityManager;
        private static bool _initialized = false;
        private static ServerGameManager? _serverGameManager;

        /// <summary>
        /// Initialize VRisingCore with proper world detection
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            _initialized = true;
            // Force refresh of world reference
 _serverWorld = null; // This is safe as we've made the field nullable
            _entityManager = default;
            _serverGameManager = null;

            Log?.LogInfo("VRisingCore initialized with working world detection pattern");
        }

        /// <summary>
        /// Gets the V Rising Server World using the working ICB.core pattern
        /// </summary>
        public static World ServerWorld
        {
            get
            {
                // Check if cached world is still valid
                if (_serverWorld != null && _serverWorld.IsCreated)
                {
                    return _serverWorld;
                }

                // Try to find the server world using the working pattern
                try
                {
                    // V Rising 1.1+ uses "Server" as world name (working ICB.core pattern)
                    foreach (var world in World.All)
                    {
                        if (world.Name == "Server" && world.IsCreated)
                        {
                            _serverWorld = world;
                            _entityManager = world.EntityManager;
                            Log?.LogInfo($"Found V Rising Server World: {world.Name} (ICB.core compatible)");
                            return world;
                        }
                    }

                    // Fallback: try DefaultGameObjectInjectionWorld
                    var defaultWorld = World.DefaultGameObjectInjectionWorld;
                    if (defaultWorld != null && defaultWorld.IsCreated)
                    {
                        _serverWorld = defaultWorld;
                        _entityManager = defaultWorld.EntityManager;
                        Log?.LogWarning($"Using DefaultGameObjectInjectionWorld as fallback: {defaultWorld.Name}");
                        return defaultWorld;
                    }
                }
                catch (Exception ex)
                {
                    Log?.LogError($"Error finding Server World: {ex.Message}");
                }

                Log?.LogError("No valid V Rising Server World found!");
                throw new InvalidOperationException("No valid V Rising Server World found!");
            }
        }

        /// <summary>
        /// Gets the EntityManager for the current world using working pattern.
        /// </summary>
        public static EntityManager EntityManager
        {
            get
            {
                // Return cached EntityManager if valid
                if (_entityManager != default && _serverWorld != null && _serverWorld.IsCreated)
                {
                    return _entityManager;
                }

                // Get fresh EntityManager from Server World
                try
                {
                    var server = ServerWorld;
                    _entityManager = server.EntityManager;
                    return _entityManager;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("EntityManager not available - Server World not found", ex);
                }
            }
        }

        /// <summary>
        /// Gets the ServerGameManager for spawning items and managing game state.
        /// </summary>
        public static ServerGameManager ServerGameManager
        {
            get
            {
                if (_serverGameManager == null)
                {
                    InitializeServerGameManager();
                }
                return _serverGameManager ?? throw new InvalidOperationException("ServerGameManager not available");
            }
        }

        /// <summary>
        /// Gets the main Server system.
        /// </summary>
        public static object Server
        {
            get
            {
                if (_serverWorld != null)
                {
                    return _serverWorld;
                }
                return ServerWorld ?? throw new InvalidOperationException("Server not available");
            }
        }

        /// <summary>
        /// Gets the current log source for logging messages.
        /// </summary>
        public static BepInEx.Logging.ManualLogSource Log
        {
            get => Plugin.Logger;
        }

        /// <summary>
        /// Initialize the ServerGameManager from the server systems.
        /// </summary>
        private static void InitializeServerGameManager()
        {
            try
            {
                // Try to get ServerScriptMapper from the correctly identified server world
                var world = ServerWorld;
                if (world != null)
                {
                    var serverScriptMapper = world.GetExistingSystemManaged<ServerScriptMapper>();
                    if (serverScriptMapper != null)
                    {
                        _serverGameManager = serverScriptMapper._ServerGameManager;
                        Log?.LogInfo("ServerGameManager initialized successfully");
                        return;
                    }
                }

                Log?.LogWarning("Could not initialize ServerGameManager - ServerScriptMapper not found");
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error initializing ServerGameManager: {ex.Message}");
            }
        }

        /// <summary>
        /// Force reinitialize all systems (useful after world changes).
        /// </summary>
        public static void Reinitialize()
        {
            Log?.LogInfo("Reinitializing VRisingCore systems...");
 _serverWorld = null; // This is safe as we've made the field nullable
            _entityManager = default;
            _serverGameManager = null;

            // Force refresh of world references
            _ = ServerWorld; // This will trigger initialization
            _ = EntityManager; // This will trigger initialization
            _ = ServerGameManager; // This will trigger initialization

            Log?.LogInfo("VRisingCore systems reinitialized with working pattern");
        }

        /// <summary>
        /// Check if all required systems are available.
        /// </summary>
        public static bool IsReady()
        {
            return _entityManager != default;
        }

        /// <summary>
        /// Get a system of the specified type from the server.
        /// </summary>
        // Commented out due to API compatibility issues - needs proper constraint updates
        /*
        public static T GetSystem<T>() where T : class
        {
            try
            {
                // Try to get the system from the world directly
                var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
                if (world != null)
                {
                    return world.GetExistingSystemManaged<T>();
                }
                return null;
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error getting system {typeof(T).Name}: {ex.Message}");
                return null;
            }
        }
        */

        /// <summary>
        /// Validate that an entity exists and has the required components.
        /// </summary>
        public static bool ValidateEntity(Entity entity, params ComponentType[] requiredComponents)
        {
            if (entity == Entity.Null)
            {
                return false;
            }

            try
            {
                foreach (var componentType in requiredComponents)
                {
                    if (!EntityManager.HasComponent(entity, componentType))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Log?.LogError($"Error validating entity {entity.Index}: {ex.Message}");
                return false;
            }
        }
    }
}
