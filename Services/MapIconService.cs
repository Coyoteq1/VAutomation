using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CrowbaneArena.Services;

public static class MapIconService
{
    static PrefabGUID mapIconProxyPrefabGUID;
    static Entity mapIconProxyPrefab;
    static EntityQuery mapIconProxyQuery;
    static bool initialized = false;

    static void Initialize()
    {
        if (initialized) return;

        if (!CrowbaneArenaCore.SystemService.PrefabCollectionSystem._SpawnableNameToPrefabGuidDictionary.TryGetValue("MapIcon_ProxyObject_POI_Unknown", out mapIconProxyPrefabGUID))
        {
            Plugin.Logger?.LogError("Failed to find MapIcon_ProxyObject_POI_Unknown PrefabGUID");
            return;
        }
        if (!CrowbaneArenaCore.SystemService.PrefabCollectionSystem._PrefabGuidToEntityMap.TryGetValue(mapIconProxyPrefabGUID, out mapIconProxyPrefab))
        {
            Plugin.Logger?.LogError("Failed to find MapIcon_ProxyObject_POI_Unknown Prefab entity");
            return;
        }

        var queryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .AddAll(ComponentType.ReadOnly<AttachMapIconsToEntity>())
            .AddAll(ComponentType.ReadOnly<SpawnedBy>())
            .AddNone(ComponentType.ReadOnly<ChunkPortal>())
            .AddNone(ComponentType.ReadOnly<ChunkWaypoint>())
            .WithOptions(EntityQueryOptions.IncludeDisabled);

        mapIconProxyQuery = VRisingCore.EntityManager.CreateEntityQuery(ref queryBuilder);
        queryBuilder.Dispose();
        initialized = true;
    }

    public static bool CreateMapIcon(Entity characterEntity, string prefabName)
    {
        Initialize();
        if (!CrowbaneArenaCore.SystemService.PrefabCollectionSystem._SpawnableNameToPrefabGuidDictionary.TryGetValue(prefabName, out var mapIcon))
            return false;

        var pos = VRisingCore.EntityManager.GetComponentData<Translation>(characterEntity).Value;
        var mapIconProxy = VRisingCore.EntityManager.Instantiate(mapIconProxyPrefab);
        VRisingCore.EntityManager.SetComponentData(mapIconProxy, new Translation { Value = pos });

        VRisingCore.EntityManager.AddComponent<SpawnedBy>(mapIconProxy);
        VRisingCore.EntityManager.SetComponentData(mapIconProxy, new SpawnedBy { Value = characterEntity });

        VRisingCore.EntityManager.RemoveComponent<SyncToUserBitMask>(mapIconProxy);
        VRisingCore.EntityManager.RemoveComponent<SyncToUserBuffer>(mapIconProxy);
        VRisingCore.EntityManager.RemoveComponent<OnlySyncToUsersTag>(mapIconProxy);

        var attachMapIconsToEntity = VRisingCore.EntityManager.GetBuffer<AttachMapIconsToEntity>(mapIconProxy);
        attachMapIconsToEntity.Clear();
        attachMapIconsToEntity.Add(new() { Prefab = mapIcon });

        return true;
    }

    public static bool RemoveMapIcon(Entity characterEntity)
    {
        Initialize();
        const float DISTANCE_TO_DESTROY = 5f;
        var pos = VRisingCore.EntityManager.GetComponentData<Translation>(characterEntity).Value;
        var mapIconProxies = mapIconProxyQuery.ToEntityArray(Allocator.Temp);
        var iconToDestroy = mapIconProxies.ToArray()
            .Where(x => VRisingCore.EntityManager.HasComponent<PrefabGUID>(x) && VRisingCore.EntityManager.GetComponentData<PrefabGUID>(x).Equals(mapIconProxyPrefabGUID))
            .OrderBy(x => math.distance(pos, VRisingCore.EntityManager.GetComponentData<Translation>(x).Value))
            .FirstOrDefault(x => math.distance(pos, VRisingCore.EntityManager.GetComponentData<Translation>(x).Value) < DISTANCE_TO_DESTROY);
        mapIconProxies.Dispose();

        if (iconToDestroy == Entity.Null)
            return false;

        if (VRisingCore.EntityManager.HasComponent<AttachedBuffer>(iconToDestroy))
        {
            var attachedBuffer = VRisingCore.EntityManager.GetBuffer<AttachedBuffer>(iconToDestroy);
            for (var i = 0; i < attachedBuffer.Length; i++)
            {
                var attachedEntity = attachedBuffer[i].Entity;
                if (attachedEntity == Entity.Null) continue;
                VRisingCore.EntityManager.DestroyEntity(attachedEntity);
            }
        }

        VRisingCore.EntityManager.DestroyEntity(iconToDestroy);
        return true;
    }

    public static List<string> GetAvailableIcons()
    {
        var icons = new List<string>();
        foreach (var entry in CrowbaneArenaCore.SystemService.PrefabCollectionSystem._SpawnableNameToPrefabGuidDictionary)
        {
            if (entry.Key.StartsWith("MapIcon_"))
            {
                icons.Add(entry.Key);
            }
        }
        return icons.OrderBy(x => x).ToList();
    }
}
