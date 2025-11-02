using System;
using System.Collections.Generic;
using CrowbaneArena.Services;
using Stunlock.Core;
using ProjectM;
using Unity.Entities;
using Unity.Transforms;
using VampireCommandFramework;
namespace CrowbaneArena.Commands
{
    [CommandGroup("waygate", "wg")]
    internal class WaygateCommands
    {
        [Command("create", "c", description: "Create waygate at current location", adminOnly: false)]
        public static void CreateWaygate(ChatCommandContext ctx, string waypointName = "TM_SunTemple_Waygate")
        {
            if (!PrefabGUID.TryParse(waypointName, out var waypointPrefab))
            {
                ctx.Reply($"Invalid waypoint prefab: {waypointName}");
                return;
            }

            var waygateService = new WaygateService();
            if (waygateService.CreateWaygate(ctx.Event.SenderCharacterEntity, waypointPrefab))
            {
                ctx.Reply("Waygate created successfully");
            }
            else
            {
                ctx.Reply("Failed to create waygate - chunk already has a waygate");
            }
        }

        [Command("tp", description: "Teleport to closest waygate", adminOnly: false)]
        public static void TeleportToWaygate(ChatCommandContext ctx)
        {
            var waygateService = new WaygateService();
            if (waygateService.TeleportToClosestWaygate(ctx.Event.SenderCharacterEntity))
            {
                ctx.Reply("Teleported to closest waygate");
            }
            else
            {
                ctx.Reply("No waygates found");
            }
        }

        [Command("destroy", "d", description: "Destroy closest waygate", adminOnly: false)]
        public static void DestroyWaygate(ChatCommandContext ctx)
        {
            var waygateService = new WaygateService();
            if (waygateService.DestroyWaygate(ctx.Event.SenderCharacterEntity))
            {
                ctx.Reply("Waygate destroyed");
            }
            else
            {
                ctx.Reply("No waygate found nearby");
            }
        }
    }
}
