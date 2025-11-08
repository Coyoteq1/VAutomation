using System.Collections.Generic;

namespace CrowbaneArena.Data.Shared
{
    /// <summary>
    /// Stat modifier data - compatible with ICB.core and System.Text.Json
    /// </summary>
    public static class StatMod
    {
        /// <summary>
        /// List of available stat modifiers with their prefabs and abbreviations
        /// </summary>
        public static readonly List<StatModInfo> Mods = new()
        {
            new("Attack Speed", "StatMod_AttackSpeed", new[] { "1" }),
            new("Physical Critical Chance", "StatMod_CriticalStrikePhysical", new[] { "2" }),
            new("Physical Critical Damage", "StatMod_CriticalStrikePhysicalPower", new[] { "3" }),
            new("Spell Critical Chance", "StatMod_CriticalStrikeSpells", new[] { "4" }),
            new("Spell Critical Damage", "StatMod_CriticalStrikeSpellPower", new[] { "5" }),
            new("Max Health", "StatMod_MaxHealth", new[] { "6" }),
            new("Movement Speed", "StatMod_MovementSpeed", new[] { "7" }),
            new("Physical Power", "StatMod_PhysicalPower", new[] { "8" }),
            new("Spell Cooldown Reduction", "StatMod_SpellCooldownReduction", new[] { "9" }),
            new("Spell Leech", "StatMod_SpellLeech", new[] { "S1" }),
            new("Spell Power", "StatMod_SpellPower", new[] { "S2" }),
            new("Travel Cooldown Reduction", "StatMod_TravelCooldownReduction", new[] { "T1" }),
            new("Weapon Cooldown Reduction", "StatMod_WeaponCooldownReduction", new[] { "W1" }),
            new("Weapon Power", "StatMod_WeaponSkillPower", new[] { "p1" })
        };

        /// <summary>
        /// Try to get stat modifier by name or abbreviation
        /// </summary>
        /// <param name="nameOrAbbrev">Name or abbreviation of the stat mod</param>
        /// <param name="mod">The found stat modifier info, or null if not found</param>
        /// <returns>True if the stat modifier was found, false otherwise</returns>
        public static bool TryGetMod(string nameOrAbbrev, out StatModInfo mod)
        {
            nameOrAbbrev = nameOrAbbrev.ToLowerInvariant();
            mod = Mods.Find(m => m.Name.ToLowerInvariant() == nameOrAbbrev || 
                                m.Abbreviations.Exists(a => a.ToLowerInvariant() == nameOrAbbrev));
            return mod != null;
        }
    }

    /// <summary>
    /// Stat modifier information
    /// </summary>
    public class StatModInfo
    {
        /// <summary>
        /// Display name of the stat modifier
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Prefab name for the stat modifier
        /// </summary>
        public string PrefabName { get; set; }

        /// <summary>
        /// Short codes/abbreviations used to reference this stat modifier
        /// </summary>
        public List<string> Abbreviations { get; set; }

        /// <summary>
        /// Creates a new stat modifier info
        /// </summary>
        /// <param name="name">Display name</param>
        /// <param name="prefabName">Prefab name</param>
        /// <param name="abbreviations">Short codes/abbreviations</param>
        public StatModInfo(string name, string prefabName, IEnumerable<string> abbreviations)
        {
            Name = name;
            PrefabName = prefabName;
            Abbreviations = new List<string>(abbreviations);
        }
    }
}
