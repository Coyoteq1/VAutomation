using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Stunlock.Network;
using System;
using Unity.Entities;
using CrowbaneArena;

namespace CrowbaneArena.Patches
{
    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserConnected))]
    public static class OnUserConnected_Patch
    {
        public static void Postfix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId)
        {
            try
            {
                var userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
                var serverClient = __instance._ApprovedUsersLookup[userIndex];
                var userEntity = serverClient.UserEntity;
                var user = __instance.EntityManager.GetComponentData<User>(userEntity);
                
                if (user.LocalCharacter.GetEntityOnServer() != Entity.Null)
                {
                    var characterEntity = user.LocalCharacter.GetEntityOnServer();
                    var steamId = Services.PlayerService.GetSteamId(userEntity);
                    
                    // Check if player was in arena before disconnect
                    if (Services.SnapshotService.IsInArena(steamId))
                    {
                        Plugin.Logger?.LogInfo($"Player {user.CharacterName} reconnected while in arena - cleaning up");
                        ZoneManager.ManualExitArena(characterEntity);
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogError($"Error in OnUserConnected: {e.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(ServerBootstrapSystem), nameof(ServerBootstrapSystem.OnUserDisconnected))]
    public static class OnUserDisconnected_Patch
    {
        private static void Prefix(ServerBootstrapSystem __instance, NetConnectionId netConnectionId, ConnectionStatusChangeReason connectionStatusReason, string extraData)
        {
            try
            {
                var userIndex = __instance._NetEndPointToApprovedUserIndex[netConnectionId];
                var serverClient = __instance._ApprovedUsersLookup[userIndex];
                var user = __instance.EntityManager.GetComponentData<User>(serverClient.UserEntity);
                
                if (user.LocalCharacter.GetEntityOnServer() != Entity.Null)
                {
                    var characterEntity = user.LocalCharacter.GetEntityOnServer();
                    var steamId = Services.PlayerService.GetSteamId(serverClient.UserEntity);
                    
                    // Auto-exit arena on disconnect
                    if (Services.SnapshotService.IsInArena(steamId))
                    {
                        Plugin.Logger?.LogInfo($"Player {user.CharacterName} disconnected while in arena - auto-exiting");
                        Services.SnapshotService.ExitArena(serverClient.UserEntity, characterEntity);
                        ZoneManager.ManualExitArena(characterEntity);
                    }
                }
            }
            catch { }
        }
    }
}
