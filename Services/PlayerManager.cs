using System;
using System.Collections.Generic;
using System.Linq;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CrowbaneArena
{
    /// <summary>
    /// Manages player states and progression.
    /// </summary>
    public static class PlayerManager
    {
        private static readonly Dictionary<Entity, PlayerState> playerStates = new Dictionary<Entity, PlayerState>();

        public static void UpdatePlayerState(Entity player, PlayerState state)
        {
            playerStates[player] = state;
        }

        public static PlayerState GetPlayerState(Entity player)
        {
            return playerStates.TryGetValue(player, out var state) ? state : new PlayerState();
        }
        /// <summary>
        /// Finds a player by their name (case-insensitive and partial matches supported)
        /// </summary>
        /// <param name="playerName">The name or partial name of the player to find</param>
        /// <param name="includeOffline">Whether to include offline players in the search (not implemented)</param>
        /// <returns>The player's character entity if found, otherwise Entity.Null</returns>
        public static Entity GetPlayerByName(string playerName, bool includeOffline = false)
        {
            Plugin.Logger?.LogInfo($"Looking up player by name: {playerName}");
            
            // Use the enhanced TryFindByName which now handles case-insensitive and partial matches
            if (CrowbaneArena.Services.PlayerService.TryFindByName(playerName, out var player))
            {
                Plugin.Logger?.LogInfo($"Found player {playerName} with character entity: {player.CharEntity}");
                return player.CharEntity;
            }

            // If we get here, the player wasn't found - log available players
            var onlinePlayers = new List<(string Name, ulong SteamId, Entity CharEntity)>();
            foreach (var p in CrowbaneArena.Services.PlayerService.GetCachedUsersOnlineAsPlayer())
            {
                var name = p.Name;
                var steamId = p.SteamId;
                onlinePlayers.Add((name, steamId, p.CharEntity));
            }
            onlinePlayers = onlinePlayers.OrderBy(p => p.Name).ToList();

            Plugin.Logger?.LogWarning($"Player '{playerName}' not found. {onlinePlayers.Count} online players:");
// Show up to 10 players in the log
            foreach (var (name, steamId, _) in onlinePlayers.Take(10))
            {
                Plugin.Logger?.LogWarning($"  - {name} (SteamID: {steamId})");
            }
            if (onlinePlayers.Count > 10)
            {
                Plugin.Logger?.LogWarning($"  ... and {onlinePlayers.Count - 10} more");
            }

            return Entity.Null;
        }

        public static float3 GetPlayerPosition(Entity player)
        {
            try
            {
                if (player.Equals(Entity.Null) || !VRisingCore.EntityManager.Exists(player))
                {
                    Plugin.Logger?.LogWarning($"Invalid player entity: {player}");
                    return float3.zero;
                }

                if (VRisingCore.EntityManager.HasComponent<Translation>(player))
                {
                    var translation = VRisingCore.EntityManager.GetComponentData<Translation>(player);
                    return translation.Value;
                }

                Plugin.Logger?.LogWarning($"Player {player} has no Translation component");
                return float3.zero;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error getting player position: {ex.Message}");
                return float3.zero;
            }
        }
    }

    public class PlayerState
    {
        public bool IsInArena { get; set; } = false;
        public int VBloodCount { get; set; } = 0;
    }
}
