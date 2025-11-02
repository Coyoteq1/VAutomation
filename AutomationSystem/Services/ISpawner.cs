using ProjectM;
using Unity.Entities;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Abstraction for spawning items and equipment for players. This default interface allows
    /// the service to compile without direct V Rising API calls and can be swapped with a real implementation.
    /// </summary>
    public interface ISpawner
    {
        bool SpawnItem(Entity player, int prefabGuid, int quantity = 1);
        bool EquipArmor(Entity player, int armorPrefabGuid);
    }
}
