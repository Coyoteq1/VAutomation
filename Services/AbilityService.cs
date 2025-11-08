using System;
using System.Collections.Generic;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace CrowbaneArena.Services
{
    public static class AbilityService
    {
        private static EntityManager EM => CrowbaneArenaCore.EntityManager;

        // Seed set of boss-derived spell school ability groups (ability group GuidHash values)
        // This should be extended to include all boss school ability groups you support.
        // Names follow patterns similar to AB_Blood_*, AB_Chaos_*, AB_Frost_*, AB_Illusion_* etc.
        private static readonly HashSet<int> BossSchoolAbilityGroups = new()
        {
            // Blood examples
            PrefabToInt(TempPrefabs.AB_Blood_BloodFountain_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Blood_BloodRage_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Blood_BloodRite_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Blood_SanguineCoil_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Blood_Shadowbolt_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Blood_VeilOfBlood_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Blood_CarrionSwarm_AbilityGroup),
            // Chaos examples
            PrefabToInt(TempPrefabs.AB_Chaos_Aftershock_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Chaos_Barrier_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Chaos_PowerSurge_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Chaos_Void_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Chaos_Volley_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Chaos_VeilOfChaos_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Chaos_RainOfChaos_AbilityGroup),
            // Frost examples
            PrefabToInt(TempPrefabs.AB_Frost_ColdSnap_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Frost_CrystalLance_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Frost_Barrier_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Frost_FrostBat_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Frost_IceNova_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Frost_VeilOfFrost_AbilityGroup),
            PrefabToInt(TempPrefabs.AB_Frost_Cone_AbilityGroup),
            // Illusion examples
            PrefabToInt(TempPrefabs.AB_Illusion_MistTrance_AbilityGroup),
            // add Shadow/Storm/etc as needed
        };

        private static int PrefabToInt(PrefabGUID p) => p.GuidHash;

        // Return list of currently unlocked boss-school ability group IDs
        public static List<int> GetCurrentAbilityIds(Entity characterEntity)
        {
            var list = new List<int>();
            try
            {
                // TODO: Replace with actual read of character's unlocked ability groups
                // Example:
                // if (EM.TryGetBuffer<UnlockedAbilityGroup>(characterEntity, out var buffer))
                //   foreach (var entry in buffer) if (BossSchoolAbilityGroups.Contains(entry.Group.GuidHash)) list.Add(entry.Group.GuidHash);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"GetCurrentAbilityIds failed: {ex.Message}");
            }
            return list;
        }

        // Unlock all boss-school abilities for arena session
        public static void UnlockAllForArena(Entity characterEntity)
        {
            try
            {
                // TODO: Replace with actual unlock API to add these groups to the character's learned/unlocked set
                // foreach (var id in BossSchoolAbilityGroups) UnlockAbilityGroup(characterEntity, new PrefabGUID(id));
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"UnlockAllForArena failed: {ex.Message}");
            }
        }

        // Restore exactly the provided boss-school abilities, clearing any other boss-school unlocks
        public static void RestoreFromSnapshot(Entity characterEntity, IReadOnlyList<int> abilityIds)
        {
            try
            {
                Plugin.Logger?.LogInfo($"Restoring {abilityIds?.Count ?? 0} abilities from snapshot for arena exit");

                // For now, just log that we're restoring abilities
                // The actual implementation would require knowledge of the ability system internals
                // TODO: Implement actual ability restoration when API is available

                if (abilityIds != null && abilityIds.Count > 0)
                {
                    Plugin.Logger?.LogInfo($"Would restore {abilityIds.Count} boss-school abilities");
                }
                else
                {
                    Plugin.Logger?.LogInfo("No abilities to restore from snapshot");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"RestoreFromSnapshot failed: {ex.Message}");
            }
        }

        // Temporary placeholder for ability group constants; replace with your data class (e.g., PrefabGUIDs)
        private static class TempPrefabs
        {
            // These should map to real PrefabGUID constants from your data (e.g., Data/SpellSchoolGUIDs.cs or a similar constants file)
            public static readonly PrefabGUID AB_Blood_BloodFountain_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Blood_BloodRage_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Blood_BloodRite_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Blood_SanguineCoil_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Blood_Shadowbolt_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Blood_VeilOfBlood_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Blood_CarrionSwarm_AbilityGroup = new(0);

            public static readonly PrefabGUID AB_Chaos_Aftershock_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Chaos_Barrier_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Chaos_PowerSurge_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Chaos_Void_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Chaos_Volley_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Chaos_VeilOfChaos_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Chaos_RainOfChaos_AbilityGroup = new(0);

            public static readonly PrefabGUID AB_Frost_ColdSnap_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Frost_CrystalLance_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Frost_Barrier_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Frost_FrostBat_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Frost_IceNova_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Frost_VeilOfFrost_AbilityGroup = new(0);
            public static readonly PrefabGUID AB_Frost_Cone_AbilityGroup = new(0);

            public static readonly PrefabGUID AB_Illusion_MistTrance_AbilityGroup = new(0);
        }
    }
}
