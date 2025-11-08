using System;
using System.Collections.Generic;
using System.Linq;
using Stunlock.Core;
using CrowbaneArena.Data;

namespace CrowbaneArena.Helpers
{
    /// <summary>
    /// General helper for accessing all game data
    /// Provides a unified interface to all data structures
    /// </summary>
    public static class DataHelper
    {
        /// <summary>
        /// Get any item GUID by name (searches all categories)
        /// </summary>
        public static PrefabGUID? GetItemGUID(string itemName)
        {
            if (Prefabs.TryGetAnyItem(itemName, out var guid, out var category))
            {
                Plugin.Logger?.LogInfo($"Found '{itemName}' in category: {category}");
                return guid;
            }
            return null;
        }

        /// <summary>
        /// Get weapon GUID (base or variant)
        /// </summary>
        public static PrefabGUID? GetWeaponGUID(string weaponName, string? modCombo = null)
        {
            // Try to get variant first if mod combo specified
            if (!string.IsNullOrEmpty(modCombo))
            {
                var variantGuid = WeaponModHelper.GetWeaponVariantGUID(weaponName, modCombo);
                if (variantGuid.HasValue)
                {
                    return variantGuid;
                }
            }

            // Fall back to base weapon
            if (Prefabs.TryGetWeapon(weaponName, out var baseGuid))
            {
                return baseGuid;
            }

            return null;
        }

        /// <summary>
        /// Get armor piece GUID
        /// </summary>
        public static PrefabGUID? GetArmorPieceGUID(string setName, string slotName)
        {
            if (Prefabs.TryGetArmorPiece(setName, slotName, out var guid))
            {
                return guid;
            }
            return null;
        }

        /// <summary>
        /// Get complete armor set GUIDs
        /// </summary>
        public static Dictionary<string, PrefabGUID> GetArmorSetGUIDs(string setName)
        {
            return Prefabs.GetArmorSet(setName);
        }

        /// <summary>
        /// Get blood type GUID
        /// </summary>
        public static PrefabGUID? GetBloodTypeGUID(string bloodTypeName)
        {
            if (BloodTypeGUIDs.TryGetBloodType(bloodTypeName.ToLowerInvariant(), out var bloodType))
            {
                return bloodType;
            }
            return null;
        }

        // Consumable methods removed

        /// <summary>
        /// Get spell/ability GUID
        /// </summary>
        public static PrefabGUID? GetSpellGUID(string spellName)
        {
            if (Prefabs.TryGetSpell(spellName, out var guid))
            {
                return guid;
            }
            return null;
        }

        /// <summary>
        /// List all available items in a category
        /// </summary>
        public static string ListCategory(string category)
        {
            return Prefabs.GetItemsInCategory(category);
        }

        /// <summary>
        /// Get all item categories with counts
        /// </summary>
        public static Dictionary<string, int> GetItemCategories()
        {
            return Prefabs.GetItemCategories();
        }

        /// <summary>
        /// Search for items by partial name
        /// </summary>
        public static List<string> SearchItems(string searchTerm)
        {
            var results = new List<string>();
            searchTerm = searchTerm.ToLowerInvariant();

            // Search weapons
            foreach (var weapon in Prefabs.Weapons.Keys)
            {
                if (weapon.Contains(searchTerm))
                {
                    results.Add($"Weapon: {weapon}");
                }
            }

            // Search armor sets
            foreach (var armorSet in Prefabs.ArmorSets.Keys)
            {
                if (armorSet.Contains(searchTerm))
                {
                    results.Add($"Armor Set: {armorSet}");
                }
            }

            // Search consumables
            foreach (var consumable in Prefabs.Consumables.Keys)
            {
                if (consumable.Contains(searchTerm))
                {
                    results.Add($"Consumable: {consumable}");
                }
            }

            // Search spells
            foreach (var spell in Prefabs.Spells.Keys)
            {
                if (spell.Contains(searchTerm))
                {
                    results.Add($"Spell: {spell}");
                }
            }

            return results;
        }

        // Loadout methods removed - loadout system excluded from build
        /*
        /// <summary>
        /// Get loadout by name
        /// </summary>
        public static ArenaLoadouts.LoadoutConfig? GetLoadout(string loadoutName)
        {
            ArenaLoadouts.TryGetLoadout(loadoutName, out var loadout);
            return loadout;
        }

        /// <summary>
        /// Get all loadout names
        /// </summary>
        public static List<string> GetAllLoadoutNames()
        {
            return ArenaLoadouts.GetLoadoutNames();
        }
        */

        /// <summary>
        /// Get zone by name
        /// </summary>
        public static ArenaZones.ZoneConfig? GetZone(string zoneName)
        {
            ArenaZones.TryGetZone(zoneName, out var zone);
            return zone;
        }

        /// <summary>
        /// Get all zone names
        /// </summary>
        public static List<string> GetAllZoneNames()
        {
            return ArenaZones.GetZoneNames();
        }

        /// <summary>
        /// Format complete data summary
        /// </summary>
        public static string GetDataSummary()
        {
            var summary = "=== CrowbaneArena Data Summary ===\n\n";

            // Weapons
            summary += $"Weapons: {Prefabs.Weapons.Count}\n";
            summary += $"Weapon Variants: {WeaponVariants.Weapons.Values.Sum(w => w.Variants.Count)}\n";

            // Armor
            summary += $"Armor Sets: {Prefabs.ArmorSets.Count}\n";

            // Consumables
            summary += $"Consumables: {Prefabs.Consumables.Count}\n";

            // Spells
            summary += $"Spells/Abilities: {Prefabs.Spells.Count}\n";

            // Blood Types
            summary += $"Blood Types: {BloodTypeGUIDs.GetBloodTypes().Count()}\n";

            // Spell Schools
            summary += $"Spell Schools: {SpellSchoolGUIDs.GetSpellSchools().Count()}\n";

            // Stat Mods
            summary += $"Stat Mods: {StatModGUIDs.GetStatMods().Count()}\n";

            // Loadouts
            summary += $"Loadouts: 0\n"; // ArenaLoadouts not available

            // Zones
            summary += $"Zones: {ArenaZones.Zones.Count}\n";

            return summary;
        }

        /// <summary>
        /// Validate that all data is loaded correctly
        /// </summary>
        public static bool ValidateData()
        {
            try
            {
                var hasWeapons = Prefabs.Weapons.Count > 0;
                var hasArmor = Prefabs.ArmorSets.Count > 0;
                var hasLoadouts = false; // ArenaLoadouts not available
                var hasZones = ArenaZones.Zones.Count > 0;
                var hasBloodTypes = BloodTypeGUIDs.GetBloodTypes().Any();
                var hasSpellSchools = SpellSchoolGUIDs.GetSpellSchools().Any();
                var hasStatMods = StatModGUIDs.GetStatMods().Any();

                var isValid = hasWeapons && hasArmor && hasLoadouts && hasZones && 
                             hasBloodTypes && hasSpellSchools && hasStatMods;

                if (isValid)
                {
                    Plugin.Logger?.LogInfo("✅ All data structures validated successfully");
                }
                else
                {
                    Plugin.Logger?.LogError("❌ Data validation failed - some structures are empty");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"❌ Data validation error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get detailed info about a specific item
        /// </summary>
        public static string GetItemInfo(string itemName)
        {
            if (Prefabs.TryGetAnyItem(itemName, out var guid, out var category))
            {
                var info = $"Item: {itemName}\n";
                info += $"Category: {category}\n";
                info += $"GUID: {guid.GuidHash}\n";

                // Add extra info for weapons
                if (category == "Weapon" && WeaponModHelper.HasVariants(itemName))
                {
                    info += "\n" + WeaponModHelper.FormatWeaponInfo(itemName);
                }

                return info;
            }

            return $"Item '{itemName}' not found";
        }
    }
}
