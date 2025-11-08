using HarmonyLib;
using ProjectM;

namespace CrowbaneArena.Patches
{
    [HarmonyPatch(typeof(InitializeNewSpawnChainSystem), nameof(InitializeNewSpawnChainSystem.OnUpdate))]
    public static class SpawnChainPatch
    {
        public static bool skipOnce = false;

        public static bool Prefix(InitializeNewSpawnChainSystem __instance)
        {
            if (skipOnce)
            {
                skipOnce = false;
                return false;
            }
            return true;
        }
    }
}
