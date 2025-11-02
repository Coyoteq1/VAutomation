using CrowbaneArena.Services;
using ProjectM;
using Stunlock.Core;
using VampireCommandFramework;

namespace CrowbaneArena.Commands
{
    [CommandGroup("arenaportal", "ap")]
    internal class ArenaPortalCommands
    {
        private static ArenaPortalManager _portalManager = new();

        // REMOVED: enter and exit commands as requested
        /*
        [Command("enter", "e", description: "Spawn an arena enter portal with Unholy buff", adminOnly: true)]
        public static void SpawnEnterPortal(ChatCommandContext ctx)
        {
            var characterEntity = ctx.Event.SenderCharacterEntity;
            if (_portalManager.SpawnEnterPortal(characterEntity, out var error))
            {
                ctx.Reply("✅ Enter portal spawned with Unholy buff");
            }
            else
            {
                ctx.Reply($"❌ Failed to spawn enter portal: {error}");
            }
        }

        [Command("exit", "x", description: "Spawn an arena exit portal with Blood buff", adminOnly: true)]
        public static void SpawnExitPortal(ChatCommandContext ctx)
        {
            var characterEntity = ctx.Event.SenderCharacterEntity;
            if (_portalManager.SpawnExitPortal(characterEntity, out var error))
            {
                ctx.Reply("✅ Exit portal spawned with Blood buff");
            }
            else
            {
                ctx.Reply($"❌ Failed to spawn exit portal: {error}");
            }
        }
        */

        [Command("remove", "r", description: "Remove the nearest portal", adminOnly: true)]
        public static void RemoveNearestPortal(ChatCommandContext ctx)
        {
            var characterEntity = ctx.Event.SenderCharacterEntity;
            if (_portalManager.RemoveNearestPortal(characterEntity, out var error))
            {
                ctx.Reply("✅ Removed nearest portal");
            }
            else
            {
                ctx.Reply($"❌ {error}");
            }
        }

        [Command("list", "l", description: "List all active portals", adminOnly: true)]
        public static void ListPortals(ChatCommandContext ctx)
        {
            // This would be implemented to show all active portals
            ctx.Reply("Portal list functionality coming soon!");
        }
    }
}
