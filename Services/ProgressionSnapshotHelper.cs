using System;
using System.IO;
using Newtonsoft.Json;
using CrowbaneArena.Models;

namespace CrowbaneArena.Services
{
    public static class ProgressionSnapshotHelper
    {
        public static string Serialize(ProgressionSnapshot snapshot)
        {
            try
            {
                return JsonConvert.SerializeObject(snapshot, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to serialize progression snapshot: {ex.Message}");
                throw;
            }
        }

        public static ProgressionSnapshot Deserialize(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<ProgressionSnapshot>(json);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to deserialize progression snapshot: {ex.Message}");
                throw;
            }
        }

        public static void SaveToFile(ProgressionSnapshot snapshot, string filePath)
        {
            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = Serialize(snapshot);
                File.WriteAllText(filePath, json);

                Plugin.Logger?.LogInfo($"Saved progression snapshot to {filePath}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to save progression snapshot to file: {ex.Message}");
                throw;
            }
        }

        public static ProgressionSnapshot LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Progression snapshot file not found", filePath);
                }

                string json = File.ReadAllText(filePath);
                var snapshot = Deserialize(json);

                Plugin.Logger?.LogInfo($"Loaded progression snapshot from {filePath}");
                return snapshot;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to load progression snapshot from file: {ex.Message}");
                throw;
            }
        }

        public static bool Validate(ProgressionSnapshot snapshot)
        {
            try
            {
                if (snapshot == null)
                    return false;

                if (string.IsNullOrEmpty(snapshot.SnapshotId))
                    return false;

                if (snapshot.SchemaVersion < 1)
                    return false;

                if (string.IsNullOrEmpty(snapshot.CharacterName))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
