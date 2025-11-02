using Unity.Entities;
using ProjectM;

namespace CrowbaneArena.Services
{
    public static class ArenaCheatService
    {
        public static void ApplyArenaBuffs(Entity player)
        {
            var em = VRisingCore.EntityManager;
            
            // Unlimited blood
            if (em.HasComponent<BloodConsumeSource>(player))
            {
                var blood = em.GetComponentData<BloodConsumeSource>(player);
                blood.BloodQuality = 1.0f;
                blood.BloodQuantity = float.MaxValue;
                em.SetComponentData(player, blood);
            }
            
            // No durability loss
            if (em.HasComponent<Durability>(player))
            {
                var durability = em.GetComponentData<Durability>(player);
                durability.Value = float.MaxValue;
                em.SetComponentData(player, durability);
            }
            
            // Remove all debuffs
            Helper.RemoveNegativeBuffs(player);
        }
    }
}
