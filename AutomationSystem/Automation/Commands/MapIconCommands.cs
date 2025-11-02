using CrowbaneArena.Services;
using Stunlock.Core;
using VampireCommandFramework;

namespace CrowbaneArena.Commands
{
    [CommandGroup("mapicon", "mi")]
    internal class MapIconCommands
    {
        [Command("create", "c", description: "Create map icon at current location", adminOnly: false)]
        public static void CreateMapIcon(ChatCommandContext ctx, string mapIconName = "General_SunTemple")
        {
            if (!PrefabGUID.TryParse(mapIconName, out var mapIcon))
            {
                ctx.Reply($"Invalid map icon: {mapIconName}");
                return;
            }

            var mapIconService = new MapIconService();
            mapIconService.CreateMapIcon(ctx.Event.SenderCharacterEntity, mapIcon);
            ctx.Reply("Map icon created");
        }

        [Command("remove", "r", description: "Remove closest map icon", adminOnly: false)]
        public static void RemoveMapIcon(ChatCommandContext ctx)
        {
            var mapIconService = new MapIconService();
            if (mapIconService.RemoveMapIcon(ctx.Event.SenderCharacterEntity))
            {
                ctx.Reply("Map icon removed");
            }
            else
            {
                ctx.Reply("No map icon found nearby");
            }
        }
    }
}
