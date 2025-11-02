using VampireCommandFramework;
using Unity.Entities;
using Unity.Mathematics;
using ProjectM;
using ProjectM.Network;
using System;
using CrowbaneArena.Services;

namespace CrowbaneArena.Commands
{
    // This class is deprecated - use ArenaCommands instead
    public static class ArenaJoinCommands
    {
        // REMOVED: join_legacy and leave_legacy commands as requested
        /*
        [Command("join_legacy", description: "Legacy join command - use .join instead")]
        public static void JoinArena(ICommandContext ctx)
        {
            ctx.Error("This command is deprecated. Use '.join' instead");
        }

        [Command("leave_legacy", description: "Legacy leave command - use .exit instead")]
        public static void LeaveArena(ICommandContext ctx)
        {
            ctx.Error("This command is deprecated. Use '.exit' instead");
        }
        */
    }
}
