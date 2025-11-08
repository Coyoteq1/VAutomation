using System;
using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;

namespace CrowbaneArena.Services
{
    public static class PrefabResolverService
    {
        private static PrefabCollectionSystem _collectionSystem;
        private static readonly Dictionary<string, PrefabGUID> _allNameToGuid = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, PrefabGUID> _spawnableNameToGuid = new(StringComparer.OrdinalIgnoreCase);

        public static void Initialize()
        {
            _collectionSystem = VRisingCore.ServerWorld.GetExistingSystemManaged<PrefabCollectionSystem>();
            _allNameToGuid.Clear();
            _spawnableNameToGuid.Clear();

            foreach (var kv in _collectionSystem._PrefabGuidToEntityMap)
            {
                // TODO: Implement proper name lookup for PrefabGUID
                var name = kv.Key.GuidHash.ToString();
                if (!_allNameToGuid.ContainsKey(name))
                {
                    _allNameToGuid[name] = kv.Key;
                }
            }
            foreach (var kv in _collectionSystem.SpawnableNameToPrefabGuidDictionary)
            {
                if (!_spawnableNameToGuid.ContainsKey(kv.Key))
                {
                    _spawnableNameToGuid[kv.Key] = kv.Value;
                }
            }
            Plugin.Logger?.LogInfo($"PrefabResolver initialized: all={_allNameToGuid.Count}, spawnable={_spawnableNameToGuid.Count}");
        }

        public static bool TryGetItem(string key, out PrefabGUID guid)
        {
            if (string.IsNullOrWhiteSpace(key)) { guid = default; return false; }
            var k = key.Trim().ToLowerInvariant();
            if (_spawnableNameToGuid.TryGetValue(k, out guid)) return true;
            if (_spawnableNameToGuid.TryGetValue($"item_{k}", out guid)) return true;
            return false;
        }

        public static bool TryGetAny(string key, out PrefabGUID guid)
        {
            if (string.IsNullOrWhiteSpace(key)) { guid = default; return false; }
            var k = key.Trim().ToLowerInvariant();
            if (_allNameToGuid.TryGetValue(k, out guid)) return true;
            return false;
        }
    }
}
