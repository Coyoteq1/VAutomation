using System;
using System.Collections.Generic;
using ProjectM.Network;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Manages game system hooks for arena functionality
    /// </summary>
    public static class GameSystems
    {
        private static readonly HashSet<ulong> _arenaPlayers = new();
        private static readonly Dictionary<ulong, DateTime> _arenaEntryTimes = new();

        /// <summary>
        /// Mark player as entered arena - activates VBlood hook
        /// </summary>
        public static void MarkPlayerEnteredArena(ulong platformId)
        {
            _arenaPlayers.Add(platformId);
            _arenaEntryTimes[platformId] = DateTime.UtcNow;
            Plugin.Logger?.LogInfo($"VBlood hook activated for player {platformId}");
        }

        /// <summary>
        /// Mark player as exited arena - deactivates VBlood hook
        /// </summary>
        public static void MarkPlayerExitedArena(ulong platformId)
        {
            _arenaPlayers.Remove(platformId);
            _arenaEntryTimes.Remove(platformId);
            Plugin.Logger?.LogInfo($"VBlood hook deactivated for player {platformId}");
        }

        /// <summary>
        /// Check if player is in arena (for VBlood hook)
        /// </summary>
        public static bool IsPlayerInArena(ulong platformId)
        {
            return _arenaPlayers.Contains(platformId);
        }

        /// <summary>
        /// Get arena entry time for player
        /// </summary>
        public static DateTime? GetArenaEntryTime(ulong platformId)
        {
            return _arenaEntryTimes.TryGetValue(platformId, out var time) ? time : null;
        }

        /// <summary>
        /// Clear all arena states (admin command)
        /// </summary>
        public static void ClearAllArenaStates()
        {
            _arenaPlayers.Clear();
            _arenaEntryTimes.Clear();
            Plugin.Logger?.LogInfo("All arena states cleared");
        }
    }
}
