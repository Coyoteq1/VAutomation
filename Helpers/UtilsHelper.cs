using System;
using System.Reflection;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Il2CppInterop.Runtime;

namespace CrowbaneArena.Helpers
{

internal static class UtilsHelper
{
    public static bool TryGetPrefabGuid(string name, out PrefabGUID guid)
    {
        var temp = GetPrefabGuid(name);
        if (temp is not null)
        {
            guid = temp.Value;
            return true;
        }

        guid = default;
        return false;
    }

    public static PrefabGUID? GetPrefabGuid(string name)
    {
        // For now, return null - this needs to be implemented with proper prefab lookup
        // TODO: Implement proper prefab GUID lookup
        return null;
    }

    public static void CreateEventFromCharacter<T>(Entity character, T eventData) where T : struct
    {
        // TODO: Implement proper event creation - FromCharacter component needs to be defined
        var entity = VRisingCore.EntityManager.CreateEntity(
            ComponentType.ReadWrite<T>()
        );
        VRisingCore.EntityManager.SetComponentData(entity, eventData);
    }

    public static NativeArray<Entity> GetEntitiesByComponentType<T1>(
        bool includeAll = false,
        bool includeDisabled = false,
        bool includeSpawn = false,
        bool includePrefab = false,
        bool includeDestroyed = false)
    {
        var options = EntityQueryOptions.Default;
        if (includeAll) options |= EntityQueryOptions.IncludeAll;
        if (includeDisabled) options |= EntityQueryOptions.IncludeDisabled;
        if (includeSpawn) options |= EntityQueryOptions.IncludeSpawnTag;
        if (includePrefab) options |= EntityQueryOptions.IncludePrefab;
        if (includeDestroyed) options |= EntityQueryOptions.IncludeDestroyTag;

        var entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<T1>()
            .WithOptions(options);

        var query = VRisingCore.EntityManager.CreateEntityQuery(ref entityQueryBuilder);

        var entities = query.ToEntityArray(Allocator.Temp);
        return entities;
    }

    public static Entity GetUserEntity(Entity character)
    {
        var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(character);
        return playerCharacter.UserEntity;
    }
}
}
