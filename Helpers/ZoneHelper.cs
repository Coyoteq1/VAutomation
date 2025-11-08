using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Entities;
using CrowbaneArena.Data;

namespace CrowbaneArena.Helpers
{
    /// <summary>
    /// Helper class for arena zone management
    /// Bridges between ArenaZones data and game API
    /// </summary>
    public static class ZoneHelper
    {
        /// <summary>
        /// Get spawn location for a zone by name
        /// </summary>
        public static float3? GetZoneSpawnLocation(string zoneName)
        {
            if (ArenaZones.TryGetZone(zoneName, out var zone))
            {
                return zone.GetSpawnPosition();
            }
            return null;
        }

        /// <summary>
        /// Get default arena spawn location
        /// </summary>
        public static float3 GetDefaultSpawnLocation()
        {
            var defaultZone = ArenaZones.GetDefaultZone();
            return defaultZone?.GetSpawnPosition() ?? new float3(-1000f, 0f, -500f);
        }

        /// <summary>
        /// Get zone radius by name
        /// </summary>
        public static float GetZoneRadius(string zoneName)
        {
            if (ArenaZones.TryGetZone(zoneName, out var zone))
            {
                return zone.Radius;
            }
            return 200f; // Default radius
        }

        /// <summary>
        /// Check if a position is within a zone
        /// </summary>
        public static bool IsInZone(float3 position, string zoneName)
        {
            if (!ArenaZones.TryGetZone(zoneName, out var zone))
            {
                return false;
            }

            var zoneCenter = zone.GetSpawnPosition();
            var distance = math.distance(position, zoneCenter);
            return distance <= zone.Radius;
        }

        /// <summary>
        /// Check if a position is within the default arena zone
        /// </summary>
        public static bool IsInDefaultArena(float3 position)
        {
            var defaultZone = ArenaZones.GetDefaultZone();
            if (defaultZone == null) return false;

            var zoneCenter = defaultZone.GetSpawnPosition();
            var distance = math.distance(position, zoneCenter);
            return distance <= defaultZone.Radius;
        }

        /// <summary>
        /// Get all enabled zone names
        /// </summary>
        public static List<string> GetEnabledZoneNames()
        {
            var enabledZones = ArenaZones.GetEnabledZones();
            var names = new List<string>();
            foreach (var zone in enabledZones)
            {
                names.Add(zone.Name);
            }
            return names;
        }

        /// <summary>
        /// Get zone info by name
        /// </summary>
        public static ArenaZones.ZoneConfig? GetZoneInfo(string zoneName)
        {
            ArenaZones.TryGetZone(zoneName, out var zone);
            return zone;
        }

        /// <summary>
        /// Teleport player to zone spawn
        /// </summary>
        public static bool TeleportToZone(Entity characterEntity, string zoneName)
        {
            try
            {
                var spawnLocation = GetZoneSpawnLocation(zoneName);
                if (!spawnLocation.HasValue)
                {
                    Plugin.Logger?.LogError($"Zone '{zoneName}' not found");
                    return false;
                }

                Plugin.Logger?.LogInfo($"Teleporting to zone '{zoneName}' at {spawnLocation.Value}");
                
                // TODO: Use TeleportService or PlayerService to teleport
                // PlayerService.SetPlayerPosition(characterEntity, spawnLocation.Value);
                
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error teleporting to zone: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get nearest zone to a position
        /// </summary>
        public static string? GetNearestZone(float3 position)
        {
            var enabledZones = ArenaZones.GetEnabledZones();
            string? nearestZone = null;
            float nearestDistance = float.MaxValue;

            foreach (var zone in enabledZones)
            {
                var zoneCenter = zone.GetSpawnPosition();
                var distance = math.distance(position, zoneCenter);
                
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestZone = zone.Name;
                }
            }

            return nearestZone;
        }

        /// <summary>
        /// Format zone info for display
        /// </summary>
        public static string FormatZoneInfo(string zoneName)
        {
            if (!ArenaZones.TryGetZone(zoneName, out var zone))
            {
                return $"Zone '{zoneName}' not found";
            }

            var pos = zone.GetSpawnPosition();
            return $"{zone.Name}: {zone.Description}\n" +
                   $"  Location: ({pos.x:F0}, {pos.y:F0}, {pos.z:F0})\n" +
                   $"  Radius: {zone.Radius:F0}m\n" +
                   $"  Status: {(zone.Enabled ? "Enabled" : "Disabled")}";
        }
    }
}
