using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Stunlock.Core;
using CrowbaneArena.Core;
using System.Text;
using System.Linq;
using Unity.Mathematics;
using CrowbaneArena.Services;
using CrowbaneArena.Systems;

namespace CrowbaneArena
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("gg.deca.VampireCommandFramework")]
    [BepInProcess("VRisingServer.exe")]
    public class Plugin : BasePlugin
    {
        public const string PluginGuid = "gg.Automation.arena";
        public const string PluginName = "CrowbaneArena";
        public const string PluginVersion = "1.0.0";

        public static BepInEx.Logging.ManualLogSource Logger { get; private set; }
        public static Plugin Instance { get; private set; }

        // Configuration entries
        public static ConfigEntry<bool> EnableArena { get; private set; }
        public static ConfigEntry<bool> EnableGodMode { get; private set; }
        public static ConfigEntry<bool> RestoreOnExit { get; private set; }
        public static ConfigEntry<bool> AllowPvP { get; private set; }
        public static ConfigEntry<bool> VBloodProgression { get; private set; }
        public static ConfigEntry<bool> EnableAutoSnapshots { get; private set; }
        public static ConfigEntry<int> AutoSnapshotInterval { get; private set; }
        public static ConfigEntry<int> MaxSnapshots { get; private set; }
        public static ConfigEntry<string> DefaultLoadout { get; private set; }
        public static ConfigEntry<string> InputItems { get; private set; }
        public static ConfigEntry<bool> EnableDebugLogging { get; private set; }
        public static ConfigEntry<bool> EnableCommandLogging { get; private set; }
        public static ConfigEntry<string> ArenaConfigPath { get; private set; }
        public static ConfigEntry<string> DefaultZone { get; private set; }
        public static ConfigEntry<float> ProximityEnterRadius { get; private set; }
        public static ConfigEntry<float> ProximityExitRadius { get; private set; }
        public static ConfigEntry<float> ProximityUpdateInterval { get; private set; }
        // --- Loadout databases ---
        public static Dictionary<string, PrefabGUID> WeaponsDB { get; private set; } = new();
        public static Dictionary<string, ArenaArmorSet> ArmorSetsDB { get; private set; } = new();
        public static Dictionary<string, ArenaConsumable> ConsumablesDB { get; private set; } = new();
        public static Dictionary<string, ArenaLoadout> LoadoutsDB { get; private set; } = new();
        public static string DataPath { get; private set; } = "";

        public override void Load()
        {
            Logger = Log;
            Instance = this;

            // Set data path for services
            DataPath = Paths.ConfigPath;

            // Initialize configuration
            InitializeConfig();

            // Initialize Harmony for patches with protection against type loading exceptions
            var harmony = new Harmony(PluginGuid);
            
            // Log assembly information for debugging
            Services.HarmonyProtectionService.LogAssemblyInfo();
            
            // Install patches with protection
            Services.HarmonyProtectionService.InstallProtectedPatches(harmony, Assembly.GetExecutingAssembly());
            
            // Install runtime UI-unlock hooks (reflection-based)
            CrowbaneArena.Patches.VBloodHookPatch.Install(harmony);

            // Register VCF commands
            VampireCommandFramework.CommandRegistry.RegisterAll();

            // Initialize default data service (provides unified access to all game data)
            DefaultDataService.Initialize();

            // Initialize loadout manager (loads custom + default loadouts)
            LoadoutManager.LoadData();

            // Initialize the ArenaConfigurationService
            Services.ArenaConfigurationService.Initialize();

            // Initialize snapshot system (ProgressionService now delegates to ISnapshotManager)
            ProgressionService.Initialize(new SnapshotManagerService());

            // Initialize the LoadoutApplicationService
            Services.LoadoutApplicationService.Initialize();

            // Initialize database system (loads all data sources)
            Data.Database.Initialize();

            // Build loadout databases from configuration
            BuildLoadoutDatabasesFromConfig();

            // Apply zones from plugin cfg (Center/Entry/Exit one-row format)
            try
            {
                ApplyZonesFromPluginConfig();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to apply zones from plugin cfg: {ex.Message}");
            }

            // Apply proximity settings
            try
            {
                ApplyProximitySettingsFromPluginConfig();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to apply proximity settings: {ex.Message}");
            }

            // Run data integrity validation
            var integrityService = new Services.DataIntegrityService();
            integrityService.ValidateAllData();

            Logger.LogInfo($"{PluginName} v{PluginVersion} loaded successfully");
            Logger.LogInfo($"Arena Enabled: {EnableArena.Value}");
            Logger.LogInfo($"God Mode: {EnableGodMode.Value}");
            Logger.LogInfo($"Data Path: {DataPath}");
            Logger.LogInfo($"Loadout DB ready – {WeaponsDB.Count} weapons, {ConsumablesDB.Count} consumables, {LoadoutsDB.Count} loadouts.");
        }

        private void InitializeConfig()
        {
            // General Settings
            InputItems = Config.Bind("General", "InputItems", "-1695880581,-1279580453,-178129731,-2121040337,-1063673554,1103020674,-2054366442,-1922303627,1773398670,-1281878061,-1528693876,-1435427143",
                "Comma-separated item prefab IDs for arena entry (weapons, armor, consumables)");
            EnableArena = Config.Bind("General", "EnableArena", true,
                "Enable or disable the arena system entirely");

            ArenaConfigPath = Config.Bind("General", "ArenaConfigPath", "config/arena_config.json",
                "Path to the arena configuration file (relative to BepInEx folder)");

            DefaultLoadout = Config.Bind("General", "DefaultLoadout", "default",
                "Default loadout to use when entering arena");

            EnableDebugLogging = Config.Bind("General", "EnableDebugLogging", false,
                "Enable detailed debug logging for troubleshooting");

            EnableCommandLogging = Config.Bind("General", "EnableCommandLogging", true,
                "Log all command executions for audit purposes");

            // Gameplay Settings
            EnableGodMode = Config.Bind("Gameplay", "EnableGodMode", true,
                "Enable god mode (invincibility) in the arena");

            RestoreOnExit = Config.Bind("Gameplay", "RestoreOnExit", true,
                "Restore player state when exiting the arena");

            AllowPvP = Config.Bind("Gameplay", "AllowPvP", false,
                "Allow PvP combat in the arena");

            VBloodProgression = Config.Bind("Gameplay", "VBloodProgression", false,
                "Require VBlood progression to unlock arena features");

            // Snapshot Settings
            EnableAutoSnapshots = Config.Bind("Snapshots", "EnableAutoSnapshots", false,
                "Automatically create snapshots at regular intervals");

            AutoSnapshotInterval = Config.Bind("Snapshots", "AutoSnapshotInterval", 300,
                new ConfigDescription(
                    "Interval in seconds between automatic snapshots",
                    new AcceptableValueRange<int>(60, 3600)));

            MaxSnapshots = Config.Bind("Snapshots", "MaxSnapshots", 10,
                new ConfigDescription(
                    "Maximum number of snapshots to keep per player",
                    new AcceptableValueRange<int>(1, 100)));

            // Zones
            DefaultZone = Config.Bind("General", "DefaultZone", "DefaultArena",
                "Name of the zone to use by default from the [Arena.Zones] section (Center/Entry/Exit rows)");

            // Proximity
            ProximityEnterRadius = Config.Bind("Arena.Proximity", "EnterRadius", 50f,
                "Enter radius (meters) for auto-enter");
            ProximityExitRadius = Config.Bind("Arena.Proximity", "ExitRadius", 75f,
                "Exit radius (meters) for auto-exit");
            ProximityUpdateInterval = Config.Bind("Arena.Update", "IntervalSeconds", 2f,
                "Proximity update interval in seconds");

            Logger.LogInfo("Configuration initialized successfully");
        }

        // -------------------------
        //  Enhanced loadout builder using CFG and JSON configuration
        // -------------------------
        private void BuildLoadoutDatabasesFromConfig()
        {
            try
            {
                WeaponsDB.Clear();
                ArmorSetsDB.Clear();
                ConsumablesDB.Clear();
                LoadoutsDB.Clear();

                // Try to load CFG configuration first, then fall back to JSON
                var cfgPath = Path.Combine(Paths.ConfigPath, "CrowbaneArena", "arena_config.cfg");
                var jsonPath = Path.Combine(Paths.ConfigPath, "CrowbaneArena", "config", "arena_config.json");

                bool loadedFromCfg = false;
                
                if (File.Exists(cfgPath))
                {
                    try
                    {
                        Logger.LogInfo("Loading configuration from CFG file...");
                        var cfgConfig = Services.CfgConfigParser.ParseCfgFile(cfgPath);
                        
                        // Convert CFG to our database format
                        LoadFromCfgConfig(cfgConfig);
                        loadedFromCfg = true;
                        Logger.LogInfo("Successfully loaded configuration from CFG file");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Failed to load CFG configuration: {ex.Message}. Trying JSON configuration...");
                    }
                }

                if (!loadedFromCfg && File.Exists(jsonPath))
                {
                    try
                    {
                        Logger.LogInfo("Loading configuration from JSON file...");
                        var jsonContent = File.ReadAllText(jsonPath);
                        // For now, create a basic config from JSON fallback
                        LoadFromJsonFallback();
                        Logger.LogInfo("Successfully loaded configuration from JSON file");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning($"Failed to load JSON configuration: {ex.Message}. Using default loadouts...");
                    }
                }

                // Load from our hardcoded Loadouts if no config worked
                if (LoadoutsDB.Count == 0)
                {
                    Logger.LogInfo("Using hardcoded default loadouts...");
                    BuildDefaultLoadouts();
                }

                Logger.LogInfo($"Loadout DB ready – {WeaponsDB.Count} weapons, {ConsumablesDB.Count} consumables, {LoadoutsDB.Count} loadouts.");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to build loadout DB: {ex.Message}");
                BuildDefaultLoadouts(); // Fallback to default
            }
        }

        private void LoadFromCfgConfig(object cfgConfig)
        {
            // Convert CFG configuration to our database format
            // This would convert the parsed CFG sections to our database structures
            BuildDefaultLoadouts(); // For now, use defaults until we have the full conversion
        }

        private void LoadFromJsonFallback()
        {
            // Create basic loadouts for JSON fallback
            BuildDefaultLoadouts();
        }

        private void BuildDefaultLoadouts()
        {
            // Use our enhanced weapon definitions from Prefabs.cs
            foreach (var weapon in Data.Prefabs.Weapons)
            {
                WeaponsDB[weapon.Key] = weapon.Value;
            }

            // Create basic armor sets
            foreach (var armorSet in Data.Prefabs.ArmorSets)
            {
                var arenaArmorSet = new ArenaArmorSet
                {
                    Name = armorSet.Key
                };
                ArmorSetsDB[armorSet.Key] = arenaArmorSet;
            }

            // Create basic consumables
            foreach (var consumable in Data.Prefabs.Consumables)
            {
                ConsumablesDB[consumable.Key] = new ArenaConsumable
                {
                    Name = consumable.Key,
                    Guid = consumable.Value,
                    DefaultAmount = 10
                };
            }

            // Create loadouts from our Loadouts.cs
            foreach (var loadout in Data.Loadouts.All)
            {
                if (!loadout.Enabled) continue;
                
                var arenaLoadout = new ArenaLoadout
                {
                    Name = loadout.Name,
                    Enabled = true
                };

                // Add weapons
                foreach (var weaponName in loadout.Weapons)
                {
                    if (WeaponsDB.TryGetValue(weaponName, out var weaponGuid))
                    {
                        arenaLoadout.Weapons.Add(weaponGuid);
                    }
                }

                // Add consumables
                foreach (var consumableName in loadout.Consumables)
                {
                    if (ConsumablesDB.TryGetValue(consumableName, out var consumable))
                    {
                        arenaLoadout.Consumables.Add(new ArenaItem
                        {
                            Guid = consumable.Guid,
                            Amount = consumable.DefaultAmount
                        });
                    }
                }

                LoadoutsDB[loadout.Name] = arenaLoadout;
            }

            Logger.LogInfo($"Built {LoadoutsDB.Count} loadouts from configuration");
        }

        public override bool Unload()
        {
            Config.Save();
            return true;
        }

        public void ReloadPluginSettings()
        {
            try
            {
                Config.Reload();
                ApplyZonesFromPluginConfig();
                ApplyProximitySettingsFromPluginConfig();
                Logger.LogInfo("Reloaded plugin cfg: zones and proximity settings applied.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"ReloadPluginSettings failed: {ex.Message}");
            }
        }

        // -------------------------------------------------
        //   Zones parsing from plugin cfg ([Arena.Zones])
        //   Supports one-row Center/Entry/Exit lines per zone
        // -------------------------------------------------
        private void ApplyZonesFromPluginConfig()
        {
            var cfgPath = Config.ConfigFilePath;
            if (string.IsNullOrEmpty(cfgPath) || !File.Exists(cfgPath))
            {
                Logger.LogInfo("Plugin cfg file not found; skipping [Arena.Zones] parsing.");
                return;
            }

            var zones = ParseZonesSection(cfgPath);
            if (zones.Count == 0)
            {
                Logger.LogInfo("No zones found in [Arena.Zones] section.");
                return;
            }

            // Select default zone
            var zoneName = DefaultZone?.Value ?? string.Empty;
            var selected = zones[0];
            if (!string.IsNullOrWhiteSpace(zoneName))
            {
                foreach (var z in zones)
                {
                    if (string.Equals(z.Name, zoneName, StringComparison.OrdinalIgnoreCase))
                    {
                        selected = z;
                        break;
                    }
                }
            }

            // Apply to systems
            var center = selected.Center;
            var radius = selected.CenterRadius > 0 ? selected.CenterRadius : 60f;

            ZoneManager.SetArenaZone(center, radius);
            ZoneManager.SetSpawnPoint(center);
            if (selected.EntryHasValue)
            {
                ZoneManager.SetEntryPoint(selected.EntryCenter, selected.EntryRadius > 0 ? selected.EntryRadius : 10f);
            }
            if (selected.ExitHasValue)
            {
                ZoneManager.SetExitPoint(selected.ExitCenter, selected.ExitRadius > 0 ? selected.ExitRadius : 10f);
            }

            // Proximity system center defaults to arena center; radii can still come from other configs
            ArenaProximitySystem.ArenaCenter = center;

            Logger.LogInfo($"Applied default zone '{selected.Name}' from plugin cfg: Center {center}, Radius {radius}");
        }

        private struct ZoneRow
        {
            public string Name;
            public float3 Center;
            public float CenterRadius;
            public bool EntryHasValue;
            public float3 EntryCenter;
            public float EntryRadius;
            public bool ExitHasValue;
            public float3 ExitCenter;
            public float ExitRadius;
        }

        private List<ZoneRow> ParseZonesSection(string cfgPath)
        {
            var list = new List<ZoneRow>();
            try
            {
                var lines = File.ReadAllLines(cfgPath);
                bool inZonesSection = false;
                string currentSection = string.Empty;
                var map = new Dictionary<string, ZoneRow>(StringComparer.OrdinalIgnoreCase);

                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("//")) continue;

                    if (line.StartsWith("["))
                    {
                        currentSection = line;
                        inZonesSection = string.Equals(line, "[Arena.Zones]", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    // Expect patterns like either:
                    //   In [Arena.Zones]:   Center = Name,x,y,z,radius (or Entry/Exit)
                    //   In [Arena.Zone.X]:  Position = x,y,z,radius (or Center/Entry/Exit)
                    var eq = line.IndexOf('=');
                    if (eq <= 0) continue;

                    var key = line.Substring(0, eq).Trim();
                    var value = line.Substring(eq + 1).Trim();

                    // Determine if we're in per-zone section
                    if (currentSection.StartsWith("[Arena.Zone.", StringComparison.OrdinalIgnoreCase) ||
                        currentSection.StartsWith("[Zone", StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract a section key (not necessarily final zone name)
                        string sectionKey;
                        if (currentSection.StartsWith("[Arena.Zone.", StringComparison.OrdinalIgnoreCase))
                        {
                            sectionKey = currentSection.Substring("[Arena.Zone.".Length);
                        }
                        else
                        {
                            // e.g., [Zone1], [Zone_Default], [Zone]
                            sectionKey = currentSection.Substring("[".Length);
                        }
                        if (sectionKey.EndsWith("]")) sectionKey = sectionKey.Substring(0, sectionKey.Length - 1);
                        if (string.IsNullOrWhiteSpace(sectionKey)) continue;

                        if (!map.TryGetValue(sectionKey, out var row))
                        {
                            row = new ZoneRow
                            {
                                Name = sectionKey,
                                Center = new float3(0, 0, 0),
                                CenterRadius = 0,
                                EntryHasValue = false,
                                ExitHasValue = false
                            };
                        }

                        // Allow overriding the zone display/name
                        if (key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                        {
                            // Accept strings like Name = Default
                            var n = value.Trim();
                            if (n.Length > 0) row.Name = n;
                            map[sectionKey] = row;
                            continue;
                        }

                        // Position acts as alias for Center
                        if (key.Equals("Position", StringComparison.OrdinalIgnoreCase) ||
                            key.Equals("Center", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = value.Split(',');
                            if (parts.Length >= 4)
                            {
                                ParseXYZR(parts, out var x, out var y, out var z, out var r);
                                row.Center = new float3(x, y, z);
                                row.CenterRadius = r;
                            }
                        }
                        else if (key.Equals("Entry", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = value.Split(',');
                            if (parts.Length >= 4)
                            {
                                ParseXYZR(parts, out var x, out var y, out var z, out var r);
                                row.EntryCenter = new float3(x, y, z);
                                row.EntryRadius = r;
                                row.EntryHasValue = true;
                            }
                        }
                        else if (key.Equals("Exit", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = value.Split(',');
                            if (parts.Length >= 4)
                            {
                                ParseXYZR(parts, out var x, out var y, out var z, out var r);
                                row.ExitCenter = new float3(x, y, z);
                                row.ExitRadius = r;
                                row.ExitHasValue = true;
                            }
                        }

                        map[sectionKey] = row;
                        continue;
                    }

                    if (!inZonesSection) continue;

                    // In [Arena.Zones] block: key = Center/Position/Entry/Exit with name first
                    if (key.Equals("Center", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("Position", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("Entry", StringComparison.OrdinalIgnoreCase) ||
                        key.Equals("Exit", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = value.Split(',');
                        if (parts.Length < 5) continue; // need name + 4 numbers (x,y,z,r)

                        var name = parts[0].Trim();
                        if (string.IsNullOrWhiteSpace(name)) continue;

                        if (!map.TryGetValue(name, out var row))
                        {
                            row = new ZoneRow
                            {
                                Name = name,
                                Center = new float3(0, 0, 0),
                                CenterRadius = 0,
                                EntryHasValue = false,
                                ExitHasValue = false
                            };
                        }

                        ParseXYZR(parts.Skip(1).ToArray(), out var x2, out var y2, out var z2, out var r2);

                        if (key.Equals("Center", StringComparison.OrdinalIgnoreCase) || key.Equals("Position", StringComparison.OrdinalIgnoreCase))
                        {
                            row.Center = new float3(x2, y2, z2);
                            row.CenterRadius = r2;
                        }
                        else if (key.Equals("Entry", StringComparison.OrdinalIgnoreCase))
                        {
                            row.EntryCenter = new float3(x2, y2, z2);
                            row.EntryRadius = r2;
                            row.EntryHasValue = true;
                        }
                        else if (key.Equals("Exit", StringComparison.OrdinalIgnoreCase))
                        {
                            row.ExitCenter = new float3(x2, y2, z2);
                            row.ExitRadius = r2;
                            row.ExitHasValue = true;
                        }

                        map[name] = row;
                    }
                }

                // Finalize list (only rows with Center defined are valid)
                foreach (var kv in map)
                {
                    if (kv.Value.CenterRadius > 0)
                    {
                        list.Add(kv.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error parsing [Arena.Zones]: {ex.Message}");
            }

            return list;
        }

        private void ApplyProximitySettingsFromPluginConfig()
        {
            try
            {
                var enter = ProximityEnterRadius?.Value ?? 50f;
                var exit = ProximityExitRadius?.Value ?? 75f;
                var interval = ProximityUpdateInterval?.Value ?? 2f;

                if (enter > 0) ArenaProximitySystem.EnterRadius = enter;
                if (exit > 0) ArenaProximitySystem.ExitRadius = exit;
                if (interval > 0) ArenaProximitySystem.UpdateIntervalSeconds = interval;

                Logger.LogInfo($"Proximity settings applied: enter={enter}, exit={exit}, interval={interval}s");
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to apply proximity settings: {ex.Message}");
            }
        }

        private static void ParseXYZR(string[] parts, out float x, out float y, out float z, out float r)
        {
            float ParseF(string s)
            {
                float.TryParse(s.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f);
                return f;
            }

            x = parts.Length > 0 ? ParseF(parts[0]) : 0f;
            y = parts.Length > 1 ? ParseF(parts[1]) : 0f;
            z = parts.Length > 2 ? ParseF(parts[2]) : 0f;
            r = parts.Length > 3 ? ParseF(parts[3]) : 0f;
        }
    }
}
