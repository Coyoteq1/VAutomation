using Unity.Entities;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System;
using System.Collections.Generic;

namespace CrowbaneArena.Services
{
    public static class ItemSpawnService
    {
        private static EntityManager EM => CrowbaneArenaCore.EntityManager;

        public static bool SpawnItem(Entity characterEntity, PrefabGUID itemGuid, int quantity = 1)
        {
            try
            {
                if (!EM.Exists(characterEntity)) return false;

                var sgm = VRisingCore.ServerGameManager;
                var response = sgm.TryAddInventoryItem(characterEntity, itemGuid, quantity);
                return response.NewEntity != Entity.Null;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error spawning item: {ex.Message}");
                return false;
            }
        }

        public static bool SpawnItemByName(Entity characterEntity, string itemName, int quantity = 1)
        {
            if (Data.Prefabs.TryGetAnyItem(itemName, out var guid, out _))
            {
                return SpawnItem(characterEntity, guid, quantity);
            }
            return false;
        }

        public static int SpawnItemBatch(Entity characterEntity, Dictionary<PrefabGUID, int> items)
        {
            int successCount = 0;
            foreach (var item in items)
            {
                if (SpawnItem(characterEntity, item.Key, item.Value))
                    successCount++;
            }
            return successCount;
        }

        public static bool HasInventorySpace(Entity characterEntity)
        {
            try
            {
                if (!EM.Exists(characterEntity)) return false;
                if (!ProjectM.InventoryUtilities.TryGetInventoryEntity(EM, characterEntity, out var inventoryEntity)) return false;
                if (!EM.HasBuffer<InventoryBuffer>(inventoryEntity)) return false;

                var buffer = EM.GetBuffer<InventoryBuffer>(inventoryEntity);
                return buffer.Length < buffer.Capacity;
            }
            catch
            {
                return false;
            }
        }
    }
}
