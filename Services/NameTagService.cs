using ProjectM.Network;
using Unity.Entities;

namespace CrowbaneArena.Services
{
    public static class NameTagService
    {
        private static EntityManager EM => CrowbaneArenaCore.EntityManager;

        public static string GetTaggedName(Entity userEntity)
        {
            if (!EM.TryGetComponentData(userEntity, out User user)) return "Unknown";
            return GetTaggedName(user);
        }

        public static string GetTaggedName(User user)
        {
            var name = user.CharacterName.ToString();
            // Return name without arena status check for now
            return name;
        }
    }
}
