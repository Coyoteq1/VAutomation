using System.Collections.Generic;
using Stunlock.Core;

namespace CrowbaneArena.Data
{
    /// <summary>
    /// Hardcoded arena loadouts - fallback when no config file exists
    /// Based on VRisingArenaBuilds pattern
    /// </summary>
    public static class HardcodedLoadouts
    {
        /// <summary>
        /// Get all hardcoded loadouts
        /// </summary>
        public static Dictionary<string, LoadoutDefinition> GetLoadouts()
        {
            return new Dictionary<string, LoadoutDefinition>
            {
                ["default"] = CreateDefaultLoadout(),
                ["warrior"] = CreateWarriorLoadout(),
                ["scholar"] = CreateRangerLoadout(),
                ["mage"] = CreateMageLoadout(),
                ["build1"] = CreateBuild1(),
                ["build2"] = CreateBuild2(),
                ["build3"] = CreateBuild3(),
                ["build4"] = CreateBuild4()
            };
        }

        private static LoadoutDefinition CreateDefaultLoadout()
        {
            return new LoadoutDefinition
            {
                Name = "default",
                Description = "Default Arena Loadout - Balanced starter build",
                BloodType = "Rogue",
                BloodQuality = 100f,
                Weapons = new List<WeaponDefinition>
                {
                    new WeaponDefinition
                    {
                        PrefabName = "Item_Weapon_Sword_Sanguine",
                        Guid = -774462329,
                        SpellSchool = "Storm",
                        StatMod = "Physical Power"
                    }
                },
                Armor = new ArmorDefinition
                {
                    Chest = new PrefabGUID(-1266262267),  // Sanguine Chest
                    Legs = new PrefabGUID(-1266262266),   // Sanguine Legs
                    Boots = new PrefabGUID(-1266262265),  // Sanguine Boots
                    Gloves = new PrefabGUID(-1266262264)  // Sanguine Gloves
                },
                Consumables = new List<ConsumableDefinition>
                {
                    new ConsumableDefinition { Guid = -1531666018, Amount = 10 }, // Blood Potion
                    new ConsumableDefinition { Guid = -437611596, Amount = 5 }    // Minor Blood Potion
                },
                Abilities = new List<string>
                {
                    "AB_Vampire_VeilOfBlood_AbilityGroup",
                    "AB_Vampire_ShadowBolt_AbilityGroup"
                }
            };
        }

        private static LoadoutDefinition CreateWarriorLoadout()
        {
            return new LoadoutDefinition
            {
                Name = "warrior",
                Description = "Warrior Loadout - Heavy melee combat",
                BloodType = "Warrior",
                BloodQuality = 100f,
                Weapons = new List<WeaponDefinition>
                {
                    new WeaponDefinition
                    {
                        PrefabName = "Item_Weapon_Axe_Sanguine",
                        Guid = -2044057823,
                        SpellSchool = "Chaos",
                        StatMod = "Physical Power"
                    }
                },
                Armor = new ArmorDefinition
                {
                    Chest = new PrefabGUID(-1266262267),
                    Legs = new PrefabGUID(-1266262266),
                    Boots = new PrefabGUID(-1266262265),
                    Gloves = new PrefabGUID(-1266262264)
                },
                Consumables = new List<ConsumableDefinition>
                {
                    new ConsumableDefinition { Guid = -1531666018, Amount = 10 },
                    new ConsumableDefinition { Guid = 1223264867, Amount = 3 }  // Physical Power Potion
                },
                Abilities = new List<string>
                {
                    "AB_Vampire_VeilOfChaos_AbilityGroup",
                    "AB_Vampire_PowerSurge_AbilityGroup"
                }
            };
        }

        private static LoadoutDefinition CreateRangerLoadout()
        {
            return new LoadoutDefinition
            {
                Name = "ranger",
                Description = "Ranger Loadout - Ranged combat specialist",
                BloodType = "Rogue",
                BloodQuality = 100f,
                Weapons = new List<WeaponDefinition>
                {
                    new WeaponDefinition
                    {
                        PrefabName = "Item_Weapon_Crossbow_Sanguine",
                        Guid = 1389040540,
                        SpellSchool = "Storm",
                        StatMod = "Attack Speed"
                    }
                },
                Armor = new ArmorDefinition
                {
                    Chest = new PrefabGUID(-1266262267),
                    Legs = new PrefabGUID(-1266262266),
                    Boots = new PrefabGUID(-1266262265),
                    Gloves = new PrefabGUID(-1266262264)
                },
                Consumables = new List<ConsumableDefinition>
                {
                    new ConsumableDefinition { Guid = -1531666018, Amount = 10 },
                    new ConsumableDefinition { Guid = -437611596, Amount = 5 }
                },
                Abilities = new List<string>
                {
                    "AB_Vampire_VeilOfStorm_AbilityGroup",
                    "AB_Vampire_MistTrance_AbilityGroup"
                }
            };
        }

        private static LoadoutDefinition CreateMageLoadout()
        {
            return new LoadoutDefinition
            {
                Name = "mage",
                Description = "Mage Loadout - Spell-focused combat",
                BloodType = "Scholar",
                BloodQuality = 100f,
                Weapons = new List<WeaponDefinition>
                {
                    new WeaponDefinition
                    {
                        PrefabName = "Item_Weapon_Reaper_Sanguine",
                        Guid = 1504279833,
                        SpellSchool = "Frost",
                        StatMod = "Spell Power"
                    }
                },
                Armor = new ArmorDefinition
                {
                    Chest = new PrefabGUID(-1266262267),
                    Legs = new PrefabGUID(-1266262266),
                    Boots = new PrefabGUID(-1266262265),
                    Gloves = new PrefabGUID(-1266262264)
                },
                Consumables = new List<ConsumableDefinition>
                {
                    new ConsumableDefinition { Guid = -1531666018, Amount = 10 },
                    new ConsumableDefinition { Guid = 1223264868, Amount = 3 }  // Spell Power Potion
                },
                Abilities = new List<string>
                {
                    "AB_Vampire_VeilOfFrost_AbilityGroup",
                    "AB_Vampire_IcySpear_AbilityGroup"
                }
            };
        }

        private static LoadoutDefinition CreateBuild1()
        {
            return new LoadoutDefinition
            {
                Name = "build1",
                Description = "Build 1 - Sword & Storm",
                BloodType = "Rogue",
                BloodQuality = 100f,
                Weapons = new List<WeaponDefinition>
                {
                    new WeaponDefinition
                    {
                        PrefabName = "Item_Weapon_Sword_Sanguine",
                        Guid = -774462329,
                        SpellSchool = "Storm",
                        StatMod = "Physical Power"
                    }
                },
                Armor = new ArmorDefinition
                {
                    Chest = new PrefabGUID(-1266262267),
                    Legs = new PrefabGUID(-1266262266),
                    Boots = new PrefabGUID(-1266262265),
                    Gloves = new PrefabGUID(-1266262264)
                },
                Consumables = new List<ConsumableDefinition>
                {
                    new ConsumableDefinition { Guid = -1531666018, Amount = 10 }
                },
                Abilities = new List<string>
                {
                    "AB_Vampire_VeilOfStorm_AbilityGroup"
                }
            };
        }

        private static LoadoutDefinition CreateBuild2()
        {
            return new LoadoutDefinition
            {
                Name = "build2",
                Description = "Build 2 - Axe & Chaos",
                BloodType = "Warrior",
                BloodQuality = 100f,
                Weapons = new List<WeaponDefinition>
                {
                    new WeaponDefinition
                    {
                        PrefabName = "Item_Weapon_Axe_Sanguine",
                        Guid = -2044057823,
                        SpellSchool = "Chaos",
                        StatMod = "Physical Power"
                    }
                },
                Armor = new ArmorDefinition
                {
                    Chest = new PrefabGUID(-1266262267),
                    Legs = new PrefabGUID(-1266262266),
                    Boots = new PrefabGUID(-1266262265),
                    Gloves = new PrefabGUID(-1266262264)
                },
                Consumables = new List<ConsumableDefinition>
                {
                    new ConsumableDefinition { Guid = -1531666018, Amount = 10 }
                },
                Abilities = new List<string>
                {
                    "AB_Vampire_VeilOfChaos_AbilityGroup"
                }
            };
        }

        private static LoadoutDefinition CreateBuild3()
        {
            return new LoadoutDefinition
            {
                Name = "build3",
                Description = "Build 3 - Spear & Blood",
                BloodType = "Rogue",
                BloodQuality = 100f,
                Weapons = new List<WeaponDefinition>
                {
                    new WeaponDefinition
                    {
                        PrefabName = "Item_Weapon_Spear_Sanguine",
                        Guid = 1532449451,
                        SpellSchool = "Blood",
                        StatMod = "Spell Power"
                    }
                },
                Armor = new ArmorDefinition
                {
                    Chest = new PrefabGUID(-1266262267),
                    Legs = new PrefabGUID(-1266262266),
                    Boots = new PrefabGUID(-1266262265),
                    Gloves = new PrefabGUID(-1266262264)
                },
                Consumables = new List<ConsumableDefinition>
                {
                    new ConsumableDefinition { Guid = -1531666018, Amount = 10 }
                },
                Abilities = new List<string>
                {
                    "AB_Vampire_VeilOfBlood_AbilityGroup"
                }
            };
        }

        private static LoadoutDefinition CreateBuild4()
        {
            return new LoadoutDefinition
            {
                Name = "build4",
                Description = "Build 4 - Reaper & Frost",
                BloodType = "Scholar",
                BloodQuality = 100f,
                Weapons = new List<WeaponDefinition>
                {
                    new WeaponDefinition
                    {
                        PrefabName = "Item_Weapon_Reaper_Sanguine",
                        Guid = 1504279833,
                        SpellSchool = "Frost",
                        StatMod = "Spell Power"
                    }
                },
                Armor = new ArmorDefinition
                {
                    Chest = new PrefabGUID(-1266262267),
                    Legs = new PrefabGUID(-1266262266),
                    Boots = new PrefabGUID(-1266262265),
                    Gloves = new PrefabGUID(-1266262264)
                },
                Consumables = new List<ConsumableDefinition>
                {
                    new ConsumableDefinition { Guid = -1531666018, Amount = 10 }
                },
                Abilities = new List<string>
                {
                    "AB_Vampire_VeilOfFrost_AbilityGroup"
                }
            };
        }
    }

    /// <summary>
    /// Complete loadout definition
    /// </summary>
    public class LoadoutDefinition
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string BloodType { get; set; }
        public float BloodQuality { get; set; }
        public List<WeaponDefinition> Weapons { get; set; } = new List<WeaponDefinition>();
        public ArmorDefinition Armor { get; set; }
        public List<ConsumableDefinition> Consumables { get; set; } = new List<ConsumableDefinition>();
        public List<string> Abilities { get; set; } = new List<string>();
    }

    /// <summary>
    /// Weapon with mods
    /// </summary>
    public class WeaponDefinition
    {
        public string PrefabName { get; set; }
        public int Guid { get; set; }
        public string SpellSchool { get; set; }
        public string StatMod { get; set; }
    }

    /// <summary>
    /// Armor set definition
    /// </summary>
    public class ArmorDefinition
    {
        public PrefabGUID Chest { get; set; }
        public PrefabGUID Legs { get; set; }
        public PrefabGUID Boots { get; set; }
        public PrefabGUID Gloves { get; set; }
    }

    /// <summary>
    /// Consumable with amount
    /// </summary>
    public class ConsumableDefinition
    {
        public int Guid { get; set; }
        public int Amount { get; set; }
    }
}
