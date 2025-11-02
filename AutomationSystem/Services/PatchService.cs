using HarmonyLib;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service for managing patches and hooks.
    /// </summary>
    public class PatchService
    {
        private Harmony harmony;

        public PatchService()
        {
            harmony = new Harmony("CrowbaneArena");
            harmony.PatchAll();
        }

        public void ApplyPatches()
        {
            harmony.PatchAll();
        }

        public void RemovePatches()
        {
            harmony.UnpatchSelf();
        }
    }
}
