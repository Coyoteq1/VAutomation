using ProjectM;
using ProjectM.Terrain;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using CrowbaneArena.Systems;
using UnityEngine;

namespace CrowbaneArena.Services
{
    public class ArenaPortalManager
    {
        private const float PORTAL_RADIUS = 2f;
        private const float BLOOD_ESSENCE_GRANT = 25f; // Blood essence granted when using portal
        private const string ENTER_PORTAL_PREFAB = "TM_General_Entrance_Gate";
        private const string EXIT_PORTAL_PREFAB = "TM_General_Exit_Gate";

        private Entity enterPortalPrefab;
        private Entity exitPortalPrefab;
        private EntityQuery portalQuery;
        private Dictionary<Entity, PortalData> activePortals = new();

        public ArenaPortalManager()
        {
            InitializePortalPrefabs();
            InitializePortalQuery();
        }

        private void InitializePortalPrefabs()
        {
            if (!CrowbaneArenaCore.PrefabCollection._SpawnableNameToPrefabGuidDictionary.TryGetValue(ENTER_PORTAL_PREFAB, out var enterGuid) ||
                !CrowbaneArenaCore.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(enterGuid, out enterPortalPrefab))
            {
                Plugin.Logger?.LogError($"Failed to find {ENTER_PORTAL_PREFAB} prefab");
            }

            if (!CrowbaneArenaCore.PrefabCollection._SpawnableNameToPrefabGuidDictionary.TryGetValue(EXIT_PORTAL_PREFAB, out var exitGuid) ||
                !CrowbaneArenaCore.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(exitGuid, out exitPortalPrefab))
            {
                Plugin.Logger?.LogError($"Failed to find {EXIT_PORTAL_PREFAB} prefab");
            }
        }

        private void InitializePortalQuery()
        {
            // Use CreateEntityQuery directly like in patches
            portalQuery = CrowbaneArenaCore.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<ChunkPortal>(),
                ComponentType.ReadOnly<Translation>(),
                ComponentType.ReadOnly<Rotation>());
        }

        public bool SpawnEnterPortal(Entity playerEntity, out string error)
        {
            return SpawnPortal(playerEntity, true, out error);
        }

        public bool SpawnExitPortal(Entity playerEntity, out string error)
        {
            return SpawnPortal(playerEntity, false, out error);
        }

        private bool SpawnPortal(Entity playerEntity, bool isEnterPortal, out string error)
        {
            error = null;
            
            if (!playerEntity.Has<Translation>() || !playerEntity.Has<Rotation>())
            {
                error = "Invalid player position or rotation";
                return false;
            }

            var position = playerEntity.Read<Translation>().Value;
            var rotation = playerEntity.Read<Rotation>().Value;
            var prefab = isEnterPortal ? enterPortalPrefab : exitPortalPrefab;

            if (prefab == Entity.Null)
            {
                error = "Failed to load portal prefab";
                return false;
            }

            var portalEntity = CrowbaneArenaCore.EntityManager.Instantiate(prefab);
            CrowbaneArenaCore.EntityManager.SetComponentData(portalEntity, new Translation { Value = position });
            CrowbaneArenaCore.EntityManager.SetComponentData(portalEntity, new Rotation { Value = rotation });

            activePortals[portalEntity] = new PortalData
            {
                Position = position,
                IsEnterPortal = isEnterPortal,
                SpawnTime = DateTime.UtcNow
            };

            return true;
        }

        public bool RemoveNearestPortal(Entity playerEntity, out string error)
        {
            error = null;
            
            if (!playerEntity.Has<Translation>())
            {
                error = "Invalid player position";
                return false;
            }

            var playerPos = playerEntity.Read<Translation>().Value;
            Entity? nearestPortal = null;
            float nearestDistance = float.MaxValue;

            foreach (var portal in activePortals.Keys)
            {
                if (!portal.Has<Translation>()) continue;
                
                var portalPos = portal.Read<Translation>().Value;
                var distance = math.distance(playerPos, portalPos);
                
                if (distance < nearestDistance && distance <= 10f) // 10m max distance
                {
                    nearestPortal = portal;
                    nearestDistance = distance;
                }
            }

            if (nearestPortal.HasValue)
            {
                CrowbaneArenaCore.EntityManager.DestroyEntity(nearestPortal.Value);
                activePortals.Remove(nearestPortal.Value);
                return true;
            }

            error = "No portal found nearby";
            return false;
        }

        public void Update()
        {
            // Portal update system - bloodcraft integration
            // Note: Proximity detection requires additional player query setup
            // Blood essence is granted via manual portal interaction commands
            // Auto-proximity detection can be added when player spatial query is available
        }
        
        /// <summary>
        /// Grants blood essence to a character using BloodCraftSystem mechanics
        /// </summary>
        private void GrantBloodEssence(Entity character, float amount)
        {
            var entityManager = CrowbaneArenaCore.EntityManager;
            
            if (!entityManager.HasComponent<BloodEssence>(character))
            {
                entityManager.AddComponentData(character, new BloodEssence { Value = amount });
            }
            else
            {
                var bloodEssence = entityManager.GetComponentData<BloodEssence>(character);
                bloodEssence.Value = Mathf.Min(bloodEssence.Value + amount, 100f); // Cap at 100
                entityManager.SetComponentData(character, bloodEssence);
            }
            
            Debug.Log($"[ArenaPortalManager] Granted {amount} blood essence to character");
        }
        
        /// <summary>
        /// Manual blood essence grant for portal usage (called from commands)
        /// </summary>
        public void GrantBloodEssenceToPlayer(Entity character, bool isEnterPortal)
        {
            if (isEnterPortal)
            {
                GrantBloodEssence(character, BLOOD_ESSENCE_GRANT);
            }
            else
            {
                // Exit portal - grant extra essence as reward
                GrantBloodEssence(character, BLOOD_ESSENCE_GRANT * 1.5f);
            }
        }

        private struct PortalData
        {
            public float3 Position;
            public bool IsEnterPortal;
            public DateTime SpawnTime;
        }
    }
}
