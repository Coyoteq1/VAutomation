using ProjectM;
using ProjectM.Terrain;
using Stunlock.Core;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CrowbaneArena.Services
{
    public static class ArenaPortalService
    {
        private static Entity portalPrefab = Entity.Null;
        private static Dictionary<Entity, (float3 pos, quaternion rot, TerrainChunk chunk, int index)> portalStartPos = new();

        public static void Initialize()
        {
            if (CrowbaneArenaCore.SystemService.PrefabCollectionSystem._SpawnableNameToPrefabGuidDictionary.TryGetValue("TM_General_Entrance_Gate", out var portalPrefabGUID))
            {
                CrowbaneArenaCore.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(portalPrefabGUID, out portalPrefab);
            }
        }

        public static bool StartPortal(Entity playerEntity)
        {
            var pos = VRisingCore.EntityManager.GetComponentData<Translation>(playerEntity).Value;
            var rot = VRisingCore.EntityManager.GetComponentData<Rotation>(playerEntity).Value;
            var chunk = new TerrainChunk { X = (sbyte)((pos.x + 3200) / 160), Y = (sbyte)((pos.z + 3200) / 160) };
            var index = GetNextAvailableIndex(chunk);

            if (index >= 9) return false;

            portalStartPos[playerEntity] = (pos, rot, chunk, index);
            return true;
        }

        public static string EndPortal(Entity playerEntity)
        {
            if (!portalStartPos.TryGetValue(playerEntity, out var start))
                return "Start portal hasn't been set. Use .portal start first";

            var endPos = VRisingCore.EntityManager.GetComponentData<Translation>(playerEntity).Value;
            var endRot = VRisingCore.EntityManager.GetComponentData<Rotation>(playerEntity).Value;
            var endChunk = new TerrainChunk { X = (sbyte)((endPos.x + 3200) / 160), Y = (sbyte)((endPos.z + 3200) / 160) };
            var endIndex = GetNextAvailableIndex(endChunk);

            if (endChunk.Equals(start.chunk)) endIndex += 1;
            if (endIndex >= 9) return "Can't have more than 9 portals in a chunk";

            CreatePortal(playerEntity, start.pos, start.rot, start.chunk, start.index, endChunk, endIndex);
            CreatePortal(playerEntity, endPos, endRot, endChunk, endIndex, start.chunk, start.index);

            portalStartPos.Remove(playerEntity);
            return null;
        }

        private static void CreatePortal(Entity creator, float3 pos, quaternion rot, TerrainChunk chunk, int index, TerrainChunk toChunk, int toIndex)
        {
            if (portalPrefab == Entity.Null) return;

            var portal = VRisingCore.EntityManager.Instantiate(portalPrefab);
            VRisingCore.EntityManager.SetComponentData(portal, new Translation { Value = pos });
            VRisingCore.EntityManager.SetComponentData(portal, new Rotation { Value = rot });
            VRisingCore.EntityManager.AddComponent<SpawnedBy>(portal);
            VRisingCore.EntityManager.SetComponentData(portal, new SpawnedBy { Value = creator });
            VRisingCore.EntityManager.AddComponent<ChunkPortal>(portal);
            VRisingCore.EntityManager.SetComponentData(portal, new ChunkPortal { FromChunk = chunk, FromChunkPortalIndex = index, ToChunk = toChunk, ToChunkPortalIndex = toIndex });
        }

        private static int GetNextAvailableIndex(TerrainChunk chunk)
        {
            // TODO: Implement proper chunk portal index management
            // if (!Core.ChunkObjectManager._ChunkPortals.TryGetValue(chunk, out var portalList))
            //     return 0;

            // for (var i = 0; i < portalList.Length; i++)
            // {
            //     if (portalList[i].PortalEntity == Entity.Null)
            //         return i;
            // }

            // return portalList.Length;
            
            // Temporary implementation: always return 0
            return 0;
        }
    }
}
