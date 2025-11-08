using System;
using ProjectM;
using Unity.Entities;
using Stunlock.Core;

namespace CrowbaneArena
{
    /// <summary>
    /// UI hook for adding merchant buttons to prefabs
    /// </summary>
    public static class UIHook
    {
        private static readonly PrefabGUID TargetPrefab = new PrefabGUID(336743839);
        private static readonly int[] InputItems = { -182923609, -1629804427, 1334469825, 1488205677, -77477508, -77477508, -77477508, -77477508, -77477508, -77477508, -77477508, -77477508, -77477508, -77477508, -77477508, -77477508 };
        
        /// <summary>
        /// Add merchant buttons to the target prefab
        /// </summary>
        public static void AddMerchantButtons()
        {
            try
            {
                var em = VRisingCore.EntityManager;
                var entities = em.CreateEntityQuery(ComponentType.ReadOnly<PrefabGUID>()).ToEntityArray(Unity.Collections.Allocator.Temp);
                
                foreach (var entity in entities)
                {
                    if (em.TryGetComponentData(entity, out PrefabGUID prefab) && prefab.Equals(TargetPrefab))
                    {
                        // Add merchant interaction component
                        if (!em.HasComponent<Interactable>(entity))
                        {
                            em.AddComponent<Interactable>(entity);
                        }
                        
                        Plugin.Logger?.LogInfo($"Added merchant buttons to prefab {TargetPrefab.GuidHash}");
                    }
                }
                
                entities.Dispose();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error adding merchant buttons: {ex.Message}");
            }
        }
    }
}
