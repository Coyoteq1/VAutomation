using ProjectM;
using ProjectM.Shared;
using Unity.Entities;
using VampireCommandFramework;
using CrowbaneArena.Services;
using CrowbaneArena.Data;

namespace CrowbaneArena.Commands
{
    [CommandGroup("force")]
    public class VBloodForceCommands
    {
        [Command("add", adminOnly: true)]
        public void ForceAddVBlood(ChatCommandContext ctx, int vBloodGuid)
        {
            var characterEntity = ctx.Event.SenderCharacterEntity;
            var userEntity = ctx.Event.SenderUserEntity;

            if (ForceVBloodCompletion(userEntity, vBloodGuid))
            {
                ctx.Reply($"Force added VBlood completion: {vBloodGuid}");
            }
            else
            {
                ctx.Reply($"Failed to add VBlood completion: {vBloodGuid}");
            }
        }

        [Command("remove", adminOnly: true)]
        public void ForceRemoveVBlood(ChatCommandContext ctx, int vBloodGuid)
        {
            var characterEntity = ctx.Event.SenderCharacterEntity;
            var userEntity = ctx.Event.SenderUserEntity;

            if (RemoveVBloodCompletion(userEntity, vBloodGuid))
            {
                ctx.Reply($"Force removed VBlood completion: {vBloodGuid}");
            }
            else
            {
                ctx.Reply($"Failed to remove VBlood completion: {vBloodGuid}");
            }
        }

        /// <summary>
        /// Force unlock all spell school passives
        /// </summary>
        [Command("all", adminOnly: true)]
        public static void ForceAllVBloods(ChatCommandContext ctx)
        {
            ctx.Reply("Spell school unlock not implemented");
        }

        private static bool ForceVBloodCompletion(Entity userEntity, int vBloodGUID)
        {
            if (BossService.Instance != null)
            {
                var boss = new FoundVBlood(new Stunlock.Core.PrefabGUID(vBloodGUID), $"Boss_{vBloodGUID}");
                return BossService.Instance.UnlockBoss(boss, userEntity);
            }
            return false;
        }

        private static bool RemoveVBloodCompletion(Entity userEntity, int vBloodGUID)
        {
            if (BossService.Instance != null)
            {
                var boss = new FoundVBlood(new Stunlock.Core.PrefabGUID(vBloodGUID), $"Boss_{vBloodGUID}");
                return BossService.Instance.LockBoss(boss, userEntity);
            }
            return false;
        }
    }
}
