using System;
using System.Collections.Generic;
using Stunlock.Core;
using Newtonsoft.Json;

namespace CrowbaneArena.Data
{
    /// <summary>
    /// Comprehensive prefab mapping system for CrowbaneArena
    /// </summary>
    public static class PrefabMappings
    {
        private static Dictionary<string, string> _prefabMap;
        private static Dictionary<string, PrefabGUID> _bossMap;
        private static Dictionary<string, PrefabGUID> _itemMap;

        static PrefabMappings()
        {
            InitializeMappings();
        }

        private static void InitializeMappings()
        {
            _prefabMap = new Dictionary<string, string>
            {
                ["1059128"] = "97246fbe-4a67-40a9-bf33-df3caf89c686",
                ["3759455"] = "1f6446ab-8f09-417d-83b5-9aa2dff92a19",
                ["4267150"] = "5094cbff-f80c-4605-8264-fce8d4c80c93",
                ["4777796"] = "a7e02b2f-4171-4257-9148-32387cbff763",
                ["5164221"] = "5a4b424f-3775-4322-84a9-a6bfa0804761",
                ["6059030"] = "819491b8-fb45-4a33-82c4-1b1395818e40",
                ["6233001"] = "c313423f-c120-4970-b079-675be6026690",
                ["6711686"] = "2137733c-02af-41c9-8f80-3226422a5fe5",
                ["7594625"] = "f9dc7770-8254-41f6-9ef4-02db6801f391",
                ["8807466"] = "ed7735e1-218f-4e19-8048-adac907e1e8f"
            };

            _bossMap = new Dictionary<string, PrefabGUID>
            {
                // Major VBlood bosses
                ["alphawolf"] = new PrefabGUID(-1905691330),
                ["keely"] = new PrefabGUID(-1342764880),
                ["rufus"] = new PrefabGUID(1699865363),
                ["errol"] = new PrefabGUID(-2025101517),
                ["lidia"] = new PrefabGUID(1362041468),
                ["jade"] = new PrefabGUID(-1065970933),
                ["putridrat"] = new PrefabGUID(435934037),
                ["goreswine"] = new PrefabGUID(-1208888966),
                ["clive"] = new PrefabGUID(1124739990),
                ["polora"] = new PrefabGUID(2054432370),
                ["bear"] = new PrefabGUID(-1449631170),
                ["nicholaus"] = new PrefabGUID(1106458752),
                ["quincey"] = new PrefabGUID(-1347412392),
                ["vincent"] = new PrefabGUID(1896428751),
                ["christina"] = new PrefabGUID(-484556888),
                ["tristan"] = new PrefabGUID(2089106511),
                ["wingedhorror"] = new PrefabGUID(-2137261854),
                ["ungora"] = new PrefabGUID(1233988687),
                ["terrorclaw"] = new PrefabGUID(-1391546313),
                ["willfred"] = new PrefabGUID(-680831417),
                ["octavian"] = new PrefabGUID(114912615),
                ["solarus"] = new PrefabGUID(-1659822956),
                
                // Extended boss definitions
                ["batboss"] = new PrefabGUID(-1905691330),
                ["frostboss"] = new PrefabGUID(-1342764880),
                ["stoneboss"] = new PrefabGUID(-2025101517)
            };

            _itemMap = new Dictionary<string, PrefabGUID>
            {
                // Furniture - Aquariums
                ["largeaquarium"] = new PrefabGUID(198047828),
                ["smallaquarium"] = new PrefabGUID(2117907654),
                
                // Furniture - Musical Instruments
                ["piano1"] = new PrefabGUID(-409684408),
                ["piano2"] = new PrefabGUID(-280549553),
                ["harp1"] = new PrefabGUID(1246795581),
                ["harp2"] = new PrefabGUID(591881817),
                ["organ1"] = new PrefabGUID(-1894293910),
                ["organ2"] = new PrefabGUID(-544504736)
            };
            
            // Add prefab mappings from JSON
            foreach (var kvp in _prefabMap)
            {
                if (Guid.TryParse(kvp.Value, out var guid))
                {
                    _itemMap[kvp.Key] = new PrefabGUID(int.Parse(kvp.Key));
                }
            }
        }

        public static bool TryGetBoss(string name, out PrefabGUID guid) =>
            _bossMap.TryGetValue(name.ToLowerInvariant(), out guid);

        public static bool TryGetItem(string id, out PrefabGUID guid) =>
            _itemMap.TryGetValue(id, out guid);

        public static PrefabGUID GetBossGUID(string name) =>
            _bossMap.TryGetValue(name.ToLowerInvariant(), out var guid) ? guid : PrefabGUID.Empty;

        public static IEnumerable<string> GetAllBossNames() => _bossMap.Keys;
        public static IEnumerable<string> GetAllItemIds() => _itemMap.Keys;
        
        public static bool TryGetFurniture(string name, out PrefabGUID guid)
        {
            var furnitureItems = new Dictionary<string, PrefabGUID>
            {
                ["largeaquarium"] = new PrefabGUID(198047828),
                ["smallaquarium"] = new PrefabGUID(2117907654),
                ["piano1"] = new PrefabGUID(-409684408),
                ["piano2"] = new PrefabGUID(-280549553),
                ["harp1"] = new PrefabGUID(1246795581),
                ["harp2"] = new PrefabGUID(591881817),
                ["organ1"] = new PrefabGUID(-1894293910),
                ["organ2"] = new PrefabGUID(-544504736)
            };
            return furnitureItems.TryGetValue(name.ToLowerInvariant(), out guid);
        }

        /// <summary>
        /// Gets a read-only dictionary of all boss names to their PrefabGUIDs
        /// </summary>
        /// <returns>Dictionary mapping boss names to PrefabGUIDs</returns>
        public static IReadOnlyDictionary<string, PrefabGUID> GetBossMap()
        {
            return _bossMap;
        }
    }
}
