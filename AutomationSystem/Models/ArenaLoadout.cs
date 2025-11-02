using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CrowbaneArena.Models
{
    public class ArenaLoadout
    {
        [JsonPropertyName("Name")]
        public string Name { get; set; } = "Default Loadout";

        [JsonPropertyName("Weapons")]
        public List<ArenaItem> Weapons { get; set; } = new();

        [JsonPropertyName("Armor")]
        public List<ArenaItem> Armor { get; set; } = new();

        [JsonPropertyName("Items")]
        public List<ArenaItem> Items { get; set; } = new();

        [JsonPropertyName("Buffs")]
        public List<string> Buffs { get; set; } = new();
    }

    public class ArenaItem
    {
        [JsonPropertyName("PrefabGUID")]
        public string PrefabGUID { get; set; } = string.Empty;

        [JsonPropertyName("Amount")]
        public int Amount { get; set; } = 1;

        [JsonPropertyName("Level")]
        public int Level { get; set; } = 1;
    }
}
