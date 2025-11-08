using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service for validating entities and their components.
    /// </summary>
    public static class EntityValidationService
    {
        /// <summary>
        /// Validates an entity and returns validation result.
        /// </summary>
        /// <param name="entity">Entity to validate</param>
        /// <returns>Validation result</returns>
        public static ValidationResult ValidateEntity(Entity entity)
        {
            var result = new ValidationResult
            {
                Entity = entity,
                ValidatedAt = DateTime.UtcNow
            };

            try
            {
                if (entity == Entity.Null)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Entity is null";
                    return result;
                }

                if (!CrowbaneArenaCore.EntityManager.Exists(entity))
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Entity does not exist";
                    return result;
                }

                result.IsValid = true;
                result.ErrorMessage = "Entity is valid";
                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Validation error: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Tries to get user entity from character entity.
        /// </summary>
        /// <param name="characterEntity">Character entity</param>
        /// <param name="userEntity">Output user entity</param>
        /// <returns>True if successful</returns>
        public static bool TryGetUserFromCharacter(Entity characterEntity, out Entity userEntity)
        {
            userEntity = Entity.Null;

            try
            {
                if (characterEntity == Entity.Null || !CrowbaneArenaCore.EntityManager.Exists(characterEntity))
                    return false;

                if (!CrowbaneArenaCore.EntityManager.HasComponent<PlayerCharacter>(characterEntity))
                    return false;

                var playerCharacter = CrowbaneArenaCore.EntityManager.GetComponentData<PlayerCharacter>(characterEntity);
                userEntity = playerCharacter.UserEntity;

                return userEntity != Entity.Null && CrowbaneArenaCore.EntityManager.Exists(userEntity);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to get user from character: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets entity position safely.
        /// </summary>
        /// <param name="entity">Entity to get position for</param>
        /// <returns>Entity position or zero if not found</returns>
        public static float3 GetEntityPosition(Entity entity)
        {
            try
            {
                if (entity == Entity.Null || !CrowbaneArenaCore.EntityManager.Exists(entity))
                    return float3.zero;

                if (CrowbaneArenaCore.EntityManager.HasComponent<Translation>(entity))
                {
                    return CrowbaneArenaCore.EntityManager.GetComponentData<Translation>(entity).Value;
                }

                return float3.zero;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to get entity position: {ex.Message}");
                return float3.zero;
            }
        }

        /// <summary>
        /// Sets entity position safely.
        /// </summary>
        /// <param name="entity">Entity to set position for</param>
        /// <param name="position">New position</param>
        /// <returns>True if successful</returns>
        public static bool SetEntityPosition(Entity entity, float3 position)
        {
            try
            {
                if (entity == Entity.Null || !CrowbaneArenaCore.EntityManager.Exists(entity))
                    return false;

                if (CrowbaneArenaCore.EntityManager.HasComponent<Translation>(entity))
                {
                    CrowbaneArenaCore.EntityManager.SetComponentData(entity, new Translation { Value = position });
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to set entity position: {ex.Message}");
                return false;
            }
        }
    }
}
