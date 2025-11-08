using System;
using System.Collections.Generic;
using System.Linq;
using Stunlock.Core;
using CrowbaneArena.Data;
using CrowbaneArena.Services;
using CrowbaneArena.Configs;
using CrowbaneArena.Models;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Comprehensive default data access service that consolidates all default data sources.
    /// Provides unified access to items, loadouts, configurations, and game data with automatic fallbacks.
    /// </summary>
    public static class DefaultDataService
    {
        #region Initialization

        private static bool _initialized = false;

        /// <summary>
        /// Initialize the default data service. Call this during plugin initialization.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
            {
                Plugin.Logger?.LogInfo("DefaultDataService already initialized");
                return;
            }

            try
            {
                // Validate that required data sources are available
                ValidateDataSources();

                _initialized = true;
                Plugin.Logger?.LogInfo("DefaultDataService initialized successfully");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to initialize DefaultDataService: {ex.Message}");
                throw;
            }
        }

        private static void ValidateDataSources()
        {
            // Ensure critical data sources are accessible
            if (Prefabs.Weapons.Count == 0)
                throw new InvalidOperationException("Weapon prefabs not loaded");

            if (HardcodedLoadouts.GetLoadouts().Count == 0)
                throw new InvalidOperationException("Hardcoded loadouts not available");

            if (BloodTypeGUIDs.GetBloodTypes().Count() == 0)
                Plugin.Logger?.LogWarning("Blood type GUIDs not loaded - some features may not work");
        }

        #endregion

        #region Item Data Access

        /// <summary>
        /// Get default weapon GUID by name with fallback options.
        /// </summary>
        public static PrefabGUID? GetDefaultWeapon(string weaponName, WeaponFallbackOptions options = null)
        {
            options ??= new WeaponFallbackOptions();

            // Try exact match first
            if (Prefabs.TryGetWeapon(weaponName, out var guid))
                return guid;

            // Try common aliases
            if (TryGetWeaponAlias(weaponName, out guid))
                return guid;

            // Try fallback weapon
            if (!string.IsNullOrEmpty(options.FallbackWeapon))
            {
                if (Prefabs.TryGetWeapon(options.FallbackWeapon, out guid))
                    return guid;
            }

            // Return default weapon
            return Prefabs.TryGetWeapon("sword", out guid) ? guid : PrefabGUID.Empty;
        }

        /// <summary>
        /// Get default armor set GUIDs by name.
        /// </summary>
        public static Dictionary<string, PrefabGUID> GetDefaultArmorSet(string setName)
        {
            var armorSet = Prefabs.GetArmorSet(setName);
            if (armorSet.Count > 0)
                return armorSet;

            // Fallback to warrior set
            return Prefabs.GetArmorSet("warrior");
        }

        /// <summary>
        /// Get default consumable GUID and amount.
        /// </summary>
        public static (PrefabGUID Guid, int Amount)? GetDefaultConsumable(string consumableName)
        {
            if (Prefabs.Consumables.TryGetValue(consumableName.ToLowerInvariant(), out var guid))
            {
                int amount = GetDefaultConsumableAmount(consumableName);
                return (guid, amount);
            }

            // Fallback to health potion
            if (Prefabs.Consumables.TryGetValue("health potion", out guid))
                return (guid, 5);

            return null;
        }

        /// <summary>
        /// Get any default item by name across all categories.
        /// </summary>
        public static PrefabGUID? GetDefaultItem(string itemName, out string category)
        {
            return Prefabs.TryGetAnyItem(itemName, out var guid, out category) ? guid : PrefabGUID.Empty;
        }

        #endregion

        #region Loadout Data Access

        /// <summary>
        /// Get default loadout by name with fallback.
        /// </summary>
        public static LoadoutDefinition GetDefaultLoadout(string loadoutName)
        {
            var loadouts = HardcodedLoadouts.GetLoadouts();

            if (loadouts.TryGetValue(loadoutName.ToLowerInvariant(), out var loadout))
                return loadout;

            // Return default loadout
            return loadouts.TryGetValue("default", out var defaultLoadout)
                ? defaultLoadout
                : CreateMinimalLoadout();
        }

        /// <summary>
        /// Get all available default loadouts.
        /// </summary>
        public static Dictionary<string, LoadoutDefinition> GetAllDefaultLoadouts()
        {
            return HardcodedLoadouts.GetLoadouts();
        }

        /// <summary>
        /// Create a minimal fallback loadout when no defaults are available.
        /// </summary>
        private static LoadoutDefinition CreateMinimalLoadout()
        {
            return new LoadoutDefinition
            {
                Name = "minimal",
                Description = "Minimal fallback loadout",
                BloodType = "Scholar",
                BloodQuality = 100f,
                Weapons = new List<WeaponDefinition>
                {
                    new WeaponDefinition
                    {
                        PrefabName = "Item_Weapon_Sword_Sanguine",
                        Guid = -774462329,
                        SpellSchool = "Physical"
                    }
                },
                Armor = new ArmorDefinition
                {
                    Chest = new PrefabGUID(unchecked((int)(uint)-1266262267)),
                    Legs = new PrefabGUID(unchecked((int)(uint)-1266262266)),
                    Boots = new PrefabGUID(unchecked((int)(uint)-1266262265)),
                    Gloves = new PrefabGUID(unchecked((int)(uint)-1266262264))
                },
                Consumables = new List<ConsumableDefinition>
                {
                    new ConsumableDefinition { Guid = unchecked((int)(uint)-1531666018), Amount = 5 }
                },
                Abilities = new List<string> { "AB_Vampire_VeilOfBlood_AbilityGroup" }
            };
        }

        #endregion

        #region Blood and Spell Data Access

        /// <summary>
        /// Get default blood type GUID.
        /// </summary>
        public static PrefabGUID GetDefaultBloodType(string bloodTypeName)
        {
            if (BloodTypeGUIDs.TryGetBloodType(bloodTypeName, out var bloodType))
                return bloodType ?? PrefabGUID.Empty;

            // Fallback to Scholar
            return BloodTypeGUIDs.TryGetBloodType("Scholar", out var scholarBlood)
                ? (scholarBlood ?? PrefabGUID.Empty)
                : PrefabGUID.Empty;
        }

        /// <summary>
        /// Get default spell school GUID.
        /// </summary>
        public static PrefabGUID GetDefaultSpellSchool(string spellSchoolName)
        {
            if (SpellSchoolGUIDs.TryGetSpellSchool(spellSchoolName, out var spellSchool))
                return new PrefabGUID(spellSchool.Guid);

            // Fallback to Physical
            return SpellSchoolGUIDs.TryGetSpellSchool("Physical", out var physicalSchool)
                ? new PrefabGUID(physicalSchool.Guid)
                : PrefabGUID.Empty;
        }

        /// <summary>
        /// Get stat modifier GUID.
        /// </summary>
        public static PrefabGUID GetDefaultStatMod(string statModName)
        {
            if (StatModGUIDs.TryGetStatMod(statModName, out var statMod))
                return new PrefabGUID(statMod.Guid);

            // Fallback to Physical Power
            return StatModGUIDs.TryGetStatMod("Physical Power", out var physicalPower)
                ? new PrefabGUID(physicalPower.Guid)
                : PrefabGUID.Empty;
        }

        #endregion

        #region Configuration Data Access

        /// <summary>
        /// Get default arena configuration.
        /// </summary>
        public static ArenaConfig GetDefaultArenaConfig()
        {
            // Return minimal config as default - ConfigService not available
            return CreateMinimalArenaConfig();
        }

        /// <summary>
        /// Get default harmony protection configuration.
        /// </summary>
        public static HarmonyProtectionConfig GetDefaultHarmonyConfig()
        {
            return new HarmonyProtectionConfig
            {
                HarmonyProtection = new HarmonyProtectionSettings
                {
                    Enabled = true,
                    LogLevel = "Info",
                    SkipProblematicAssemblies = true,
                    MaxRetries = 3,
                    ProblematicAssemblies = new List<string>
                    {
                        "__Generated",
                        "UnityEngine.PhysicsModule",
                        "UnityEngine.ParticleSystemModule",
                        "UnityEngine.UIElementsModule",
                        "UnityEngine.VirtualTexturingModule"
                    },
                    EnableAssemblyInfoLogging = true,
                    EnableDetailedErrorReporting = true,
                    GracefulDegradation = true
                },
                VBloodHookSettings = new VBloodHookSettings
                {
                    Enabled = true,
                    FallbackOnFailure = true,
                    MaxTypeDiscoveryAttempts = 2,
                    LogSuccessfulPatches = true,
                    LogFailedPatches = true
                },
                Debugging = new DebuggingSettings
                {
                    EnableReflectionDebugging = false,
                    LogTypeLoadingTimes = false,
                    VerboseAssemblyScanning = false
                }
            };
        }

        /// <summary>
        /// Create minimal arena config when ConfigService is not available.
        /// </summary>
        private static ArenaConfig CreateMinimalArenaConfig()
        {
            return new ArenaConfig
            {
                Enabled = true,
                Location = new ZoneLocation
                {
                    Center = new Unity.Mathematics.float3(-1000f, 0f, -500f),
                    Radius = 60f
                },
                Weapons = new List<Weapon>
                {
                    new Weapon { Name = "sword", Guid = -774462329, Enabled = true }
                },
                ArmorSets = new List<ArmorSet>
                {
                    new ArmorSet
                    {
                        Name = "warrior",
                        ChestGuid = unchecked((uint)-1266262267),
                        LegsGuid = unchecked((uint)-1266262266),
                        BootsGuid = unchecked((uint)-1266262265),
                        GlovesGuid = unchecked((uint)-1266262264),
                        Enabled = true
                    }
                },
                Consumables = new List<Consumable>
                {
                    new Consumable { Name = "health_potion", Guid = unchecked((uint)-1531666018), DefaultAmount = 5, Enabled = true }
                },
                Loadouts = new List<Loadout>
                {
                    new Loadout
                    {
                        Name = "default",
                        Weapons = new List<string> { "sword" },
                        ArmorSets = new List<string> { "warrior" },
                        Consumables = new List<string> { "health_potion" },
                        Enabled = true
                    }
                }
            };
        }

        #endregion

        #region Progression Data Access

        /// <summary>
        /// Create a default progression snapshot for new players.
        /// </summary>
        public static ProgressionSnapshot CreateDefaultProgressionSnapshot()
        {
            return new ProgressionSnapshot
            {
                SnapshotId = Guid.NewGuid().ToString(),
                CreatedAtUtc = DateTime.UtcNow,
                SchemaVersion = 1,
                CharacterName = "New Player",
                PlatformId = 0,
                Experience = 0,
                Level = 1,
                SkillLevels = new Dictionary<string, int>(),
                UnlockedResearch = new HashSet<PrefabGUID>(),
                UnlockedVBlood = new HashSet<PrefabGUID>(),
                UnlockedAbilities = new HashSet<PrefabGUID>(),
                UnlockedPassives = new HashSet<PrefabGUID>(),
                CompletedAchievements = new HashSet<PrefabGUID>(),
                AchievementProgress = new Dictionary<PrefabGUID, float>(),
                UnlockedWaypoints = new HashSet<int>(),
                RevealedMapChunks = new HashSet<Unity.Mathematics.int2>(),
                SpellSchoolLevels = new Dictionary<PrefabGUID, float>(),
                UIStates = new Dictionary<string, bool>(),
                GameSettings = new Dictionary<string, string>(),
                Items = new Dictionary<PrefabGUID, int>(),
                Equipped = new Dictionary<PrefabGUID, int>()
            };
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get default consumable amount for a given consumable name.
        /// </summary>
        private static int GetDefaultConsumableAmount(string consumableName)
        {
            return consumableName.ToLowerInvariant() switch
            {
                "health potion" => 5,
                "blood potion" => 3,
                "physical power potion" => 2,
                "spell power potion" => 2,
                _ => 1
            };
        }

        /// <summary>
        /// Try to get weapon GUID using common aliases.
        /// </summary>
        private static bool TryGetWeaponAlias(string weaponName, out PrefabGUID guid)
        {
            var aliases = new Dictionary<string, string>
            {
                { "blade", "sword" },
                { "swords", "sword" },
                { "axe", "axe" },
                { "axes", "axe" },
                { "hammer", "mace" },
                { "club", "mace" },
                { "polearm", "spear" },
                { "lance", "spear" },
                { "bow", "longbow" },
                { "crossbow", "crossbow" },
                { "gun", "pistols" },
                { "guns", "pistols" },
                { "reaper", "reaper" },
                { "scythe", "reaper" },
                { "slashers", "slashers" },
                { "claws", "claws" },
                { "twinblades", "twinblades" },
                { "whip", "whip" }
            };

            if (aliases.TryGetValue(weaponName.ToLowerInvariant(), out var realName))
            {
                return Prefabs.TryGetWeapon(realName, out guid);
            }

            guid = PrefabGUID.Empty;
            return false;
        }

        /// <summary>
        /// Get summary of all available default data.
        /// </summary>
        public static DefaultDataSummary GetDataSummary()
        {
            return new DefaultDataSummary
            {
                WeaponsCount = Prefabs.Weapons.Count,
                ArmorSetsCount = Prefabs.ArmorSets.Count,
                ConsumablesCount = Prefabs.Consumables.Count,
                SpellsCount = Prefabs.Spells.Count,
                LoadoutsCount = HardcodedLoadouts.GetLoadouts().Count,
                BloodTypesCount = BloodTypeGUIDs.GetBloodTypes().Count(),
                SpellSchoolsCount = SpellSchoolGUIDs.GetSpellSchools().Count(),
                StatModsCount = StatModGUIDs.GetStatMods().Count()
            };
        }

        #endregion
    }

    #region Helper Classes

    /// <summary>
    /// Options for weapon fallback behavior.
    /// </summary>
    public class WeaponFallbackOptions
    {
        public string FallbackWeapon { get; set; } = "sword";
        public bool AllowVariantFallback { get; set; } = true;
    }

    /// <summary>
    /// Summary of available default data.
    /// </summary>
    public class DefaultDataSummary
    {
        public int WeaponsCount { get; set; }
        public int ArmorSetsCount { get; set; }
        public int ConsumablesCount { get; set; }
        public int SpellsCount { get; set; }
        public int LoadoutsCount { get; set; }
        public int BloodTypesCount { get; set; }
        public int SpellSchoolsCount { get; set; }
        public int StatModsCount { get; set; }

        public override string ToString()
        {
            return $"Weapons: {WeaponsCount}, Armor Sets: {ArmorSetsCount}, Consumables: {ConsumablesCount}, " +
                   $"Spells: {SpellsCount}, Loadouts: {LoadoutsCount}, Blood Types: {BloodTypesCount}, " +
                   $"Spell Schools: {SpellSchoolsCount}, Stat Mods: {StatModsCount}";
        }
    }

    #endregion
}
