using System.Collections.Generic;
using Stunlock.Core;

namespace CrowbaneArena.Data.Shared
{
    /// <summary>
    /// Spell School GUIDs - compatible with ICB.core and System.Text.Json
    /// </summary>
    public static class SpellSchoolGUIDs
    {
        // Spell School Asset GUIDs (from ICB.core)
        public const int Blood = 969332277;
        public const int Chaos = 597438920;
        public const int Frost = -823811825;
        public const int Illusion = -526263322;
        public const int Shadow = -420104199;
        public const int Storm = -829934972;
        public const int Unholy = 232985690;

        /// <summary>
        /// Spell school information structure
        /// </summary>
        public class SpellSchoolInfo
        {
            public string Name { get; set; }
            public string Prefix { get; set; }
            public int GuidHash { get; set; }
            public PrefabGUID PrefabGUID => new(GuidHash);
        }

        /// <summary>
        /// All spell schools with their metadata
        /// </summary>
        public static readonly Dictionary<string, SpellSchoolInfo> SpellSchools = new()
        {
            { "blood", new SpellSchoolInfo { Name = "Blood", Prefix = "b", GuidHash = Blood } },
            { "chaos", new SpellSchoolInfo { Name = "Chaos", Prefix = "c", GuidHash = Chaos } },
            { "frost", new SpellSchoolInfo { Name = "Frost", Prefix = "f", GuidHash = Frost } },
            { "illusion", new SpellSchoolInfo { Name = "Illusion", Prefix = "i", GuidHash = Illusion } },
            { "shadow", new SpellSchoolInfo { Name = "Shadow", Prefix = "d", GuidHash = Shadow } },
            { "storm", new SpellSchoolInfo { Name = "Storm", Prefix = "s", GuidHash = Storm } },
            { "unholy", new SpellSchoolInfo { Name = "Unholy", Prefix = "u", GuidHash = Unholy } }
        };

        /// <summary>
        /// Try to get spell school info by name
        /// </summary>
        public static bool TryGetSpellSchool(string name, out SpellSchoolInfo info)
        {
            name = name.ToLowerInvariant();
            return SpellSchools.TryGetValue(name, out info);
        }

        public static int Count => SpellSchools.Count;
    }
}