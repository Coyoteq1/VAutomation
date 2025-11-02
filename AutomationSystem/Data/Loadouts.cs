using System.Collections.Generic;

namespace CrowbaneArena.Data
{
/// <summary>
/// Complete loadout configurations for CrowbaneArena.
/// These provide balanced sets of weapons, armor, and consumables for PvP matches.
/// </summary>
public static class Loadouts
    {
        /// <summary>
        /// Loadout 1: Warrior Build - Balanced tank/attacker with Chaos enhancements
        /// Weapon: Storm-enchanted Greatsword for strong melee damage
        /// Armor: Full Dracula Warrior set for survivability
        /// Consumables: Mix of healing and empowerment potions
        /// </summary>
        public static readonly LoadoutConfig Warrior = new()
        {
            Name = "warrior",
            Description = "Warrior Build: Storm Greatsword + Chaos Physical Power, full Warrior armor set",
            Weapons = new List<string> { "greatsword" },
            WeaponMods = new Dictionary<string, string> { { "greatsword", "s4" } }, // Storm Physical Power
            ArmorSets = new List<string> { "warrior" },
            Consumables = new List<string>
            {
                "blood_rose_potion",
                "exquisite_brew",
                "physical_brew",
                "spell_brew"
            },
            BloodType = "Warrior",
            Enabled = true
        };

        /// <summary>
        /// Loadout 2: Assassin Build - High mobility and glass cannon damage
        /// Weapon: Illusion-enchanted Daggers for stealth and speed
        /// Armor: Dracula Rogue set for mobility and utilities
        /// Consumables: Speed and damage-focused potions
        /// </summary>
        public static readonly LoadoutConfig Assassin = new()
        {
            Name = "assassin",
            Description = "Assassin Build: Illusion Daggers + Attack Speed, full Rogue armor set",
            Weapons = new List<string> { "daggers" },
            WeaponMods = new Dictionary<string, string> { { "daggers", "i2" } }, // Illusion Attack Speed
            ArmorSets = new List<string> { "rogue" },
            Consumables = new List<string>
            {
                "blood_rose_potion",
                "exquisite_brew",
                "physical_brew",
                "spell_brew"
            },
            BloodType = "Rogue",
            Enabled = true
        };

        /// <summary>
        /// Loadout 3: Mage Build - High magical damage with area control
        /// Weapon: Frost-enchanted Spear for spell power and utility
        /// Armor: Dracula Scholar set for magical enhancements
        /// Consumables: Mana and resistance potions
        /// </summary>
        public static readonly LoadoutConfig Mage = new()
        {
            Name = "mage",
            Description = "Mage Build: Frost Spear + Spell Power, full Scholar armor set",
            Weapons = new List<string> { "spear" },
            WeaponMods = new Dictionary<string, string> { { "spear", "f3" } }, // Frost Spell Power
            ArmorSets = new List<string> { "scholar" },
            Consumables = new List<string>
            {
                "blood_rose_potion",
                "exquisite_brew",
                "physical_brew",
                "spell_brew"
            },
            BloodType = "Scholar",
            Enabled = true
        };

        /// <summary>
        /// Loadout 4: Brute Build - Pure tank with devastating close-range attacks
        /// Weapon: Unholy-enchanted Mace for movement speed and survivability
        /// Armor: Dracula Brute set for maximum defense
        /// Consumables: Survivability and burst damage potions
        /// </summary>
        public static readonly LoadoutConfig Brute = new()
        {
            Name = "brute",
            Description = "Brute Build: Unholy Mace + Movement Speed, full Brute armor set",
            Weapons = new List<string> { "mace" },
            WeaponMods = new Dictionary<string, string> { { "mace", "u1" } }, // Unholy Movement Speed
            ArmorSets = new List<string> { "brute" },
            Consumables = new List<string>
            {
                "blood_rose_potion",
                "exquisite_brew",
                "physical_brew",
                "spell_brew"
            },
            BloodType = "Brute",
            Enabled = true
        };

        /// <summary>
        /// Gets all loadouts as a list.
        /// </summary>
        public static List<LoadoutConfig> All => new()
        {
            Warrior,
            Assassin,
            Mage,
            Brute
        };

        /// <summary>
        /// Gets a loadout by build number (1-4).
        /// </summary>
        /// <param name="buildNumber">Build number (1=Warrior, 2=Assassin, 3=Mage, 4=Brute).</param>
        /// <returns>The corresponding loadout config, or null if invalid number.</returns>
        public static LoadoutConfig GetByNumber(int buildNumber) => buildNumber switch
        {
            1 => Warrior,
            2 => Assassin,
            3 => Mage,
            4 => Brute,
            _ => null
        };

        /// <summary>
        /// Loadout configuration class for PvP setups.
        /// </summary>
        public class LoadoutConfig
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public List<string> Weapons { get; set; } = new();
            public Dictionary<string, string> WeaponMods { get; set; } = new();
            public List<string> ArmorSets { get; set; } = new();
            public List<string> Consumables { get; set; } = new();
            public string BloodType { get; set; } = "";
            public bool Enabled { get; set; } = true;
        }
    }
}
