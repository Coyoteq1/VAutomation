using System.Collections.Generic;
using Stunlock.Core;

namespace CrowbaneArena.Data
{
    /// <summary>
    /// Spell school GUIDs and mappings
    /// Converted from arena_config.json GameData.SpellSchools
    /// </summary>
    public static class SpellSchoolGUIDs
    {
        public class SpellSchoolInfo
        {
            public string Name { get; set; } = "";
            public string Prefix { get; set; } = "";
            public int Guid { get; set; }
            
            public PrefabGUID GetPrefabGUID() => new PrefabGUID(Guid);
        }

        /// <summary>
        /// All spell schools with their prefixes and GUIDs
        /// </summary>
        public static readonly Dictionary<string, SpellSchoolInfo> SpellSchools = new()
        {
            ["storm"] = new SpellSchoolInfo { Name = "Storm", Prefix = "s", Guid = 419215380 },
            ["chaos"] = new SpellSchoolInfo { Name = "Chaos", Prefix = "c", Guid = -1706926836 },
            ["frost"] = new SpellSchoolInfo { Name = "Frost", Prefix = "f", Guid = 2110406288 },
            ["unholy"] = new SpellSchoolInfo { Name = "Unholy", Prefix = "u", Guid = 1293762372 },
            ["blood"] = new SpellSchoolInfo { Name = "Blood", Prefix = "b", Guid = -1007451621 },
            ["illusion"] = new SpellSchoolInfo { Name = "Illusion", Prefix = "i", Guid = -1063090297 }
        };

        /// <summary>
        /// Reverse lookup by prefix
        /// </summary>
        public static readonly Dictionary<string, string> PrefixToName = new()
        {
            ["s"] = "storm",
            ["c"] = "chaos",
            ["f"] = "frost",
            ["u"] = "unholy",
            ["b"] = "blood",
            ["i"] = "illusion"
        };

        /// <summary>
        /// Try to get spell school by name
        /// </summary>
        public static bool TryGetSpellSchool(string name, out SpellSchoolInfo? info)
        {
            return SpellSchools.TryGetValue(name.ToLowerInvariant(), out info);
        }

        /// <summary>
        /// Try to get spell school by prefix
        /// </summary>
        public static bool TryGetSpellSchoolByPrefix(string prefix, out SpellSchoolInfo? info)
        {
            info = null;
            if (PrefixToName.TryGetValue(prefix.ToLowerInvariant(), out var name))
            {
                return SpellSchools.TryGetValue(name, out info);
            }
            return false;
        }

        /// <summary>
        /// Get PrefabGUID for a spell school by name
        /// </summary>
        public static PrefabGUID GetGUID(string name)
        {
            return SpellSchools.TryGetValue(name.ToLowerInvariant(), out var info)
                ? new PrefabGUID(info.Guid)
                : PrefabGUID.Empty;
        }

        /// <summary>
        /// Gets all available spell school names
        /// </summary>
        public static IEnumerable<string> GetSpellSchools()
        {
            return SpellSchools.Keys;
        }
    }
}
