using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;

namespace CrowbaneArena.Data
{
    /// <summary>
    /// Arena zone configurations
    /// Converted from arena_config.json Zones
    /// </summary>
    public static class ArenaZones
    {
        public class ZoneConfig
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public float SpawnX { get; set; }
            public float SpawnY { get; set; }
            public float SpawnZ { get; set; }
            public float Radius { get; set; }
            public bool Enabled { get; set; } = true;
            public bool Default { get; set; } = false;

            public float3 GetSpawnPosition() => new float3(SpawnX, SpawnY, SpawnZ);
        }

        /// <summary>
        /// All available arena zones
        /// </summary>
        public static readonly Dictionary<string, ZoneConfig> Zones = new()
        {
            ["default"] = new ZoneConfig
            {
                Name = "default",
                Description = "Default PvP Arena - Balanced gameplay",
                SpawnX = -1000.0f,
                SpawnY = 0.0f,
                SpawnZ = -500.0f,
                Radius = 200.0f,
                Enabled = true,
                Default = true
            },
            ["duel"] = new ZoneConfig
            {
                Name = "duel",
                Description = "1v1 Duel Arena - Small, fast-paced",
                SpawnX = -800.0f,
                SpawnY = 0.0f,
                SpawnZ = -300.0f,
                Radius = 50.0f,
                Enabled = true
            },
            ["battle_royale"] = new ZoneConfig
            {
                Name = "battle_royale",
                Description = "Large Battle Royale - Up to 20 players",
                SpawnX = -1200.0f,
                SpawnY = 0.0f,
                SpawnZ = -700.0f,
                Radius = 400.0f,
                Enabled = true
            },
            ["training"] = new ZoneConfig
            {
                Name = "training",
                Description = "Training Grounds - Practice without blood loss",
                SpawnX = -600.0f,
                SpawnY = 0.0f,
                SpawnZ = -100.0f,
                Radius = 100.0f,
                Enabled = true
            },
            ["tournament"] = new ZoneConfig
            {
                Name = "tournament",
                Description = "Tournament Arena - Official matches",
                SpawnX = -1400.0f,
                SpawnY = 0.0f,
                SpawnZ = -900.0f,
                Radius = 150.0f,
                Enabled = true
            },
            ["experimental"] = new ZoneConfig
            {
                Name = "experimental",
                Description = "Experimental Zone - Testing new features",
                SpawnX = -400.0f,
                SpawnY = 0.0f,
                SpawnZ = 100.0f,
                Radius = 75.0f,
                Enabled = false
            }
        };

        /// <summary>
        /// Get all enabled zones
        /// </summary>
        public static List<ZoneConfig> GetEnabledZones()
        {
            return Zones.Values.Where(z => z.Enabled).ToList();
        }

        /// <summary>
        /// Get default zone
        /// </summary>
        public static ZoneConfig? GetDefaultZone()
        {
            return Zones.Values.FirstOrDefault(z => z.Default) ?? Zones.Values.FirstOrDefault();
        }

        /// <summary>
        /// Try to get zone by name
        /// </summary>
        public static bool TryGetZone(string name, out ZoneConfig? zone)
        {
            return Zones.TryGetValue(name.ToLowerInvariant(), out zone);
        }

        /// <summary>
        /// Get all zone names
        /// </summary>
        public static List<string> GetZoneNames()
        {
            return Zones.Keys.ToList();
        }
    }
}
