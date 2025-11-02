using System;
using System.Collections.Generic;

namespace AutomationSystem
{
    /// <summary>
    /// Interface for snapshot management functionality
    /// </summary>
    public interface ISnapshotManager
    {
        /// <summary>
        /// Saves a snapshot to persistent storage
        /// </summary>
        /// <typeparam name="T">The type of data being snapshotted</typeparam>
        /// <param name="filePath">The file path to save the snapshot to</param>
        /// <param name="data">The data to snapshot</param>
        /// <returns>True if the snapshot was saved successfully</returns>
        bool SaveSnapshot<T>(string filePath, T data) where T : class;

        /// <summary>
        /// Loads a snapshot from persistent storage
        /// </summary>
        /// <typeparam name="T">The type of data to load</typeparam>
        /// <param name="filePath">The file path to load the snapshot from</param>
        /// <returns>The loaded data, or null if loading failed</returns>
        T? LoadSnapshot<T>(string filePath) where T : class;

        /// <summary>
        /// Creates a backup of an existing file
        /// </summary>
        /// <param name="filePath">The original file path</param>
        /// <returns>True if backup was created successfully</returns>
        bool BackupFile(string filePath);

        /// <summary>
        /// Creates an atomic save operation
        /// </summary>
        /// <param name="filePath">The file path to save to</param>
        /// <param name="content">The content to save</param>
        /// <returns>True if the save was successful</returns>
        bool AtomicSave(string filePath, string content);

        /// <summary>
        /// Restores from backup if main file doesn't exist or is corrupted
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <param name="backupPath">The backup file path</param>
        /// <returns>True if restoration was successful</returns>
        bool RestoreFromBackup(string filePath, string backupPath);

        /// <summary>
        /// Validates if a JSON file is well-formed
        /// </summary>
        /// <param name="filePath">The file path to validate</param>
        /// <returns>True if the file contains valid JSON</returns>
        bool ValidateJson(string filePath);
    }
}