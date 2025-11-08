using System.Collections.Generic;
using Stunlock.Core;

namespace CrowbaneArena.Data
{
    /// <summary>
    /// Weapon variants with spell school and stat mod combinations
    /// Converted from arena_config.json GameData.Weapons
    /// </summary>
    public static class WeaponVariants
    {
        public class WeaponVariant
        {
            public string ModCombo { get; set; } = "";
            public int VariantGuid { get; set; }
            public string FriendlyName { get; set; } = "";
        }

        public class WeaponInfo
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public int Guid { get; set; }
            public List<WeaponVariant> Variants { get; set; } = new();
        }

        /// <summary>
        /// All weapon variants with their mod combinations
        /// </summary>
        public static readonly Dictionary<string, WeaponInfo> Weapons = new()
        {
            ["sword"] = new WeaponInfo
            {
                Name = "sword",
                Description = "Sanguine Sword",
                Guid = -774462329,
                Variants = new List<WeaponVariant>
                {
                    new() { ModCombo = "anc", VariantGuid = 2106567892, FriendlyName = "Ancestor's Sword" },
                    new() { ModCombo = "s1", VariantGuid = -774462328, FriendlyName = "Storm Blade" },
                    new() { ModCombo = "b4", VariantGuid = -774462327, FriendlyName = "Blood Knight Sword" },
                    new() { ModCombo = "s4", VariantGuid = -774462326, FriendlyName = "Storm Reaver" }
                }
            },
            ["axe"] = new WeaponInfo
            {
                Name = "axe",
                Description = "Sanguine Axe",
                Guid = -2044057823,
                Variants = new List<WeaponVariant>
                {
                    new() { ModCombo = "c4", VariantGuid = -2044057822, FriendlyName = "Chaos Cleaver" },
                    new() { ModCombo = "f4", VariantGuid = -2044057821, FriendlyName = "Frost Breaker" }
                }
            },
            ["mace"] = new WeaponInfo
            {
                Name = "mace",
                Description = "Sanguine Mace",
                Guid = -1569279652,
                Variants = new List<WeaponVariant>
                {
                    new() { ModCombo = "u1", VariantGuid = -1569279651, FriendlyName = "Unholy Crusher" },
                    new() { ModCombo = "i1", VariantGuid = -1569279650, FriendlyName = "Illusion Smasher" },
                    new() { ModCombo = "s4", VariantGuid = -1569279649, FriendlyName = "Storm Hammer" }
                }
            },
            ["spear"] = new WeaponInfo
            {
                Name = "spear",
                Description = "Sanguine Spear",
                Guid = 1532449451,
                Variants = new List<WeaponVariant>
                {
                    new() { ModCombo = "f3", VariantGuid = 1532449452, FriendlyName = "Frost Pike" },
                    new() { ModCombo = "b3", VariantGuid = 1532449453, FriendlyName = "Blood Drinker" },
                    new() { ModCombo = "i3", VariantGuid = 1532449454, FriendlyName = "Illusion Piercer" }
                }
            },
            ["greatsword"] = new WeaponInfo
            {
                Name = "greatsword",
                Description = "Sanguine Greatsword",
                Guid = 147836723,
                Variants = new List<WeaponVariant>
                {
                    new() { ModCombo = "c4", VariantGuid = 147836724, FriendlyName = "Chaos Greatblade" },
                    new() { ModCombo = "f1", VariantGuid = 147836725, FriendlyName = "Frost Colossus" },
                    new() { ModCombo = "u4", VariantGuid = 147836726, FriendlyName = "Unholy Reaper" }
                }
            },
            ["crossbow"] = new WeaponInfo
            {
                Name = "crossbow",
                Description = "Sanguine Crossbow",
                Guid = 1389040540,
                Variants = new List<WeaponVariant>
                {
                    new() { ModCombo = "s3", VariantGuid = 1389040541, FriendlyName = "Storm Striker" },
                    new() { ModCombo = "c3", VariantGuid = 1389040542, FriendlyName = "Chaos Launcher" }
                }
            },
            ["daggers"] = new WeaponInfo
            {
                Name = "daggers",
                Description = "Sanguine Daggers",
                Guid = 1031107636,
                Variants = new List<WeaponVariant>
                {
                    new() { ModCombo = "i2", VariantGuid = 1031107637, FriendlyName = "Shadow Stingers" },
                    new() { ModCombo = "u2", VariantGuid = 1031107638, FriendlyName = "Death Whisper" },
                    new() { ModCombo = "b2", VariantGuid = 1031107639, FriendlyName = "Blood Talons" }
                }
            },
            ["pistols"] = new WeaponInfo
            {
                Name = "pistols",
                Description = "Sanguine Pistols",
                Guid = 1651523865,
                Variants = new List<WeaponVariant>
                {
                    new() { ModCombo = "s2", VariantGuid = 1651523866, FriendlyName = "Storm Pair" },
                    new() { ModCombo = "f2", VariantGuid = 1651523867, FriendlyName = "Frost Twins" }
                }
            },
            ["reaper"] = new WeaponInfo
            {
                Name = "reaper",
                Description = "Sanguine Reaper",
                Guid = -1266262267,
                Variants = new List<WeaponVariant>
                {
                    new() { ModCombo = "u3", VariantGuid = -1266262266, FriendlyName = "Death Harvester" },
                    new() { ModCombo = "b4", VariantGuid = -1266262265, FriendlyName = "Blood Crescent" }
                }
            },
            ["slashers"] = new WeaponInfo
            {
                Name = "slashers",
                Description = "Sanguine Slashers",
                Guid = 600395942,
                Variants = new List<WeaponVariant>
                {
                    new() { ModCombo = "c2", VariantGuid = 600395943, FriendlyName = "Chaos Edges" },
                    new() { ModCombo = "s4", VariantGuid = 600395944, FriendlyName = "Storm Blades" }
                }
            }
        };

        /// <summary>
        /// Try to get weapon variant by base weapon name and mod combo
        /// </summary>
        public static bool TryGetVariant(string weaponName, string modCombo, out WeaponVariant? variant)
        {
            variant = null;
            if (!Weapons.TryGetValue(weaponName.ToLowerInvariant(), out var weaponInfo))
                return false;

            variant = weaponInfo.Variants.Find(v => v.ModCombo.Equals(modCombo, System.StringComparison.OrdinalIgnoreCase));
            return variant != null;
        }

        /// <summary>
        /// Get all variants for a weapon
        /// </summary>
        public static List<WeaponVariant> GetVariants(string weaponName)
        {
            return Weapons.TryGetValue(weaponName.ToLowerInvariant(), out var weaponInfo) 
                ? weaponInfo.Variants 
                : new List<WeaponVariant>();
        }
    }
}
