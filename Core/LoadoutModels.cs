using Stunlock.Core;
using System.Collections.Generic;

namespace CrowbaneArena.Core
{
    /// <summary>
    /// A simple container for any item that has an amount.
    /// </summary>
    public class ArenaItem
    {
        public PrefabGUID Guid { get; set; }
        public int Amount { get; set; }
    }

    /// <summary>
    /// A container representing one complete armor set (boots / gloves / chest / legs).
    /// </summary>
    public class ArenaArmorSet
    {
        public string Name { get; set; } = string.Empty;
        public List<PrefabGUID> Guids { get; set; } = new();
    }

    /// <summary>
    /// A consumable definition with a default amount.
    /// </summary>
    public class ArenaConsumable
    {
        public string Name { get; set; } = string.Empty;
        public PrefabGUID Guid { get; set; }
        public int DefaultAmount { get; set; } = 1;
    }

    /// <summary>
    /// Final assembled loadout that can be granted to the player on arena entry.
    /// </summary>
    public class ArenaLoadout
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
        public List<PrefabGUID> Weapons { get; set; } = new();
        public List<PrefabGUID> Armor { get; set; } = new();
        public List<ArenaItem> Consumables { get; set; } = new();
        public string WeaponMods { get; set; } = string.Empty;
        public string BloodType { get; set; } = string.Empty;
    }
}
