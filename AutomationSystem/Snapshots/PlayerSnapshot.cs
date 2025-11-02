using System;
using System.Collections.Generic;

namespace AutomationSystem.Models
{
    public class PlayerSnapshot
    {
        public Guid SnapshotId { get; set; } = Guid.NewGuid();
        public ulong PlatformId { get; set; } = 0; // Player who owns this snapshot
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsInArena { get; set; } = false;
        public bool WasPvPEnabled { get; set; } = false; // Track PvP state before entering arena
        public bool IsPvPEnabled { get; set; } = false; // Current PvP state
        public string OriginalName { get; set; } = "";
        public int BloodTypeGuid { get; set; } = 0;
        public float BloodQuality { get; set; } = 0f;
        public int OriginalLevel { get; set; } = 1;

        public List<int> UnlockedVBloods { get; set; } = new();
        public List<int> SpellSchools { get; set; } = new();
        public Dictionary<int, ItemData> InventoryItems { get; set; } = new();
        public List<ItemData> EquippedItems { get; set; } = new();
    }

    public class ItemData
    {
        public int ItemGuidHash { get; set; }
        public int Amount { get; set; }
    }
}
