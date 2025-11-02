using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Defines the state of a player's progress in the arena/reset system.
    /// </summary>
    public enum PlayerProgressState
    {
        // --- Arena Entry Flow ---
        /// <summary>
        /// Player has been reset and needs to be kicked on next join to start the arena flow.
        /// </summary>
        PendingArenaKick,
        /// <summary>
        /// Player has been kicked once and now needs to be spawned in the arena on next join.
        /// </summary>
        PendingArenaSpawn,
        /// <summary>
        /// Player is currently active in the arena with reset progress.
        /// </summary>
        InArena,

        // --- Progress Restore Flow ---
        /// <summary>
        /// Player has exited the arena and needs to be kicked to restore their original progress.
        /// </summary>
        PendingRestoreKick
    }

    /// <summary>
    /// Manages the full lifecycle of a player's progress: resetting for the arena and restoring it upon exit.
    /// This service archives a player's original character bind and tracks their state through the process.
    /// </summary>
    public class ProgressManagerService : IDisposable
    {
        public static ProgressManagerService Instance { get; private set; }

        private const string StatesFileName = "PlayerProgressStates.json";
        private const string ArchiveFileName = "ArchivedBinds.json";
        private const string ZonesFileName = "PlayerZones.json";

        private readonly List<string> _dataFiles = new() { StatesFileName, ArchiveFileName, ZonesFileName };

        // Tracks the current state of players in the reset/restore process.
        private ConcurrentDictionary<ulong, PlayerProgressState> _playerStates = new();

        // Archives the original character name for restoration. Key: PlatformId, Value: Character Name.
        private ConcurrentDictionary<ulong, string> _archivedBinds = new();

        // Tracks which zone each player is assigned to. Key: PlatformId, Value: Zone Name.
        private ConcurrentDictionary<ulong, string> _playerZones = new();

        public static void Initialize()
        {
            if (Instance == null)
            {
                Instance = new ProgressManagerService();
                Instance.LoadData();
                Console.WriteLine("ProgressManagerService Initialized."); // Adapted for CrowbaneArena
            }
        }

        private bool _isDirty;
        private DateTime _lastSave;
        private const string BackupExtension = ".bak";
        private readonly string _dataPath;

        public ProgressManagerService()
        {
            _dataPath = "config/CrowbaneArena"; // Adapted for CrowbaneArena
        }

        private void LoadData()
        {
            try
            {
                EnsureDirectories();

                // Load with fallback to backup files if primary files fail
                _playerStates = LoadFileWithBackup<ConcurrentDictionary<ulong, PlayerProgressState>>(StatesFileName) ?? new();
                _archivedBinds = LoadFileWithBackup<ConcurrentDictionary<ulong, string>>(ArchiveFileName) ?? new();
                _playerZones = LoadFileWithBackup<ConcurrentDictionary<ulong, string>>(ZonesFileName) ?? new();

                // Validate loaded data
                ValidateLoadedData();

                Console.WriteLine($"Loaded {_playerStates.Count} player states, {_archivedBinds.Count} archived binds, and {_playerZones.Count} player zones.");
            }
            catch (IOException e)
            {
                Console.WriteLine($"Critical error loading progress data: {e.Message}");
                InitializeEmptyData();
            }
            catch (System.Exception e)
            {
                Console.WriteLine($"Unexpected error loading progress data: {e.Message}");
                InitializeEmptyData();
            }
        }

        private void SaveData()
        {
            if (!_isDirty) return;

            try
            {
                BackupExistingFiles();

                // Use helper to save data to prevent code duplication
                SaveFile(StatesFileName, _playerStates);
                SaveFile(ArchiveFileName, _archivedBinds);
                SaveFile(ZonesFileName, _playerZones);

                _isDirty = false;
                _lastSave = DateTime.UtcNow;
                Console.WriteLine("Player progress data saved successfully.");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to save player progress data.");
                Console.WriteLine(e.Message);
            }
        }

        private void SaveFile<T>(string fileName, T data)
        {
            var filePath = Path.Combine(_dataPath, fileName);
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }

        private void EnsureDirectories()
        {
            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }
        }

        private void BackupExistingFiles()
        {
            foreach (var fileName in _dataFiles)
            {
                BackupFile(fileName);
            }
        }

        private void BackupFile(string fileName)
        {
            var filePath = Path.Combine(_dataPath, fileName);
            var backupPath = filePath + BackupExtension;

            if (File.Exists(filePath))
            {
                try
                {
                    File.Copy(filePath, backupPath, true);
                }
                catch (IOException e)
                {
                    Console.WriteLine($"Failed to create backup of {fileName}: {e.Message}");
                }
            }
        }

        private T LoadFileWithBackup<T>(string fileName) where T : new()
        {
            var filePath = Path.Combine(_dataPath, fileName);
            var backupPath = filePath + BackupExtension;

            try
            {
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<T>(json);
                }
                else if (File.Exists(backupPath))
                {
                    Console.WriteLine($"Data file '{fileName}' not found. Attempting to restore from backup.");
                    var json = File.ReadAllText(backupPath);
                    var data = JsonSerializer.Deserialize<T>(json);
                    // Restore the backup for future loads
                    File.Copy(backupPath, filePath);
                    Console.WriteLine($"Successfully restored '{fileName}' from backup.");
                    return data;
                }
            }
            catch (JsonException e)
            {
                Console.WriteLine($"Error deserializing '{fileName}': {e.Message}. Checking for backup.");
                if (File.Exists(backupPath))
                {
                    try
                    {
                        Console.WriteLine($"Attempting to load from backup '{backupPath}'.");
                        var json = File.ReadAllText(backupPath);
                        var data = JsonSerializer.Deserialize<T>(json);
                        // Restore the backup, overwriting the corrupt file
                        File.Copy(backupPath, filePath, true);
                        Console.WriteLine($"Successfully restored '{fileName}' from backup, overwriting corrupt file.");
                        return data;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load from backup for '{fileName}': {ex.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"An unexpected error occurred while loading '{fileName}': {e.Message}");
            }

            Console.WriteLine($"Could not load data for '{fileName}'. A new empty data set will be used.");
            return new T();
        }

        // Note: The following method is commented out as it might not be needed in the current implementation,
        // but kept for historical reference in case restoration from backup is required later.
        /*
        private void AttemptRestoreFromBackup()
        {
            try
            {
                Console.WriteLine("Attempting to restore data from backups...");
                LoadData(); // This will use backups if main files are corrupted
            }
            catch (IOException e)
            {
                Console.WriteLine($"Failed to restore from backup: {e.Message}");
                InitializeEmptyData();
            }
        }
        */

        private void InitializeEmptyData()
        {
            Console.WriteLine("Initializing empty data structures due to load failure");
            _playerStates = new ConcurrentDictionary<ulong, PlayerProgressState>();
            _archivedBinds = new ConcurrentDictionary<ulong, string>();
            _playerZones = new ConcurrentDictionary<ulong, string>();
        }

        private void ValidateLoadedData()
        {
            // Clean up any invalid state entries
            var invalidStates = _playerStates
                .Where(kvp => !Enum.IsDefined(typeof(PlayerProgressState), kvp.Value))
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in invalidStates)
            {
                Console.WriteLine($"Removing invalid state for player {key}");
                _playerStates.TryRemove(key, out _);
                _isDirty = true;
            }

            // Clean up orphaned binds
            var orphanedBinds = _archivedBinds.Keys
                .Except(_playerStates.Keys)
                .ToList();

            foreach (var key in orphanedBinds)
            {
                Console.WriteLine($"Removing orphaned bind for player {key}");
                _archivedBinds.TryRemove(key, out _);
                _isDirty = true;
            }

            // Clean up orphaned zones
            var orphanedZones = _playerZones.Keys
                .Except(_playerStates.Keys)
                .ToList();

            foreach (var key in orphanedZones)
            {
                Console.WriteLine($"Removing orphaned zone for player {key}");
                _playerZones.TryRemove(key, out _);
                _isDirty = true;
            }
        }

        /// <summary>
        /// Archives a player's progress and begins the reset-to-arena process.
        /// </summary>
        public void ArchiveAndBeginReset(ulong platformId, string characterNameToArchive, string zoneName = null)
        {
            if (platformId == 0 || string.IsNullOrEmpty(characterNameToArchive))
            {
                Console.WriteLine("ArchiveAndBeginReset called with invalid arguments.");
                return;
            }

            try
            {
                Console.WriteLine($"Archiving progress for player {characterNameToArchive} (ID: {platformId}) in zone '{zoneName}'");

                // Perform all in-memory operations before saving.
                _archivedBinds[platformId] = characterNameToArchive;
                _playerStates[platformId] = PlayerProgressState.PendingArenaKick;
                if (!string.IsNullOrEmpty(zoneName))
                {
                    _playerZones[platformId] = zoneName;
                }
                _isDirty = true;
                SaveData();

                Console.WriteLine($"Successfully archived progress for {platformId} in zone '{zoneName}' and flagged for arena entry.");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error in ArchiveAndBeginReset for player {platformId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Begins the progress restoration process for a player exiting the arena.
        /// </summary>
        public void BeginRestoreProcess(ulong platformId)
        {
            try
            {
                // Defensive check: Only players currently in the arena can start the restore process.
                if (!GetPlayerState(platformId).HasValue || _playerStates[platformId] != PlayerProgressState.InArena)
                {
                    Console.WriteLine($"BeginRestoreProcess called for player {platformId} who is not in the InArena state.");
                    return;
                }

                AdvancePlayerState(platformId, PlayerProgressState.PendingRestoreKick);
                Console.WriteLine($"Player {platformId} has exited the arena and is pending progress restoration.");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error in BeginRestoreProcess for player {platformId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Restores a player's original progress by re-binding their archived character name.
        /// </summary>
        /// <returns>True if progress was successfully restored.</returns>
        public bool RestorePlayerProgress(ulong platformId)
        {
            try
            {
                // Defensive check: Ensure the player is in the correct state for restoration.
                if (GetPlayerState(platformId) != PlayerProgressState.PendingRestoreKick)
                {
                    Console.WriteLine($"RestorePlayerProgress called for player {platformId} who is not in the PendingRestoreKick state.");
                    return false;
                }

                if (_archivedBinds.TryGetValue(platformId, out string originalCharacterName))
                {
                    Console.WriteLine($"Restoring progress for player {platformId} to character '{originalCharacterName}'");

                    // Re-apply the original bind (placeholder for BindService)
                    // BindService.Instance.RegisterBind(platformId, originalCharacterName);

                    // Clean up the archives and state files.
                    _archivedBinds.TryRemove(platformId, out _);
                    _playerStates.TryRemove(platformId, out _);
                    _playerZones.TryRemove(platformId, out _);
                    _isDirty = true;
                    SaveData();

                    Console.WriteLine($"Successfully restored progress for {platformId} to character '{originalCharacterName}'.");
                    return true;
                }

                Console.WriteLine($"Could not restore progress for {platformId}: No archived bind found.");
                return false;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error in RestorePlayerProgress for player {platformId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks the current state of a player.
        /// </summary>
        public PlayerProgressState? GetPlayerState(ulong platformId)
        {
            return _playerStates.TryGetValue(platformId, out var state) ? state : null;
        }

        /// <summary>
        /// Advances a player to the next state in the process.
        /// </summary>
        public void AdvancePlayerState(ulong platformId, PlayerProgressState newState)
        {
            _playerStates[platformId] = newState;
            _isDirty = true;
        }

        /// <summary>
        /// Check if player is in arena (for command permission patch)
        /// </summary>
        public bool IsInArena(ulong platformId)
        {
            return _playerStates.ContainsKey(platformId);
        }

        /// <summary>
        /// Get the zone a player is assigned to
        /// </summary>
        public string GetPlayerZone(ulong platformId)
        {
            return _playerZones.TryGetValue(platformId, out var zoneName) ? zoneName : null;
        }

        /// <summary>
        /// Set the zone a player is assigned to
        /// </summary>
        public void SetPlayerZone(ulong platformId, string zoneName)
        {
            _playerZones[platformId] = zoneName;
            _isDirty = true;
        }

        /// <summary>
        /// Clear zone assignment for a player
        /// </summary>
        public void ClearPlayerZone(ulong platformId)
        {
            if (_playerZones.TryRemove(platformId, out _))
            {
                _isDirty = true;
            }
        }

        public void Dispose()
        {
            SaveData();
            Instance = null;
        }
    }
}
