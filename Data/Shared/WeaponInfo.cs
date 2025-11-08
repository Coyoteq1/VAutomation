using System.Collections.Generic;

namespace CrowbaneArena.Data.Shared
{
    /// <summary>
    /// Weapon data and utilities shared between CrowbaneArena and ICB.core
    /// </summary>
    public static class WeaponInfo
    {
        /// <summary>
        /// Base weapon types and their prefab GUIDs
        /// </summary>
        public static readonly Dictionary<WeaponType, WeaponData> Weapons = new()
        {
            { WeaponType.Sword, new("Sword", 1658217462, "Equipment_Weapon_Sword") },
            { WeaponType.Axe, new("Axe", -1872259030, "Equipment_Weapon_Axe") },
            { WeaponType.Spear, new("Spear", -278503386, "Equipment_Weapon_Spear") },
            { WeaponType.Slasher, new("Slasher", 645018743, "Equipment_Weapon_Slashers") },
            { WeaponType.Crossbow, new("Crossbow", -1892959271, "Equipment_Weapon_Crossbow") },
            { WeaponType.Mace, new("Mace", -2022172226, "Equipment_Weapon_Mace") },
            { WeaponType.Reaper, new("Reaper", 1957544031, "Equipment_Weapon_Reaper") },
            { WeaponType.Staff, new("Staff", 1957544031, "Equipment_Weapon_Staff") }};

        /// <summary>
        /// Try to get weapon data by type
        /// </summary>
        /// <param name="type">Weapon type to look up</param>
        /// <param name="data">The found weapon data, or null if not found</param>
        /// <returns>True if the weapon was found, false otherwise</returns>
        public static bool TryGetWeapon(WeaponType type, out WeaponData? data)
        {
            return Weapons.TryGetValue(type, out data);
        }

        /// <summary>
        /// Try to get weapon data by name
        /// </summary>
        /// <param name="name">Weapon name to look up</param>
        /// <param name="data">The found weapon data, or null if not found</param>
        /// <returns>True if the weapon was found, false otherwise</returns>
        public static bool TryGetWeapon(string name, out WeaponData? data)
        {
            name = name.ToLowerInvariant();
            foreach (var weapon in Weapons)
            {
                if (weapon.Value.Name.ToLowerInvariant() == name)
                {
                    data = weapon.Value;
                    return true;
                }
            }
            data = null;
            return false;
        }
    }

    /// <summary>
    /// Available weapon types
    /// </summary>
    public enum WeaponType
    {
        /// <summary>One-handed sword</summary>
        Sword,
        /// <summary>Two-handed axe</summary>
        Axe,
        /// <summary>Two-handed spear</summary>
        Spear,
        /// <summary>Dual-wielded slashers</summary>
        Slasher,
        /// <summary>Two-handed crossbow</summary>
        Crossbow,
        /// <summary>One-handed mace</summary>
        Mace,
        /// <summary>Two-handed scythe</summary>
        Reaper,
        /// <summary>Two-handed staff</summary>
        Staff
    }

    /// <summary>
    /// Weapon data including name, GUID, and prefab information
    /// </summary>
    public class WeaponData
    {
        /// <summary>
        /// Display name of the weapon
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// GUID for the base weapon prefab
        /// </summary>
        public int PrefabGuid { get; set; }

        /// <summary>
        /// Base prefab name without variants or modifiers
        /// </summary>
        public string BasePrefabName { get; set; }

        /// <summary>
        /// Creates new weapon data
        /// </summary>
        /// <param name="name">Display name</param>
        /// <param name="prefabGuid">Base prefab GUID</param>
        /// <param name="basePrefabName">Base prefab name</param>
        public WeaponData(string name, int prefabGuid, string basePrefabName)
        {
            Name = name;
            PrefabGuid = prefabGuid;
            BasePrefabName = basePrefabName;
        }
    }
}
