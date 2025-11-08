using ProjectM.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Collections;
using Unity.Entities;
using ProjectM;
using CrowbaneArena.Models;
using Unity.Transforms;
using Unity.Mathematics;

namespace CrowbaneArena.Services;

/// <summary>
/// Service for player management and queries with performance caching
/// Updated for V Rising 1.1+ compatibility
/// </summary>
public static class PlayerService
{
    private static EntityManager EM => PlayerTracker.IsInitialized ? PlayerTracker.EntityManager : CrowbaneArenaCore.EntityManager;

    #region Cache Implementation

    private static readonly object _cacheLock = new();
    private static readonly Dictionary<ulong, Entity> _steamIdToUserCache = new();
    private static readonly Dictionary<string, Entity> _nameToUserCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<Entity, Entity> _characterToUserCache = new();
    private static readonly Dictionary<Entity, PlayerBasicInfo> _playerInfoCache = new();
    private static readonly HashSet<Entity> _onlinePlayersCache = new();
    private static DateTime _lastCacheRefresh = DateTime.MinValue;
    private static readonly TimeSpan _cacheExpiry = TimeSpan.FromSeconds(30); // Cache expires every 30 seconds

    private class PlayerBasicInfo
    {
        public string Name { get; set; } = "Unknown";
        public ulong SteamId { get; set; }
        public bool IsOnline { get; set; }
        public bool IsAdmin { get; set; }
        public Entity UserEntity { get; set; }
        public Entity CharacterEntity { get; set; }
        public DateTime LastUpdated { get; set; }

        public bool IsExpired(TimeSpan expiry)
        {
            return DateTime.UtcNow - LastUpdated > expiry;
        }
    }

    internal static void Initialize()
    {
        try
        {
            lock (_cacheLock)
            {
                _steamIdToUserCache.Clear();
                _nameToUserCache.Clear();
                _characterToUserCache.Clear();
                _playerInfoCache.Clear();
                _onlinePlayersCache.Clear();
                _lastCacheRefresh = DateTime.UtcNow;

                Plugin.Logger?.LogInfo("PlayerService caching system initialized");
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error initializing PlayerService cache: {ex.Message}");
        }
    }

    internal static void UpdatePlayerCache(Entity userEntity, string oldName, string newName, bool forceOffline = false)
    {
        try
        {
            lock (_cacheLock)
            {
                if (!EM.HasComponent<User>(userEntity)) return;

                var user = EM.GetComponentData<User>(userEntity);
                var steamId = user.PlatformId;
                var name = user.CharacterName.ToString();

                // Update cache entries
                if (steamId > 0)
                {
                    if (user.IsConnected || forceOffline)
                    {
                        _steamIdToUserCache[steamId] = userEntity;
                    }
                    else
                    {
                        _steamIdToUserCache.Remove(steamId);
                    }
                }

                if (!string.IsNullOrEmpty(name))
                {
                    if (user.IsConnected || forceOffline)
                    {
                        _nameToUserCache[name] = userEntity;
                    }
                    else
                    {
                        _nameToUserCache.Remove(name);
                    }
                }

                // Update character mapping
                var characterEntity = user.LocalCharacter._Entity;
                if (characterEntity != Entity.Null)
                {
                    if (user.IsConnected || forceOffline)
                    {
                        _characterToUserCache[characterEntity] = userEntity;
                    }
                    else
                    {
                        _characterToUserCache.Remove(characterEntity);
                    }
                }

                // Update player info cache
                var info = new PlayerBasicInfo
                {
                    Name = name,
                    SteamId = steamId,
                    IsOnline = user.IsConnected,
                    IsAdmin = user.IsAdmin,
                    UserEntity = userEntity,
                    CharacterEntity = characterEntity,
                    LastUpdated = DateTime.UtcNow
                };

                if (user.IsConnected || forceOffline)
                {
                    _playerInfoCache[userEntity] = info;

                    if (user.IsConnected)
                    {
                        _onlinePlayersCache.Add(userEntity);
                    }
                    else
                    {
                        _onlinePlayersCache.Remove(userEntity);
                    }
                }
                else
                {
                    _playerInfoCache.Remove(userEntity);
                    _onlinePlayersCache.Remove(userEntity);
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error updating player cache: {ex.Message}");
        }
    }

    private static void RefreshCacheIfNeeded()
    {
        try
        {
            if (DateTime.UtcNow - _lastCacheRefresh < _cacheExpiry)
                return;

            Plugin.Logger?.LogDebug("Refreshing PlayerService cache...");

            lock (_cacheLock)
            {
                _steamIdToUserCache.Clear();
                _nameToUserCache.Clear();
                _characterToUserCache.Clear();
                _playerInfoCache.Clear();
                _onlinePlayersCache.Clear();

                var query = EM.CreateEntityQuery(ComponentType.ReadOnly<User>());
                var userEntities = query.ToEntityArray(Allocator.Temp);

                try
                {
                    foreach (var userEntity in userEntities)
                    {
                        if (EM.HasComponent<User>(userEntity))
                        {
                            var user = EM.GetComponentData<User>(userEntity);
                            if (user.IsConnected)
                            {
                                var steamId = user.PlatformId;
                                var name = user.CharacterName.ToString();
                                var characterEntity = user.LocalCharacter._Entity;

                                if (steamId > 0)
                                    _steamIdToUserCache[steamId] = userEntity;

                                if (!string.IsNullOrEmpty(name))
                                    _nameToUserCache[name] = userEntity;

                                if (characterEntity != Entity.Null)
                                    _characterToUserCache[characterEntity] = userEntity;

                                var info = new PlayerBasicInfo
                                {
                                    Name = name,
                                    SteamId = steamId,
                                    IsOnline = user.IsConnected,
                                    IsAdmin = user.IsAdmin,
                                    UserEntity = userEntity,
                                    CharacterEntity = characterEntity,
                                    LastUpdated = DateTime.UtcNow
                                };

                                _playerInfoCache[userEntity] = info;
                                _onlinePlayersCache.Add(userEntity);
                            }
                        }
                    }
                }
                finally
                {
                    userEntities.Dispose();
                }

                _lastCacheRefresh = DateTime.UtcNow;
            }

            Plugin.Logger?.LogDebug($"PlayerService cache refreshed - {_steamIdToUserCache.Count} Steam IDs, {_nameToUserCache.Count} names, {_onlinePlayersCache.Count} online players");
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error refreshing cache: {ex.Message}");
        }
    }

    #endregion

    #region Player Queries

    /// <summary>
    /// Get player character entity from user entity
    /// </summary>
    public static Entity GetPlayerCharacter(Entity userEntity)
    {
        try
        {
            if (userEntity == Entity.Null)
                return Entity.Null;

            RefreshCacheIfNeeded();

            // Try cache first
            lock (_cacheLock)
            {
                if (_playerInfoCache.TryGetValue(userEntity, out var info))
                {
                    if (!info.IsExpired(_cacheExpiry))
                    {
                        return info.CharacterEntity;
                    }
                }
            }

            // Fallback to direct query
            if (!EM.HasComponent<User>(userEntity))
                return Entity.Null;

            var user = EM.GetComponentData<User>(userEntity);
            return user.LocalCharacter._Entity;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error getting player character: {ex.Message}");
            return Entity.Null;
        }
    }

    /// <summary>
    /// Get user entity from character entity
    /// </summary>
    public static Entity GetUserFromCharacter(Entity characterEntity)
    {
        try
        {
            if (characterEntity == Entity.Null)
                return Entity.Null;

            RefreshCacheIfNeeded();

            // Try cache first
            lock (_cacheLock)
            {
                if (_characterToUserCache.TryGetValue(characterEntity, out var userEntity))
                {
                    return userEntity;
                }
            }

            // Fallback to direct query
            if (!EM.HasComponent<PlayerCharacter>(characterEntity))
                return Entity.Null;

            var playerCharacter = EM.GetComponentData<PlayerCharacter>(characterEntity);
            return playerCharacter.UserEntity;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error getting user from character: {ex.Message}");
            return Entity.Null;
        }
    }

    /// <summary>
    /// Get player name from user entity
    /// </summary>
    public static string GetPlayerName(Entity userEntity)
    {
        try
        {
            if (userEntity == Entity.Null)
                return "Unknown";

            RefreshCacheIfNeeded();

            // Try cache first
            lock (_cacheLock)
            {
                if (_playerInfoCache.TryGetValue(userEntity, out var info))
                {
                    if (!info.IsExpired(_cacheExpiry))
                    {
                        return info.Name;
                    }
                }
            }

            // Fallback to direct query
            if (!EM.HasComponent<User>(userEntity))
                return "Unknown";

            var user = EM.GetComponentData<User>(userEntity);
            return user.CharacterName.ToString();
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error getting player name: {ex.Message}");
            return "Unknown";
        }
    }

    /// <summary>
    /// Get player Steam ID
    /// </summary>
    public static ulong GetSteamId(Entity userEntity)
    {
        try
        {
            if (userEntity == Entity.Null)
                return 0;

            RefreshCacheIfNeeded();

            // Try cache first
            lock (_cacheLock)
            {
                if (_playerInfoCache.TryGetValue(userEntity, out var info))
                {
                    if (!info.IsExpired(_cacheExpiry))
                    {
                        return info.SteamId;
                    }
                }
            }

            // Fallback to direct query
            if (!EM.HasComponent<User>(userEntity))
                return 0;

            var user = EM.GetComponentData<User>(userEntity);
            return user.PlatformId;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error getting Steam ID: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Check if player is online
    /// </summary>
    public static bool IsPlayerOnline(Entity userEntity)
    {
        try
        {
            if (userEntity == Entity.Null)
                return false;

            RefreshCacheIfNeeded();

            // Try cache first
            lock (_cacheLock)
            {
                if (_playerInfoCache.TryGetValue(userEntity, out var info))
                {
                    if (!info.IsExpired(_cacheExpiry))
                    {
                        return info.IsOnline;
                    }
                }
            }

            // Fallback to direct query
            if (!EM.HasComponent<User>(userEntity))
                return false;

            var user = EM.GetComponentData<User>(userEntity);
            return user.IsConnected;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error checking if player is online: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if player is admin
    /// </summary>
    public static bool IsPlayerAdmin(Entity userEntity)
    {
        try
        {
            if (userEntity == Entity.Null)
                return false;

            RefreshCacheIfNeeded();

            // Try cache first
            lock (_cacheLock)
            {
                if (_playerInfoCache.TryGetValue(userEntity, out var info))
                {
                    if (!info.IsExpired(_cacheExpiry))
                    {
                        return info.IsAdmin;
                    }
                }
            }

            // Fallback to direct query
            if (!EM.HasComponent<User>(userEntity))
                return false;

            var user = EM.GetComponentData<User>(userEntity);
            return user.IsAdmin;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error checking if player is admin: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get all online players
    /// </summary>
    public static List<Entity> GetOnlinePlayers()
    {
        var players = new List<Entity>();

        try
        {
            RefreshCacheIfNeeded();

            lock (_cacheLock)
            {
                players.AddRange(_onlinePlayersCache);
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error getting online players from cache: {ex.Message}");

            // Fallback to direct query
            try
            {
                if (EM == default)
                    return players;

                var query = EM.CreateEntityQuery(ComponentType.ReadOnly<User>());
                var userEntities = query.ToEntityArray(Allocator.Temp);

                try
                {
                    foreach (var userEntity in userEntities)
                    {
                        if (IsPlayerOnline(userEntity))
                        {
                            players.Add(userEntity);
                        }
                    }
                }
                finally
                {
                    userEntities.Dispose();
                }
            }
            catch (Exception fallbackEx)
            {
                Plugin.Logger?.LogError($"Error getting online players (fallback): {fallbackEx.Message}");
            }
        }

        return players;
    }

    /// <summary>
    /// Find user by Steam ID
    /// </summary>
    public static Entity FindUserBySteamId(ulong steamId)
    {
        try
        {
            if (steamId == 0)
                return Entity.Null;

            RefreshCacheIfNeeded();

            // Try cache first
            lock (_cacheLock)
            {
                if (_steamIdToUserCache.TryGetValue(steamId, out var userEntity))
                {
                    return userEntity;
                }
            }

            // Fallback to direct query
            if (EM == default)
                return Entity.Null;

            var query = EM.CreateEntityQuery(ComponentType.ReadOnly<User>());
            var userEntities = query.ToEntityArray(Allocator.Temp);

            try
            {
                foreach (var userEntity in userEntities)
                {
                    if (GetSteamId(userEntity) == steamId)
                    {
                        return userEntity;
                    }
                }
            }
            finally
            {
                userEntities.Dispose();
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error finding user by Steam ID: {ex.Message}");
        }

        return Entity.Null;
    }

    #endregion

    /// <summary>
    /// Get player health
    /// </summary>
    public static (float current, float max) GetPlayerHealth(Entity characterEntity)
    {
        try
        {
            if (EM == default || characterEntity == Entity.Null || !EM.HasComponent<Health>(characterEntity))
                return (0, 0);

            var health = EM.GetComponentData<Health>(characterEntity);
            return (health.Value, health.MaxHealth._Value);
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error getting player health: {ex.Message}");
            return (0, 0);
        }
    }

    /// <summary>
    /// Get player position
    /// </summary>
    public static float3 GetPlayerPosition(Entity characterEntity)
    {
        try
        {
            if (EM == default || characterEntity == Entity.Null)
                return float3.zero;

            // Try LocalToWorld first (most common)
            if (EM.HasComponent<LocalToWorld>(characterEntity))
            {
                var ltw = EM.GetComponentData<LocalToWorld>(characterEntity);
                return ltw.Position;
            }

            // Fallback to Translation component
            if (EM.HasComponent<Translation>(characterEntity))
            {
                var translation = EM.GetComponentData<Translation>(characterEntity);
                return translation.Value;
            }

            return float3.zero;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error getting player position: {ex.Message}");
            return float3.zero;
        }
    }

    /// <summary>
    /// Get player level (gear level)
    /// </summary>
    public static int GetPlayerLevel(Entity characterEntity)
    {
        try
        {
            if (EM == default || characterEntity == Entity.Null || !EM.HasComponent<Equipment>(characterEntity))
                return 0;

            var equipment = EM.GetComponentData<Equipment>(characterEntity);
            return (int)Math.Round(equipment.GetFullLevel());
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error getting player level: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Set player position (teleport)
    /// </summary>
    public static bool SetPlayerPosition(Entity characterEntity, float3 position)
    {
        try
        {
            if (EM == default || characterEntity == Entity.Null)
                return false;

            // Update Translation component
            if (EM.HasComponent<Translation>(characterEntity))
            {
                EM.SetComponentData(characterEntity, new Translation { Value = position });
            }

            // Update LastTranslation for proper teleportation
            if (EM.HasComponent<LastTranslation>(characterEntity))
            {
                EM.SetComponentData(characterEntity, new LastTranslation { Value = position });
            }

            return true;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error setting player position: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if entity is a valid player character
    /// </summary>
    public static bool IsValidPlayerCharacter(Entity characterEntity)
    {
        try
        {
            return EM != default &&
                   characterEntity != Entity.Null &&
                   EM.HasComponent<PlayerCharacter>(characterEntity);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if entity is a valid user
    /// </summary>
    public static bool IsValidUser(Entity userEntity)
    {
        try
        {
            return EM != default &&
                   userEntity != Entity.Null &&
                   EM.HasComponent<User>(userEntity);
        }
        catch
        {
            return false;
        }
    }

    #region Legacy Methods (for backward compatibility)

    /// <summary>
    /// Attempts to find a player by their Steam ID.
    /// </summary>
    public static bool TryFindBySteam(ulong steamId, out PlayerData playerData)
    {
        playerData = default;

        try
        {
            if (steamId == 0)
                return false;

            RefreshCacheIfNeeded();

            // Try cache first
            lock (_cacheLock)
            {
                if (_steamIdToUserCache.TryGetValue(steamId, out var userEntity))
                {
                    if (EM.HasComponent<User>(userEntity))
                    {
                        var user = EM.GetComponentData<User>(userEntity);
                        var characterEntity = user.LocalCharacter._Entity;
                        playerData = new PlayerData(
                            GetPlayerName(userEntity),
                            steamId,
                            user.IsConnected,
                            userEntity,
                            characterEntity
                        );
                        return true;
                    }
                }
            }

            // Fallback to direct query
            if (EM == default)
                return false;

            var query = EM.CreateEntityQuery(ComponentType.ReadOnly<User>());
            var userEntities = query.ToEntityArray(Allocator.Temp);

            try
            {
                foreach (var userEntity in userEntities)
                {
                    if (EM.HasComponent<User>(userEntity))
                    {
                        var user = EM.GetComponentData<User>(userEntity);
                        if (user.PlatformId == steamId)
                        {
                            var characterEntity = user.LocalCharacter._Entity;
                            playerData = new PlayerData(
                                GetPlayerName(userEntity),
                                steamId,
                                user.IsConnected,
                                userEntity,
                                characterEntity
                            );
                            return true;
                        }
                    }
                }
            }
            finally
            {
                userEntities.Dispose();
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error in TryFindBySteam: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Attempts to find a player by their character name.
    /// </summary>
    public static bool TryFindByName(string name, out PlayerData playerData)
    {
        playerData = default;
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var lowerName = name.Trim().ToLowerInvariant();

        try
        {
            RefreshCacheIfNeeded();

            // Try cache first for exact match
            lock (_cacheLock)
            {
                if (_nameToUserCache.TryGetValue(name, out var exactUserEntity))
                {
                    if (EM.HasComponent<User>(exactUserEntity))
                    {
                        var user = EM.GetComponentData<User>(exactUserEntity);
                        var characterEntity = user.LocalCharacter._Entity;
                        playerData = new PlayerData(
                            user.CharacterName.ToString(),
                            user.PlatformId,
                            user.IsConnected,
                            exactUserEntity,
                            characterEntity
                        );
                        return true;
                    }
                }
            }

            // Try partial match from cache
            List<PlayerData> matchingPlayers = new();
            lock (_cacheLock)
            {
                foreach (var kvp in _nameToUserCache)
                {
                    if (kvp.Key.ToLowerInvariant().Contains(lowerName))
                    {
                        var userEntity = kvp.Value;
                        if (EM.HasComponent<User>(userEntity))
                        {
                            var user = EM.GetComponentData<User>(userEntity);
                            var characterEntity = user.LocalCharacter._Entity;
                            matchingPlayers.Add(new PlayerData(
                                user.CharacterName.ToString(),
                                user.PlatformId,
                                user.IsConnected,
                                userEntity,
                                characterEntity
                            ));
                        }
                    }
                }
            }

            // If exactly one partial match, use it
            if (matchingPlayers.Count == 1)
            {
                playerData = matchingPlayers[0];
                return true;
            }

            // Fallback to direct query if cache miss or multiple matches
            if (EM == default)
                return false;

            var query = EM.CreateEntityQuery(ComponentType.ReadOnly<User>());
            var userEntities = query.ToEntityArray(Allocator.Temp);

            List<string> onlinePlayers = new();
            matchingPlayers.Clear();

            try
            {
                foreach (var userEntity in userEntities)
                {
                    if (EM.HasComponent<User>(userEntity))
                    {
                        var user = EM.GetComponentData<User>(userEntity);
                        var playerName = user.CharacterName.ToString();
                        var lowerPlayerName = playerName.ToLowerInvariant();

                        if (user.IsConnected)
                        {
                            onlinePlayers.Add(playerName);
                        }

                        // Exact match
                        if (lowerPlayerName == lowerName)
                        {
                            var characterEntity = user.LocalCharacter._Entity;
                            playerData = new PlayerData(
                                playerName,
                                user.PlatformId,
                                user.IsConnected,
                                userEntity,
                                characterEntity
                            );
                            return true;
                        }

                        // Partial match
                        if (lowerPlayerName.Contains(lowerName))
                        {
                            var characterEntity = user.LocalCharacter._Entity;
                            matchingPlayers.Add(new PlayerData(
                                playerName,
                                user.PlatformId,
                                user.IsConnected,
                                userEntity,
                                characterEntity
                            ));
                        }
                    }
                }
            }
            finally
            {
                userEntities.Dispose();
            }

            // If exactly one partial match, use it
            if (matchingPlayers.Count == 1)
            {
                playerData = matchingPlayers[0];
                return true;
            }

            // Not found, log online players
            Plugin.Logger?.LogWarning($"Player '{name}' not found. {onlinePlayers.Count} online players: {string.Join(", ", onlinePlayers.OrderBy(n => n))}");
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error in TryFindByName: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Attempts to find a player by their network ID.
    /// </summary>
    /// <param name="networkId">The network ID of the player to find</param>
    /// <param name="playerData">The player data if found</param>
    /// <returns>True if the player was found, false otherwise</returns>
    public static bool TryFindByNetworkId(NetworkId networkId, out PlayerData playerData)
    {
        playerData = default;

        try
        {
            if (EM == default)
                return false;

            // Find user entity that has this network ID as character
            var query = EM.CreateEntityQuery(ComponentType.ReadOnly<User>());
            var userEntities = query.ToEntityArray(Allocator.Temp);

            try
            {
                foreach (var userEntity in userEntities)
                {
                    if (EM.HasComponent<User>(userEntity))
                    {
                        var user = EM.GetComponentData<User>(userEntity);
                        var characterEntity = user.LocalCharacter._Entity;

                        if (characterEntity != Entity.Null && EM.HasComponent<NetworkId>(characterEntity))
                        {
                            var charNetworkId = EM.GetComponentData<NetworkId>(characterEntity);
                            if (charNetworkId == networkId)
                            {
                                playerData = new PlayerData(
                                    user.CharacterName.ToString(),
                                    user.PlatformId,
                                    user.IsConnected,
                                    userEntity,
                                    characterEntity
                                );
                                return true;
                            }
                        }
                    }
                }
            }
            finally
            {
                userEntities.Dispose();
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error in TryFindByNetworkId: {ex.Message}");
        }

        return false;
    }

    /// <summary>
    /// Creates a new Player instance from the provided User.
    /// </summary>
    /// <param name="user">The user to create a Player for</param>
    /// <returns>A new Player instance</returns>
    public static Player PlayerFromUser(User user)
    {
        var player = new Player();
        
        if (TryFindBySteam(user.PlatformId, out var playerData))
        {
            player.User = playerData.UserEntity;
            player.Character = playerData.CharEntity; // Fixed property name from CharacterEntity to CharEntity
        }
        else
        {
            player.User = Entity.Null;
            player.Character = user.LocalCharacter._Entity;
            
            // If we have a character but no user, try to find the user entity
            if (player.Character != Entity.Null)
            {
                try
                {
                    if (EM.HasComponent<PlayerCharacter>(player.Character))
                    {
                        var playerCharacter = EM.GetComponentData<PlayerCharacter>(player.Character);
                        player.User = playerCharacter.UserEntity;
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"Error getting user from character: {ex.Message}");
                }
            }
        }
        
        return player;
    }

    /// <summary>
    /// Get all online players (legacy method for compatibility).
    /// </summary>
    public static IEnumerable<PlayerData> GetCachedUsersOnlineAsPlayer()
    {
        // Direct query instead of cache
        var result = new List<PlayerData>();

        try
        {
            if (EM == default)
                return result;

            var query = EM.CreateEntityQuery(ComponentType.ReadOnly<User>());
            var userEntities = query.ToEntityArray(Allocator.Temp);

            try
            {
                foreach (var userEntity in userEntities)
                {
                    if (EM.HasComponent<User>(userEntity))
                    {
                        var user = EM.GetComponentData<User>(userEntity);
                        if (user.IsConnected)
                        {
                            var characterEntity = user.LocalCharacter._Entity;
                            result.Add(new PlayerData(
                                user.CharacterName.ToString(),
                                user.PlatformId,
                                user.IsConnected,
                                userEntity,
                                characterEntity
                            ));
                        }
                    }
                }
            }
            finally
            {
                userEntities.Dispose();
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error in GetCachedUsersOnlineAsPlayer: {ex.Message}");
        }

        return result;
    }

    #endregion
}
