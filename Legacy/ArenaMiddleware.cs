using VampireCommandFramework;
using System.Reflection;
using ProjectM;
using Unity.Entities;
using CrowbaneArena.Services;

namespace CrowbaneArena
{
    /// <summary>
    /// Custom middleware for Arena-specific command handling
    /// </summary>
    public class ArenaMiddleware : CommandMiddleware
    {
        public override bool CanExecute(ICommandContext ctx, CommandAttribute command, MethodInfo method)
        {
            // Check if command requires admin and user is admin
            if (command.AdminOnly && !ctx.IsAdmin)
            {
                ctx.Error("This command requires admin privileges!");
                return false;
            }

            return true;
        }

        public override void BeforeExecute(ICommandContext ctx, CommandAttribute command, MethodInfo method)
        {
            // Log command usage for analytics
            Plugin.Logger?.LogInfo($"Arena command executed: {command.Id} by {ctx.Name}");

            // Check if player is in arena for certain commands
            if (command.Id.StartsWith("arena") && !command.AdminOnly)
            {
                var player = PlayerManager.GetPlayerByName(ctx.Name);
                if (player.Equals(Entity.Null)) return;

                var inArena = ArenaController.IsPlayerInArena(player);
                if (inArena && (command.Id == "arena.enter" || command.Id == "arena.setzone"))
                {
                    ctx.Error("You cannot use this command while in the arena!");
                    return;
                }
            }
        }

        public override void AfterExecute(ICommandContext ctx, CommandAttribute command, MethodInfo method)
        {
            // Post-command logic could go here
            Plugin.Logger?.LogInfo($"Arena command completed: {command.Id} by {ctx.Name}");
        }
    }
}
