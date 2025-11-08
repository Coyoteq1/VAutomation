using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace CrowbaneArena.Helpers
{
    public static class Buffs
    {
        public static void AddBuff(Entity userEntity, Entity characterEntity, PrefabGUID buffGuid, float duration = -1)
        {
            // TODO: Fix buff application API
            // var des = VRisingCore.ServerGameManager.GetDebugEventsSystem();
            // var fromCharacter = new FromCharacter { User = userEntity, Character = characterEntity };
            // var buffEvent = new ApplyBuffDebugEvent { BuffPrefabGUID = buffGuid };
            // des.ApplyBuff(fromCharacter, buffEvent);
        }

        public static void RemoveBuff(Entity characterEntity, PrefabGUID buffGuid)
        {
            if (VRisingCore.EntityManager.TryGetBuffer<BuffBuffer>(characterEntity, out var buffBuffer))
            {
                for (int i = buffBuffer.Length - 1; i >= 0; i--)
                {
                    if (buffBuffer[i].PrefabGuid == buffGuid)
                    {
                        var buffEntity = buffBuffer[i].Entity;
                        if (VRisingCore.EntityManager.Exists(buffEntity))
                        {
                            VRisingCore.EntityManager.DestroyEntity(buffEntity);
                        }
                    }
                }
            }
        }
    }
}
