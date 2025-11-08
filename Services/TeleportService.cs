using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using System;
using System.Linq;
using ProjectM;
using Stunlock.Core;
using ProjectM.Network;

namespace CrowbaneArena.Services;

/// <summary>
/// Service for teleporting players
/// </summary>
public static class TeleportService
{
    private static EntityManager EM => CrowbaneArenaCore.EntityManager;

    /// <summary>
    /// Teleport a character to a specific position
    /// </summary>
    public static bool Teleport(Entity characterEntity, float3 position, bool checkRestrictions = true)
    {
        try
        {
            if (characterEntity == Entity.Null || !EM.Exists(characterEntity))
            {
                Plugin.Logger?.LogWarning("Teleport: Invalid character entity");
                return false;
            }

            // Check if player is in a restricted zone if needed
            if (checkRestrictions && IsRestrictedPosition(position))
            {
                Plugin.Logger?.LogWarning("Cannot teleport to restricted position");
                return false;
            }

            // Update position components
            if (EM.HasComponent<Translation>(characterEntity))
            {
                // Update Translation
                var translation = EM.GetComponentData<Translation>(characterEntity);
                translation.Value = position;
                EM.SetComponentData(characterEntity, translation);

                // Update LastTranslation if it exists
                if (EM.HasComponent<LastTranslation>(characterEntity))
                {
                    var lastTranslation = EM.GetComponentData<LastTranslation>(characterEntity);
                    lastTranslation.Value = position;
                    EM.SetComponentData(characterEntity, lastTranslation);
                }

                // Force update position by toggling character state
                if (EM.HasComponent<LocalToWorld>(characterEntity))
                {
                    // This is a workaround to force the transform update
                    var localToWorld = EM.GetComponentData<LocalToWorld>(characterEntity);
                    EM.SetComponentData(characterEntity, localToWorld);
                }
            }

            Plugin.Logger?.LogInfo($"Teleported character to: {position}");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error teleporting character: {ex.Message}\n{ex.StackTrace}");
            return false;
        }
    }
    

    /// <summary>
    /// Check if a position is in a restricted zone
    /// </summary>
    private static bool IsRestrictedPosition(float3 position)
    {
        // Add your restricted zone checks here if needed
        // Example: return position.x < 0 || position.z < 0; // Restrict negative coordinates
        return false;
    }

    /// <summary>
    /// Teleport a user (finds character entity automatically)
    /// </summary>
    public static bool TeleportUser(Entity userEntity, float3 position)
    {
        var characterEntity = PlayerService.GetPlayerCharacter(userEntity);
        if (characterEntity == Entity.Null)
        {
            Plugin.Logger?.LogWarning("TeleportUser: Could not find character entity");
            return false;
        }

        return Teleport(characterEntity, position);
    }

    /// <summary>
    /// Get current position of a character
    /// </summary>
    public static float3 GetPosition(Entity characterEntity)
    {
        return PlayerService.GetPlayerPosition(characterEntity);
    }

    /// <summary>
    /// Teleport character to another character
    /// </summary>
    public static bool TeleportToPlayer(Entity sourceCharacter, Entity targetCharacter)
    {
        var targetPosition = GetPosition(targetCharacter);
        if (targetPosition.Equals(float3.zero))
        {
            Plugin.Logger?.LogWarning("TeleportToPlayer: Could not get target position");
            return false;
        }

        return Teleport(sourceCharacter, targetPosition);
    }

    /// <summary>
    /// Teleport character to user (finds character's position automatically)
    /// </summary>
    public static bool TeleportToUser(Entity sourceCharacter, Entity targetUser, bool checkRestrictions = true)
    {
        var targetCharacter = PlayerService.GetPlayerCharacter(targetUser);
        if (targetCharacter == Entity.Null)
        {
            Plugin.Logger?.LogWarning("TeleportToUser: Could not find target character entity");
            return false;
        }

        var targetPosition = GetPosition(targetCharacter);
        return Teleport(sourceCharacter, targetPosition, checkRestrictions);
    }

    /// <summary>
    /// Teleport to arena spawn point from config
    /// </summary>
    public static bool TeleportToArena(Entity characterEntity, bool checkRestrictions = true)
    {
        var spawnPoint = GetArenaSpawnFromConfig();
        if (spawnPoint.x == 0 && spawnPoint.y == 0 && spawnPoint.z == 0)
        {
            Plugin.Logger?.LogWarning("Arena spawn point not configured");
            return false;
        }
        return Teleport(characterEntity, spawnPoint, checkRestrictions);
    }

    /// <summary>
    /// Get arena spawn point from configuration
    /// </summary>
    private static float3 GetArenaSpawnFromConfig()
    {
        if (ArenaConfigurationService.ArenaSettings?.Zones != null && ArenaConfigurationService.ArenaSettings.Zones.Count > 0)
        {
            var enabledZone = ArenaConfigurationService.ArenaSettings.Zones.FirstOrDefault(z => z.Enabled);
            if (enabledZone != null)
            {
                return new float3(enabledZone.SpawnX, enabledZone.SpawnY, enabledZone.SpawnZ);
            }
        }
        return float3.zero;
    }

    /// <summary>
    /// Teleport user to arena spawn point
    /// </summary>
    public static bool TeleportUserToArena(Entity userEntity, bool checkRestrictions = true)
    {
        var characterEntity = PlayerService.GetPlayerCharacter(userEntity);
        if (characterEntity == Entity.Null)
        {
            Plugin.Logger?.LogWarning("TeleportUserToArena: Could not find character entity");
            return false;
        }
        return TeleportToArena(characterEntity, checkRestrictions);
    }

    /// <summary>
    /// Teleport user to user (both find character entities automatically)
    /// </summary>
    public static bool TeleportUserToUser(Entity sourceUser, Entity targetUser)
    {
        var sourceCharacter = PlayerService.GetPlayerCharacter(sourceUser);
        if (sourceCharacter == Entity.Null)
        {
            Plugin.Logger?.LogWarning("TeleportUserToUser: Could not find source character entity");
            return false;
        }

        return TeleportToUser(sourceCharacter, targetUser);
    }
}
