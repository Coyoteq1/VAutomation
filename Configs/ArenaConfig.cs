using System.Collections.Generic;
using Newtonsoft.Json;

namespace CrowbaneArena.Configs
{
    public class ArenaConfig
    {
        public BuildConfig Offensive { get; set; }
        public BuildConfig Defensive { get; set; }
        public BuildConfig Minions { get; set; }
    }

    public class BuildConfig
    {
        public BuildSettings Settings { get; set; }
        public BloodConfig Blood { get; set; }
        public ArmorConfig Armors { get; set; }
        public List<WeaponConfig> Weapons { get; set; }
        public List<ItemConfig> Items { get; set; }
        public AbilitiesConfig Abilities { get; set; }
        public PassiveSpellsConfig PassiveSpells { get; set; }
    }

    public class BuildSettings
    {
        public bool ClearInventory { get; set; }
    }

    public class BloodConfig
    {
        public bool FillBloodPool { get; set; }
        public bool GiveBloodPotion { get; set; }
        public string PrimaryType { get; set; }
        public string SecondaryType { get; set; }
        public int PrimaryQuality { get; set; }
        public int SecondaryQuality { get; set; }
        public int SecondaryBuffIndex { get; set; }
    }

    public class ArmorConfig
    {
        public string Boots { get; set; }
        public string Chest { get; set; }
        public string Gloves { get; set; }
        public string Legs { get; set; }
        public string MagicSource { get; set; }
        public string Head { get; set; }
        public string Cloak { get; set; }
        public string Bag { get; set; }
    }

    public class WeaponConfig
    {
        public string Name { get; set; }
        public string InfuseSpellMod { get; set; }
        public string SpellMod1 { get; set; }
        public string SpellMod2 { get; set; }
        public string StatMod1 { get; set; }
        public float StatMod1Power { get; set; }
        public string StatMod2 { get; set; }
        public float StatMod2Power { get; set; }
        public string StatMod3 { get; set; }
        public float StatMod3Power { get; set; }
        public string StatMod4 { get; set; }
        public float StatMod4Power { get; set; }
    }

    public class ItemConfig
    {
        public string Name { get; set; }
        public int Amount { get; set; }
    }

    public class AbilitiesConfig
    {
        public AbilityConfig Travel { get; set; }
        public AbilityConfig Ability1 { get; set; }
        public AbilityConfig Ability2 { get; set; }
        public UltimateConfig Ultimate { get; set; }
    }

    public class AbilityConfig
    {
        public string Name { get; set; }
        public JewelConfig Jewel { get; set; }
    }

    public class JewelConfig
    {
        public string SpellMod1 { get; set; }
        public float SpellMod1Power { get; set; }
        public string SpellMod2 { get; set; }
        public float SpellMod2Power { get; set; }
        public string SpellMod3 { get; set; }
        public float SpellMod3Power { get; set; }
        public string SpellMod4 { get; set; }
        public float SpellMod4Power { get; set; }
    }

    public class UltimateConfig
    {
        public string Name { get; set; }
    }

    public class PassiveSpellsConfig
    {
        public string PassiveSpell1 { get; set; }
        public string PassiveSpell2 { get; set; }
        public string PassiveSpell3 { get; set; }
        public string PassiveSpell4 { get; set; }
        public string PassiveSpell5 { get; set; }
    }

    public static class ArenaConfigHelper
    {
        public static ArenaConfig Load(string configPath)
        {
            if (!System.IO.File.Exists(configPath))
                throw new System.IO.FileNotFoundException("Arena config file not found", configPath);

            string json = System.IO.File.ReadAllText(configPath);
            return JsonConvert.DeserializeObject<ArenaConfig>(json);
        }

        public static void Save(ArenaConfig config, string configPath)
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            System.IO.File.WriteAllText(configPath, json);
        }

        public static bool Validate(ArenaConfig config)
        {
            // Basic validation
            if (config.Offensive == null || config.Defensive == null || config.Minions == null)
                return false;

            return true;
        }
    }
}
