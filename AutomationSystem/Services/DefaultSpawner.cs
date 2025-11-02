using ProjectM;
using Unity.Entities;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Default no-op spawner that only logs actions. Replace with a ProjectM-backed implementation to actually spawn.
    /// </summary>
    public class DefaultSpawner : ISpawner
    {
        private readonly LoggingService _log = new();

        public bool SpawnItem(Entity player, int prefabGuid, int quantity = 1)
        {
            _log.LogEvent($"[SpawnItem] Player={player} PrefabGUID={prefabGuid} Qty={quantity}");
            return true;
        }

        public bool EquipArmor(Entity player, int armorPrefabGuid)
        {
            _log.LogEvent($"[EquipArmor] Player={player} ArmorGUID={armorPrefabGuid}");
            return true;
        }
    }
}
