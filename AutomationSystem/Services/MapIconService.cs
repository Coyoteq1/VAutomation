using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CrowbaneArena.Services
{
    class MapIconService
    {
        PrefabGUID mapIconProxyPrefabGUID;
        Entity mapIconProxyPrefab;
        EntityQuery mapIconProxyQuery;

        public MapIconService()
        {
            if (!CrowbaneArenaCore.PrefabCollection._SpawnableNameToPrefabGuidDictionary.TryGetValue("MapIcon_ProxyObject_POI_Unknown", out mapIconProxyPrefabGUID))
                Plugin.Logger?.LogError("Failed to find MapIcon_ProxyObject_POI_Unknown PrefabGUID");
            if (!CrowbaneArenaCore.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(mapIconProxyPrefabGUID, out mapIconProxyPrefab))
                Plugin.Logger?.LogError("Failed to find MapIcon_ProxyObject_POI_Unknown Prefab entity");

            var queryBuilder = new EntityQueryBuilder(Allocator.Temp)
                .AddAll(ComponentType.ReadOnly<AttachMapIconsToEntity>())
                .AddAll(ComponentType.ReadOnly<SpawnedBy>())
                .AddNone(ComponentType.ReadOnly<ChunkPortal>())
                .AddNone(ComponentType.ReadOnly<ChunkWaypoint>())
                .WithOptions(EntityQueryOptions.IncludeDisabled);

            mapIconProxyQuery = CrowbaneArenaCore.EntityManager.CreateEntityQuery(ref queryBuilder);
            queryBuilder.Dispose();
        }

        public void CreateMapIcon(Entity characterEntity, PrefabGUID mapIcon)
        {
            var pos = characterEntity.Read<Translation>().Value;
            var mapIconProxy = CrowbaneArenaCore.EntityManager.Instantiate(mapIconProxyPrefab);
            mapIconProxy.Write(new Translation { Value = pos });

            mapIconProxy.Add<SpawnedBy>();
            mapIconProxy.Write(new SpawnedBy { Value = characterEntity });

            mapIconProxy.Remove<SyncToUserBitMask>();
            mapIconProxy.Remove<SyncToUserBuffer>();
            mapIconProxy.Remove<OnlySyncToUsersTag>();

            var attachMapIconsToEntity = CrowbaneArenaCore.EntityManager.GetBuffer<AttachMapIconsToEntity>(mapIconProxy);
            attachMapIconsToEntity.Clear();
            attachMapIconsToEntity.Add(new() { Prefab = mapIcon });
        }

        public bool RemoveMapIcon(Entity characterEntity)
        {
            const float DISTANCE_TO_DESTROY = 5f;
            var pos = characterEntity.Read<Translation>().Value;
            var mapIconProxies = mapIconProxyQuery.ToEntityArray(Allocator.Temp);
            var iconToDestroy = mapIconProxies.ToArray()
                .Where(x => x.Has<PrefabGUID>() && x.Read<PrefabGUID>().Equals(mapIconProxyPrefabGUID))
                .OrderBy(x => math.distance(pos, x.Read<Translation>().Value))
                .FirstOrDefault(x => math.distance(pos, x.Read<Translation>().Value) < DISTANCE_TO_DESTROY);
            mapIconProxies.Dispose();

            if (iconToDestroy == Entity.Null)
                return false;

            if (iconToDestroy.Has<AttachedBuffer>())
            {
                var attachedBuffer = CrowbaneArenaCore.EntityManager.GetBuffer<AttachedBuffer>(iconToDestroy);
                for (var i = 0; i < attachedBuffer.Length; i++)
                {
                    var attachedEntity = attachedBuffer[i].Entity;
                    if (attachedEntity == Entity.Null) continue;
                    CrowbaneArenaCore.EntityManager.DestroyEntity(attachedEntity);
                }
            }

            CrowbaneArenaCore.EntityManager.DestroyEntity(iconToDestroy);
            return true;
        }
    }
}
