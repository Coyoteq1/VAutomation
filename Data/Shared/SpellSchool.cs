using System.Collections.Generic;

namespace CrowbaneArena.Data.Shared
{
    /// <summary>
    /// Spell school data and mappings
    /// </summary>
    public static class SpellSchool
    {
        /// <summary>
        /// Spell school mappings
        /// </summary>
        public static readonly Dictionary<SkillSchoolGuid, SchoolInfo> Schools = new()
        {
            { SkillSchoolGuid.Warrior, new("Warrior", -949105687) },
            { SkillSchoolGuid.Rogue, new("Rogue", 1692713560) },
            { SkillSchoolGuid.Scholar, new("Scholar", -147175582) },
            { SkillSchoolGuid.Shadow, new("Shadow", 1640279551) },
            { SkillSchoolGuid.Blood, new("Blood", 760403672) },
            { SkillSchoolGuid.Fire, new("Fire", 1767769020) },
            { SkillSchoolGuid.Frost, new("Frost", -1673696349) },
            { SkillSchoolGuid.Holy, new("Holy", -767534946) },
            { SkillSchoolGuid.Unholy, new("Unholy", 1732301163) },
            { SkillSchoolGuid.Nature, new("Nature", -1226374539) },
            { SkillSchoolGuid.Illusion, new("Illusion", -1082046471) },
            { SkillSchoolGuid.Chaos, new("Chaos", -1311455756) }
        };

        /// <summary>
        /// Try to get spell school info by GUID
        /// </summary>
        /// <param name="guid">School GUID to look up</param>
        /// <param name="info">The found school info, or null if not found</param>
        /// <returns>True if the school was found, false otherwise</returns>
        public static bool TryGetSchool(SkillSchoolGuid guid, out SchoolInfo? info)
        {
            return Schools.TryGetValue(guid, out info);
        }

        /// <summary>
        /// Try to get spell school info by name
        /// </summary>
        /// <param name="name">School name to look up</param>
        /// <param name="info">The found school info, or null if not found</param>
        /// <returns>True if the school was found, false otherwise</returns>
        public static bool TryGetSchool(string name, out SchoolInfo? info)
        {
            name = name.ToLowerInvariant();
            foreach (var school in Schools)
            {
                if (school.Value.Name.ToLowerInvariant() == name)
                {
                    info = school.Value;
                    return true;
                }
            }
            info = null;
            return false;
        }
    }

    /// <summary>
    /// Available spell school GUIDs
    /// </summary>
    public enum SkillSchoolGuid
    {
        /// <summary>Warrior school</summary>
        Warrior = 0,
        /// <summary>Rogue school</summary>
        Rogue = 1,
        /// <summary>Scholar school</summary>
        Scholar = 2,
        /// <summary>Shadow school</summary>
        Shadow = 3,
        /// <summary>Blood school</summary>
        Blood = 4,
        /// <summary>Fire school</summary>
        Fire = 5,
        /// <summary>Frost school</summary>
        Frost = 6,
        /// <summary>Holy school</summary>
        Holy = 7,
        /// <summary>Unholy school</summary>
        Unholy = 8,
        /// <summary>Nature school</summary>
        Nature = 9,
        /// <summary>Illusion school</summary>
        Illusion = 10,
        /// <summary>Chaos school</summary>
        Chaos = 11
    }

    /// <summary>
    /// Spell school information
    /// </summary>
    public class SchoolInfo
    {
        /// <summary>
        /// Display name of the spell school
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Prefab GUID for the spell school
        /// </summary>
        public int PrefabGuid { get; set; }

        /// <summary>
        /// Creates a new spell school info
        /// </summary>
        /// <param name="name">Display name</param>
        /// <param name="prefabGuid">Prefab GUID</param>
        public SchoolInfo(string name, int prefabGuid)
        {
            Name = name;
            PrefabGuid = prefabGuid;
        }
    }
}
