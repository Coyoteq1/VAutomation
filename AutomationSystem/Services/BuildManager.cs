using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CrowbaneArena.Services;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Manages arena build presets loaded from builds.json
    /// Only active when players are in arena mode
    /// </summary>
    public static class BuildManager
    {
        private static readonly string FileDirectory = Path.Combine("config", "crowbanearena");
        private const string BuildFile = "builds.json";
        private static readonly string BuildPath = Path.Combine(FileDirectory, BuildFile);
        private static Dictionary<string, BuildModel> Builds = new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Gets the total number of loaded builds
        /// </summary>
        public static int GetBuildCount() => Builds.Count;

        /// <summary>
        /// Loads build data from builds.json in the config directory
        /// </summary>
        public static void LoadData()
        {
            try
            {
                if (!File.Exists(BuildPath))
                {
                    Plugin.Logger?.LogInfo($"Builds.json not found in {BuildPath}, creating default builds");
                    CreateDefaultBuildsFile();
                    return;
                }

                var jsonString = File.ReadAllText(BuildPath);
                var tempDict = JsonSerializer.Deserialize<Dictionary<string, BuildModel>>(jsonString,
                    new JsonSerializerOptions
                    {
                        AllowTrailingCommas = true
                    });

                if (tempDict != null)
                {
                    Builds = new Dictionary<string, BuildModel>(tempDict, StringComparer.OrdinalIgnoreCase);
                    Plugin.Logger?.LogInfo($"Loaded {Builds.Count} builds from builds.json");
                }
                else
                {
                    Plugin.Logger?.LogWarning("Failed to deserialize builds.json, using empty builds");
                    Builds = new Dictionary<string, BuildModel>(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error loading builds: {ex.Message}");
                Builds = new Dictionary<string, BuildModel>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Creates a default builds.json file with basic arena builds
        /// </summary>
        private static void CreateDefaultBuildsFile()
        {
            try
            {
                if (!Directory.Exists(FileDirectory))
                {
                    Directory.CreateDirectory(FileDirectory);
                }

                // Copy the builds.json from the downloaded repository
                var sourcePath = Path.Combine("temp_builds", "VRisingArenaBuilds-master", "builds.json");
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, BuildPath, true);
                    Plugin.Logger?.LogInfo($"Copied default builds.json to {BuildPath}");

                    // Reload after copying
                    LoadData();
                }
                else
                {
                    // Fallback: create empty builds file
                    var emptyBuilds = new Dictionary<string, BuildModel> { { "default", new BuildModel() } };
                    var json = JsonSerializer.Serialize(emptyBuilds, new JsonSerializerOptions
                    {
                        WriteIndented = true,
                    });
                    File.WriteAllText(BuildPath, json);
                    Plugin.Logger?.LogInfo($"Created empty builds.json at {BuildPath}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error creating default builds file: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets a formatted list of available build names
        /// </summary>
        public static string GetBuildList()
        {
            if (Builds == null || Builds.Count == 0)
            {
                return "No builds available";
            }

            return string.Join(", ", Builds.Keys);
        }

        /// <summary>
        /// Gets a specific build by name (case-insensitive)
        /// </summary>
        public static BuildModel? GetBuild(string buildName)
        {
            if (Builds.TryGetValue(buildName, out var build))
            {
                return build;
            }
            return null;
        }

        /// <summary>
        /// Checks if a build exists
        /// </summary>
        public static bool BuildExists(string buildName)
        {
            return Builds.ContainsKey(buildName);
        }
    }

    /// <summary>
    /// Model representing a build configuration
    /// </summary>
    public class BuildModel
    {
        public BuildSettings Settings { get; set; } = new();
        public BloodConfig Blood { get; set; } = new();
        public ArmorConfig Armors { get; set; } = new();
        public List<WeaponConfig> Weapons { get; set; } = new();
        public List<ItemConfig> Items { get; set; } = new();
        public AbilityConfig Abilities { get; set; } = new();
        public PassiveSpellsConfig PassiveSpells { get; set; } = new();
    }

    public class BuildSettings
    {
        public bool ClearInventory { get; set; } = true;
    }

    public class BloodConfig
    {
        public bool FillBloodPool { get; set; } = true;
        public bool GiveBloodPotion { get; set; } = false;
        public string PrimaryType { get; set; } = "BloodType_Draculin";
        public string SecondaryType { get; set; } = "BloodType_Scholar";
        public int PrimaryQuality { get; set; } = 100;
        public int SecondaryQuality { get; set; } = 100;
        public int SecondaryBuffIndex { get; set; } = 2;
    }

    public class ArmorConfig
    {
        public string Boots { get; set; } = "";
        public string Chest { get; set; } = "";
        public string Gloves { get; set; } = "";
        public string Legs { get; set; } = "";
        public string MagicSource { get; set; } = "";
        public string Head { get; set; } = "";
        public string Cloak { get; set; } = "";
        public string Bag { get; set; } = "";
    }

    public class WeaponConfig
    {
        public string Name { get; set; } = "";
        public string InfuseSpellMod { get; set; } = "";
        public string SpellMod1 { get; set; } = "";
        public string SpellMod2 { get; set; } = "";
        public string StatMod1 { get; set; } = "";
        public double StatMod1Power { get; set; } = 1.0;
        public string StatMod2 { get; set; } = "";
        public double StatMod2Power { get; set; } = 1.0;
        public string StatMod3 { get; set; } = "";
        public double StatMod3Power { get; set; } = 1.0;
        public string StatMod4 { get; set; } = "";
        public double StatMod4Power { get; set; } = 1.0;
    }

    public class ItemConfig
    {
        public string Name { get; set; } = "";
        public int Amount { get; set; } = 1;
    }

    public class AbilityConfig
    {
        public AbilityInfo Travel { get; set; } = new();
        public AbilityInfo Ability1 { get; set; } = new();
        public AbilityInfo Ability2 { get; set; } = new();
        public AbilityInfo Ultimate { get; set; } = new();
    }

    public class AbilityInfo
    {
        public string Name { get; set; } = "";
        public JewelConfig Jewel { get; set; } = new();
    }

    public class JewelConfig
    {
        public string SpellMod1 { get; set; } = "";
        public double SpellMod1Power { get; set; } = 1.0;
        public string SpellMod2 { get; set; } = "";
        public double SpellMod2Power { get; set; } = 1.0;
        public string SpellMod3 { get; set; } = "";
        public double SpellMod3Power { get; set; } = 1.0;
        public string SpellMod4 { get; set; } = "";
        public double SpellMod4Power { get; set; } = 1.0;
    }

    public class PassiveSpellsConfig
    {
        public string PassiveSpell1 { get; set; } = "";
        public string PassiveSpell2 { get; set; } = "";
        public string PassiveSpell3 { get; set; } = "";
        public string PassiveSpell4 { get; set; } = "";
        public string PassiveSpell5 { get; set; } = "";
    }
}
