using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CrowbaneArena.Services
{
    public class PrefabRemapService
    {
        private static readonly string CONFIG_PATH = "config/crowbanearena";
        private static readonly string PREFAB_REMAP_PATH = Path.Combine(CONFIG_PATH, "prefabRemaps.txt");

        private readonly Dictionary<string, string> _remap = new Dictionary<string, string>();

        public PrefabRemapService()
        {
            LoadMappings();
        }

        public string GetPrefabMapping(string prefabName)
        {
            if (_remap.TryGetValue(prefabName, out var mapping))
                return mapping;
            return prefabName;
        }

        public void AddPrefabMapping(string prefabName, string mapping)
        {
            _remap[prefabName] = mapping;
            SaveMappings();
        }

        private void SaveMappings()
        {
            if (!Directory.Exists(CONFIG_PATH))
                Directory.CreateDirectory(CONFIG_PATH);
            var sb = new StringBuilder();
            foreach (var kvp in _remap)
            {
                sb.AppendLine($"{kvp.Key}={kvp.Value}");
            }
            File.WriteAllText(PREFAB_REMAP_PATH, sb.ToString());
        }

        private void LoadMappings()
        {
            if (!File.Exists(PREFAB_REMAP_PATH))
            {
                // Create a default file if it doesn't exist
                SaveMappings();
                return;
            }

            _remap.Clear();
            var lines = File.ReadAllLines(PREFAB_REMAP_PATH);
            foreach (var line in lines)
            {
                var parts = line.Split('=');
                if (parts.Length != 2) continue;
                _remap[parts[0].Trim()] = parts[1].Trim();
            }
        }
    }
}
