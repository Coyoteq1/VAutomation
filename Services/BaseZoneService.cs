using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Unity.Mathematics;
using UnityEngine;
using BepInEx;
using CrowbaneArena.Utils;

namespace CrowbaneArena.Services
{
    abstract internal class BaseZoneService
    {
        protected static readonly string CONFIG_PATH = Path.Combine(BepInEx.Paths.ConfigPath, Plugin.PluginName);
        protected abstract string ZONES_PATH { get; }
        protected abstract string ZONE_SERVICE_NAME { get; }

        readonly List<Zone> zones;

        public struct Zone
        {
            public string Name { get; set; }
            public float2 Location { get; set; }
            public float Radius { get; set; }
            public bool Enabled { get; set; }
        }

        public BaseZoneService()
        {
            zones = GetDefaultZones();
            LoadZones();
        }

        protected virtual List<Zone> GetDefaultZones()
        {
            return [];
        }

        public IEnumerable<Zone> GetZones()
        {
            return zones;
        }

        void LoadZones()
        {
            if (File.Exists(ZONES_PATH))
            {
                var json = File.ReadAllText(ZONES_PATH);
                zones.Clear();
                zones.AddRange(JsonSerializer.Deserialize<Zone[]>(json, new JsonSerializerOptions { Converters = { new Float2JsonConverter() } }));
            }
            else
            {
                SaveZones();
            }
        }

        void SaveZones()
        {
            if (!Directory.Exists(CONFIG_PATH)) Directory.CreateDirectory(CONFIG_PATH);

            var options = new JsonSerializerOptions
            {
                Converters = { new Float2JsonConverter() },
                WriteIndented = true,
            };

            var json = JsonSerializer.Serialize(zones, options);
            File.WriteAllText(ZONES_PATH, json);
        }

        bool RetrieveZone(string name, out Zone zone, out int zoneIndex)
        {
            name = name.ToLowerInvariant();
            zone = zones.Find(z => z.Name.ToLowerInvariant() == name);
            zoneIndex = zones.IndexOf(zone);
            return zoneIndex != -1;
        }

        public bool CreateZone(string name, float2 location, float radius)
        {
            if (RetrieveZone(name, out var _, out var _)) return false;

            var zone = new Zone
            {
                Name = name,
                Location = location,
                Radius = radius,
                Enabled = true
            };
            zones.Add(zone);
            SaveZones();
            Plugin.Logger?.LogInfo($"{ZONE_SERVICE_NAME} zone '{name}' created at {location} with a radius of {radius}");
            return true;
        }

        public bool RemoveZone(string name)
        {
            var nameLower = name.ToLowerInvariant();
            int numRemoved = zones.RemoveAll(z => z.Name.ToLowerInvariant() == nameLower);
            if (numRemoved > 0)
            {
                Plugin.Logger?.LogInfo($"{ZONE_SERVICE_NAME} zone '{name}' removed");
            }
            SaveZones();
            return numRemoved > 0;
        }

        public bool ChangeZoneCenter(string name, float2 location)
        {
            if (!RetrieveZone(name, out var zone, out var zoneIndex)) return false;

            zone.Location = location;
            zones[zoneIndex] = zone;
            SaveZones();
            Plugin.Logger?.LogInfo($"{ZONE_SERVICE_NAME} zone '{name}' center changed to {location}");
            return true;
        }

        public bool ChangeZoneRadius(string name, float radius)
        {
            if (!RetrieveZone(name, out var zone, out var zoneIndex)) return false;

            zone.Radius = radius;
            zones[zoneIndex] = zone;
            SaveZones();
            Plugin.Logger?.LogInfo($"{ZONE_SERVICE_NAME} zone '{name}' radius changed to {radius}");
            return true;
        }

        public bool ChangeZoneRadius(string name, float2 location, out float newRadius)
        {
            if (!RetrieveZone(name, out var zone, out var zoneIndex))
            {
                newRadius = 0;
                return false;
            }

            zone.Radius = Vector2.Distance(location, zone.Location);
            zones[zoneIndex] = zone;
            SaveZones();
            newRadius = zone.Radius;
            Plugin.Logger?.LogInfo($"{ZONE_SERVICE_NAME} zone '{name}' radius changed to {newRadius}");
            return true;
        }

        public bool EnableZone(string name)
        {
            if (!RetrieveZone(name, out var zone, out var zoneIndex)) return false;

            zone.Enabled = true;
            zones[zoneIndex] = zone;
            SaveZones();
            Plugin.Logger?.LogInfo($"{ZONE_SERVICE_NAME} zone '{name}' enabled");
            return true;
        }

        public bool DisableZone(string name)
        {
            if (!RetrieveZone(name, out var zone, out var zoneIndex)) return false;

            zone.Enabled = false;
            zones[zoneIndex] = zone;
            SaveZones();
            Plugin.Logger?.LogInfo($"{ZONE_SERVICE_NAME} zone '{name}' disabled");
            return true;
        }

        public bool IsInZone(float2 pos)
        {
            foreach (var zone in zones)
            {
                if (zone.Enabled && Vector2.Distance(zone.Location, pos) < zone.Radius)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
