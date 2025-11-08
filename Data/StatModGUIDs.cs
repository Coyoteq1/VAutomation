using System.Collections.Generic;
using Stunlock.Core;

namespace CrowbaneArena.Data
{
    /// <summary>
    /// Stat modifier GUIDs and mappings
    /// Converted from arena_config.json GameData.StatMods
    /// </summary>
    public static class StatModGUIDs
    {
        public class StatModInfo
        {
            public string Name { get; set; } = "";
            public int Number { get; set; }
            public int Guid { get; set; }
            
            public PrefabGUID GetPrefabGUID() => new PrefabGUID(Guid);
        }

        /// <summary>
        /// All stat mods with their numbers and GUIDs
        /// </summary>
        public static readonly Dictionary<string, StatModInfo> StatMods = new()
        {
            ["movement_speed"] = new StatModInfo { Name = "Movement Speed", Number = 1, Guid = -285192213 },
            ["attack_speed"] = new StatModInfo { Name = "Attack Speed", Number = 2, Guid = -542568600 },
            ["spell_power"] = new StatModInfo { Name = "Spell Power", Number = 3, Guid = 1705753146 },
            ["physical_power"] = new StatModInfo { Name = "Physical Power", Number = 4, Guid = -1917650844 }
        };

        /// <summary>
        /// Reverse lookup by number
        /// </summary>
        public static readonly Dictionary<int, string> NumberToName = new()
        {
            [1] = "movement_speed",
            [2] = "attack_speed",
            [3] = "spell_power",
            [4] = "physical_power"
        };

        /// <summary>
        /// Try to get stat mod by name
        /// </summary>
        public static bool TryGetStatMod(string name, out StatModInfo? info)
        {
            return StatMods.TryGetValue(name.ToLowerInvariant().Replace(" ", "_"), out info);
        }

        /// <summary>
        /// Try to get stat mod by number
        /// </summary>
        public static bool TryGetStatModByNumber(int number, out StatModInfo? info)
        {
            info = null;
            if (NumberToName.TryGetValue(number, out var name))
            {
                return StatMods.TryGetValue(name, out info);
            }
            return false;
        }

        /// <summary>
        /// Get PrefabGUID for a stat mod by name
        /// </summary>
        public static PrefabGUID GetGUID(string name)
        {
            return StatMods.TryGetValue(name.ToLowerInvariant().Replace(" ", "_"), out var info) 
                ? new PrefabGUID(info.Guid) 
                : PrefabGUID.Empty;
        }

        /// <summary>
        /// Parse mod combo string (e.g., "s4" = Storm + Physical Power)
        /// </summary>
        public static (string? spellSchool, int? statMod) ParseModCombo(string modCombo)
        {
            if (string.IsNullOrEmpty(modCombo) || modCombo.Length < 2)
                return (null, null);

            var prefix = modCombo.Substring(0, 1).ToLowerInvariant();
            if (int.TryParse(modCombo.Substring(1), out var number))
            {
                return (prefix, number);
            }

            return (null, null);
        }

        /// <summary>
        /// Gets all available stat mod names
        /// </summary>
        public static IEnumerable<string> GetStatMods()
        {
            return StatMods.Keys;
        }
    }
}
