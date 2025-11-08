using System;
using System.Threading.Tasks;
using CrowbaneArena.Models;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;
using CrowbaneArena.Services;

namespace CrowbaneArena.Commands
{
    internal static class ProgressionCommands
    {
        // Snapshot progression for a player by name
        public static async Task SnapshotProgression(ChatCommandContext ctx, string playerName = null)
        {
            try
            {
                Entity userEntity;
                Entity characterEntity;

                if (string.IsNullOrEmpty(playerName))
                {
                    userEntity = ctx.Event.SenderUserEntity;
                    characterEntity = ctx.Event.SenderCharacterEntity;
                }
                else
                {
                    characterEntity = PlayerManager.GetPlayerByName(playerName);
                    if (characterEntity == Entity.Null)
                    {
                        ctx.Error($"Player '{playerName}' not found");
                        return;
                    }

                    if (!VRisingCore.EntityManager.TryGetComponentData<PlayerCharacter>(characterEntity, out var pc))
                    {
                        ctx.Error("Could not get player data");
                        return;
                    }
                    userEntity = pc.UserEntity;
                }

                if (await ProgressionService.CreateSnapshot(userEntity, characterEntity))
                {
                    var targetName = string.IsNullOrEmpty(playerName) ? "you" : playerName;
                    ctx.Reply($"Created progression snapshot for {targetName}");
                }
                else
                {
                    ctx.Error("Failed to create progression snapshot");
                }
            }
            catch (Exception ex)
            {
                ctx.Error($"Error: {ex.Message}");
            }
        }

        // (FoundPlayer overload removed - not available in this build of the command framework)

        // Restore progression by player name
        public static async Task RestoreProgression(ChatCommandContext ctx, string playerName = null)
        {
            try
            {
                Entity userEntity;
                Entity characterEntity;

                if (string.IsNullOrEmpty(playerName))
                {
                    userEntity = ctx.Event.SenderUserEntity;
                    characterEntity = ctx.Event.SenderCharacterEntity;
                }
                else
                {
                    characterEntity = PlayerManager.GetPlayerByName(playerName);
                    if (characterEntity == Entity.Null)
                    {
                        ctx.Error($"Player '{playerName}' not found");
                        return;
                    }

                    if (!VRisingCore.EntityManager.TryGetComponentData<PlayerCharacter>(characterEntity, out var pc))
                    {
                        ctx.Error("Could not get player data");
                        return;
                    }
                    userEntity = pc.UserEntity;
                }

                if (await ProgressionService.RestoreSnapshot(userEntity, characterEntity))
                {
                    var targetName = string.IsNullOrEmpty(playerName) ? "you" : playerName;
                    ctx.Reply($"Restored progression for {targetName}");
                }
                else
                {
                    ctx.Error("Failed to restore progression");
                }
            }
            catch (Exception ex)
            {
                ctx.Error($"Error: {ex.Message}");
            }
        }

        // (FoundPlayer overload removed - not available in this build of the command framework)

        // Check if a player has a progression snapshot (by name)
        public static void HasProgressionSnapshot(ChatCommandContext ctx, string playerName = null)
        {
            try
            {
                Entity userEntity;

                if (string.IsNullOrEmpty(playerName))
                {
                    userEntity = ctx.Event.SenderUserEntity;
                }
                else
                {
                    var characterEntity = PlayerManager.GetPlayerByName(playerName);
                    if (characterEntity == Entity.Null)
                    {
                        ctx.Error($"Player '{playerName}' not found");
                        return;
                    }

                    if (!VRisingCore.EntityManager.TryGetComponentData<PlayerCharacter>(characterEntity, out var pc))
                    {
                        ctx.Error("Could not get player data");
                        return;
                    }
                    userEntity = pc.UserEntity;
                }

                if (!VRisingCore.EntityManager.TryGetComponentData(userEntity, out User user))
                {
                    ctx.Error("Failed to get user info");
                    return;
                }

                var hasSnapshot = ProgressionService.HasSnapshot(user.PlatformId);
                var targetName = string.IsNullOrEmpty(playerName) ? "You" : playerName;
                ctx.Reply($"{targetName} {(hasSnapshot ? "have" : "do not have")} a progression snapshot");
            }
            catch (Exception ex)
            {
                ctx.Error($"Error: {ex.Message}");
            }
        }

        // (FoundPlayer overload removed - not available in this build of the command framework)

        [Command("delete-progression-snapshot", description: "Deletes a player's progression snapshot", adminOnly: true)]
        public static void DeleteProgressionSnapshot(ChatCommandContext ctx, string playerName = null)
        {
            try
            {
                Entity userEntity;

                if (string.IsNullOrEmpty(playerName))
                {
                    userEntity = ctx.Event.SenderUserEntity;
                }
                else
                {
                    var characterEntity = PlayerManager.GetPlayerByName(playerName);
                    if (characterEntity == Entity.Null)
                    {
                        ctx.Error($"Player '{playerName}' not found");
                        return;
                    }

                    if (!VRisingCore.EntityManager.TryGetComponentData<PlayerCharacter>(characterEntity, out var pc))
                    {
                        ctx.Error("Could not get player data");
                        return;
                    }
                    userEntity = pc.UserEntity;
                }

                if (!VRisingCore.EntityManager.TryGetComponentData(userEntity, out User user))
                {
                    ctx.Error("Failed to get user info");
                    return;
                }

                ProgressionService.DeleteSnapshot(user.PlatformId);
                var targetName = string.IsNullOrEmpty(playerName) ? "you" : playerName;
                ctx.Reply($"Deleted progression snapshot for {targetName}");
            }
            catch (Exception ex)
            {
                ctx.Error($"Error: {ex.Message}");
            }
        }

        [Command("clear-all-progression-snapshots", description: "Deletes all progression snapshots", adminOnly: true)]
        public static void ClearAllProgressionSnapshots(ChatCommandContext ctx)
        {
            try
            {
                ProgressionService.DeleteAllSnapshots();
                ctx.Reply("Deleted all progression snapshots");
            }
            catch (Exception ex)
            {
                ctx.Error($"Error: {ex.Message}");
            }
        }
    }
}