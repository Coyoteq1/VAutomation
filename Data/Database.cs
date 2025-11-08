using System.Collections.Generic;
using System.Linq;
using CrowbaneArena.Data;
using CrowbaneArena.Services;

namespace CrowbaneArena.Data
{
    /// <summary>
    /// Database initialization manager for VRising automation system.
    /// Initializes all data sources and services in the correct order.
    /// Similar to ArenaBuilds Database class but adapted for VRising data.
    /// </summary>
    internal static class Database
    {
        /// <summary>
        /// Initialize all databases and data services.
        /// Call this during plugin initialization after core services are ready.
        /// </summary>
        public static void Initialize()
        {
            var databases = new List<IDataInitializer>
            {
                new PrefabDatabase(),
                new BloodTypeDatabase(),
                new SpellSchoolDatabase(),
                new StatModDatabase(),
                new LoadoutDatabase(),
                new DefaultDataServiceInitializer()
            };

            Plugin.Logger?.LogInfo("Initializing VRising automation databases...");

            foreach (var db in databases)
            {
                try
                {
                    db.Initialize();
                    Plugin.Logger?.LogInfo($"Initialized: {db.GetType().Name}");
                }
                catch (System.Exception ex)
                {
                    Plugin.Logger?.LogError($"Failed to initialize {db.GetType().Name}: {ex.Message}");
                    throw; // Re-throw to prevent partial initialization
                }
            }

            Plugin.Logger?.LogInfo("All VRising automation databases initialized successfully.");
        }

        /// <summary>
        /// Validate that all required data is loaded and accessible.
        /// </summary>
        public static bool ValidateDataIntegrity()
        {
            try
            {
                // Check critical data sources
                if (Prefabs.Weapons.Count == 0)
                {
                    Plugin.Logger?.LogError("Weapon prefabs not loaded!");
                    return false;
                }

                if (Prefabs.Consumables.Count == 0)
                {
                    Plugin.Logger?.LogError("Consumable prefabs not loaded!");
                    return false;
                }

                if (Prefabs.ArmorSets.Count == 0)
                {
                    Plugin.Logger?.LogError("Armor sets not loaded!");
                    return false;
                }

                if (BloodTypeGUIDs.GetBloodTypes().Count() == 0)
                {
                    Plugin.Logger?.LogWarning("Blood type data not available - some features may not work");
                }

                if (DefaultDataService.GetAllDefaultLoadouts().Count == 0)
                {
                    Plugin.Logger?.LogWarning("No default loadouts available");
                }

                Plugin.Logger?.LogInfo("Data integrity validation passed");
                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.Logger?.LogError($"Data integrity validation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get database statistics for monitoring.
        /// </summary>
        public static DatabaseStats GetStats()
        {
            return new DatabaseStats
            {
                WeaponsCount = Prefabs.Weapons.Count,
                ArmorSetsCount = Prefabs.ArmorSets.Count,
                ConsumablesCount = Prefabs.Consumables.Count,
                SpellsCount = Prefabs.Spells.Count,
                UnitsCount = Prefabs.Units.Count,
                BloodTypesCount = BloodTypeGUIDs.GetBloodTypes().Count(),
                SpellSchoolsCount = SpellSchoolGUIDs.GetSpellSchools().Count(),
                StatModsCount = StatModGUIDs.GetStatMods().Count(),
                DefaultLoadoutsCount = DefaultDataService.GetAllDefaultLoadouts().Count,
                CustomLoadoutsCount = LoadoutManager.CustomLoadouts.Count
            };
        }
    }

    /// <summary>
    /// Interface for data initializers.
    /// </summary>
    internal interface IDataInitializer
    {
        void Initialize();
    }

    /// <summary>
    /// Initializes prefab data.
    /// </summary>
    internal class PrefabDatabase : IDataInitializer
    {
        public void Initialize()
        {
            // Prefabs are static classes, so they're already "initialized"
            // Just validate they're accessible
            var testWeapon = Prefabs.Weapons.Count;
            var testConsumable = Prefabs.Consumables.Count;
            var testArmor = Prefabs.ArmorSets.Count;

            if (testWeapon == 0 || testConsumable == 0 || testArmor == 0)
            {
                throw new System.InvalidOperationException("Prefab data not properly loaded");
            }
        }
    }

    /// <summary>
    /// Initializes blood type data.
    /// </summary>
    internal class BloodTypeDatabase : IDataInitializer
    {
        public void Initialize()
        {
            // BloodTypeGUIDs is a static class, validate it's accessible
            var bloodTypes = BloodTypeGUIDs.GetBloodTypes();
            if (bloodTypes == null)
            {
                throw new System.InvalidOperationException("Blood type data not available");
            }
        }
    }

    /// <summary>
    /// Initializes spell school data.
    /// </summary>
    internal class SpellSchoolDatabase : IDataInitializer
    {
        public void Initialize()
        {
            // SpellSchoolGUIDs is a static class, validate it's accessible
            var spellSchools = SpellSchoolGUIDs.GetSpellSchools();
            if (spellSchools == null)
            {
                throw new System.InvalidOperationException("Spell school data not available");
            }
        }
    }

    /// <summary>
    /// Initializes stat modifier data.
    /// </summary>
    internal class StatModDatabase : IDataInitializer
    {
        public void Initialize()
        {
            // StatModGUIDs is a static class, validate it's accessible
            var statMods = StatModGUIDs.GetStatMods();
            if (statMods == null)
            {
                throw new System.InvalidOperationException("Stat modifier data not available");
            }
        }
    }

    /// <summary>
    /// Initializes loadout data.
    /// </summary>
    internal class LoadoutDatabase : IDataInitializer
    {
        public void Initialize()
        {
            // Load hardcoded loadouts (they're static)
            var defaultLoadouts = HardcodedLoadouts.GetLoadouts();
            if (defaultLoadouts == null || defaultLoadouts.Count == 0)
            {
                Plugin.Logger?.LogWarning("No hardcoded loadouts available");
            }
        }
    }

    /// <summary>
    /// Initializes the DefaultDataService.
    /// </summary>
    internal class DefaultDataServiceInitializer : IDataInitializer
    {
        public void Initialize()
        {
            DefaultDataService.Initialize();
        }
    }

    /// <summary>
    /// Database statistics for monitoring.
    /// </summary>
    public class DatabaseStats
    {
        public int WeaponsCount { get; set; }
        public int ArmorSetsCount { get; set; }
        public int ConsumablesCount { get; set; }
        public int SpellsCount { get; set; }
        public int UnitsCount { get; set; }
        public int BloodTypesCount { get; set; }
        public int SpellSchoolsCount { get; set; }
        public int StatModsCount { get; set; }
        public int DefaultLoadoutsCount { get; set; }
        public int CustomLoadoutsCount { get; set; }

        public override string ToString()
        {
            return $"Weapons: {WeaponsCount}, Armor Sets: {ArmorSetsCount}, Consumables: {ConsumablesCount}, " +
                   $"Spells: {SpellsCount}, Units: {UnitsCount}, Blood Types: {BloodTypesCount}, " +
                   $"Spell Schools: {SpellSchoolsCount}, Stat Mods: {StatModsCount}, " +
                   $"Default Loadouts: {DefaultLoadoutsCount}, Custom Loadouts: {CustomLoadoutsCount}";
        }
    }
}
