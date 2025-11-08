using System;
using VampireCommandFramework;
using ProjectM.Network;
using Unity.Entities;
using CrowbaneArena.Models;

namespace CrowbaneArena.Converters
{
    /// <summary>
    /// Converts VampireCommandFramework ICommandContext to our data structures
    /// </summary>
    public static class CommandContextConverter
    {
        /// <summary>
        /// Get User from ChatCommandContext
        /// </summary>
        public static User GetUser(ChatCommandContext ctx)
        {
            return ctx.Event.User;
        }

        /// <summary>
        /// Get character entity from ChatCommandContext
        /// </summary>
        public static Entity GetCharacterEntity(ChatCommandContext ctx)
        {
            return ctx.Event.SenderCharacterEntity;
        }

        /// <summary>
        /// Get user entity from ChatCommandContext
        /// </summary>
        public static Entity GetUserEntity(ChatCommandContext ctx)
        {
            return ctx.Event.SenderUserEntity;
        }

        /// <summary>
        /// Convert ChatCommandContext to PlayerData (struct)
        /// </summary>
        public static PlayerData ToPlayerData(ChatCommandContext ctx)
        {
            var user = GetUser(ctx);
            var characterEntity = GetCharacterEntity(ctx);
            var userEntity = GetUserEntity(ctx);

            return new PlayerData(
                name: user.CharacterName.ToString(),
                steamId: user.PlatformId,
                isConnected: user.IsConnected,
                userEntity: userEntity,
                characterEntity: characterEntity
            );
        }

        /// <summary>
        /// Convert ChatCommandContext to PlayerData (class)
        /// </summary>
        public static CrowbaneArena.Models.PlayerData ToPlayerDataClass(ChatCommandContext ctx)
        {
            var user = GetUser(ctx);
            var userEntity = GetUserEntity(ctx);
            var characterEntity = GetCharacterEntity(ctx);

            return new CrowbaneArena.Models.PlayerData(
                name: user.CharacterName.ToString(),
                steamId: user.PlatformId,
                isConnected: user.IsConnected,
                userEntity: userEntity,
                characterEntity: characterEntity
            );
        }

        /// <summary>
        /// Get character name from context
        /// </summary>
        public static string GetCharacterName(ChatCommandContext ctx)
        {
            return ctx.Event.User.CharacterName.ToString();
        }

        /// <summary>
        /// Get Steam ID from context
        /// </summary>
        public static ulong GetSteamId(ChatCommandContext ctx)
        {
            return ctx.Event.User.PlatformId;
        }

        /// <summary>
        /// Check if user is admin
        /// </summary>
        public static bool IsAdmin(ChatCommandContext ctx)
        {
            return ctx.Event.User.IsAdmin;
        }

        /// <summary>
        /// Check if user is connected
        /// </summary>
        public static bool IsConnected(ChatCommandContext ctx)
        {
            return ctx.Event.User.IsConnected;
        }
    }
}
