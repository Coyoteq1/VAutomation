using System.Collections.Generic;

namespace CrowbaneArena.Data.Shared
{
    /// <summary>
    /// Infusion spell modifier data - compatible with ICB.core and System.Text.Json
    /// </summary>
    public static class InfuseSpellMod
    {
        /// <summary>
        /// List of available infusion spell modifiers
        /// </summary>
        public static readonly List<InfuseSpellModInfo> Mods = new()
        {
            new("Blood", "SpellMod_Weapon_BloodInfused", new[] { "blood", "leech" }),
            new("Chaos", "SpellMod_Weapon_ChaosInfused", new[] { "chaos", "ignite" }),
            new("Frost", "SpellMod_Weapon_FrostInfused", new[] { "frost", "chill" }),
            new("Illusion", "SpellMod_Weapon_IllusionInfused", new[] { "illusion", "weaken" }),
            new("Storm", "SpellMod_Weapon_StormInfused", new[] { "storm", "static" }),
            new("Undead", "SpellMod_Weapon_UndeadInfused", new[] { "undead", "unholy", "green" })
        };

        /// <summary>
        /// Try to get infusion spell mod by name or keyword
        /// </summary>
        /// <param name="name">Name or keyword to search for</param>
        /// <param name="mod">The found spell modifier info, or null if not found</param>
        /// <returns>True if the spell modifier was found, false otherwise</returns>
        public static bool TryGetMod(string name, out InfuseSpellModInfo? mod)
        {
            name = name.ToLowerInvariant();
            mod = Mods.Find(m => m.Name.ToLowerInvariant() == name || 
                                m.Keywords.Exists(k => k.ToLowerInvariant() == name));
            return mod != null;
        }
    }

    /// <summary>
    /// Infusion spell modifier information
    /// </summary>
    public class InfuseSpellModInfo
    {
        /// <summary>
        /// Display name of the spell modifier
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Prefab name for the spell modifier
        /// </summary>
        public string PrefabName { get; set; }

        /// <summary>
        /// Alternative keywords/names for this spell modifier
        /// </summary>
        public List<string> Keywords { get; set; }

        /// <summary>
        /// Creates a new infusion spell modifier info
        /// </summary>
        /// <param name="name">Display name</param>
        /// <param name="prefabName">Prefab name</param>
        /// <param name="keywords">Alternative keywords/names</param>
        public InfuseSpellModInfo(string name, string prefabName, IEnumerable<string> keywords)
        {
            Name = name;
            PrefabName = prefabName;
            Keywords = new List<string>(keywords);
        }
    }
}