using VampireCommandFramework;
using Stunlock.Core;
using ProjectM.Network;
using Unity.Entities;

namespace CrowbaneArena.Commands
{
    [CommandGroup("pvp", "p")]
    internal class PvPCommandGroup
    {
        // Example command – give a buff (only works on secondary character)
        [Command("givebuff", description: "Give yourself a buff (secondary character only)")]
        public static void GiveBuff(ChatCommandContext ctx, string buffName)
        {
            var userEntity = ctx.Event.SenderUserEntity;
            // Only run if the user is on the secondary
            if (!VRising.EntityManager.HasComponent<IsSecondaryComponent>(userEntity))
            {
                ctx.Reply("This command is only available for the secondary (arena) character.");
                return;
            }

            // Apply buff logic here
            ctx.Reply($"Buff '{buffName}' applied to {VRising.EntityManager.GetComponentData<User>(userEntity).CharacterName}.");
        }

        // Give end-game items (only on secondary character)
        [Command("getitem", description: "Get end-game items (secondary character only)")]
        public static void GetEndGameItem(ChatCommandContext ctx, string itemName)
        {
            var userEntity = ctx.Event.SenderUserEntity;
            if (!VRising.EntityManager.HasComponent<IsSecondaryComponent>(userEntity))
            {
                ctx.Reply("This command is only available for the secondary (arena) character.");
                return;
            }

            // Simple item giving logic
            ctx.Reply($"Gave {itemName} to your arena character.");
        }

        // Toggle PvP mode (only on secondary character)
        [Command("toggle", description: "Toggle PvP mode (secondary character only)")]
        public static void TogglePvP(ChatCommandContext ctx)
        {
            var userEntity = ctx.Event.SenderUserEntity;
            if (!VRising.EntityManager.HasComponent<IsSecondaryComponent>(userEntity))
            {
                ctx.Reply("This command is only available for the secondary (arena) character.");
                return;
            }

            // PvP toggle logic here
            ctx.Reply("PvP mode toggled for your arena character.");
        }

        // Check current character status
        [Command("status", description: "Check if you're on primary or secondary character")]
        public static void CheckStatus(ChatCommandContext ctx)
        {
            var userEntity = ctx.Event.SenderUserEntity;
            var user = VRising.EntityManager.GetComponentData<User>(userEntity);
            var isSecondary = VRising.EntityManager.HasComponent<IsSecondaryComponent>(userEntity);

            ctx.Reply($"Character: {user.CharacterName}");
            ctx.Reply($"Type: {(isSecondary ? "Secondary (Arena)" : "Primary (Normal)")}");
            ctx.Reply($"SteamID: {user.PlatformId}");
        }
    }
}
