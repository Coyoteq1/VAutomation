using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using ProjectM;
using ProjectM.Network;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service for performance optimizations including entity query caching and string operation reduction.
    /// Provides efficient entity queries and reduces memory allocations.
    /// </summary>
    public static class PerformanceOptimizationService
    {
        private static readonly Dictionary<Type, EntityQuery> _cachedQueries = new Dictionary<Type, EntityQuery>();
        private static readonly object _queryLock = new object();
        private static EntityManager _entityManager;
        private static bool _isInitialized = false;

        /// <summary>
        /// Initializes the performance optimization service.
        /// </summary>
        public static void Initialize(EntityManager entityManager)
        {
            if (_isInitialized)
                return;

            try
            {
                _entityManager = entityManager;
                _isInitialized = true;
                
                // Pre-warm common queries
                WarmupCommonQueries();
                
                Plugin.Logger?.LogInfo("PerformanceOptimizationService initialized with query caching");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to initialize PerformanceOptimizationService: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a cached entity query for the specified component type.
        /// </summary>
        /// <typeparam name="T">Component type to query</typeparam>
        /// <returns>Cached entity query</returns>
        public static EntityQuery GetCachedQuery<T>() where T : IComponentData
        {
            var componentType = typeof(T);
            
            lock (_queryLock)
            {
                if (_cachedQueries.TryGetValue(componentType, out var cachedQuery))
                {
                    return cachedQuery;
                }

                // Create new query and cache it
                var newQuery = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
                _cachedQueries[componentType] = newQuery;
                
                return newQuery;
            }
        }

        /// <summary>
        /// Efficiently gets all player character entities with minimal allocations.
        /// </summary>
        /// <returns>Array of player character entities</returns>
        public static Entity[] GetAllPlayerCharacters()
        {
            try
            {
                var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<PlayerCharacter>());
                var entities = query.ToEntityArray(Allocator.Temp);
                var result = entities.ToArray();
                entities.Dispose();
                return result;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to get player characters: {ex.Message}");
                return Array.Empty<Entity>();
            }
        }

        /// <summary>
        /// Efficiently gets all connected user entities with minimal allocations.
        /// </summary>
        /// <returns>Array of connected user entities</returns>
        public static Entity[] GetAllConnectedUsers()
        {
            try
            {
                var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());
                var allUsers = query.ToEntityArray(Allocator.Temp);
                
                // Filter for connected users without additional allocations
                var connectedUsers = new NativeList<Entity>(Allocator.Temp);
                
                for (int i = 0; i < allUsers.Length; i++)
                {
                    var userEntity = allUsers[i];
                    if (_entityManager.HasComponent<User>(userEntity))
                    {
                        var user = _entityManager.GetComponentData<User>(userEntity);
                        if (user.IsConnected)
                        {
                            connectedUsers.Add(ref userEntity);
                        }
                    }
                }

                var result = new Entity[connectedUsers.Length];
                for (int i = 0; i < connectedUsers.Length; i++)
                {
                    result[i] = connectedUsers[i];
                }
                
                connectedUsers.Dispose();
                allUsers.Dispose();
                
                return result;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to get connected users: {ex.Message}");
                return Array.Empty<Entity>();
            }
        }

        /// <summary>
        /// Finds players by name using optimized string comparison.
        /// </summary>
        /// <param name="name">Player name to search for</param>
        /// <returns>Array of matching user entities</returns>
        public static Entity[] FindPlayersByNameOptimized(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Array.Empty<Entity>();

            try
            {
                var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());
                var allUsers = query.ToEntityArray(Allocator.Temp);
                var lowerSearchName = name.Trim().ToLowerInvariant();
                
                var matches = new NativeList<Entity>(Allocator.Temp);
                
                for (int i = 0; i < allUsers.Length; i++)
                {
                    var userEntity = allUsers[i];
                    if (_entityManager.HasComponent<User>(userEntity))
                    {
                        var user = _entityManager.GetComponentData<User>(userEntity);
                        
                        // Use ordinal comparison for performance
                        if (user.CharacterName.ToString().Equals(lowerSearchName, StringComparison.OrdinalIgnoreCase) ||
                            user.CharacterName.ToString().Contains(lowerSearchName, StringComparison.OrdinalIgnoreCase))
                        {
                            matches.Add(ref userEntity);
                        }
                    }
                }

                var result = new Entity[matches.Length];
                for (int i = 0; i < matches.Length; i++)
                {
                    result[i] = matches[i];
                }
                
                matches.Dispose();
                allUsers.Dispose();
                
                return result;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to find players by name: {ex.Message}");
                return Array.Empty<Entity>();
            }
        }

        /// <summary>
        /// Finds players by Steam ID efficiently.
        /// </summary>
        /// <param name="steamId">Steam ID to search for</param>
        /// <returns>The user entity if found, Entity.Null otherwise</returns>
        public static Entity FindPlayerBySteamId(ulong steamId)
        {
            try
            {
                var query = _entityManager.CreateEntityQuery(ComponentType.ReadOnly<User>());
                var allUsers = query.ToEntityArray(Allocator.Temp);
                
                for (int i = 0; i < allUsers.Length; i++)
                {
                    var userEntity = allUsers[i];
                    if (_entityManager.HasComponent<User>(userEntity))
                    {
                        var user = _entityManager.GetComponentData<User>(userEntity);
                        if (user.PlatformId == steamId)
                        {
                            allUsers.Dispose();
                            return userEntity;
                        }
                    }
                }

                allUsers.Dispose();
                return Entity.Null;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to find player by Steam ID: {ex.Message}");
                return Entity.Null;
            }
        }

        /// <summary>
        /// Gets player information batch for multiple entities efficiently.
        /// </summary>
        /// <param name="userEntities">Array of user entities</param>
        /// <returns>Array of player info objects</returns>
        public static PlayerInfo[] GetPlayerInfoBatch(Entity[] userEntities)
        {
            if (userEntities == null || userEntities.Length == 0)
                return Array.Empty<PlayerInfo>();

            var results = new List<PlayerInfo>(userEntities.Length);

            foreach (var userEntity in userEntities)
            {
                if (userEntity == Entity.Null || !_entityManager.HasComponent<User>(userEntity))
                    continue;

                try
                {
                    var user = _entityManager.GetComponentData<User>(userEntity);
                    var characterEntity = user.LocalCharacter._Entity;
                    
                    var playerInfo = new PlayerInfo
                    {
                        UserEntity = userEntity,
                        CharacterEntity = characterEntity,
                        SteamId = user.PlatformId,
                        PlayerName = user.CharacterName.ToString(),
                        IsConnected = user.IsConnected,
                        IsAdmin = user.IsAdmin
                    };

                    results.Add(playerInfo);
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"Failed to get player info for entity {userEntity}: {ex.Message}");
                }
            }

            return results.ToArray();
        }

        /// <summary>
        /// Performs optimized batch validation of entities.
        /// </summary>
        /// <param name="entities">Array of entities to validate</param>
        /// <returns>Array of validation results</returns>
        public static ValidationResult[] ValidateEntitiesBatch(Entity[] entities)
        {
            if (entities == null)
                return Array.Empty<ValidationResult>();

            var results = new ValidationResult[entities.Length];

            for (int i = 0; i < entities.Length; i++)
            {
                results[i] = EntityValidationService.ValidateEntity(entities[i]);
            }

            return results;
        }

        /// <summary>
        /// Gets performance statistics for the optimization service.
        /// </summary>
        /// <returns>Performance statistics</returns>
        public static PerformanceStats GetPerformanceStats()
        {
            return new PerformanceStats
            {
                CachedQueryCount = _cachedQueries.Count,
                EntityManagerReady = _entityManager != default,
                IsInitialized = _isInitialized
            };
        }

        private static void WarmupCommonQueries()
        {
            try
            {
                // Pre-warm frequently used queries - removed due to type constraints
                Plugin.Logger?.LogInfo("Query warmup disabled - using direct queries instead");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to warmup queries: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all cached queries (use with caution).
        /// </summary>
        public static void ClearCachedQueries()
        {
            lock (_queryLock)
            {
                foreach (var query in _cachedQueries.Values)
                {
                    try
                    {
                        query.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger?.LogError($"Failed to dispose cached query: {ex.Message}");
                    }
                }

                _cachedQueries.Clear();
                Plugin.Logger?.LogInfo("Cleared all cached queries");
            }
        }

        /// <summary>
        /// Performs memory cleanup and returns statistics.
        /// </summary>
        /// <returns>Memory cleanup statistics</returns>
        public static MemoryCleanupStats PerformMemoryCleanup()
        {
            var beforeGC = GC.GetTotalMemory(false);
            
            // Force collection of generation 0
            GC.Collect(0);
            
            var afterGC = GC.GetTotalMemory(false);
            var freedMemory = beforeGC - afterGC;

            return new MemoryCleanupStats
            {
                MemoryBeforeGC = beforeGC,
                MemoryAfterGC = afterGC,
                MemoryFreed = freedMemory,
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            };
        }
    }

    /// <summary>
    /// Performance statistics for the optimization service.
    /// </summary>
    public class PerformanceStats
    {
        public int CachedQueryCount { get; set; }
        public bool EntityManagerReady { get; set; }
        public bool IsInitialized { get; set; }
    }

    /// <summary>
    /// Memory cleanup statistics.
    /// </summary>
    public class MemoryCleanupStats
    {
        public long MemoryBeforeGC { get; set; }
        public long MemoryAfterGC { get; set; }
        public long MemoryFreed { get; set; }
        public int Gen0Collections { get; set; }
        public int Gen1Collections { get; set; }
        public int Gen2Collections { get; set; }
    }

}
