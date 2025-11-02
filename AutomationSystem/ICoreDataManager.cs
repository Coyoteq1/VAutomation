using System;
using System.Collections.Generic;

namespace AutomationSystem
{
    /// <summary>
    /// Interface for core data management functionality
    /// </summary>
    public interface ICoreDataManager
    {
        /// <summary>
        /// Gets or sets the data directory path
        /// </summary>
        string DataDirectory { get; set; }

        /// <summary>
        /// Initializes the data manager
        /// </summary>
        void Initialize();

        /// <summary>
        /// Creates a data snapshot for the specified entity
        /// </summary>
        /// <param name="entityId">The ID of the entity to snapshot</param>
        /// <param name="description">Optional description for the snapshot</param>
        /// <returns>The snapshot ID</returns>
        string CreateSnapshot(string entityId, string? description = null);

        /// <summary>
        /// Restores data from a snapshot
        /// </summary>
        /// <param name="entityId">The ID of the entity to restore</param>
        /// <param name="snapshotId">The ID of the snapshot to restore</param>
        /// <returns>True if restoration was successful</returns>
        bool RestoreSnapshot(string entityId, string snapshotId);

        /// <summary>
        /// Gets all snapshots for a specific entity
        /// </summary>
        /// <param name="entityId">The ID of the entity</param>
        /// <returns>List of snapshots</returns>
        List<AutomationSnapshot> GetSnapshots(string entityId);

        /// <summary>
        /// Deletes a specific snapshot
        /// </summary>
        /// <param name="entityId">The ID of the entity</param>
        /// <param name="snapshotId">The ID of the snapshot to delete</param>
        /// <returns>True if deletion was successful</returns>
        bool DeleteSnapshot(string entityId, string snapshotId);

        /// <summary>
        /// Saves data with persistence suppression support
        /// </summary>
        /// <param name="entityId">The ID of the entity</param>
        /// <param name="dataType">The type of data</param>
        /// <param name="data">The data to save</param>
        /// <returns>True if save was successful</returns>
        bool SaveData(string entityId, string dataType, object data);

        /// <summary>
        /// Loads data
        /// </summary>
        /// <param name="entityId">The ID of the entity</param>
        /// <param name="dataType">The type of data</param>
        /// <param name="data">When this method returns, contains the loaded data if successful, or default if failed</param>
        /// <returns>True if load was successful</returns>
        bool LoadData(string entityId, string dataType, out object data);

        /// <summary>
        /// Creates a backup of all data for an entity
        /// </summary>
        /// <param name="entityId">The ID of the entity</param>
        /// <param name="timestamp">Optional timestamp for the backup</param>
        /// <returns>True if backup was successful</returns>
        bool BackupData(string entityId, string? timestamp = null);

        /// <summary>
        /// Restores data from a backup
        /// </summary>
        /// <param name="entityId">The ID of the entity</param>
        /// <param name="timestamp">The timestamp of the backup to restore</param>
        /// <returns>True if restoration was successful</returns>
        bool RestoreDataFromBackup(string entityId, string timestamp);
    }
}