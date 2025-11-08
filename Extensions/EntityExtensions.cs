using Unity.Entities;
using Unity.Collections;

namespace CrowbaneArena.Extensions
{
    /// <summary>
    /// Extension methods for Entity operations following KindredCommands pattern
    /// </summary>
    public static class EntityExtensions
    {
        public static T Read<T>(this Entity entity) where T : unmanaged
        {
            return CrowbaneArenaCore.EntityManager.GetComponentData<T>(entity);
        }

        public static void Write<T>(this Entity entity, T componentData) where T : unmanaged
        {
            CrowbaneArenaCore.EntityManager.SetComponentData(entity, componentData);
        }

        public static bool Has<T>(this Entity entity) where T : unmanaged
        {
            return CrowbaneArenaCore.EntityManager.HasComponent<T>(entity);
        }

        public static DynamicBuffer<T> ReadBuffer<T>(this Entity entity) where T : unmanaged
        {
            return CrowbaneArenaCore.EntityManager.GetBuffer<T>(entity);
        }

        public static void Remove<T>(this Entity entity) where T : unmanaged
        {
            CrowbaneArenaCore.EntityManager.RemoveComponent<T>(entity);
        }
    }
}
