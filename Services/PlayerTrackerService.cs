using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Tracks player arena data - loadouts, stats, preferences
    /// Saves to playertracker.json
    /// </summary>
    public static class PlayerTrackerService
    {
        private static readonly string TrackerPath = Path.Combine("BepInEx", "config", "CrowbaneArena", "playertracker.json");
        private static Dictionary<ulong, PlayerTrackerData> _trackedPlayers = new Dictionary<ulong, PlayerTrackerData>();
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Initialize tracker service
        /// </summary>
        public static void Initialize()
        {
            try
            {
                LoadTracker();
                Plugin.Logger?.LogInfo($"PlayerTracker initialized with {_trackedPlayers.Count} tracked players");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to initialize PlayerTracker: {ex.Message}");
                _trackedPlayers = new Dictionary<ulong, PlayerTrackerData>();
            }
        }

        /// <summary>
        /// Track player arena entry
        /// </summary>
        public static void TrackArenaEntry(ulong steamId, string playerName, string loadoutUsed)
        {
            try
            {
                if (!_trackedPlayers.ContainsKey(steamId))
                {
                    _trackedPlayers[steamId] = new PlayerTrackerData
                    {
                        SteamId = steamId,
                        PlayerName = playerName,
                        FirstSeen = DateTime.UtcNow
                    };
                }

                var data = _trackedPlayers[steamId];
                data.PlayerName = playerName; // Update name in case it changed
                data.LastSeen = DateTime.UtcNow;
                data.TotalArenaEntries++;
                data.LastLoadoutUsed = loadoutUsed;
                data.PreferredLoadout = loadoutUsed; // Simple preference tracking

                SaveTracker();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error tracking arena entry: {ex.Message}");
            }
        }

        /// <summary>
        /// Track loadout change
        /// </summary>
        public static void TrackLoadoutChange(ulong steamId, string oldLoadout, string newLoadout)
        {
            try
            {
                if (!_trackedPlayers.ContainsKey(steamId))
                    return;

                var data = _trackedPlayers[steamId];
                data.TotalLoadoutChanges++;
                data.LastLoadoutUsed = newLoadout;
                data.PreferredLoadout = newLoadout;

                if (!data.LoadoutHistory.Contains(newLoadout))
                {
                    data.LoadoutHistory.Add(newLoadout);
                }

                SaveTracker();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error tracking loadout change: {ex.Message}");
            }
        }

        /// <summary>
        /// Track arena exit
        /// </summary>
        public static void TrackArenaExit(ulong steamId, TimeSpan timeInArena)
        {
            try
            {
                if (!_trackedPlayers.ContainsKey(steamId))
                    return;

                var data = _trackedPlayers[steamId];
                data.TotalArenaExits++;
                data.TotalTimeInArena += timeInArena;
                data.LastSeen = DateTime.UtcNow;

                SaveTracker();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error tracking arena exit: {ex.Message}");
            }
        }

        /// <summary>
        /// Get player tracker data
        /// </summary>
        public static PlayerTrackerData GetPlayerData(ulong steamId)
        {
            return _trackedPlayers.TryGetValue(steamId, out var data) ? data : null;
        }

        /// <summary>
        /// Get preferred loadout for player
        /// </summary>
        public static string GetPreferredLoadout(ulong steamId)
        {
            if (_trackedPlayers.TryGetValue(steamId, out var data))
            {
                return data.PreferredLoadout ?? "default";
            }
            return "default";
        }

        /// <summary>
        /// Load tracker from JSON
        /// </summary>
        private static void LoadTracker()
        {
            try
            {
                if (!File.Exists(TrackerPath))
                {
                    Plugin.Logger?.LogInfo("No playertracker.json found, creating new tracker");
                    _trackedPlayers = new Dictionary<ulong, PlayerTrackerData>();
                    SaveTracker();
                    return;
                }

                var json = File.ReadAllText(TrackerPath);
                var loadedData = JsonSerializer.Deserialize<Dictionary<ulong, PlayerTrackerData>>(json, JsonOptions);
                _trackedPlayers = loadedData ?? new Dictionary<ulong, PlayerTrackerData>();

                Plugin.Logger?.LogInfo($"Loaded {_trackedPlayers.Count} player records from playertracker.json");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to load playertracker.json: {ex.Message}");
                _trackedPlayers = new Dictionary<ulong, PlayerTrackerData>();
            }
        }

        /// <summary>
        /// Save tracker to JSON
        /// </summary>
        private static void SaveTracker()
        {
            try
            {
                var directory = Path.GetDirectoryName(TrackerPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(_trackedPlayers, JsonOptions);
                File.WriteAllText(TrackerPath, json);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to save playertracker.json: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all tracker data
        /// </summary>
        public static void ClearAll()
        {
            _trackedPlayers.Clear();
            SaveTracker();
            Plugin.Logger?.LogInfo("Cleared all player tracker data");
        }
    }

    /// <summary>
    /// Player tracker data model
    /// </summary>
    public class PlayerTrackerData
    {
        public ulong SteamId { get; set; }
        public string PlayerName { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }
        public int TotalArenaEntries { get; set; }
        public int TotalArenaExits { get; set; }
        public int TotalLoadoutChanges { get; set; }
        public TimeSpan TotalTimeInArena { get; set; }
        public string LastLoadoutUsed { get; set; }
        public string PreferredLoadout { get; set; }
        public List<string> LoadoutHistory { get; set; } = new List<string>();
    }
}
