using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using CrowbaneArena.Services;
using CrowbaneArena;

namespace CrowbaneArena.Patches;

[HarmonyPatch]
internal static class EquipmentPatches
{
    [HarmonyPatch(typeof(WeaponLevelSystem_Spawn), nameof(WeaponLevelSystem_Spawn.OnUpdate))]
    [HarmonyPostfix]
    static void WeaponLevelPatch(WeaponLevelSystem_Spawn __instance)
    {
        // Placeholder check since VRisingCore.hasInitialized equivalent needed
        if (!CrowbaneArenaCore.HasInitialized) return;

        NativeArray<Entity> entities = __instance.__query_1111682356_0.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (!CrowbaneArenaCore.EntityManager.HasComponent<EntityOwner>(entity)) continue;
                var entityOwner = CrowbaneArenaCore.EntityManager.GetComponentData<EntityOwner>(entity);
                if (!CrowbaneArenaCore.EntityManager.Exists(entityOwner.Owner)) continue;
                if (CrowbaneArenaCore.EntityManager.HasComponent<PlayerCharacter>(entityOwner.Owner))
                {
                    var playerCharacter = CrowbaneArenaCore.EntityManager.GetComponentData<PlayerCharacter>(entityOwner.Owner);
                    // Record level system not implemented - placeholder
                    Plugin.Logger?.LogInfo($"Weapon level update for player: {CrowbaneArenaCore.EntityManager.GetComponentData<User>(playerCharacter.UserEntity).CharacterName}");
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    [HarmonyPatch(typeof(BuffDebugSystem), nameof(BuffDebugSystem.OnUpdate))]
    [HarmonyPostfix]
    static void UserConnectionPatch(BuffDebugSystem __instance)
    {
        if (!CrowbaneArenaCore.HasInitialized) return;

        try
        {
            // Update PlayerService cache with current online users
            var userEntities = CrowbaneArenaCore.EntityManager.CreateEntityQuery(
                ComponentType.ReadOnly<User>()).ToEntityArray(Allocator.Temp);

            foreach (var userEntity in userEntities)
            {
                var userData = CrowbaneArenaCore.EntityManager.GetComponentData<User>(userEntity);

                // Only process connected users
                if (userData.IsConnected && !string.IsNullOrEmpty(userData.CharacterName.ToString()))
                {
                    var characterEntity = userData.LocalCharacter._Entity;

                    // Update the PlayerService cache
                    PlayerService.UpdatePlayerCache(userEntity, "", userData.CharacterName.ToString());
                }
            }

            userEntities.Dispose();
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error updating PlayerService cache: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(ArmorLevelSystem_Spawn), nameof(ArmorLevelSystem_Spawn.OnUpdate))]
    [HarmonyPostfix]
    static void ArmorLevelPatch(ArmorLevelSystem_Spawn __instance)
    {
        if (!CrowbaneArenaCore.HasInitialized) return;

        NativeArray<Entity> entities = __instance.__query_663986227_0.ToEntityArray(Allocator.Temp);

        try
        {
            foreach (Entity entity in entities)
            {
                if (!CrowbaneArenaCore.EntityManager.HasComponent<EntityOwner>(entity)) continue;
                var entityOwner = CrowbaneArenaCore.EntityManager.GetComponentData<EntityOwner>(entity);
                if (!CrowbaneArenaCore.EntityManager.Exists(entityOwner.Owner)) continue;
                if (CrowbaneArenaCore.EntityManager.HasComponent<PlayerCharacter>(entityOwner.Owner))
                {
                    var playerCharacter = CrowbaneArenaCore.EntityManager.GetComponentData<PlayerCharacter>(entityOwner.Owner);
                    // Record level system not implemented - placeholder
                    Plugin.Logger?.LogInfo($"Armor level update for player: {CrowbaneArenaCore.EntityManager.GetComponentData<User>(playerCharacter.UserEntity).CharacterName}");
                }
            }
        }
        finally
        {
            entities.Dispose();
        }
    }

    private static readonly HashSet<PrefabGUID> MagicSourceGuids = new HashSet<PrefabGUID>
    {
        // Using sample GUIDs - replace with actual values from the mod
        new PrefabGUID(0), // Item_EquipBuff_MagicSource_BloodKey_T01
        new PrefabGUID(0), // Item_EquipBuff_MagicSource_General
        // Add other magic source GUIDs as needed
    };

    [HarmonyPatch(typeof(BuffDebugSystem), nameof(BuffDebugSystem.OnUpdate))]
    [HarmonyPostfix]
    private static void BuffDebugPatch(BuffDebugSystem __instance)
    {
        if (!CrowbaneArenaCore.HasInitialized) return;

        // Simplified query - in original it used specific query
        var entities = CrowbaneArenaCore.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<PrefabGUID>()).ToEntityArray(Allocator.Temp);

        foreach (var entity in entities)
        {
            var guid = CrowbaneArenaCore.EntityManager.GetComponentData<PrefabGUID>(entity);

            if (MagicSourceGuids.Contains(guid))
            {
                // Record level system not implemented - placeholder
                Plugin.Logger?.LogInfo($"Magic source buff for entity: {entity}");
            }
        }

        entities.Dispose();
    }
}
