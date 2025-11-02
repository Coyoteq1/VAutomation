using System;
using System.IO;
using BepInEx;
using Newtonsoft.Json;

namespace CrowbaneArena.Services
{
    public static class ConfigService
    {
        private static ArenaConfig? _config;
        private static readonly string ConfigPath = Path.Combine(Paths.ConfigPath, "CrowbaneArena", "config.json");

        public static ArenaConfig? Config
        {
            get
            {
                if (_config == null)
                {
                    LoadConfig();
                }
                return _config;
            }
        }

        public static void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    _config = JsonConvert.DeserializeObject<ArenaConfig>(json);
                    Plugin.Logger?.LogInfo("Config loaded successfully");
                }
                else
                {
                    _config = new ArenaConfig();
                    SaveConfig();
                    Plugin.Logger?.LogInfo("Default config created");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error loading config: {ex.Message}");
                _config = new ArenaConfig();
            }
        }

        public static void SaveConfig()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(_config, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
                Plugin.Logger?.LogInfo("Config saved successfully");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error saving config: {ex.Message}");
            }
        }

        public static void Reload()
        {
            _config = null;
            LoadConfig();
        }
    }

    public class ArenaConfig
    {
        public int MaxPlayers { get; set; } = 8;
        public bool AutoProximityEnabled { get; set; } = true;
        public bool VBloodHookEnabled { get; set; } = true;
        public Unity.Mathematics.float3 ArenaEnterPoint { get; set; } = Unity.Mathematics.float3.zero;
        public Unity.Mathematics.float3 ArenaExitPoint { get; set; } = Unity.Mathematics.float3.zero;

        public GameplaySettings? Gameplay { get; set; } = new();
        public Loadout[]? Loadouts { get; set; } = Array.Empty<Loadout>();
        public Weapon[]? Weapons { get; set; } = Array.Empty<Weapon>();
        public ArmorSet[]? ArmorSets { get; set; } = Array.Empty<ArmorSet>();
        public Consumable[]? Consumables { get; set; } = Array.Empty<Consumable>();
    }

    public class GameplaySettings
    {
        public bool EnableArenaMode { get; set; } = true;
        public float ArenaRadius { get; set; } = 50f;
        public bool EnableEquipmentTracking { get; set; } = true;
    }

    public class Loadout
    {
        public string Name { get; set; } = "default";
        public string Description { get; set; } = "Default loadout";
        public bool Enabled { get; set; } = true;
        public string[] Weapons { get; set; } = Array.Empty<string>();
        public string[] ArmorSets { get; set; } = Array.Empty<string>();
        public string[] Consumables { get; set; } = Array.Empty<string>();
    }

    public class Weapon
    {
        public string Name { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public int Guid { get; set; } = 0;
    }

    public class ArmorSet
    {
        public string Name { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public int ChestGuid { get; set; } = 0;
        public int LegsGuid { get; set; } = 0;
        public int BootsGuid { get; set; } = 0;
        public int GlovesGuid { get; set; } = 0;
    }

    public class Consumable
    {
        public string Name { get; set; } = "";
        public bool Enabled { get; set; } = true;
        public int Guid { get; set; } = 0;
        public int DefaultAmount { get; set; } = 1;
    }
}
