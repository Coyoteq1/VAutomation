using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using BepInEx;

namespace CrowbaneArena.Utils
{
    /// <summary>
    /// Handles persistent storage of configuration data using JSON files
    /// Follows ICB.core pattern for consistency
    /// </summary>
    public static class Persistence
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() },
            ReferenceHandler = ReferenceHandler.Preserve,
            IncludeFields = true
        };

        /// <summary>
        /// Saves data to a JSON file in the BepInEx config directory
        /// </summary>
        /// <typeparam name="T">The type of data to save</typeparam>
        /// <param name="configFolderName">Name of the config subfolder</param>
        /// <param name="fileName">Name of the file to save to</param>
        /// <param name="data">The data to save</param>
        public static void SaveToFile<T>(string configFolderName, string fileName, T data)
        {
            try
            {
                string configDir = Path.Combine(Paths.ConfigPath, configFolderName);
                Directory.CreateDirectory(configDir);

                string fullPath = Path.Combine(configDir, fileName);
                
                // Create backup if file exists
                if (File.Exists(fullPath))
                {
                    try
                    {
                        File.Copy(fullPath, fullPath + ".bak", true);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger?.LogWarning($"[Persistence] Failed to create backup: {ex.Message}");
                    }
                }
                
                string json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(fullPath, json);
                Plugin.Logger?.LogInfo($"[Persistence] Saved {fileName} successfully");
            }
            catch (IOException ex)
            {
                Plugin.Logger?.LogError($"[Persistence] Failed to save data to {fileName}: {ex.Message}");
            }
            catch (JsonException ex)
            {
                Plugin.Logger?.LogError($"[Persistence] Failed to serialize data for {fileName}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[Persistence] An unexpected error occurred while saving data to {fileName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads data from a JSON file in the BepInEx config directory
        /// </summary>
        /// <typeparam name="T">The type of data to load</typeparam>
        /// <param name="configFolderName">Name of the config subfolder</param>
        /// <param name="fileName">Name of the file to load from</param>
        /// <returns>The loaded data, or a new instance if the file doesn't exist or is invalid</returns>
        public static T LoadFromFile<T>(string configFolderName, string fileName) where T : new()
        {
            string fullPath = Path.Combine(Paths.ConfigPath, configFolderName, fileName);

            if (!File.Exists(fullPath))
            {
                Plugin.Logger?.LogInfo($"[Persistence] File not found: {fileName}. Returning new default object.");
                return new T();
            }

            try
            {
                string json = File.ReadAllText(fullPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Plugin.Logger?.LogWarning($"[Persistence] File {fileName} is empty or whitespace. Returning new default object.");
                    return new T();
                }

                var data = JsonSerializer.Deserialize<T>(json, JsonOptions);
                return data ?? new T();
            }
            catch (JsonException ex)
            {
                Plugin.Logger?.LogError($"[Persistence] Failed to deserialize data from {fileName}. Attempting backup restore. Error: {ex.Message}");
                
                // Try to restore from backup
                var backupPath = fullPath + ".bak";
                if (File.Exists(backupPath))
                {
                    try
                    {
                        var backupJson = File.ReadAllText(backupPath);
                        var backupData = JsonSerializer.Deserialize<T>(backupJson, JsonOptions);
                        if (backupData != null)
                        {
                            Plugin.Logger?.LogInfo($"[Persistence] Successfully restored {fileName} from backup");
                            return backupData;
                        }
                    }
                    catch (Exception backupEx)
                    {
                        Plugin.Logger?.LogError($"[Persistence] Backup restore failed: {backupEx.Message}");
                    }
                }
                
                return new T();
            }
            catch (IOException ex)
            {
                Plugin.Logger?.LogError($"[Persistence] Failed to load data from {fileName}. A new default object will be used. Error: {ex.Message}");
                return new T();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[Persistence] An unexpected error occurred while loading data from {fileName}. A new default object will be used. Error: {ex.Message}");
                return new T();
            }
        }
    }
}
