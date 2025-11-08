using System;
using ProjectM.Network;
using Unity.Entities;
using CrowbaneArena.Models;

namespace CrowbaneArena.Converters
{
    /// <summary>
    /// Converts VRising User type to our PlayerData structure
    /// </summary>
    public static class UserConverter
    {
        /// <summary>
        /// Convert User to PlayerData (struct version from Models.cs)
        /// </summary>
        public static PlayerData ToPlayerData(User user, Entity characterEntity)
        {
            return new PlayerData(
                name: user.CharacterName.ToString(),
                steamId: user.PlatformId,
                isConnected: user.IsConnected,
                userEntity: Entity.Null, // Will be set by caller if needed
                characterEntity: characterEntity
            );
        }

        /// <summary>
        /// Convert User to PlayerData with user entity
        /// </summary>
        public static PlayerData ToPlayerData(User user, Entity userEntity, Entity characterEntity)
        {
            return new PlayerData(
                name: user.CharacterName.ToString(),
                steamId: user.PlatformId,
                isConnected: user.IsConnected,
                userEntity: userEntity,
                characterEntity: characterEntity
            );
        }

        /// <summary>
        /// Convert User to CrowbaneArena.Models.PlayerData (class version)
        /// </summary>
        public static CrowbaneArena.Models.PlayerData ToPlayerDataClass(User user, Entity userEntity, Entity characterEntity)
        {
            return new CrowbaneArena.Models.PlayerData(
                name: user.CharacterName.ToString(),
                steamId: user.PlatformId,
                isConnected: user.IsConnected,
                userEntity: userEntity,
                characterEntity: characterEntity
            );
        }

        /// <summary>
        /// Get character name from User
        /// </summary>
        public static string GetCharacterName(User user)
        {
            return user.CharacterName.ToString();
        }

        /// <summary>
        /// Get Steam ID from User
        /// </summary>
        public static ulong GetSteamId(User user)
        {
            return user.PlatformId;
        }

        /// <summary>
        /// Check if user is connected
        /// </summary>
        public static bool IsConnected(User user)
        {
            return user.IsConnected;
        }

        /// <summary>
        /// Check if user is admin
        /// </summary>
        public static bool IsAdmin(User user)
        {
            return user.IsAdmin;
        }
    }
}
