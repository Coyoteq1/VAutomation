using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using CrowbaneArena.Data;
using CrowbaneArena.Models;
using CrowbaneArena.Helpers;
using Stunlock.Core;

namespace CrowbaneArena.Services
{
    public class DataIntegrityService
    {
        private readonly List<string> _validationErrors = new();
        private readonly List<string> _validationWarnings = new();
        private readonly Dictionary<string, int> _guidUsageCount = new();

        public void ValidateAllData()
        {
            _validationErrors.Clear();
            _validationWarnings.Clear();
            _guidUsageCount.Clear();

            Plugin.Logger?.LogInfo("Starting comprehensive data validation...");

            ValidateConfigFiles();
            ValidatePrefabMappings();
            ValidateLoadouts();
            ValidateSnapshotFiles();
            ValidateWeaponMods();
            ValidateBloodTypes();
            CheckGUIDConflicts();

            PrintValidationResults();
        }

        private void ValidateConfigFiles()
        {
            try
            {
                // Validate arena_config.json
                var configPath = Path.Combine(Plugin.DataPath, "config", "arena_config.json");
                if (File.Exists(configPath))
                {
                    var content = File.ReadAllText(configPath);
                    if (IsValidJson(content))
                    {
                        Plugin.Logger?.LogInfo("arena_config.json is valid JSON");
                    }
                    else
                    {
                        _validationErrors.Add("arena_config.json contains invalid JSON");
                    }
                }

                // Validate CFG files
                ValidateCfgFile(Path.Combine(Plugin.DataPath, "config", "arena_config.cfg"));
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"Error validating config files: {ex.Message}");
            }
        }

        private void ValidatePrefabMappings()
        {
            try
            {
                // Test weapon mappings
                foreach (var weapon in Prefabs.Weapons)
                {
                    if (weapon.Value == PrefabGUID.Empty)
                    {
                        _validationErrors.Add($"Weapon '{weapon.Key}' has empty GUID");
                    }
                    else
                    {
                        _guidUsageCount[GuidConverter.GetGuidHash(weapon.Value).ToString()] = _guidUsageCount.GetValueOrDefault(GuidConverter.GetGuidHash(weapon.Value).ToString()) + 1;
                    }
                }

                // Test boss mappings
                foreach (var boss in PrefabMappings.GetBossMap())
                {
                    if (boss.Value == PrefabGUID.Empty)
                    {
                        _validationErrors.Add($"Boss '{boss.Key}' has empty GUID");
                    }
                }
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"Error validating prefab mappings: {ex.Message}");
            }
        }

        private void ValidateLoadouts()
        {
            try
            {
                foreach (var loadout in Loadouts.All)
                {
                    // Validate weapons exist
                    foreach (var weapon in loadout.Weapons)
                    {
                        if (!Prefabs.Weapons.ContainsKey(weapon))
                        {
                            _validationErrors.Add($"Loadout '{loadout.Name}' references unknown weapon '{weapon}'");
                        }
                    }

                    // Validate armor sets exist
                    foreach (var armorSet in loadout.ArmorSets)
                    {
                        if (!Prefabs.ArmorSets.ContainsKey(armorSet))
                        {
                            _validationErrors.Add($"Loadout '{loadout.Name}' references unknown armor set '{armorSet}'");
                        }
                    }

                    // Validate blood type
                    if (string.IsNullOrEmpty(loadout.BloodType))
                    {
                        _validationErrors.Add($"Loadout '{loadout.Name}' has empty blood type");
                    }
                }
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"Error validating loadouts: {ex.Message}");
            }
        }

        private void ValidateSnapshotFiles()
        {
            try
            {
                var snapshotsPath = Path.Combine(Plugin.DataPath, "Snapshots");
                if (Directory.Exists(snapshotsPath))
                {
                    var snapshotFiles = Directory.GetFiles(snapshotsPath, "*.json", SearchOption.AllDirectories);
                    foreach (var file in snapshotFiles)
                    {
                        ValidateSnapshotFile(file);
                    }
                }
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"Error validating snapshot files: {ex.Message}");
            }
        }

        private void ValidateSnapshotFile(string filePath)
        {
            try
            {
                var content = File.ReadAllText(filePath);
                if (IsValidJson(content))
                {
                    var jsonDoc = JsonDocument.Parse(content);
                    var root = jsonDoc.RootElement;

                    // Check required fields
                    if (!root.TryGetProperty("SnapshotId", out _))
                        _validationErrors.Add($"Snapshot file {Path.GetFileName(filePath)} missing SnapshotId");

                    if (!root.TryGetProperty("CreatedAtUtc", out _))
                        _validationErrors.Add($"Snapshot file {Path.GetFileName(filePath)} missing CreatedAtUtc");

                    if (!root.TryGetProperty("SchemaVersion", out _))
                        _validationErrors.Add($"Snapshot file {Path.GetFileName(filePath)} missing SchemaVersion");

                    Plugin.Logger?.LogInfo($"Snapshot file {Path.GetFileName(filePath)} is valid");
                }
                else
                {
                    _validationErrors.Add($"Snapshot file {Path.GetFileName(filePath)} contains invalid JSON");
                }
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"Error validating snapshot file {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        private void ValidateWeaponMods()
        {
            // Validate weapon mod references in config
            var weaponMods = new Dictionary<string, string>
            {
                ["storm_infusion"] = "Storm infused weapon",
                ["frost_infusion"] = "Frost infused weapon", 
                ["chaos_infusion"] = "Chaos infused weapon",
                ["unholy_infusion"] = "Unholy infused weapon",
                ["blood_infusion"] = "Blood infused weapon",
                ["illusion_infusion"] = "Illusion infused weapon"
            };

            // Add weapon mod validation logic here
        }

        private void ValidateBloodTypes()
        {
            var validBloodTypes = new[] { "Warrior", "Rogue", "Scholar", "Brute", "Creature", "Draculin" };
            
            foreach (var loadout in Loadouts.All)
            {
                if (!validBloodTypes.Contains(loadout.BloodType, StringComparer.OrdinalIgnoreCase))
                {
                    _validationErrors.Add($"Loadout '{loadout.Name}' has invalid blood type '{loadout.BloodType}'");
                }
            }
        }

        private void CheckGUIDConflicts()
        {
            foreach (var kvp in _guidUsageCount)
            {
                if (kvp.Value > 1)
                {
                    _validationWarnings.Add($"GUID {kvp.Key} is used {kvp.Value} times - potential duplicate");
                }
            }
        }

        private void ValidateCfgFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var content = File.ReadAllText(filePath);
                    var lines = content.Split('\n');
                    
                    for (int i = 0; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        
                        // Skip comments and empty lines
                        if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                            continue;

                        // Basic CFG format validation
                        if (line.Contains("="))
                        {
                            var parts = line.Split('=', 2);
                            if (parts.Length != 2)
                            {
                                _validationErrors.Add($"CFG file {Path.GetFileName(filePath)} line {i + 1}: Invalid key=value format");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _validationErrors.Add($"Error validating CFG file {Path.GetFileName(filePath)}: {ex.Message}");
            }
        }

        private bool IsValidJson(string json)
        {
            try
            {
                JsonDocument.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void PrintValidationResults()
        {
            if (_validationErrors.Count == 0 && _validationWarnings.Count == 0)
            {
                Plugin.Logger?.LogInfo("✅ Data validation completed - all checks passed");
                return;
            }

            if (_validationErrors.Count > 0)
            {
                Plugin.Logger?.LogError($"❌ Data validation found {_validationErrors.Count} errors:");
                foreach (var error in _validationErrors)
                {
                    Plugin.Logger?.LogError($"  - {error}");
                }
            }

            if (_validationWarnings.Count > 0)
            {
                Plugin.Logger?.LogWarning($"⚠️  Data validation found {_validationWarnings.Count} warnings:");
                foreach (var warning in _validationWarnings)
                {
                    Plugin.Logger?.LogWarning($"  - {warning}");
                }
            }
        }

        public List<string> GetValidationErrors() => new(_validationErrors);
        public List<string> GetValidationWarnings() => new(_validationWarnings);
    }
}