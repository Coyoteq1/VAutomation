using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using Unity.Mathematics;
using CrowbaneArena.Models;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Parser for .cfg configuration files commonly used in V Rising mods
    /// </summary>
    public static class CfgConfigParser
    {
        /// <summary>
        /// Parse boss configuration from CFG file
        /// </summary>
        public static BossConfig ParseBossConfig(string filePath)
        {
            // Placeholder implementation - return default config
            return new BossConfig
            {
                Bosses = new Dictionary<string, BossService.BossData>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Alphawolf"] = new BossService.BossData
                    {
                        Name = "Alphawolf",
                        VBloodId = -1905691330,
                        Level = 16,
                        Region = "Farbane Woods",
                        Rewards = new List<string> { "WolfVBlood", "Leather" }
                    },
                    ["Keely"] = new BossService.BossData
                    {
                        Name = "Keely",
                        VBloodId = -1342764880,
                        Level = 20,
                        Region = "Farbane Woods",
                        Rewards = new List<string> { "BanditVBlood", "Copper" }
                    }
                    }
            };
        }
        /// <summary>
        /// Parse a .cfg file and convert it to ArenaConfig
        /// </summary>
        public static ArenaConfig ParseCfgFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Configuration file not found: {filePath}");
            }

            var lines = File.ReadAllLines(filePath);
            var sections = ParseSections(lines);
            
            return BuildArenaConfig(sections);
        }

        /// <summary>
        /// Parse lines into sections and key-value pairs
        /// </summary>
        private static Dictionary<string, Dictionary<string, string>> ParseSections(string[] lines)
        {
            var sections = new Dictionary<string, Dictionary<string, string>>();
            string currentSection = "";
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                // Skip empty lines and comments
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;
                
                // Section header
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                    if (!sections.ContainsKey(currentSection))
                    {
                        sections[currentSection] = new Dictionary<string, string>();
                    }
                    continue;
                }
                
                // Key-value pair
                var equalIndex = trimmed.IndexOf('=');
                if (equalIndex > 0 && !string.IsNullOrEmpty(currentSection))
                {
                    var key = trimmed.Substring(0, equalIndex).Trim();
                    var value = trimmed.Substring(equalIndex + 1).Trim();
                    sections[currentSection][key] = value;
                }
            }
            
            return sections;
        }

        /// <summary>
        /// Build ArenaConfig from parsed sections
        /// </summary>
        private static ArenaConfig BuildArenaConfig(Dictionary<string, Dictionary<string, string>> sections)
        {
            var config = new ArenaConfig();
            
            // General settings
            if (sections.TryGetValue("General", out var general))
            {
                config.Enabled = GetBool(general, "Enabled", true);
            }
            
            // Location settings
            config.Location = ParseLocation(sections);
            
            // Portal settings
            config.Portal = ParsePortal(sections);
            
            // Gameplay settings
            config.Gameplay = ParseGameplay(sections);
            
            // Weapons
            config.Weapons = ParseWeapons(sections);
            
            // Armor sets
            config.ArmorSets = ParseArmorSets(sections);
            
            // Consumables
            config.Consumables = ParseConsumables(sections);
            
            // Loadouts
            config.Loadouts = ParseLoadouts(sections);
            
            return config;
        }

        private static ZoneLocation ParseLocation(Dictionary<string, Dictionary<string, string>> sections)
        {
            var location = new ZoneLocation();
            
            if (sections.TryGetValue("Arena.Location", out var locationSection))
            {
                location.Center = new float3(
                    GetFloat(locationSection, "CenterX", -1000f),
                    GetFloat(locationSection, "CenterY", 0f),
                    GetFloat(locationSection, "CenterZ", -500f)
                );
                
                location.Radius = GetFloat(locationSection, "Radius", 60f);
                
                location.SpawnPoint = new float3(
                    GetFloat(locationSection, "SpawnX", -1000f),
                    GetFloat(locationSection, "SpawnY", 0f),
                    GetFloat(locationSection, "SpawnZ", -500f)
                );
            }
            
            return location;
        }

        private static PortalConfig ParsePortal(Dictionary<string, Dictionary<string, string>> sections)
        {
            var portal = new PortalConfig();
            
            // Entry portal
            if (sections.TryGetValue("Arena.Portal.Entry", out var entrySection))
            {
                portal.Entry = new ZoneLocation
                {
                    Center = new float3(
                        GetFloat(entrySection, "CenterX", -950f),
                        GetFloat(entrySection, "CenterY", 0f),
                        GetFloat(entrySection, "CenterZ", -450f)
                    ),
                    Radius = GetFloat(entrySection, "Radius", 15f)
                };
            }
            
            // Exit portal
            if (sections.TryGetValue("Arena.Portal.Exit", out var exitSection))
            {
                portal.Exit = new ZoneLocation
                {
                    Center = new float3(
                        GetFloat(exitSection, "CenterX", -1050f),
                        GetFloat(exitSection, "CenterY", 0f),
                        GetFloat(exitSection, "CenterZ", -550f)
                    ),
                    Radius = GetFloat(exitSection, "Radius", 15f)
                };
            }
            
            return portal;
        }

        private static GameplaySettings ParseGameplay(Dictionary<string, Dictionary<string, string>> sections)
        {
            var gameplay = new GameplaySettings();
            
            if (sections.TryGetValue("Gameplay", out var gameplaySection))
            {
                gameplay.EnableGodMode = GetBool(gameplaySection, "EnableGodMode", true);
                gameplay.RestoreOnExit = GetBool(gameplaySection, "RestoreOnExit", true);
                gameplay.AllowPvP = GetBool(gameplaySection, "AllowPvP", true);
                gameplay.VBloodProgression = GetBool(gameplaySection, "VBloodProgression", false);
            }
            
            return gameplay;
        }

        private static List<Weapon> ParseWeapons(Dictionary<string, Dictionary<string, string>> sections)
        {
            var weapons = new List<Weapon>();
            
            foreach (var section in sections.Where(s => s.Key.StartsWith("Weapons.")))
            {
                var weaponName = section.Key.Substring("Weapons.".Length);
                var weaponData = section.Value;
                
                var weapon = new Weapon
                {
                    Name = GetString(weaponData, "Name", weaponName.ToLower()),
                    Description = GetString(weaponData, "Description", ""),
                    Guid = GetInt(weaponData, "Guid", 0),
                    Enabled = GetBool(weaponData, "Enabled", true),
                    Variants = ParseWeaponVariants(weaponData)
                };
                
                weapons.Add(weapon);
            }
            
            return weapons;
        }

        private static List<WeaponVariant> ParseWeaponVariants(Dictionary<string, string> weaponData)
        {
            var variants = new List<WeaponVariant>();
            var variantGroups = new Dictionary<string, WeaponVariant>();
            
            foreach (var kvp in weaponData.Where(kv => kv.Key.StartsWith("Variant.")))
            {
                var parts = kvp.Key.Split('.');
                if (parts.Length >= 3)
                {
                    var variantName = parts[1];
                    var property = parts[2];
                    
                    if (!variantGroups.ContainsKey(variantName))
                    {
                        variantGroups[variantName] = new WeaponVariant();
                    }
                    
                    var variant = variantGroups[variantName];
                    
                    switch (property)
                    {
                        case "Name":
                            variant.Name = kvp.Value;
                            break;
                        case "ModCombo":
                            variant.ModCombo = kvp.Value;
                            break;
                        case "Guid":
                            variant.VariantGuid = int.Parse(kvp.Value);
                            break;
                        case "FriendlyName":
                            variant.FriendlyName = kvp.Value;
                            break;
                    }
                }
            }

            return variantGroups.Values.ToList();
        }

        private static List<ArmorSet> ParseArmorSets(Dictionary<string, Dictionary<string, string>> sections)
        {
            var armorSets = new List<ArmorSet>();
            
            foreach (var section in sections.Where(s => s.Key.StartsWith("ArmorSets.")))
            {
                var armorName = section.Key.Substring("ArmorSets.".Length);
                var armorData = section.Value;
                
                var armorSet = new ArmorSet
                {
                    Name = GetString(armorData, "Name", armorName.ToLower()),
                    Description = GetString(armorData, "Description", ""),
                    ChestGuid = (uint)GetInt(armorData, "ChestGuid", 0),
                    LegsGuid = (uint)GetInt(armorData, "LegsGuid", 0),
                    BootsGuid = (uint)GetInt(armorData, "BootsGuid", 0),
                    GlovesGuid = (uint)GetInt(armorData, "GlovesGuid", 0),
                    Enabled = GetBool(armorData, "Enabled", true)
                };
                
                armorSets.Add(armorSet);
            }
            
            return armorSets;
        }

        private static List<Consumable> ParseConsumables(Dictionary<string, Dictionary<string, string>> sections)
        {
            var consumables = new List<Consumable>();
            
            foreach (var section in sections.Where(s => s.Key.StartsWith("Consumables.")))
            {
                var consumableName = section.Key.Substring("Consumables.".Length);
                var consumableData = section.Value;
                
                var consumable = new Consumable
                {
                    Name = GetString(consumableData, "Name", consumableName.ToLower()),
                    Guid = (uint)GetInt(consumableData, "Guid", 0),
                    DefaultAmount = GetInt(consumableData, "DefaultAmount", 1),
                    Enabled = GetBool(consumableData, "Enabled", true)
                };
                
                consumables.Add(consumable);
            }
            
            return consumables;
        }

        private static List<Loadout> ParseLoadouts(Dictionary<string, Dictionary<string, string>> sections)
        {
            var loadouts = new List<Loadout>();
            
            // Handle both "Loadouts." prefix and direct loadout section names
            foreach (var section in sections.Where(s =>
                s.Key.StartsWith("Loadouts.") ||
                s.Key.EndsWith("Loadout", StringComparison.OrdinalIgnoreCase)))
            {
                string loadoutName;
                if (section.Key.StartsWith("Loadouts."))
                {
                    loadoutName = section.Key.Substring("Loadouts.".Length);
                }
                else
                {
                    // For sections like "WarriorLoadout", "AssassinLoadout", etc.
                    loadoutName = section.Key.Replace("Loadout", "").ToLower();
                }
                
                var loadoutData = section.Value;
                
                var loadout = new Loadout
                {
                    Name = GetString(loadoutData, "Name", loadoutName.ToLower()),
                    Description = GetString(loadoutData, "Description", ""),
                    Enabled = GetBool(loadoutData, "Enabled", true),
                    Weapons = ParseStringList(GetString(loadoutData, "Weapons", "")),
                    ArmorSets = ParseStringList(GetString(loadoutData, "ArmorSets", "")),
                    Consumables = ParseStringList(GetString(loadoutData, "Consumables", "")),
                    BloodType = GetString(loadoutData, "BloodType", ""),
                    WeaponMods = GetString(loadoutData, "WeaponMods", "")
                };
                
                loadouts.Add(loadout);
            }
            
            return loadouts;
        }

        // Helper methods
        private static string GetString(Dictionary<string, string> section, string key, string defaultValue = "")
        {
            return section.TryGetValue(key, out var value) ? value : defaultValue;
        }

        private static bool GetBool(Dictionary<string, string> section, string key, bool defaultValue = false)
        {
            if (section.TryGetValue(key, out var value))
            {
                return bool.TryParse(value, out var result) ? result : defaultValue;
            }
            return defaultValue;
        }

        private static int GetInt(Dictionary<string, string> section, string key, int defaultValue = 0)
        {
            if (section.TryGetValue(key, out var value))
            {
                return int.TryParse(value, out var result) ? result : defaultValue;
            }
            return defaultValue;
        }

        private static float GetFloat(Dictionary<string, string> section, string key, float defaultValue = 0f)
        {
            if (section.TryGetValue(key, out var value))
            {
                return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
            }
            return defaultValue;
        }

        private static uint GetUint(Dictionary<string, string> section, string key, uint defaultValue = 0)
        {
            if (section.TryGetValue(key, out var value))
            {
                return uint.TryParse(value, out var result) ? result : defaultValue;
            }
            return defaultValue;
        }

        private static List<string> ParseStringList(string value)
        {
            if (string.IsNullOrEmpty(value))
                return new List<string>();

            return value.Split(',')
                       .Select(s => s.Trim())
                       .Where(s => !string.IsNullOrEmpty(s))
                       .ToList();
        }

        /// <summary>
        /// Convert ArenaConfig to ArenaConfiguration for service compatibility
        /// </summary>
        public static ArenaConfiguration ConvertToArenaConfiguration(ArenaConfig legacyConfig)
        {
            if (legacyConfig == null)
                return new ArenaConfiguration();

            var config = new ArenaConfiguration();
            
            // Convert loadouts
            if (legacyConfig.Loadouts != null)
            {
                config.Loadouts = new List<ConfigArenaLoadout>();
                foreach (var legacyLoadout in legacyConfig.Loadouts)
                {
                    config.Loadouts.Add(new ConfigArenaLoadout
                    {
                        Name = legacyLoadout.Name,
                        Weapons = legacyLoadout.Weapons ?? new List<string>(),
                        ArmorSets = legacyLoadout.ArmorSets ?? new List<string>(),
                        Consumables = legacyLoadout.Consumables ?? new List<string>(),
                        WeaponMods = string.Empty,
                        BloodType = string.Empty,
                        Enabled = legacyLoadout.Enabled
                    });
                }
            }
            
            // Convert zones
            if (legacyConfig.Zones != null)
            {
                config.Zones = new List<ArenaZone>();
                foreach (var legacyZone in legacyConfig.Zones)
                {
                    config.Zones.Add(new ArenaZone
                    {
                        Name = legacyZone.Name,
                        Enabled = legacyZone.Enabled,
                        SpawnX = legacyZone.SpawnX,
                        SpawnY = legacyZone.SpawnY,
                        SpawnZ = legacyZone.SpawnZ,
                        Radius = legacyZone.Radius,
                        EntryX = legacyZone.SpawnX,
                        EntryY = legacyZone.SpawnY,
                        EntryZ = legacyZone.SpawnZ,
                        EntryRadius = 10f,
                        ExitX = legacyZone.SpawnX,
                        ExitY = legacyZone.SpawnY,
                        ExitZ = legacyZone.SpawnZ,
                        ExitRadius = 10f
                    });
                }
            }
            
            // Convert weapons
            if (legacyConfig.Weapons != null)
            {
                config.Weapons = new List<WeaponData>();
                foreach (var legacyWeapon in legacyConfig.Weapons)
                {
                    config.Weapons.Add(new WeaponData
                    {
                        Name = legacyWeapon.Name,
                        Guid = legacyWeapon.Guid
                    });
                }
            }
            
            // Convert armor sets
            if (legacyConfig.ArmorSets != null)
            {
                config.ArmorSets = new List<ArmorData>();
                foreach (var legacyArmor in legacyConfig.ArmorSets)
                {
                    config.ArmorSets.Add(new ArmorData
                    {
                        Name = legacyArmor.Name,
                        ChestGuid = legacyArmor.ChestGuid,
                        LegsGuid = legacyArmor.LegsGuid,
                        BootsGuid = legacyArmor.BootsGuid,
                        GlovesGuid = legacyArmor.GlovesGuid
                    });
                }
            }
            
            // Convert consumables
            if (legacyConfig.Consumables != null)
            {
                config.Consumables = new List<ConsumableData>();
                foreach (var legacyConsumable in legacyConfig.Consumables)
                {
                    config.Consumables.Add(new ConsumableData
                    {
                        Name = legacyConsumable.Name,
                        Guid = legacyConsumable.Guid,
                        DefaultAmount = legacyConsumable.DefaultAmount
                    });
                }
            }
            
            return config;
        }
    }
}
