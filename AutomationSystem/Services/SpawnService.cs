using CrowbaneArena.Data;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CrowbaneArena.Services;

/// <summary>
///     Service for spawning items and entities
/// </summary>
public static class SpawnService
{
    private static EntityManager EM => CrowbaneArenaCore.EntityManager;

    /// <summary>
    ///     Spawn item at character location
    /// </summary>
    public static bool SpawnItem(Entity characterEntity, string itemName, int amount = 1)
    {
        if (Prefabs.TryGetAnyItem(itemName, out var guid, out var category))
        {
            var serverGameManager = VRisingCore.ServerGameManager;
            var response = serverGameManager.TryAddInventoryItem(characterEntity, guid, amount);

            if (response.NewEntity != Entity.Null)
            {
                Plugin.Logger?.LogInfo($"Spawned {amount}x {itemName} ({category})");
                return true;
            }
        }

        Plugin.Logger?.LogWarning($"Failed to spawn item: {itemName}");
        return false;
    }

    /// <summary>
    ///     Spawn weapon with specific name
    /// </summary>
    public static bool SpawnWeapon(Entity characterEntity, string weaponName)
    {
        if (Prefabs.TryGetWeapon(weaponName, out var guid))
        {
            var serverGameManager = VRisingCore.ServerGameManager;
            var response = serverGameManager.TryAddInventoryItem(characterEntity, guid, 1);
            return response.NewEntity != Entity.Null;
        }

        return false;
    }

    /// <summary>
    ///     Spawn complete armor set
    /// </summary>
    public static bool SpawnArmorSet(Entity characterEntity, string setName)
    {
        var armorSet = Prefabs.GetArmorSet(setName);
        if (armorSet.Count == 0) return false;

        var serverGameManager = VRisingCore.ServerGameManager;
        var spawnedCount = 0;

        foreach (var piece in armorSet)
        {
            var response = serverGameManager.TryAddInventoryItem(characterEntity, piece.Value, 1);
            if (response.NewEntity != Entity.Null) spawnedCount++;
        }

        Plugin.Logger?.LogInfo($"Spawned {spawnedCount}/{armorSet.Count} pieces of {setName} armor set");
        return spawnedCount > 0;
    }

    /// <summary>
    ///     Spawn consumable items
    /// </summary>
    public static bool SpawnConsumable(Entity characterEntity, string consumableName, int amount = 1)
    {
        if (Prefabs.TryGetConsumable(consumableName, out var guid))
        {
            var serverGameManager = VRisingCore.ServerGameManager;
            var response = serverGameManager.TryAddInventoryItem(characterEntity, guid, amount);
            return response.NewEntity != Entity.Null;
        }

        return false;
    }

    /// <summary>
    ///     Spawn bottoms/legs
    /// </summary>
    public static bool SpawnBottoms(Entity characterEntity, string bottomsName)
    {
        if (Prefabs.TryGetBottoms(bottomsName, out var guid))
        {
            var serverGameManager = VRisingCore.ServerGameManager;
            var response = serverGameManager.TryAddInventoryItem(characterEntity, guid, 1);
            return response.NewEntity != Entity.Null;
        }

        return false;
    }

    /// <summary>
    ///     Spawn cave entrance at character location
    /// </summary>
    public static bool SpawnCaveEntrance(Entity characterEntity)
    {
        var caveEntranceGuid = new PrefabGUID(1393214003); // AB_Interact_UseEntryway_Cave
        var serverGameManager = VRisingCore.ServerGameManager;

        // Get character position
        if (EM.HasComponent<Translation>(characterEntity))
        {
            var translation = EM.GetComponentData<Translation>(characterEntity);
            var position = translation.Value;

            // Spawn cave entrance at position
            var spawnedEntity = EM.CreateEntity();
            EM.AddComponentData(spawnedEntity, new PrefabGUID(caveEntranceGuid.GuidHash));
            EM.AddComponentData(spawnedEntity, new Translation { Value = position });

            Plugin.Logger?.LogInfo($"Spawned cave entrance at position: {position}");
            return true;
        }

        Plugin.Logger?.LogWarning("Failed to spawn cave entrance - no position found");
        return false;
    }

    /// <summary>
    ///     Spawn cave exit at character location
    /// </summary>
    public static bool SpawnCaveExit(Entity characterEntity)
    {
        var caveExitGuid = new PrefabGUID(83453710); // AB_Interact_UseEntryway_Cave_Travel
        var serverGameManager = VRisingCore.ServerGameManager;

        // Get character position
        if (EM.HasComponent<Translation>(characterEntity))
        {
            var translation = EM.GetComponentData<Translation>(characterEntity);
            var position = translation.Value;

            // Spawn cave exit at position
            var spawnedEntity = EM.CreateEntity();
            EM.AddComponentData(spawnedEntity, new PrefabGUID(caveExitGuid.GuidHash));
            EM.AddComponentData(spawnedEntity, new Translation { Value = position });

            Plugin.Logger?.LogInfo($"Spawned cave exit at position: {position}");
            return true;
        }

        Plugin.Logger?.LogWarning("Failed to spawn cave exit - no position found");
        return false;
    }

    /// <summary>
    ///     Spawn castle heart at character location
    /// </summary>
    public static bool SpawnCastle(Entity characterEntity)
    {
        var castleHeartGuid = new PrefabGUID(-1905691330); // TM_Castle_Heart

        // Get character position
        if (EM.HasComponent<Translation>(characterEntity))
        {
            var translation = EM.GetComponentData<Translation>(characterEntity);
            var position = translation.Value;

            // Spawn castle heart at position
            var spawnedEntity = EM.CreateEntity();
            EM.AddComponentData(spawnedEntity, new PrefabGUID(castleHeartGuid.GuidHash));
            EM.AddComponentData(spawnedEntity, new Translation { Value = position });

            Plugin.Logger?.LogInfo($"Spawned castle heart at position: {position}");
            return true;
        }

        Plugin.Logger?.LogWarning("Failed to spawn castle heart - no position found");
        return false;
    }

    /// <summary>
    ///     Remove fallen from spawn for Salarus
    /// </summary>
    public static void RemoveFallenFromSpawn()
    {
        try
        {
            var fallenPrefabGuid = new PrefabGUID(1106458752); // Nicholaus the Fallen
            var query = EM.CreateEntityQuery(ComponentType.ReadOnly<PrefabGUID>(),
                ComponentType.ReadOnly<Translation>());
            var entities = query.ToEntityArray(Allocator.Temp);
            var removedCount = 0;

            foreach (var entity in entities)
            {
                var prefabGuid = EM.GetComponentData<PrefabGUID>(entity);
                if (prefabGuid.GuidHash == fallenPrefabGuid.GuidHash)
                {
                    // Check if entity is near Salarus spawn
                    var position = EM.GetComponentData<Translation>(entity).Value;
                    var salarusSpawn = new float3(0, 0, 0); // Update with actual Salarus spawn coordinates
                    var distance = math.distance(position, salarusSpawn);

                    // Remove if within 30 units of Salarus spawn
                    if (distance < 30f)
                    {
                        EM.DestroyEntity(entity);
                        removedCount++;
                    }
                }
            }

            entities.Dispose();
            query.Dispose();
            Plugin.Logger?.LogInfo($"Removed {removedCount} Fallen entities near Salarus spawn");
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error removing Fallen from spawn: {ex}");
        }
    }
}