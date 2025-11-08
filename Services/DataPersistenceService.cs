using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CrowbaneArena.Models;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Manages all game data persistence in a single file
    /// </summary>
    public static class DataPersistenceService
    {
    private static readonly string _dataDirectory = Path.Combine("BepInEx", "config", "CrowbaneArena", "Data");

        private static readonly string _dataFilePath = Path.Combine(_dataDirectory, "game_data.json");
        
        private static GameData _gameData = new();
        
        /// <summary>
        /// Initialize the data persistence service
        /// </summary>
        public static bool Initialize()
        {
            try
            {
                // Ensure data directory exists
                if (!Directory.Exists(_dataDirectory))
                {
                    Directory.CreateDirectory(_dataDirectory);
                }

                // Load existing data or create new
                if (File.Exists(_dataFilePath))
                {
                    var json = File.ReadAllText(_dataFilePath);
                    _gameData = JsonSerializer.Deserialize<GameData>(json) ?? new GameData();
                    Plugin.Logger?.LogInfo($"Loaded game data from {_dataFilePath}");
                }
                else
                {
                    _gameData = new GameData();
                    SaveData();
                    Plugin.Logger?.LogInfo($"Created new game data file at {_dataFilePath}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error initializing DataPersistenceService: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save all game data to file
        /// </summary>
        public static void SaveData()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(_gameData, options);
                File.WriteAllText(_dataFilePath, json);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error saving game data: {ex.Message}");
            }
        }

        #region Player Data Methods
        
        public static PersistedPlayerData GetPlayerData(ulong steamId)
        {
            if (!_gameData.Players.TryGetValue(steamId, out var playerData))
            {
                playerData = new PersistedPlayerData();
                _gameData.Players[steamId] = playerData;
            }
            return playerData;
        }

        public static void UpdatePlayerData(ulong steamId, PersistedPlayerData data)
        {
            _gameData.Players[steamId] = data;
            SaveData();
        }

        #endregion

        #region Boss Data Methods
        
        public static PersistedBossData GetBossData(string bossId)
        {
            if (!_gameData.Bosses.TryGetValue(bossId, out var bossData))
            {
                bossData = new PersistedBossData();
                _gameData.Bosses[bossId] = bossData;
            }
            return bossData;
        }

        public static void UpdateBossData(string bossId, PersistedBossData data)
        {
            _gameData.Bosses[bossId] = data;
            SaveData();
        }

        public static Dictionary<string, PersistedBossData> GetAllBosses()
        {
            return _gameData.Bosses;
        }

        #endregion

        #region Arena Data Methods
        
        public static ArenaData GetArenaData(string arenaId)
        {
            if (!_gameData.Arenas.TryGetValue(arenaId, out var arenaData))
            {
                arenaData = new ArenaData();
                _gameData.Arenas[arenaId] = arenaData;
            }
            return arenaData;
        }

        public static void UpdateArenaData(string arenaId, ArenaData data)
        {
            _gameData.Arenas[arenaId] = data;
            SaveData();
        }

        #endregion
    }

    /// <summary>
    /// Root game data container
    /// </summary>
    public class GameData
    {
        public Dictionary<ulong, PersistedPlayerData> Players { get; set; } = new();
        public Dictionary<string, PersistedBossData> Bosses { get; set; } = new();
        public Dictionary<string, ArenaData> Arenas { get; set; } = new();
        // Add other data types as needed
    }

    /// <summary>
    /// Player-specific data
    /// </summary>
    public class PersistedPlayerData
    {
        public string LastKnownName { get; set; } = string.Empty;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Stats { get; set; } = new();
        public List<string> UnlockedAbilities { get; set; } = new();
        public List<string> DefeatedBosses { get; set; } = new();
        // Add other player-specific data
    }

    /// <summary>
    /// Boss-specific data
    /// </summary>
    public class PersistedBossData
    {
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Region { get; set; } = string.Empty;
        public List<string> Rewards { get; set; } = new();
        public Dictionary<string, object> Stats { get; set; } = new();
        // Add other boss-specific data
    }

    /// <summary>
    /// Arena-specific data
    /// </summary>
    public class ArenaData
    {
        public string Name { get; set; } = string.Empty;
        public string MapRegion { get; set; } = string.Empty;
        public List<ulong> ActivePlayers { get; set; } = new();
        public Dictionary<string, object> Stats { get; set; } = new();
        // Add other arena-specific data
    }
}
