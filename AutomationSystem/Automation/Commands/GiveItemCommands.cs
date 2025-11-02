
using CrowbaneArena.Commands.Converters;
using ProjectM;
using VampireCommandFramework;

namespace CrowbaneArena.Commands;

internal class GiveItemCommands
{
    [Command("give", "g", "<Prefab GUID or name> [quantity=1]", "Gives the specified item to the player", adminOnly: true)]
    public static void GiveItem(ChatCommandContext ctx, ItemParameter item, int quantity = 1)
    {
        //
    }
}
