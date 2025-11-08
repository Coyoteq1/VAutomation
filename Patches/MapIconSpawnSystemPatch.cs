using HarmonyLib;
using ProjectM;
using Unity.Collections;
using Unity.Entities;

namespace VAutomation.Patches;

[HarmonyPatch(typeof(MapIconSpawnSystem), nameof(MapIconSpawnSystem.OnUpdate))]
static class MapIconSpawnSystemPatch
{
    public static void Prefix(MapIconSpawnSystem __instance)
    {
        var em = __instance.World.EntityManager;
        var entities = __instance.__query_1050583545_0.ToEntityArray(Allocator.Temp);
        foreach (var entity in entities)
        {
            if (!em.HasComponent<Attach>(entity)) continue;

            var attachParent = em.GetComponentData<Attach>(entity).Parent;
            if (attachParent.Equals(Entity.Null)) continue;

            if (!em.HasComponent<SpawnedBy>(attachParent)) continue;

            var mapIconData = em.GetComponentData<MapIconData>(entity);

            mapIconData.RequiresReveal = false;
            mapIconData.AllySetting = MapIconShowSettings.Global;
            mapIconData.EnemySetting = MapIconShowSettings.Global;
            em.SetComponentData(entity, mapIconData);
        }
        entities.Dispose();
    }
}
