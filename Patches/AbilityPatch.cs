using HarmonyLib;
using ProjectM;
using ProjectM.Gameplay.Systems;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using CrowbaneArena;

namespace CrowbaneArena.Patches
{
    [HarmonyPatch(typeof(AbilityRunScriptsSystem), nameof(AbilityRunScriptsSystem.OnUpdate))]
    internal class AbilityPatch
    {
        public static bool Prefix(AbilityRunScriptsSystem __instance)
        {
            var entities = __instance._OnCastStartedQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                if (!VRisingCore.EntityManager.TryGetComponentData(entity, out AbilityCastStartedEvent acse))
                    continue;

                var characterEntity = acse.Character;
                if (characterEntity == Entity.Null)
                    continue;

                if (!VRisingCore.EntityManager.TryGetComponentData(acse.AbilityGroup, out PrefabGUID abilityGuid))
                    continue;

                // Allow all abilities in arena (bypass unlock checks)
                if (ZoneManager.IsPlayerInArena(characterEntity))
                    continue;
            }
            entities.Dispose();
            return true;
        }
    }
}
