using ProjectM;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace CrowbaneArena.Services
{
    public static class InventoryHelper
    {
        private static EntityManager EM => CrowbaneArenaCore.EntityManager;

        public static void ClearAll(Entity characterEntity)
        {
            if (InventoryUtilities.TryGetInventoryEntity(EM, characterEntity, out var invEntity))
            {
                var buffer = EM.GetBuffer<InventoryBuffer>(invEntity);
                for (int i = 0; i < buffer.Length; i++)
                    if (buffer[i].ItemType.GuidHash != 0)
                        VRisingCore.ServerGameManager.TryRemoveInventoryItem(characterEntity, buffer[i].ItemType, buffer[i].Amount);
            }

            if (EM.TryGetComponentData(characterEntity, out Equipment equipment))
            {
                var equipped = new NativeList<Entity>(Allocator.Temp);
                equipment.GetAllEquipmentEntities(equipped);
                foreach (var e in equipped)
                    if (e != Entity.Null && EM.Exists(e) && EM.TryGetComponentData(e, out PrefabGUID guid))
                        VRisingCore.ServerGameManager.TryRemoveInventoryItem(characterEntity, guid, 1);
                equipped.Dispose();
            }
        }
    }
}
