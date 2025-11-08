using System;
using System.Collections.Generic;
using Stunlock.Core;
using CrowbaneArena.Data;

namespace CrowbaneArena.Helpers
{
    /// <summary>
    /// Helper class for weapon mods and variants
    /// Bridges between WeaponVariants, SpellSchoolGUIDs, StatModGUIDs and game API
    /// </summary>
    public static class WeaponModHelper
    {
        /// <summary>
        /// Get weapon variant GUID by weapon name and mod combo
        /// </summary>
        public static PrefabGUID? GetWeaponVariantGUID(string weaponName, string modCombo)
        {
            if (WeaponVariants.TryGetVariant(weaponName, modCombo, out var variant))
            {
                return new PrefabGUID(variant.VariantGuid);
            }
            return null;
        }

        /// <summary>
        /// Get weapon variant friendly name
        /// </summary>
        public static string? GetWeaponVariantName(string weaponName, string modCombo)
        {
            if (WeaponVariants.TryGetVariant(weaponName, modCombo, out var variant))
            {
                return variant.FriendlyName;
            }
            return null;
        }

        /// <summary>
        /// Get all variants for a weapon
        /// </summary>
        public static List<WeaponVariants.WeaponVariant> GetWeaponVariants(string weaponName)
        {
            return WeaponVariants.GetVariants(weaponName);
        }

        /// <summary>
        /// Parse mod combo and get both spell school and stat mod GUIDs
        /// </summary>
        public static (PrefabGUID? spellSchool, PrefabGUID? statMod) GetModGUIDs(string modCombo)
        {
            var (schoolPrefix, statNumber) = StatModGUIDs.ParseModCombo(modCombo);

            PrefabGUID? spellSchoolGuid = null;
            PrefabGUID? statModGuid = null;

            if (schoolPrefix != null && SpellSchoolGUIDs.TryGetSpellSchoolByPrefix(schoolPrefix, out var school))
            {
                spellSchoolGuid = school.GetPrefabGUID();
            }

            if (statNumber.HasValue && StatModGUIDs.TryGetStatModByNumber(statNumber.Value, out var statMod))
            {
                statModGuid = statMod.GetPrefabGUID();
            }

            return (spellSchoolGuid, statModGuid);
        }

        /// <summary>
        /// Get spell school name from prefix
        /// </summary>
        public static string? GetSpellSchoolName(string prefix)
        {
            if (SpellSchoolGUIDs.TryGetSpellSchoolByPrefix(prefix, out var school))
            {
                return school.Name;
            }
            return null;
        }

        /// <summary>
        /// Get stat mod name from number
        /// </summary>
        public static string? GetStatModName(int number)
        {
            if (StatModGUIDs.TryGetStatModByNumber(number, out var statMod))
            {
                return statMod.Name;
            }
            return null;
        }

        /// <summary>
        /// Format mod combo for display (e.g., "s4" -> "Storm + Physical Power")
        /// </summary>
        public static string FormatModCombo(string modCombo)
        {
            var (schoolPrefix, statNumber) = StatModGUIDs.ParseModCombo(modCombo);

            if (schoolPrefix == null || !statNumber.HasValue)
            {
                return modCombo;
            }

            var schoolName = GetSpellSchoolName(schoolPrefix) ?? schoolPrefix;
            var statName = GetStatModName(statNumber.Value) ?? statNumber.ToString();

            return $"{schoolName} + {statName}";
        }

        /// <summary>
        /// Get all available spell schools
        /// </summary>
        public static Dictionary<string, string> GetAllSpellSchools()
        {
            var schools = new Dictionary<string, string>();
            foreach (var kvp in SpellSchoolGUIDs.SpellSchools)
            {
                schools[kvp.Value.Prefix] = kvp.Value.Name;
            }
            return schools;
        }

        /// <summary>
        /// Get all available stat mods
        /// </summary>
        public static Dictionary<int, string> GetAllStatMods()
        {
            var mods = new Dictionary<int, string>();
            foreach (var kvp in StatModGUIDs.StatMods)
            {
                mods[kvp.Value.Number] = kvp.Value.Name;
            }
            return mods;
        }

        /// <summary>
        /// Validate mod combo format
        /// </summary>
        public static bool IsValidModCombo(string modCombo)
        {
            var (schoolPrefix, statNumber) = StatModGUIDs.ParseModCombo(modCombo);
            
            if (schoolPrefix == null || !statNumber.HasValue)
            {
                return false;
            }

            return SpellSchoolGUIDs.TryGetSpellSchoolByPrefix(schoolPrefix, out _) &&
                   StatModGUIDs.TryGetStatModByNumber(statNumber.Value, out _);
        }

        /// <summary>
        /// Get weapon info with all variants
        /// </summary>
        public static string FormatWeaponInfo(string weaponName)
        {
            if (!WeaponVariants.Weapons.TryGetValue(weaponName.ToLowerInvariant(), out var weaponInfo))
            {
                return $"Weapon '{weaponName}' not found";
            }

            var info = $"{weaponInfo.Description} (Base GUID: {weaponInfo.Guid})\n";
            info += "Variants:\n";

            foreach (var variant in weaponInfo.Variants)
            {
                var modDisplay = FormatModCombo(variant.ModCombo);
                info += $"  • {variant.FriendlyName} ({variant.ModCombo} = {modDisplay})\n";
                info += $"    GUID: {variant.VariantGuid}\n";
            }

            return info;
        }

        /// <summary>
        /// Get all weapon names
        /// </summary>
        public static List<string> GetAllWeaponNames()
        {
            return new List<string>(WeaponVariants.Weapons.Keys);
        }

        /// <summary>
        /// Check if weapon has variants
        /// </summary>
        public static bool HasVariants(string weaponName)
        {
            var variants = WeaponVariants.GetVariants(weaponName);
            return variants.Count > 0;
        }
    }
}
