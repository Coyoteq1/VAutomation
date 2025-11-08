using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CrowbaneArena.Data;
using CrowbaneArena.Services;
using Stunlock.Core;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// LoadoutManager provides centralized management of loadout configurations.
    /// Handles loading, saving, and managing custom loadouts alongside default ones.
    /// Similar to ArenaBuilds BuildManager but adapted for VRising loadouts.
    /// </summary>
    internal static class LoadoutManager
    {
        private static readonly string FileDirectory = Path.Combine("BepInEx", "config", "CrowbaneArena");
        private const string LoadoutFile = "loadouts.json";
        private static readonly string LoadoutPath = Path.Combine(FileDirectory, LoadoutFile);

        /// <summary>
        /// Custom loadouts loaded from JSON file (case-insensitive keys)
        /// </summary>
        public static Dictionary<string, LoadoutDefinition> CustomLoadouts { get; private set; } =
            new Dictionary<string, LoadoutDefinition>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Get all available loadouts (custom + default)
        /// </summary>
        public static Dictionary<string, LoadoutDefinition> AllLoadouts
        {
            get
            {
                var allLoadouts = new Dictionary<string, LoadoutDefinition>(StringComparer.OrdinalIgnoreCase);

                // Add default loadouts first
                foreach (var defaultLoadout in DefaultDataService.GetAllDefaultLoadouts())
                {
                    allLoadouts[defaultLoadout.Key] = defaultLoadout.Value;
                }

                // Add/override with custom loadouts
                foreach (var customLoadout in CustomLoadouts)
                {
                    allLoadouts[customLoadout.Key] = customLoadout.Value;
                }

                return allLoadouts;
            }
        }

        /// <summary>
        /// Load custom loadouts from JSON file. Creates file if it doesn't exist.
        /// </summary>
        public static void LoadData()
        {
            try
            {
                if (!File.Exists(LoadoutPath))
                {
                    Plugin.Logger?.LogInfo($"Loadouts.json not found in {LoadoutPath}");
                    CreateDefaultLoadoutsFile();
                    return;
                }

                var jsonString = File.ReadAllText(LoadoutPath);
                var tempDict = JsonSerializer.Deserialize<Dictionary<string, LoadoutDefinition>>(jsonString,
                    new JsonSerializerOptions
                    {
                        AllowTrailingCommas = true,
                        PropertyNameCaseInsensitive = true
                    });

                if (tempDict != null)
                {
                    CustomLoadouts = new Dictionary<string, LoadoutDefinition>(tempDict, StringComparer.OrdinalIgnoreCase);
                    Plugin.Logger?.LogInfo($"Loaded {CustomLoadouts.Count} custom loadouts from loadouts.json");
                }
                else
                {
                    Plugin.Logger?.LogWarning("Failed to deserialize loadouts.json, using empty collection");
                    CustomLoadouts = new Dictionary<string, LoadoutDefinition>(StringComparer.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error loading loadouts.json: {ex.Message}");
                CustomLoadouts = new Dictionary<string, LoadoutDefinition>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Save current custom loadouts to JSON file.
        /// </summary>
        public static void SaveData()
        {
            try
            {
                if (!Directory.Exists(FileDirectory))
                {
                    Directory.CreateDirectory(FileDirectory);
                }

                var json = JsonSerializer.Serialize(CustomLoadouts, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    AllowTrailingCommas = true
                });

                File.WriteAllText(LoadoutPath, json);
                Plugin.Logger?.LogInfo($"Saved {CustomLoadouts.Count} custom loadouts to loadouts.json");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error saving loadouts.json: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Create default loadouts.json file with sample loadouts.
        /// </summary>
        private static void CreateDefaultLoadoutsFile()
        {
            try
            {
                if (!Directory.Exists(FileDirectory))
                {
                    Directory.CreateDirectory(FileDirectory);
                }

                // Create sample loadouts based on hardcoded defaults
                var sampleLoadouts = new Dictionary<string, LoadoutDefinition>();

                // Add a few sample custom loadouts
                sampleLoadouts["custom_warrior"] = new LoadoutDefinition
                {
                    Name = "custom_warrior",
                    Description = "Custom warrior loadout with unique weapons",
                    BloodType = "Warrior",
                    BloodQuality = 100f,
                    Weapons = new List<WeaponDefinition>
                    {
                        new WeaponDefinition
                        {
                            PrefabName = "Item_Weapon_GreatSword_Unique_T08_Variation01",
                            Guid = 147836723,
                            SpellSchool = "Chaos",
                            StatMod = "Physical Power"
                        }
                    },
                    Armor = new ArmorDefinition
                    {
                        Chest = new PrefabGUID(1392314162),  // Dracula Warrior Chest
                        Legs = new PrefabGUID(205207385),    // Dracula Warrior Legs
                        Boots = new PrefabGUID(-382349289),  // Dracula Warrior Boots
                        Gloves = new PrefabGUID(1982551454)  // Dracula Warrior Gloves
                    },
                    Consumables = new List<ConsumableDefinition>
                    {
                        new ConsumableDefinition { Guid = -1531666018, Amount = 10 }, // Blood Potion
                        new ConsumableDefinition { Guid = 1223264867, Amount = 5 }   // Physical Power Potion
                    },
                    Abilities = new List<string>
                    {
                        "AB_Vampire_VeilOfChaos_Group",
                        "AB_Chaos_MercilessCharge_AbilityGroup"
                    }
                };

                sampleLoadouts["custom_mage"] = new LoadoutDefinition
                {
                    Name = "custom_mage",
                    Description = "Custom mage loadout with spell focus",
                    BloodType = "Scholar",
                    BloodQuality = 100f,
                    Weapons = new List<WeaponDefinition>
                    {
                        new WeaponDefinition
                        {
                            PrefabName = "Item_Weapon_Spear_Unique_T08_Variation01",
                            Guid = 1532449451,
                            SpellSchool = "Frost",
                            StatMod = "Spell Power"
                        }
                    },
                    Armor = new ArmorDefinition
                    {
                        Chest = new PrefabGUID(114259912),   // Dracula Scholar Chest
                        Legs = new PrefabGUID(1592149279),   // Dracula Scholar Legs
                        Boots = new PrefabGUID(1531721602),  // Dracula Scholar Boots
                        Gloves = new PrefabGUID(-1899539896) // Dracula Scholar Gloves
                    },
                    Consumables = new List<ConsumableDefinition>
                    {
                        new ConsumableDefinition { Guid = -1531666018, Amount = 8 }, // Blood Potion
                        new ConsumableDefinition { Guid = -437611596, Amount = 3 }   // Minor Blood Potion
                    },
                    Abilities = new List<string>
                    {
                        "AB_Illusion_PhantomAegis_AbilityGroup",
                        "AB_Illusion_WispDance_AbilityGroup"
                    }
                };

                var json = JsonSerializer.Serialize(sampleLoadouts, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    AllowTrailingCommas = true
                });

                File.WriteAllText(LoadoutPath, json);
                Plugin.Logger?.LogInfo($"Created default loadouts.json at {LoadoutPath} with {sampleLoadouts.Count} sample loadouts");

                // Load the newly created file
                LoadData();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error creating default loadouts.json: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Get formatted list of all available loadouts (custom + default).
        /// </summary>
        public static string GetLoadoutList()
        {
            var allLoadouts = AllLoadouts;
            if (allLoadouts == null || allLoadouts.Count == 0)
            {
                return "No loadouts available";
            }

            var loadoutNames = allLoadouts.Keys.ToList();
            return string.Join(", ", loadoutNames.OrderBy(name => name));
        }

        /// <summary>
        /// Get formatted list of only custom loadouts.
        /// </summary>
        public static string GetCustomLoadoutList()
        {
            if (CustomLoadouts == null || CustomLoadouts.Count == 0)
            {
                return "No custom loadouts available";
            }

            var loadoutNames = CustomLoadouts.Keys.ToList();
            return string.Join(", ", loadoutNames.OrderBy(name => name));
        }

        /// <summary>
        /// Add or update a custom loadout.
        /// </summary>
        public static void AddOrUpdateLoadout(string name, LoadoutDefinition loadout)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Loadout name cannot be empty", nameof(name));

            if (loadout == null)
                throw new ArgumentNullException(nameof(loadout));

            // Ensure the loadout name matches the key
            loadout.Name = name;

            CustomLoadouts[name] = loadout;
            Plugin.Logger?.LogInfo($"Added/updated custom loadout: {name}");
        }

        /// <summary>
        /// Remove a custom loadout.
        /// </summary>
        public static bool RemoveLoadout(string name)
        {
            if (CustomLoadouts.Remove(name))
            {
                Plugin.Logger?.LogInfo($"Removed custom loadout: {name}");
                return true;
            }

            Plugin.Logger?.LogWarning($"Custom loadout not found: {name}");
            return false;
        }

        /// <summary>
        /// Get a loadout by name (checks custom first, then defaults).
        /// </summary>
        public static LoadoutDefinition GetLoadout(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            // Check custom loadouts first
            if (CustomLoadouts.TryGetValue(name, out var customLoadout))
                return customLoadout;

            // Check default loadouts
            var defaultLoadouts = DefaultDataService.GetAllDefaultLoadouts();
            if (defaultLoadouts.TryGetValue(name, out var defaultLoadout))
                return defaultLoadout;

            return null;
        }

        /// <summary>
        /// Check if a loadout exists (custom or default).
        /// </summary>
        public static bool LoadoutExists(string name)
        {
            return CustomLoadouts.ContainsKey(name) ||
                   DefaultDataService.GetAllDefaultLoadouts().ContainsKey(name);
        }

        /// <summary>
        /// Get loadout statistics.
        /// </summary>
        public static (int customCount, int defaultCount, int totalCount) GetLoadoutStats()
        {
            int customCount = CustomLoadouts?.Count ?? 0;
            int defaultCount = DefaultDataService.GetAllDefaultLoadouts().Count;
            int totalCount = customCount + defaultCount;

            return (customCount, defaultCount, totalCount);
        }
    }
}
