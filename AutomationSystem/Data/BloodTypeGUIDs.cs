using ProjectM;
using Stunlock.Core;
using System.Collections.Generic;

namespace CrowbaneArena.Data
{
    /// <summary>
    /// Maps blood type names to their corresponding PrefabGUIDs
    /// </summary>
    public static class BloodTypeGUIDs
    {
        private static readonly Dictionary<string, PrefabGUID> _bloodTypeMap = new()
        {
            { "rogue", new PrefabGUID(-1094467405) },    // Rogue blood
            { "rog", new PrefabGUID(-1094467405) },      // Rogue blood (short)
            { "warrior", new PrefabGUID(-700632469) },   // Warrior blood
            { "warr", new PrefabGUID(-700632469) },      // Warrior blood (short)
            { "brute", new PrefabGUID(-700632469) },     // Brute blood (same as warrior)
            { "scholar", new PrefabGUID(-946157678) },   // Scholar blood
            { "creature", new PrefabGUID(1897056612) },  // Creature blood
            { "mutant", new PrefabGUID(-2017994753) },   // Mutant blood
            { "dracula", new PrefabGUID(-327335305) },   // Dracula blood
            { "corrupted", new PrefabGUID(-1413040101) }  // Corrupted blood
        };

        /// <summary>
        /// Gets the PrefabGUID for a blood type by name
        /// </summary>
        public static PrefabGUID GetBloodTypeGUID(string bloodType)
        {
            return _bloodTypeMap.TryGetValue(bloodType.ToLower(), out var guid) ? guid : new PrefabGUID(0);
        }

        /// <summary>
        /// Checks if a blood type name is valid
        /// </summary>
        public static bool IsValidBloodType(string bloodType)
        {
            return _bloodTypeMap.ContainsKey(bloodType.ToLower());
        }
    }
}