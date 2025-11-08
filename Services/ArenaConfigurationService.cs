using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using Stunlock.Core;
using ProjectM;
using CrowbaneArena.Services;
using CrowbaneArena.Core;
using CrowbaneArena.Helpers;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Consolidated configuration service that manages all arena-related configurations.
    /// Provides a single point of access for all configuration data.
    /// </summary>
    public static class ArenaConfigurationService
    {
        private static ArenaConfiguration _currentConfig;
        private static readonly object _configLock = new object();
        private static bool _isInitialized = false;

        /// <summ
        /// Gets the current arena configuration.
        /// </summary>
        public static ArenaConfiguration CurrentConfig
        {
            get
            {
                if (_currentConfig == null && !_isInitialized)
                {
                    Initialize();
                }
                return _currentConfig ?? new ArenaConfiguration();
            }
        }

        /// <summary>
        /// Gets whether the configuration service is initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes the configuration service by loading configuration files.
        /// </summary>
        public static void Initialize()
        {
            lock (_configLock)
            {
                if (_isInitialized)
                    return;

                try
                {
                    Plugin.Logger?.LogInfo("Initializing ArenaConfigurationService...");
                    
                    _currentConfig = LoadConfiguration();
                    _isInitialized = true;
                    
                    Plugin.Logger?.LogInfo($"ArenaConfigurationService initialized with {GetTotalLoadoutCount()} loadouts");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"Failed to initialize ArenaConfigurationService: {ex.Message}");
                    _currentConfig = new ArenaConfiguration();
                    _isInitialized = true; // Mark as initialized even on failure to prevent repeated attempts
                }
            }
        }

        /// <summary>
        /// Reloads the configuration from disk.
        /// </summary>
        public static void ReloadConfiguration()
        {
            try
            {
                Plugin.Logger?.LogInfo("Reloading arena configuration...");
                
                var newConfig = LoadConfiguration();
                
                lock (_configLock)
                {
                    _currentConfig = newConfig;
                }
                
                Plugin.Logger?.LogInfo("Arena configuration reloaded successfully");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to reload configuration: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets a loadout by name from the current configuration.
        /// </summary>
        /// <param name="loadoutName">Name of the loadout</param>
        /// <returns>The loadout configuration if found, null otherwise</returns>
        public static ArenaLoadout GetLoadout(string loadoutName)
        {
            if (string.IsNullOrWhiteSpace(loadoutName))
                return null;

            if (TryGetLoadout(loadoutName, out var converted))
            {
                return converted;
            }

            return null;
        }

        /// <summary>
        /// Gets all enabled loadouts.
        /// </summary>
        /// <returns>List of enabled loadouts</returns>
        public static List<ArenaLoadout> GetEnabledLoadouts()
        {
            var config = CurrentConfig;
            var list = new List<ArenaLoadout>();

            if (config.Loadouts == null) return list;

            foreach (var loadout in config.Loadouts)
            {
                if (!loadout.Enabled) continue;

                var converted = ConvertConfigToArenaLoadout(loadout);
                if (converted != null)
                {
                    list.Add(converted);
                }
            }

            // Fallback: if no enabled loadouts found in config, use Plugin loadout DB (data-driven defaults)
            if (list.Count == 0 && CrowbaneArena.Plugin.LoadoutsDB?.Count > 0)
            {
                try
                {
                    return CrowbaneArena.Plugin.LoadoutsDB.Values.ToList();
                }
                catch { }
            }

            return list;
        }

        /// <summary>
        /// Gets arena zones from configuration.
        /// </summary>
        /// <returns>List of arena zones</returns>
        public static List<ArenaZone> GetArenaZones()
        {
            var config = CurrentConfig;
            return config.Zones ?? new List<ArenaZone>();
        }

        /// <summary>
        /// Gets enabled arena zones.
        /// </summary>
        /// <returns>List of enabled arena zones</returns>
        public static List<ArenaZone> GetEnabledZones()
        {
            var zones = GetArenaZones();
            return zones.FindAll(z => z.Enabled);
        }

        /// <summary>
        /// Gets the default arena zone (first enabled zone).
        /// </summary>
        /// <returns>The default zone if found, null otherwise</returns>
        public static ArenaZone GetDefaultZone()
        {
            var enabledZones = GetEnabledZones();
            return enabledZones.Count > 0 ? enabledZones[0] : null;
        }

        /// <summary>
        /// Gets the spawn point from the default zone.
        /// </summary>
        /// <returns>Spawn point coordinates</returns>
        public static Unity.Mathematics.float3 GetDefaultSpawnPoint()
        {
            var defaultZone = GetDefaultZone();
            if (defaultZone != null)
            {
                return new Unity.Mathematics.float3(defaultZone.SpawnX, defaultZone.SpawnY, defaultZone.SpawnZ);
            }

            // Fallback to hardcoded location
            return new Unity.Mathematics.float3(-1000f, 0f, -500f);
        }

        /// <summary>
        /// Validates the current configuration.
        /// </summary>
        /// <returns>Configuration validation result</returns>
        public static ConfigurationValidationResult ValidateConfiguration()
        {
            var result = new ConfigurationValidationResult();
            
            try
            {
                var config = CurrentConfig;

                // Validate loadouts
                if (config.Loadouts != null)
                {
                    foreach (var loadout in config.Loadouts)
                    {
                        var loadoutValidation = ValidateLoadout(loadout);
                        if (!loadoutValidation.IsValid)
                        {
                            result.AddError($"Loadout '{loadout.Name}': {loadoutValidation.ErrorMessage}");
                        }
                    }
                }

                // Validate zones
                if (config.Zones != null)
                {
                    foreach (var zone in config.Zones)
                    {
                        var zoneValidation = ValidateZone(zone);
                        if (!zoneValidation.IsValid)
                        {
                            result.AddError($"Zone '{zone.Name}': {zoneValidation.ErrorMessage}");
                        }
                    }
                }

                Plugin.Logger?.LogInfo($"Configuration validation completed: {result.ErrorCount} errors found");
            }
            catch (Exception ex)
            {
                result.AddError($"Validation failed: {ex.Message}");
                Plugin.Logger?.LogError($"Configuration validation error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets configuration statistics for monitoring.
        /// </summary>
        /// <returns>Configuration statistics</returns>
        public static ConfigurationStats GetConfigurationStats()
        {
            var config = CurrentConfig;
            
            return new ConfigurationStats
            {
                TotalLoadouts = config.Loadouts?.Count ?? 0,
                EnabledLoadouts = config.Loadouts?.FindAll(l => l.Enabled).Count ?? 0,
                TotalZones = config.Zones?.Count ?? 0,
                EnabledZones = config.Zones?.FindAll(z => z.Enabled).Count ?? 0,
                LastReloadTime = _lastReloadTime,
                IsInitialized = _isInitialized
            };
        }

        private static DateTime _lastReloadTime = DateTime.MinValue;

        private static ArenaConfiguration LoadConfiguration()
        {
            // Try JSON format first (newer format)
            var jsonConfig = TryLoadFromJson();
            if (jsonConfig != null)
            {
                _lastReloadTime = DateTime.UtcNow;
                Plugin.Logger?.LogInfo("Loaded configuration from JSON format");
                return jsonConfig;
            }

            // Fallback to CFG format (Bloodcraft format)
            var cfgConfig = TryLoadFromCfg();
            if (cfgConfig != null)
            {
                _lastReloadTime = DateTime.UtcNow;
                Plugin.Logger?.LogInfo("Loaded configuration from CFG format");
                return cfgConfig;
            }

            // Use default configuration
            Plugin.Logger?.LogWarning("No valid configuration file found, using default configuration");
            return CreateDefaultConfiguration();
        }

        private static ArenaConfiguration TryLoadFromCfg()
        {
            var cfgPath = FindCfgConfigurationFile();
            if (cfgPath == null)
            {
                return null;
            }

            try
            {
                Plugin.Logger?.LogInfo($"Loading configuration from CFG file: {cfgPath}");
                var legacyConfig = CfgConfigParser.ParseCfgFile(cfgPath);
                return CfgConfigParser.ConvertToArenaConfiguration(legacyConfig);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to load CFG configuration from {cfgPath}: {ex.Message}");
                return null;
            }
        }

        private static ArenaConfiguration TryLoadFromJson()
        {
            var jsonPath = FindJsonConfigurationFile();
            if (jsonPath == null)
            {
                return null;
            }

            try
            {
                Plugin.Logger?.LogInfo($"Loading loadouts from JSON file: {jsonPath}");
                var json = File.ReadAllText(jsonPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                };

                var rawConfig = JsonSerializer.Deserialize<JsonElement>(json, options);
                return ParseArenaJsonConfiguration(rawConfig);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to load JSON configuration from {jsonPath}: {ex.Message}");
                return null;
            }
        }

        private static ArenaConfiguration ParseArenaJsonConfiguration(JsonElement rawConfig)
        {
            try
            {
                var loadouts = new List<ConfigArenaLoadout>();
                var zones = new List<ArenaZone>();
                var weapons = new List<WeaponData>();
                var armorSets = new List<ArmorData>();
                var consumables = new List<ConsumableData>();

                // First, try the direct root-level arrays (new format)
                bool parsedRootLevel = ParseRootLevelConfiguration(rawConfig, loadouts, zones, weapons, armorSets, consumables);
                
                if (!parsedRootLevel)
                {
                    // Fallback to ArenaContent format
                    ParseArenaContentConfiguration(rawConfig, loadouts, zones);
                }

                var config = new ArenaConfiguration
                {
                    Loadouts = loadouts,
                    Zones = zones,
                    Weapons = weapons,
                    ArmorSets = armorSets,
                    Consumables = consumables
                };

                Plugin.Logger?.LogInfo($"Parsed JSON configuration: {loadouts.Count} loadouts, {zones.Count} zones, {weapons.Count} weapons, {armorSets.Count} armor sets, {consumables.Count} consumables");
                return config;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to parse JSON configuration: {ex.Message}");
                return CreateDefaultConfiguration();
            }
        }

        private static bool ParseRootLevelConfiguration(JsonElement rawConfig, List<ConfigArenaLoadout> loadouts, List<ArenaZone> zones, List<WeaponData> weapons, List<ArmorData> armorSets, List<ConsumableData> consumables)
        {
            try
            {
                // Parse loadouts from root level
                if (rawConfig.TryGetProperty("Loadouts", out JsonElement loadoutsArray))
                {
                    foreach (var loadoutElement in loadoutsArray.EnumerateArray())
                    {
                        if (loadoutElement.TryGetProperty("Enabled", out JsonElement enabledElement) && enabledElement.GetBoolean())
                        {
                            var loadoutName = loadoutElement.TryGetProperty("Name", out JsonElement nameElement)
                                ? nameElement.GetString() ?? "default"
                                : "default";
                                
                            var loadout = new ConfigArenaLoadout
                            {
                                Name = loadoutName,
                                Weapons = new List<string>(),
                                ArmorSets = new List<string>(),
                                Consumables = new List<string>(),
                                WeaponMods = null,
                                BloodType = null,
                                Enabled = true
                            };

                            // Extract weapons
                            if (loadoutElement.TryGetProperty("Weapons", out JsonElement weaponsElement))
                            {
                                foreach (var weaponElement in weaponsElement.EnumerateArray())
                                {
                                    var weaponName = weaponElement.GetString();
                                    if (!string.IsNullOrEmpty(weaponName))
                                    {
                                        loadout.Weapons.Add(weaponName);
                                    }
                                }
                            }

                            // Extract armor sets
                            if (loadoutElement.TryGetProperty("ArmorSets", out JsonElement armorSetsElement))
                            {
                                foreach (var armorElement in armorSetsElement.EnumerateArray())
                                {
                                    var armorName = armorElement.GetString();
                                    if (!string.IsNullOrEmpty(armorName))
                                    {
                                        loadout.ArmorSets.Add(armorName);
                                    }
                                }
                            }

                            // Extract consumables
                            if (loadoutElement.TryGetProperty("Consumables", out JsonElement consumablesElement))
                            {
                                foreach (var consumableElement in consumablesElement.EnumerateArray())
                                {
                                    var consumableName = consumableElement.GetString();
                                    if (!string.IsNullOrEmpty(consumableName))
                                    {
                                        loadout.Consumables.Add(consumableName);
                                    }
                                }
                            }

                            // Extract weapon mods
                            if (loadoutElement.TryGetProperty("WeaponMods", out JsonElement weaponModsElement) &&
                                weaponModsElement.ValueKind != JsonValueKind.Null)
                            {
                                loadout.WeaponMods = weaponModsElement.GetString();
                            }

                            // Extract blood type
                            if (loadoutElement.TryGetProperty("BloodType", out JsonElement bloodTypeElement) &&
                                bloodTypeElement.ValueKind != JsonValueKind.Null)
                            {
                                loadout.BloodType = bloodTypeElement.GetString();
                            }

                            loadouts.Add(loadout);
                            Plugin.Logger?.LogInfo($"Loaded loadout '{loadoutName}' with {loadout.Weapons.Count} weapons, {loadout.ArmorSets.Count} armor sets, {loadout.Consumables.Count} consumables");
                        }
                    }
                }

                // Parse zones, weapons, armor, consumables similarly...
                return true; // Successfully parsed root-level configuration
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"Failed to parse root-level configuration: {ex.Message}");
                return false;
            }
        }

        private static void ParseArenaContentConfiguration(JsonElement rawConfig, List<ConfigArenaLoadout> loadouts, List<ArenaZone> zones)
        {
            // Parse loadouts from ArenaContent
            if (rawConfig.TryGetProperty("ArenaContent", out JsonElement arenaContent))
            {
                if (arenaContent.TryGetProperty("Loadouts", out JsonElement loadoutsArray))
                {
                    foreach (var loadoutElement in loadoutsArray.EnumerateArray())
                    {
                        if (loadoutElement.TryGetProperty("Enabled", out JsonElement enabledElement) && enabledElement.GetBoolean())
                        {
                            var loadoutName = loadoutElement.TryGetProperty("Name", out JsonElement nameElement)
                                ? nameElement.GetString() ?? "default"
                                : "default";
                             
                            var loadout = new ConfigArenaLoadout
                            {
                                Name = loadoutName,
                                Weapons = new List<string>(),
                                ArmorSets = new List<string>(),
                                Consumables = new List<string>(),
                                WeaponMods = null,
                                BloodType = null,
                                Enabled = true
                            };

                            // Extract weapons, armor sets, consumables, etc. similarly
                            loadouts.Add(loadout);
                            Plugin.Logger?.LogInfo($"Loaded loadout '{loadoutName}' with {loadout.Weapons.Count} weapons, {loadout.ArmorSets.Count} armor sets, {loadout.Consumables.Count} consumables");
                        }
                    }
                }
            }
        }

        private static string FindCfgConfigurationFile()
        {
            var candidatePaths = new[]
            {
                Path.Combine("config", "arena_config.cfg"),
                Path.Combine("BepInEx", "config", "CrowbaneArena", "arena_config.cfg"),
                Path.Combine("arena_config.cfg")
            };

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    Plugin.Logger?.LogInfo($"Found CFG configuration file: {path}");
                    return path;
                }
            }

            return null;
        }

        private static string FindJsonConfigurationFile()
        {
            var candidatePaths = new[]
            {
                Path.Combine("BepInEx", "config", "CrowbaneArena", "arena_config.json"),
                Path.Combine("arena_config.json"),
                Path.Combine("config", "arena_config.json"),
                Path.Combine("crowbanearena", "arena_config.json")
            };

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    Plugin.Logger?.LogInfo($"Found JSON configuration file: {path}");
                    return path;
                }
            }

            return null;
        }

        private static ArenaConfiguration CreateDefaultConfiguration()
        {
            return new ArenaConfiguration
            {
                Loadouts = new List<ConfigArenaLoadout>
                {
                    // Default Warrior Loadout - Balanced melee with Chaos Greatsword
                    new ConfigArenaLoadout
                    {
                        Name = "default_warrior",
                        Description = "Default Warrior Loadout - Balanced melee with Chaos Greatsword",
                        Weapons = new List<string> { "Greatsword" },
                        WeaponMods = "c4", // Chaos Greatsword
                        ArmorSets = new List<string> { "Warrior" },
                        BloodType = "Warrior",
                        Consumables = new List<string> { "BloodRosePotion", "ExquisiteBrew", "PhysicalBrew", "SpellBrew" },
                        Enabled = true
                    },
                    // Rogue Loadout - Fast attack speed with Illusion Daggers
                    new ConfigArenaLoadout
                    {
                        Name = "rogue_assassin",
                        Description = "Rogue Loadout - Fast attack speed with Illusion Daggers",
                        Weapons = new List<string> { "Daggers" },
                        WeaponMods = "i2", // Illusion Daggers
                        ArmorSets = new List<string> { "Rogue" },
                        BloodType = "Rogue",
                        Consumables = new List<string> { "BloodRosePotion", "ExquisiteBrew", "PhysicalBrew", "SpellBrew" },
                        Enabled = true
                    },
                    // Mage Loadout - High spell damage with Frost Staff
                    new ConfigArenaLoadout
                    {
                        Name = "mage_frost",
                        Description = "Mage Loadout - High spell damage with Frost Staff",
                        Weapons = new List<string> { "FrostStaff" },
                        WeaponMods = "f3", // Frost Staff
                        ArmorSets = new List<string> { "Scholar" },
                        BloodType = "Scholar",
                        Consumables = new List<string> { "BloodRosePotion", "ExquisiteBrew", "SpellBrew", "ManaPotion" },
                        Enabled = true
                    },
                    // Tank Loadout - High defense with Mace and Shield
                    new ConfigArenaLoadout
                    {
                        Name = "tank_defender",
                        Description = "Tank Loadout - High defense with Mace and Shield",
                        Weapons = new List<string> { "Mace" },
                        WeaponMods = "p1", // Protection Mace
                        ArmorSets = new List<string> { "Warrior" },
                        BloodType = "Brute",
                        Consumables = new List<string> { "BloodRosePotion", "ExquisiteBrew", "DefenseBrew", "HealthPotion" },
                        Enabled = true
                    },
                    // Ranger Loadout - Ranged combat with Crossbow
                    new ConfigArenaLoadout
                    {
                        Name = "ranger_sniper",
                        Description = "Ranger Loadout - Ranged combat with Crossbow",
                        Weapons = new List<string> { "Crossbow" },
                        WeaponMods = "h3", // Hunting Crossbow
                        ArmorSets = new List<string> { "Ranger" },
                        BloodType = "Hunter",
                        Consumables = new List<string> { "BloodRosePotion", "ExquisiteBrew", "RangedBrew", "SpeedPotion" },
                        Enabled = true
                    }
                },
                // Define weapon data
                Weapons = new List<WeaponData>
                {
                    new WeaponData { Name = "Sword", Guid = -774462329 },
                    new WeaponData { Name = "Axe", Guid = -2044057823 },
                    new WeaponData { Name = "Mace", Guid = -1569279652 },
                    new WeaponData { Name = "Spear", Guid = 1532449451 },
                    new WeaponData { Name = "Crossbow", Guid = 1389040540 },
                    new WeaponData { Name = "Greatsword", Guid = 147836723 },
                    new WeaponData { Name = "Slashers", Guid = 1031107636 },
                    new WeaponData { Name = "Reaper", Guid = 1504279833 },
                    new WeaponData { Name = "Pistols", Guid = 1651523865 },
                    new WeaponData { Name = "Longbow", Guid = -1484517133 },
                    new WeaponData { Name = "Whip", Guid = -1366738809 },
                    new WeaponData { Name = "TwinBlades", Guid = -1366738810 },
                    new WeaponData { Name = "Claws", Guid = -1366738811 }
                },
                // Define armor sets
                ArmorSets = new List<ArmorData>
                {
                    new ArmorData 
                    { 
                        Name = "Sanguine",
                        ChestGuid = -1266262267,
                        LegsGuid = -1266262266,
                        BootsGuid = -1266262265,
                        GlovesGuid = -1266262264,
                        Description = "Sanguine armor set"
                    },
                },
                // Define consumables
                Consumables = new List<ConsumableData>
                {
                    new ConsumableData { Name = "BloodPotion", Guid = -1531666018, DefaultAmount = 5 },
                    new ConsumableData { Name = "MinorBloodPotion", Guid = -437611596, DefaultAmount = 10 },
                    new ConsumableData { Name = "HealingPotion", Guid = -1060631101, DefaultAmount = 5 },
                    new ConsumableData { Name = "SpellPowerPotion", Guid = 1223264868, DefaultAmount = 3 },
                    new ConsumableData { Name = "PhysicalPowerPotion", Guid = 1223264867, DefaultAmount = 3 },
                    new ConsumableData { Name = "MovementSpeedPotion", Guid = 1223264866, DefaultAmount = 2 }
                },
                // Define arena zones
                Zones = new List<ArenaZone>
                {
                    new ArenaZone
                    {
                        Name = "DefaultArena",
                        Enabled = true,
                        SpawnX = -1000f,
                        SpawnY = 0f,
                        SpawnZ = -500f,
                        Radius = 50f,
                        EntryX = -1050f,
                        EntryY = 0f,
                        EntryZ = -500f,
                        EntryRadius = 10f,
                        ExitX = -950f,
                        ExitY = 0f,
                        ExitZ = -500f,
                        ExitRadius = 10f
                    },
                    new ArenaZone
                    {
                        Name = "DuelingPit",
                        Enabled = true,
                        SpawnX = 0f,
                        SpawnY = 0f,
                        SpawnZ = 0f,
                        Radius = 30f,
                        EntryX = 0f,
                        EntryY = 0f,
                        EntryZ = -40f,
                        EntryRadius = 5f,
                        ExitX = 0f,
                        ExitY = 0f,
                        ExitZ = 40f,
                        ExitRadius = 5f
                    },
                    new ArenaZone
                    {
                        Name = "TeamBattleArena",
                        Enabled = true,
                        SpawnX = 500f,
                        SpawnY = 0f,
                        SpawnZ = 500f,
                        Radius = 75f,
                        EntryX = 400f,
                        EntryY = 0f,
                        EntryZ = 400f,
                        EntryRadius = 10f,
                        ExitX = 600f,
                        ExitY = 0f,
                        ExitZ = 600f,
                        ExitRadius = 10f
                    },
                    new ArenaZone
                    {
                        Name = "RoyalColiseum",
                        Enabled = true,
                        SpawnX = -750f,
                        SpawnY = 0f,
                        SpawnZ = 750f,
                        Radius = 100f,
                        EntryX = -800f,
                        EntryY = 0f,
                        EntryZ = 700f,
                        EntryRadius = 15f,
                        ExitX = -700f,
                        ExitY = 0f,
                        ExitZ = 800f,
                        ExitRadius = 15f
                    }
                }
            };
        }

        private static ConfigurationValidationResult ValidateLoadout(ConfigArenaLoadout loadout)
        {
            var result = new ConfigurationValidationResult();

            if (string.IsNullOrWhiteSpace(loadout.Name))
            {
                result.AddError("Loadout name is required");
            }

            if (loadout.Weapons == null || loadout.Weapons.Count == 0)
            {
                result.AddError("At least one weapon is required");
            }

            if (loadout.ArmorSets == null || loadout.ArmorSets.Count == 0)
            {
                result.AddError("At least one armor set is required");
            }

            return result;
        }

        private static ConfigurationValidationResult ValidateZone(ArenaZone zone)
        {
            var result = new ConfigurationValidationResult();

            if (string.IsNullOrWhiteSpace(zone.Name))
            {
                result.AddError("Zone name is required");
            }

            if (zone.Radius <= 0)
            {
                result.AddError("Zone radius must be positive");
            }

            if (zone.EntryRadius <= 0)
            {
                result.AddError("Entry radius must be positive");
            }

            if (zone.ExitRadius <= 0)
            {
                result.AddError("Exit radius must be positive");
            }

            return result;
        }

        private static int GetTotalLoadoutCount()
        {
            return CurrentConfig.Loadouts?.Count ?? 0;
        }

        // ========================================
        // Try-pattern methods for compatibility
        // ========================================

        /// <summary>
        /// Try to get a loadout by name
        /// </summary>
        public static bool TryGetLoadout(string loadoutName, out ArenaLoadout loadout)
        {
            loadout = null;
            var configLoadout = CurrentConfig.Loadouts?.FirstOrDefault(l => l.Name.Equals(loadoutName, StringComparison.OrdinalIgnoreCase));
            if (configLoadout == null) return false;

            loadout = ConvertConfigToArenaLoadout(configLoadout);
            return loadout != null;
        }

        private static ArenaLoadout ConvertConfigToArenaLoadout(ConfigArenaLoadout configLoadout)
        {
            if (configLoadout == null) return null;

            var loadout = new ArenaLoadout
            {
                Name = configLoadout.Name,
                Description = configLoadout.Description,
                Enabled = configLoadout.Enabled,
                WeaponMods = configLoadout.WeaponMods,
                BloodType = configLoadout.BloodType,
                Weapons = new List<PrefabGUID>(),
                Armor = new List<PrefabGUID>(),
                Consumables = new List<ArenaItem>()
            };

            foreach (var token in configLoadout.Weapons)
            {
                if (string.IsNullOrWhiteSpace(token)) continue;

                if (TryGetWeapon(token, out var weaponData))
                {
                    loadout.Weapons.Add(GuidConverter.ToPrefabGUID(weaponData.Guid));
                }
                else if (int.TryParse(token, out var guidInt))
                {
                    var prefab = new PrefabGUID(guidInt);
                    if (GuidConverter.IsValid(prefab)) loadout.Weapons.Add(prefab);
                }
                else if (long.TryParse(token, out var guidLong))
                {
                    var prefab = GuidConverter.ToPrefabGUID(guidLong);
                    if (GuidConverter.IsValid(prefab)) loadout.Weapons.Add(prefab);
                }
            }

            foreach (var token in configLoadout.ArmorSets)
            {
                if (string.IsNullOrWhiteSpace(token)) continue;

                if (TryGetArmorSet(token, out var armorData))
                {
                    loadout.Armor.AddRange(GuidConverter.ToPrefabGUIDs(new[] { armorData.ChestGuid, armorData.LegsGuid, armorData.BootsGuid, armorData.GlovesGuid }));
                }
                else if (int.TryParse(token, out var guidInt))
                {
                    var prefab = new PrefabGUID(guidInt);
                    if (GuidConverter.IsValid(prefab)) loadout.Armor.Add(prefab);
                }
                else if (long.TryParse(token, out var guidLong))
                {
                    var prefab = GuidConverter.ToPrefabGUID(guidLong);
                    if (GuidConverter.IsValid(prefab)) loadout.Armor.Add(prefab);
                }
            }

            foreach (var token in configLoadout.Consumables)
            {
                if (string.IsNullOrWhiteSpace(token)) continue;

                if (TryGetConsumable(token, out var consumableData))
                {
                    loadout.Consumables.Add(new ArenaItem
                    {
                        Guid = GuidConverter.ToPrefabGUID(consumableData.Guid),
                        Amount = consumableData.DefaultAmount
                    });
                }
                else if (int.TryParse(token, out var guidInt))
                {
                    loadout.Consumables.Add(new ArenaItem
                    {
                        Guid = new PrefabGUID(guidInt),
                        Amount = 1
                    });
                }
                else if (long.TryParse(token, out var guidLong))
                {
                    loadout.Consumables.Add(new ArenaItem
                    {
                        Guid = GuidConverter.ToPrefabGUID(guidLong),
                        Amount = 1
                    });
                }
            }

            return loadout;
        }

        /// <summary>
        /// Try to get a weapon by name
        /// </summary>
        public static bool TryGetWeapon(string name, out WeaponData weapon)
        {
            weapon = CurrentConfig.Weapons?.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return weapon != null;
        }

        /// <summary>
        /// Try to get an armor set by name
        /// </summary>
        public static bool TryGetArmorSet(string name, out ArmorData armorSet)
        {
            armorSet = CurrentConfig.ArmorSets?.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return armorSet != null;
        }

        /// <summary>
        /// Try to get a consumable by name
        /// </summary>
        public static bool TryGetConsumable(string name, out ConsumableData consumable)
        {
            consumable = CurrentConfig.Consumables?.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return consumable != null;
        }

        /// <summary>
        /// Get all loadouts
        /// </summary>
        public static List<ConfigArenaLoadout> GetLoadouts()
        {
            return CurrentConfig.Loadouts ?? new List<ConfigArenaLoadout>();
        }

        /// <summary>
        /// Get all weapons
        /// </summary>
        public static List<WeaponData> GetWeapons()
        {
            return CurrentConfig.Weapons ?? new List<WeaponData>();
        }

        /// <summary>
        /// Get all armor sets
        /// </summary>
        public static List<ArmorData> GetArmorSets()
        {
            return CurrentConfig.ArmorSets ?? new List<ArmorData>();
        }

        /// <summary>
        /// Get all consumables
        /// </summary>
        public static List<ConsumableData> GetConsumables()
        {
            return CurrentConfig.Consumables ?? new List<ConsumableData>();
        }

        /// <summary>
        /// Save configuration (placeholder)
        /// </summary>
        public static void SaveConfiguration()
        {
            Plugin.Logger?.LogInfo("SaveConfiguration called - configuration is read-only from JSON/CFG files");
        }

        /// <summary>
        /// Generates a JSON representation of a full loadout.
        /// </summary>
        /// <param name="loadout">The loadout to serialize</param>
        /// <returns>JSON string representation of the loadout</returns>
        public static string GenerateLoadoutJson(Loadout loadout)
        {
            if (loadout == null)
            {
                Plugin.Logger?.LogWarning("Cannot generate JSON for null loadout");
                return "{}";
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(loadout, options);
                Plugin.Logger?.LogInfo($"Generated JSON for loadout '{loadout.Name}'");
                return json;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to generate JSON for loadout '{loadout.Name}': {ex.Message}");
                return "{}";
            }
        }

        /// <summary>
        /// Generates a snapshot of a loadout with additional metadata.
        /// </summary>
        /// <param name="loadout">The loadout to snapshot</param>
        /// <param name="playerId">Optional player ID for context</param>
        /// <returns>JSON string of the loadout snapshot</returns>
        public static string GenerateLoadoutSnapshot(Loadout loadout, ulong playerId = 0)
        {
            if (loadout == null)
            {
                Plugin.Logger?.LogWarning("Cannot generate snapshot for null loadout");
                return "{}";
            }

            try
            {
                var snapshot = new
                {
                    Loadout = loadout,
                    Metadata = new
                    {
                        GeneratedAt = DateTime.UtcNow,
                        PlayerId = playerId,
                        Version = "1.0",
                        Source = "ArenaConfigurationService"
                    }
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(snapshot, options);
                Plugin.Logger?.LogInfo($"Generated snapshot for loadout '{loadout.Name}' (Player: {playerId})");
                return json;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to generate snapshot for loadout '{loadout.Name}': {ex.Message}");
                return "{}";
            }
        }

        #region Internal API Methods

        /// <summary>
        /// API1: Get a loadout by name with all items converted to PrefabGUID
        /// </summary>
        /// <param name="loadoutName">Name of the loadout to retrieve</param>
        /// <returns>Loadout with PrefabGUID items or null if not found</returns>
        public static ArenaLoadout GetLoadoutWithGuids(string loadoutName)
        {
            if (TryGetLoadout(loadoutName, out var loadout))
            {
                return loadout;
            }
            return null;
        }

        /// <summary>
        /// API2: Get all loadouts with items converted to PrefabGUID
        /// </summary>
        /// <returns>List of all loadouts with PrefabGUID items</returns>
        public static List<ArenaLoadout> GetAllLoadoutsWithGuids()
        {
            var result = new List<ArenaLoadout>();
            
            // Add loadouts from main configuration
            if (CurrentConfig.Loadouts != null)
            {
                foreach (var configLoadout in CurrentConfig.Loadouts)
                {
                    if (TryGetLoadout(configLoadout.Name, out var loadout))
                    {
                        result.Add(loadout);
                    }
                }
            }
            
            // Add loadouts from JSON configuration if available
            var jsonLoadouts = CurrentConfig.GetType().GetProperty("LoadoutsFromJson")?.GetValue(CurrentConfig) as IEnumerable<Models.ArenaLoadout>;
            if (jsonLoadouts != null)
            {
                foreach (var jsonLoadout in jsonLoadouts)
                {
                    if (TryGetLoadout(jsonLoadout.Name, out var loadout))
                    {
                        result.Add(loadout);
                    }
                }
            }
            
            return result;
        }

        #endregion

        /// <summary>
        /// Arena settings accessor for compatibility
        /// </summary>
        public static ArenaConfiguration ArenaSettings => CurrentConfig;
    }

    /// <summary>
    /// Configuration for arena loadouts.
    /// </summary>
    public class ArenaConfiguration
    {
        public List<ConfigArenaLoadout> Loadouts { get; set; } = new List<ConfigArenaLoadout>();
        public List<ArenaZone> Zones { get; set; } = new List<ArenaZone>();
        public List<WeaponData> Weapons { get; set; } = new List<WeaponData>();
        public List<ArmorData> ArmorSets { get; set; } = new List<ArmorData>();
        public List<ConsumableData> Consumables { get; set; } = new List<ConsumableData>();
    }

    /// <summary>
    /// Weapon data with GUID information.
    /// </summary>
    public class WeaponData
    {
        public string Name { get; set; }
        public long Guid { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Variants { get; set; } = new();
    }

    /// <summary>
    /// Armor set data with individual piece GUIDs.
    /// </summary>
    public class ArmorData
    {
        public string Name { get; set; }
        public long ChestGuid { get; set; }
        public long LegsGuid { get; set; }
        public long BootsGuid { get; set; }
        public long GlovesGuid { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Consumable data with GUID and default amount.
    /// </summary>
    public class ConsumableData
    {
        public string Name { get; set; }
        public long Guid { get; set; }
        public int DefaultAmount { get; set; } = 1;
    }

    /// <summary>
    /// Arena loadout configuration.
    /// This is a simplified version for configuration purposes.
    /// The actual loadout used in-game is defined in LoadoutModels.cs
    /// </summary>
    public class ConfigArenaLoadout
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Weapons { get; set; } = new List<string>();
        public List<string> ArmorSets { get; set; } = new List<string>();
        public List<string> Consumables { get; set; } = new List<string>();
        public string WeaponMods { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Arena zone configuration.
    /// </summary>
    public class ArenaZone
    {
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        
        // Spawn point
        public float SpawnX { get; set; }
        public float SpawnY { get; set; }
        public float SpawnZ { get; set; }
        
        // Zone radius
        public float Radius { get; set; } = 50f;
        
        // Entry point
        public float EntryX { get; set; }
        public float EntryY { get; set; }
        public float EntryZ { get; set; }
        public float EntryRadius { get; set; } = 10f;
        
        // Exit point
        public float ExitX { get; set; }
        public float ExitY { get; set; }
        public float ExitZ { get; set; }
        public float ExitRadius { get; set; } = 10f;

        /// <summary>
        /// Gets the spawn point as a float3.
        /// </summary>
        public Unity.Mathematics.float3 SpawnPoint => new Unity.Mathematics.float3(SpawnX, SpawnY, SpawnZ);

        /// <summary>
        /// Gets the entry point as a float3.
        /// </summary>
        public Unity.Mathematics.float3 EntryPoint => new Unity.Mathematics.float3(EntryX, EntryY, EntryZ);

        /// <summary>
        /// Gets the exit point as a float3.
        /// </summary>
        public Unity.Mathematics.float3 ExitPoint => new Unity.Mathematics.float3(ExitX, ExitY, ExitZ);
    }

    /// <summary>
    /// Result of configuration validation.
    /// </summary>
    public class ConfigurationValidationResult
    {
        private readonly List<string> _errors = new List<string>();

        public bool IsValid => _errors.Count == 0;
        public int ErrorCount => _errors.Count;
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();
        public string ErrorMessage => string.Join("; ", _errors);

        public void AddError(string error)
        {
            _errors.Add(error);
        }
    }

    /// <summary>
    /// Statistics about the current configuration.
    /// </summary>
    public class ConfigurationStats
    {
        public int TotalLoadouts { get; set; }
        public int EnabledLoadouts { get; set; }
        public int TotalZones { get; set; }
        public int EnabledZones { get; set; }
        public DateTime LastReloadTime { get; set; }
        public bool IsInitialized { get; set; }
    }
}
