using CrowbaneArena.Services;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using VampireCommandFramework;
using System;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace CrowbaneArena.Commands
{
    [CommandGroup("portal", "p")]
    internal class PortalCommands
    {
        [Command("start", "s", description: "Start arena portal with map icon and snapshot capture", adminOnly: false)]
        public static void StartPortal(ChatCommandContext ctx, string mapIconName = "POI_BloodMoon_Arena_BloodWars")
        {
            if (!PrefabGUID.TryParse(mapIconName, out var mapIcon))
            {
                ctx.Reply($"Invalid map icon: {mapIconName}");
                return;
            }

            var characterEntity = ctx.Event.SenderCharacterEntity;
            var userEntity = ZoneManager.GetUserFromCharacter(characterEntity);

            if (userEntity == Entity.Null)
            {
                ctx.Reply("Unable to find user entity");
                return;
            }

            var user = CrowbaneArenaCore.EntityManager.GetComponentData<User>(userEntity);
            var steamId = user.PlatformId;

            // Check if player is already in arena
            if (SnapshotManagerService.IsInArena(steamId))
            {
                ctx.Reply("Already in arena! Use .portal end to exit first");
                return;
            }

            // Create map icon
            var mapIconService = new MapIconService();
            try
            {
                mapIconService.CreateMapIcon(characterEntity, mapIcon);
                ctx.Reply($"🏛️ Map icon '{mapIconName}' created at location");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to create map icon: {ex.Message}");
                ctx.Reply("Map icon creation failed, but continuing...");
            }

            // Enter arena with snapshot
            var currentPos = characterEntity.Read<Translation>().Value;
            if (SnapshotManagerService.EnterArena(userEntity, characterEntity, ZoneManager.SpawnPoint))
            {
                ctx.Reply("🎭 Arena entered! State captured and loadout applied");
                Plugin.Logger?.LogInfo($"Player {user.CharacterName} entered arena with portal");
            }
            else
            {
                ctx.Reply("❌ Arena entry failed");
                return;
            }

            // Set portal start location as backup
            var portalService = new PortalService();
            if (portalService.StartPortal(characterEntity, mapIcon))
            {
                ctx.Reply("✅ Portal location set as backup teleport point");
            }
        }

        [Command("end", "e", description: "End arena portal with map icon removal and snapshot restore", adminOnly: false)]
        public static void EndPortal(ChatCommandContext ctx, string mapIconName = "POI_BloodMoon_Arena_BloodWars")
        {
            if (!PrefabGUID.TryParse(mapIconName, out var mapIcon))
            {
                ctx.Reply($"Invalid map icon: {mapIconName}");
                return;
            }

            var characterEntity = ctx.Event.SenderCharacterEntity;
            var userEntity = ZoneManager.GetUserFromCharacter(characterEntity);

            if (userEntity == Entity.Null)
            {
                ctx.Reply("Unable to find user entity");
                return;
            }

            var user = CrowbaneArenaCore.EntityManager.GetComponentData<User>(userEntity);
            var steamId = user.PlatformId;

            // Check if player is in arena
            if (!SnapshotManagerService.IsInArena(steamId))
            {
                ctx.Reply("Not in arena! Use .portal start to enter first");
                return;
            }

            // Remove nearby map icon
            var mapIconService = new MapIconService();
            try
            {
                if (mapIconService.RemoveMapIcon(characterEntity))
                {
                    ctx.Reply($"🗑️ Map icon '{mapIconName}' removed");
                }
                else
                {
                    ctx.Reply("No map icons found nearby to remove");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Failed to remove map icon: {ex.Message}");
                ctx.Reply("Map icon removal failed, but continuing...");
            }

            // Leave arena with snapshot restore
            if (SnapshotManagerService.LeaveArena(steamId, userEntity, characterEntity))
            {
                ctx.Reply("🏠 Arena exited! State restored");
                Plugin.Logger?.LogInfo($"Player {user.CharacterName} left arena with portal");
            }
            else
            {
                ctx.Reply("❌ Arena exit failed");
                return;
            }

            // Remove portal endpoint
            var portalService = new PortalService();
            var error = portalService.EndPortal(characterEntity, mapIcon);
            if (error != null)
            {
                ctx.Reply($"Portal removal: {error}");
            }
            else
            {
                ctx.Reply("✅ Portal endpoint removed");
            }
        }

        [Command("tp", description: "Teleport to closest portal", adminOnly: false)]
        public static void TeleportToPortal(ChatCommandContext ctx)
        {
            var portalService = new PortalService();
            if (portalService.TeleportToClosestPortal(ctx.Event.SenderCharacterEntity))
            {
                ctx.Reply("🌀 Teleported to closest portal");
            }
            else
            {
                ctx.Reply("❌ No portals found nearby");
            }
        }

        [Command("destroy", "d", description: "Destroy closest portal", adminOnly: false)]
        public static void DestroyPortal(ChatCommandContext ctx)
        {
            var portalService = new PortalService();
            if (portalService.DestroyPortal(ctx.Event.SenderCharacterEntity))
            {
                ctx.Reply("💥 Portal destroyed");
            }
            else
            {
                ctx.Reply("❌ No portal found nearby");
            }
        }

        // Additional snapshot management commands
        [Command("status", description: "Check current arena status", adminOnly: false)]
        public static void Status(ChatCommandContext ctx)
        {
            var characterEntity = ctx.Event.SenderCharacterEntity;
            var userEntity = ZoneManager.GetUserFromCharacter(characterEntity);

            if (userEntity == Entity.Null)
            {
                ctx.Reply("Unable to find user entity");
                return;
            }

            var user = CrowbaneArenaCore.EntityManager.GetComponentData<User>(userEntity);
            var steamId = user.PlatformId;

            bool inArena = SnapshotManagerService.IsInArena(steamId);
            int snapshotCount = SnapshotManagerService.GetSnapshotCount();

            ctx.Reply($"📊 Status: {(inArena ? "In Arena" : "Not in Arena")}, {snapshotCount} total snapshots");
        }

        [Command("snapshots", description: "Show snapshot statistics", adminOnly: true)]
        public static void Snapshots(ChatCommandContext ctx)
        {
            int count = SnapshotManagerService.GetSnapshotCount();
            ctx.Reply($"📋 Total snapshots: {count}");

            var characterEntity = ctx.Event.SenderCharacterEntity;
            var userEntity = ZoneManager.GetUserFromCharacter(characterEntity);
            if (userEntity != Entity.Null)
            {
                var user = CrowbaneArenaCore.EntityManager.GetComponentData<User>(userEntity);
                bool inArena = SnapshotManagerService.IsInArena(user.PlatformId);
                ctx.Reply($"👤 Your status: {(inArena ? "In Arena" : "Not in Arena")}");
            }
        }

        [Command("clearsnapshots", description: "Clear all snapshots (admin only)", adminOnly: true)]
        public static void ClearSnapshots(ChatCommandContext ctx)
        {
            SnapshotManagerService.ClearAllSnapshots();
            ctx.Reply("🗑️ All snapshots cleared");
            Plugin.Logger?.LogWarning($"Snapshots cleared by admin {ctx.Event.SenderUserEntity.Read<User>().CharacterName}");
        }

        [Command("tpspawn", description: "Teleport to arena spawn point", adminOnly: false)]
        public static void TeleportToSpawn(ChatCommandContext ctx)
        {
            var characterEntity = ctx.Event.SenderCharacterEntity;
            ZoneManager.TeleportToSpawn(characterEntity);
            var spawnPos = ZoneManager.SpawnPoint;
            ctx.Reply($"🚀 Teleported to arena spawn: {spawnPos}");
        }

        [Command("setspawn", description: "Set current location as arena spawn point (admin only)", adminOnly: true)]
        public static void SetSpawn(ChatCommandContext ctx)
        {
            var characterEntity = ctx.Event.SenderCharacterEntity;
            var pos = characterEntity.Read<Translation>().Value;
            ZoneManager.SetSpawnPoint(pos);
            ctx.Reply($"🎯 Arena spawn point set to: {pos}");
        }
    }
}
