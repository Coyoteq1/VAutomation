using System.Collections.Generic;
using Unity.Mathematics;
using CrowbaneArena.Services;

namespace CrowbaneArena
{
    /// <summary>
    /// Legacy model classes for backward compatibility with ArenaConfigLoader
    /// These are simple DTOs used for JSON deserialization
    /// For actual data access, use the Data structures and Helpers
    /// </summary>

    public class Weapon
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Guid { get; set; }
        public List<WeaponVariant> Variants { get; set; } = new List<WeaponVariant>();
        public bool Enabled { get; set; }
    }

    public class WeaponVariant
    {
        public string Name { get; set; } = string.Empty;
        public int Guid { get; set; }
        public string ModCombo { get; set; } = string.Empty;
        public int VariantGuid { get; set; }
        public string FriendlyName { get; set; } = string.Empty;
    }

    public class ArmorSet
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public uint ChestGuid { get; set; }
        public uint LegsGuid { get; set; }
        public uint BootsGuid { get; set; }
        public uint GlovesGuid { get; set; }
        public bool Enabled { get; set; }
    }

    public class Consumable
    {
        public string Name { get; set; } = string.Empty;
        public uint Guid { get; set; }
        public int DefaultAmount { get; set; }
        public bool Enabled { get; set; }
    }

    public class Build
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Weapon { get; set; } = string.Empty;
        public string WeaponMods { get; set; } = string.Empty;
        public string ArmorSet { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
        public bool Enabled { get; set; }
    }

    public class SpellSchool
    {
        public string Name { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public int Guid { get; set; }
        public bool Enabled { get; set; }
    }

    public class StatMod
    {
        public string Name { get; set; } = string.Empty;
        public int Number { get; set; }
        public int Guid { get; set; }
        public bool Enabled { get; set; }
    }

    public class BloodType
    {
        public string Name { get; set; } = string.Empty;
        public int Guid { get; set; }
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Legacy ArenaConfig class for backward compatibility with ArenaConfigLoader
    /// </summary>
    public class ArenaConfig
    {
        public bool Enabled { get; set; } = true;
        public ZoneLocation Location { get; set; } = new ZoneLocation();
        public PortalConfig Portal { get; set; } = new PortalConfig();
        public GameplaySettings Gameplay { get; set; } = new GameplaySettings();
        public List<Weapon> Weapons { get; set; } = new List<Weapon>();
        public List<ArmorSet> ArmorSets { get; set; } = new List<ArmorSet>();
        public List<Consumable> Consumables { get; set; } = new List<Consumable>();
        public List<Loadout> Loadouts { get; set; } = new List<Loadout>();
        public List<ArenaZone> Zones { get; set; } = new List<ArenaZone>();
    }

    /// <summary>
    /// Zone location configuration
    /// </summary>
    public class ZoneLocation
    {
        public float3 Center { get; set; } = float3.zero;
        public float Radius { get; set; } = 50f;
        public float3 SpawnPoint { get; set; } = float3.zero;
    }

    /// <summary>
    /// Portal configuration for zone transitions
    /// </summary>
    public class PortalConfig
    {
        public ZoneLocation Entry { get; set; } = new ZoneLocation();
        public ZoneLocation Exit { get; set; } = new ZoneLocation();
    }

    /// <summary>
    /// Gameplay settings for arena configuration
    /// </summary>
    public class GameplaySettings
    {
        public bool EnableGodMode { get; set; } = false;
        public bool RestoreOnExit { get; set; } = true;
        public bool AllowPvP { get; set; } = true;
        public bool VBloodProgression { get; set; } = true;
    }
}
