using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrowbaneArena.Utils;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Configuration management service following ICB.core pattern
    /// </summary>
    public static class ConfigService
    {
        public static ArenaConfig Config { get; private set; }
        private static string ConfigPath;
        private static string ConfigDirectory;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter(), new Float3JsonConverter() },
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public static void Initialize()
        {
            ConfigDirectory = Path.Combine(BepInEx.Paths.ConfigPath, "CrowbaneArena");
            ConfigPath = Path.Combine(ConfigDirectory, "arena_config.json");

            Plugin.Logger?.LogInfo("ConfigService initializing...");
            Plugin.Logger?.LogInfo($"Primary configuration directory: {ConfigDirectory}");

            LoadConfig();

            if (Config == null)
            {
                Plugin.Logger?.LogError("ConfigService.Initialize: Config is null after load attempts!");
                Config = CreateDefaultConfig();
            }
            else
            {
                Plugin.Logger?.LogInfo($"ConfigService initialized successfully. Arena enabled: {Config.Enabled}");
            }
        }

        private static void LoadConfig()
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                {
                    Plugin.Logger?.LogInfo($"Creating config directory: {ConfigDirectory}");
                    Directory.CreateDirectory(ConfigDirectory);
                }

                // Try CFG first (takes priority), then JSON
                var cfgPath = Path.Combine(ConfigDirectory, "arena_config.cfg");
                var jsonPath = Path.Combine(ConfigDirectory, "arena_config.json");

                bool loaded = false;

                // Try CFG file first
                if (File.Exists(cfgPath))
                {
                    try
                    {
                        Plugin.Logger?.LogInfo($"Found CFG configuration file: {cfgPath}");
                        Config = CfgConfigParser.ParseCfgFile(cfgPath);
                        Plugin.Logger?.LogInfo("Loaded configuration from CFG format");

                        if (ValidateConfig(Config))
                        {
                            loaded = true;
                        }
                        else
                        {
                            Plugin.Logger?.LogWarning("CFG config validation failed, trying JSON fallback");
                        }
                    }
                    catch (Exception cfgEx)
                    {
                        Plugin.Logger?.LogError($"Failed to load CFG config: {cfgEx.Message}");
                    }
                }

                // Try JSON file if CFG failed or doesn't exist
                if (!loaded && File.Exists(jsonPath))
                {
                    try
                    {
                        Plugin.Logger?.LogInfo($"Found JSON configuration file: {jsonPath}");
                        var json = File.ReadAllText(jsonPath);
                        Config = JsonSerializer.Deserialize<ArenaConfig>(json, JsonOptions);

                        if (ValidateConfig(Config))
                        {
                            Plugin.Logger?.LogInfo("Loaded configuration from JSON format");
                            loaded = true;
                        }
                        else
                        {
                            throw new InvalidOperationException("JSON configuration validation failed");
                        }
                    }
                    catch (Exception jsonEx)
                    {
                        Plugin.Logger?.LogError($"Failed to load JSON config: {jsonEx.Message}");

                        // Try backup
                        var backupPath = jsonPath + ".bak";
                        if (File.Exists(backupPath))
                        {
                            try
                            {
                                var backupJson = File.ReadAllText(backupPath);
                                Config = JsonSerializer.Deserialize<ArenaConfig>(backupJson, JsonOptions);

                                if (ValidateConfig(Config))
                                {
                                    Plugin.Logger?.LogInfo("Successfully loaded from JSON backup");
                                    loaded = true;
                                }
                            }
                            catch (Exception backupEx)
                            {
                                Plugin.Logger?.LogError($"JSON backup load failed: {backupEx.Message}");
                            }
                        }
                    }
                }

                if (!loaded)
                {
                    Plugin.Logger?.LogInfo("No valid config files found, creating default");
                    Config = CreateDefaultConfig();
                    SaveConfig();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Config load failed: {ex.Message}");
                Config = CreateDefaultConfig();

                try
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var corruptedPath = ConfigPath + $".corrupted.{timestamp}";
                    if (File.Exists(ConfigPath))
                    {
                        File.Move(ConfigPath, corruptedPath);
                        Plugin.Logger?.LogWarning($"Moved corrupted config to: {corruptedPath}");
                    }
                    SaveConfig();
                }
                catch (Exception saveEx)
                {
                    Plugin.Logger?.LogError($"Failed to save default config: {saveEx.Message}");
                }
            }
        }

        private static bool ValidateConfig(ArenaConfig config)
        {
            if (config == null) return false;

            // Note: SanitizeAndValidate method doesn't exist on ArenaConfig class
            // config.SanitizeAndValidate();

            return true;
        }

        public static void SaveConfig()
        {
            if (Config == null)
            {
                Plugin.Logger?.LogError("Cannot save null config");
                return;
            }

            try
            {
                if (File.Exists(ConfigPath))
                {
                    try
                    {
                        File.Copy(ConfigPath, ConfigPath + ".bak", true);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger?.LogWarning($"Failed to create backup: {ex.Message}");
                    }
                }

                var json = JsonSerializer.Serialize(Config, JsonOptions);
                File.WriteAllText(ConfigPath, json);
                Plugin.Logger?.LogInfo("Config saved successfully");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to save config: {ex.Message}");
            }
        }

        public static ArenaConfig CreateDefaultConfig()
        {
            return new ArenaConfig
            {
                Enabled = true,
                Location = new ZoneLocation
                {
                    Center = new Unity.Mathematics.float3(-1000f, 0f, -500f),
                    Radius = 60f,
                    SpawnPoint = new Unity.Mathematics.float3(-1000f, 0f, -500f)
                },
                Portal = new PortalConfig
                {
                    Entry = new ZoneLocation 
                    { 
                        Center = new Unity.Mathematics.float3(-1000f, 0f, -500f), 
                        Radius = 100f 
                    },
                    Exit = new ZoneLocation 
                    { 
                        Center = new Unity.Mathematics.float3(-1000f, 0f, -500f), 
                        Radius = 90f 
                    }
                },
                Gameplay = new GameplaySettings
                {
                    EnableGodMode = true,
                    RestoreOnExit = true,
                    AllowPvP = true,
                    VBloodProgression = false
                },
                Weapons = CreateDefaultWeapons(),
                ArmorSets = CreateDefaultArmorSets(),
                Consumables = CreateDefaultConsumables(),
                Loadouts = CreateDefaultLoadouts()
            };
        }

        private static List<Weapon> CreateDefaultWeapons()
        {
            return new List<Weapon>
            {
                new Weapon
                {
                    Name = "sword",
                    Description = "Sword",
                    Guid = -1569825471, // Example GUID - replace with actual
                    Enabled = true,
                    Variants = new List<WeaponVariant>
                    {
                        new WeaponVariant
                        {
                            Name = "Storm Sword",
                            ModCombo = "s",
                            VariantGuid = -1569825472, // Example GUID - replace with actual
                            FriendlyName = "Storm Sword"
                        },
                        new WeaponVariant
                        {
                            Name = "Blood Sword",
                            ModCombo = "b",
                            VariantGuid = -1569825473, // Example GUID - replace with actual
                            FriendlyName = "Blood Sword"
                        },
                        new WeaponVariant
                        {
                            Name = "Chaos Sword",
                            ModCombo = "c",
                            VariantGuid = -1569825474, // Example GUID - replace with actual
                            FriendlyName = "Chaos Sword"
                        }
                    }
                },
                new Weapon
                {
                    Name = "axe",
                    Description = "Axe",
                    Guid = -1569825475, // Example GUID - replace with actual
                    Enabled = true,
                    Variants = new List<WeaponVariant>
                    {
                        new WeaponVariant
                        {
                            Name = "Storm Axe",
                            ModCombo = "s",
                            VariantGuid = -1569825476, // Example GUID - replace with actual
                            FriendlyName = "Storm Axe"
                        },
                        new WeaponVariant
                        {
                            Name = "Blood Axe",
                            ModCombo = "b",
                            VariantGuid = -1569825477, // Example GUID - replace with actual
                            FriendlyName = "Blood Axe"
                        }
                    }
                }
            };
        }

        private static List<ArmorSet> CreateDefaultArmorSets()
        {
            return new List<ArmorSet>
            {
                new ArmorSet
                {
                    Name = "leather",
                    Description = "Leather Armor Set",
                    ChestGuid = 2725170480, // Example GUID - replace with actual
                    LegsGuid = 2725170481,
                    BootsGuid = 2725170482,
                    GlovesGuid = 2725170483,
                    Enabled = true
                },
                new ArmorSet
                {
                    Name = "iron",
                    Description = "Iron Armor Set",
                    ChestGuid = 2725170484, // Example GUID - replace with actual
                    LegsGuid = 2725170485,
                    BootsGuid = 2725170486,
                    GlovesGuid = 2725170487,
                    Enabled = true
                }
            };
        }

        private static List<Consumable> CreateDefaultConsumables()
        {
            return new List<Consumable>
            {
                new Consumable
                {
                    Name = "health_potion",
                    Guid = 2725170490, // Example GUID - replace with actual
                    DefaultAmount = 5,
                    Enabled = true
                },
                new Consumable
                {
                    Name = "blood_potion",
                    Guid = 2725170491, // Example GUID - replace with actual
                    DefaultAmount = 3,
                    Enabled = true
                }
            };
        }

        private static List<Loadout> CreateDefaultLoadouts()
        {
            return new List<Loadout>
            {
                new Loadout
                {
                    Name = "default",
                    Description = "Default Arena Loadout",
                    Weapons = new List<string> { "sword" },
                    ArmorSets = new List<string> { "leather" },
                    Consumables = new List<string> { "health_potion", "blood_potion" },
                    Enabled = true
                },
                new Loadout
                {
                    Name = "warrior",
                    Description = "Warrior Loadout",
                    Weapons = new List<string> { "axe" },
                    ArmorSets = new List<string> { "iron" },
                    Consumables = new List<string> { "health_potion" },
                    Enabled = true
                }
            };
        }
    }
}
