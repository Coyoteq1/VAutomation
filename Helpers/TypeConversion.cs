using Unity.Entities;
using Unity.Mathematics;
using ProjectM;
using ProjectM.Network;
using System;

namespace CrowbaneArena.Utils
{
    /// <summary>
    /// Provides utility methods for type conversion between different vector types and entity operations.
    /// </summary>
    public static class TypeConversion
    {
        /// <summary>
        /// Converts a UnityEngine.Vector3 to Unity.Mathematics.float3.
        /// </summary>
        /// <param name="vec">The source Vector3 to convert</param>
        /// <returns>A new float3 with the same component values</returns>
        public static float3 ToFloat3(UnityEngine.Vector3 vec) => new float3(vec.x, vec.y, vec.z);
        
        /// <summary>
        /// Converts a UnityEngine.Vector2 to Unity.Mathematics.float3 with an optional Y component.
        /// </summary>
        /// <param name="vec">The source Vector2 to convert (XZ plane)</param>
        /// <param name="y">The Y component value (default: 0)</param>
        /// <returns>A new float3 with X and Z from vec, and the specified Y value</returns>
        public static float3 ToFloat3(UnityEngine.Vector2 vec, float y = 0f) => new float3(vec.x, y, vec.y);
        
        /// <summary>
        /// Gets the position of a Transform as a float3.
        /// </summary>
        /// <param name="transform">The Transform to get position from</param>
        /// <returns>The position as float3, or float3.zero if transform is null</returns>
        public static float3 ToFloat3(UnityEngine.Transform transform) => 
            transform != null ? ToFloat3(transform.position) : float3.zero;
            
        /// <summary>
        /// Gets the user entity associated with a character entity.
        /// </summary>
        /// <param name="em">The EntityManager to use for the query</param>
        /// <param name="characterEntity">The character entity to look up</param>
        /// <returns>The associated user entity, or Entity.Null if not found</returns>
        /// <exception cref="ArgumentNullException">Thrown if em is null</exception>
        public static Entity GetUserEntity(EntityManager em, Entity characterEntity)
        {
            if (em == null)
                throw new ArgumentNullException(nameof(em));
                
            if (characterEntity == Entity.Null || !em.Exists(characterEntity))
                return Entity.Null;
                
            if (em.HasComponent<PlayerCharacter>(characterEntity))
            {
                var playerChar = em.GetComponentData<PlayerCharacter>(characterEntity);
                return playerChar.UserEntity;
            }
            
            return Entity.Null;
        }
        
        /// <summary>
        /// Gets the platform ID (Steam ID) for a user entity.
        /// </summary>
        /// <param name="em">The EntityManager to use for the query</param>
        /// <param name="userEntity">The user entity to look up</param>
        /// <returns>The platform ID, or 0 if not found</returns>
        /// <exception cref="ArgumentNullException">Thrown if em is null</exception>
        public static ulong GetPlatformId(EntityManager em, Entity userEntity)
        {
            if (em == null)
                throw new ArgumentNullException(nameof(em));
                
            if (userEntity == Entity.Null || !em.Exists(userEntity))
                return 0;
                
            if (em.HasComponent<User>(userEntity))
            {
                var user = em.GetComponentData<User>(userEntity);
                return user.PlatformId;
            }
            
            return 0;
        }
    }
}
