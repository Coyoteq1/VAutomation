using ProjectM;
using ProjectM.Gameplay;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service for managing gear items, especially bloodbound items
    /// </summary>
    public static class GearService
    {
        private static EntityManager EM => VRisingCore.EntityManager;

        /// <summary>
        /// Make headgear items bloodbound or remove bloodbound status
        /// </summary>
        public static void SetHeadgearBloodbound(bool bloodBound)
        {
            try
            {
                // Get all headgear entities with equipment data
                var headgearQuery = EM.CreateEntityQuery(
                    ComponentType.ReadOnly<EquippableData>(),
                    ComponentType.ReadOnly<ItemData>(),
                    ComponentType.ReadOnly<PrefabGUID>());
                
                var headgearEntities = headgearQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                int processedCount = 0;

                foreach (var headgear in headgearEntities)
                {
                    if (!EM.TryGetComponentData(headgear, out EquippableData equipData)) continue;
                    if (!EM.TryGetComponentData(headgear, out ItemData itemData)) continue;
                    if (!EM.TryGetComponentData(headgear, out PrefabGUID prefabGUID)) continue;

                    // Special handling for specific headgear item
                    if (prefabGUID.GuidHash == -511360389)
                    {
                        itemData.ItemCategory |= ItemCategory.BloodBound;
                        EM.SetComponentData(headgear, itemData);
                    }

                    // Only process actual headgear items
                    if (equipData.EquipmentType != EquipmentType.Headgear) continue;
                    
                    if (bloodBound)
                        itemData.ItemCategory |= ItemCategory.BloodBound;
                    else
                        itemData.ItemCategory &= ~ItemCategory.BloodBound;
                    
                    EM.SetComponentData(headgear, itemData);
                    processedCount++;
                }

                headgearEntities.Dispose();
                Plugin.Logger?.LogInfo($"{(bloodBound ? "Made" : "Removed bloodbound from")} {processedCount} headgear items");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error setting headgear bloodbound status: {ex.Message}");
            }
        }

        /// <summary>
        /// Make equipment bloodbound for arena gameplay
        /// </summary>
        public static void SetEquipmentBloodbound(bool bloodBound)
        {
            try
            {
                // Get all equipment entities
                var equipmentQuery = EM.CreateEntityQuery(
                    ComponentType.ReadOnly<EquippableData>(),
                    ComponentType.ReadOnly<ItemData>(),
                    ComponentType.ReadOnly<PrefabGUID>());
                
                var equipmentEntities = equipmentQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                int processedCount = 0;

                foreach (var equipment in equipmentEntities)
                {
                    if (!EM.TryGetComponentData(equipment, out EquippableData equipData)) continue;
                    if (!EM.TryGetComponentData(equipment, out ItemData itemData)) continue;
                    if (!EM.TryGetComponentData(equipment, out PrefabGUID prefabGUID)) continue;

                    // Make all equipment bloodbound for arena
                    if (bloodBound)
                        itemData.ItemCategory |= ItemCategory.BloodBound;
                    else
                        itemData.ItemCategory &= ~ItemCategory.BloodBound;
                    
                    EM.SetComponentData(equipment, itemData);
                    processedCount++;
                }

                equipmentEntities.Dispose();
                Plugin.Logger?.LogInfo($"{(bloodBound ? "Made" : "Removed bloodbound from")} {processedCount} equipment items");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error setting equipment bloodbound status: {ex.Message}");
            }
        }
    }
}