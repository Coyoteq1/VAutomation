using System;
using System.Collections.Generic;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using Unity.Mathematics;
using Stunlock.Core;

namespace CrowbaneArena.Models
{
    public class ProgressionSnapshot
    {
        // Core identifiers
        public string SnapshotId { get; set; } = Guid.NewGuid().ToString();
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public int SchemaVersion { get; set; } = 1;

        // Player identity
        public ulong PlatformId { get; set; }
        public string CharacterName { get; set; }

        // Research and VBlood states
        public HashSet<PrefabGUID> UnlockedResearch { get; set; } = new();
        public HashSet<PrefabGUID> UnlockedVBlood { get; set; } = new();
        public HashSet<PrefabGUID> UnlockedAbilities { get; set; } = new();

        // Achievement progress
        public HashSet<PrefabGUID> CompletedAchievements { get; set; } = new();
        public Dictionary<PrefabGUID, float> AchievementProgress { get; set; } = new();

        // Map and waypoint data
        public HashSet<int> UnlockedWaypoints { get; set; } = new();
        public HashSet<int2> RevealedMapChunks { get; set; } = new();

        // Spell school progression
        public Dictionary<PrefabGUID, float> SpellSchoolLevels { get; set; } = new();
        public HashSet<PrefabGUID> UnlockedPassives { get; set; } = new();

        // UI and game preferences
        public Dictionary<string, bool> UIStates { get; set; } = new();
        public Dictionary<string, string> GameSettings { get; set; } = new();

        // Experience and level data
        public float Experience { get; set; }
        public int Level { get; set; }
        public Dictionary<string, int> SkillLevels { get; set; } = new();

        // Inventory data
        public Dictionary<PrefabGUID, int> Items { get; set; } = new();
        public Dictionary<PrefabGUID, int> Equipped { get; set; } = new();

        public static ProgressionSnapshot Create(Entity userEntity, Entity characterEntity)
        {
            var entityManager = VRisingCore.EntityManager;
            var snapshot = new ProgressionSnapshot();

            try
            {
                if (entityManager.TryGetComponentData(userEntity, out User user))
                {
                    snapshot.PlatformId = user.PlatformId;
                    snapshot.CharacterName = user.CharacterName.ToString();
                }

                // Get progression entity
                Plugin.Logger?.LogInfo($"[CAPTURE] Checking ProgressionMapper on userEntity {userEntity.Index}");
                if (entityManager.TryGetComponentData(userEntity, out ProgressionMapper mapper))
                {
                    var progressionEntity = mapper.ProgressionEntity.GetEntityOnServer();
                    Plugin.Logger?.LogInfo($"[CAPTURE] Found progression entity {progressionEntity.Index}");
                    CaptureProgression(progressionEntity, snapshot);
                }
                else
                {
                    Plugin.Logger?.LogWarning($"[CAPTURE] No ProgressionMapper found on userEntity");
                }

                // Capture VBlood unlocks
                Plugin.Logger?.LogInfo($"[CAPTURE] Checking VBloodConsumed component on userEntity {userEntity.Index}");
                if (entityManager.HasComponent<VBloodConsumed>(userEntity))
                {
                    var vbloodBuffer = entityManager.GetBuffer<VBloodConsumed>(userEntity);
                    Plugin.Logger?.LogInfo($"[CAPTURE] Found VBloodConsumed buffer with {vbloodBuffer.Length} entries");
                    for (int i = 0; i < vbloodBuffer.Length; i++)
                    {
                        snapshot.UnlockedVBlood.Add(vbloodBuffer[i].Source);
                        if (i < 5) Plugin.Logger?.LogInfo($"[CAPTURE] VBlood {i}: {vbloodBuffer[i].Source.GuidHash}");
                    }
                }
                else
                {
                    Plugin.Logger?.LogWarning($"[CAPTURE] No VBloodConsumed component found on userEntity");
                }
                Plugin.Logger?.LogInfo($"[CAPTURE] Total captured: {snapshot.UnlockedVBlood.Count} VBlood unlocks");

                CaptureAbilities(characterEntity, snapshot);
                CaptureAchievements(characterEntity, snapshot);
                CaptureMapProgress(userEntity, snapshot);

                Plugin.Logger?.LogInfo($"Created progression snapshot for {snapshot.CharacterName} with {snapshot.UnlockedVBlood.Count} VBlood unlocks");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error creating progression snapshot: {ex.Message}");
            }

            return snapshot;
        }

        private static void CaptureProgression(Entity progressionEntity, ProgressionSnapshot snapshot)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                
                // Capture research unlocks - TODO: Implement when correct component type is identified
                // if (em.HasComponent<UnlockedResearchBuffer>(progressionEntity))
                // {
                //     var researchBuffer = em.GetBuffer<UnlockedResearchBuffer>(progressionEntity);
                //     for (int i = 0; i < researchBuffer.Length; i++)
                //     {
                //         snapshot.UnlockedResearch.Add(researchBuffer[i].ResearchGuid);
                //     }
                // }

                Plugin.Logger?.LogInfo($"Research capture skipped - component type not available");
                
                Plugin.Logger?.LogInfo($"Captured progression data: {snapshot.UnlockedResearch.Count} researches");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error capturing progression data: {ex.Message}");
            }
        }

        private static void CaptureAbilities(Entity characterEntity, ProgressionSnapshot snapshot)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                
                Plugin.Logger?.LogInfo($"[CAPTURE] Checking abilities on characterEntity {characterEntity.Index}");
                // TODO: Replace with correct ability component when available
                // if (em.HasComponent<UnlockedAbilityElement>(characterEntity))
                // {
                //     var abilityBuffer = em.GetBuffer<UnlockedAbilityElement>(characterEntity);
                //     Plugin.Logger?.LogInfo($"[CAPTURE] Found {abilityBuffer.Length} abilities");
                //     for (int i = 0; i < abilityBuffer.Length; i++)
                //     {
                //         snapshot.UnlockedAbilities.Add(abilityBuffer[i].Guid);
                //         if (i < 5) Plugin.Logger?.LogInfo($"[CAPTURE] Ability {i}: {abilityBuffer[i].Guid.GuidHash}");
                //     }
                // }
                // else
                // {
                //     Plugin.Logger?.LogWarning($"[CAPTURE] No UnlockedAbilityElement component found");
                // }

                Plugin.Logger?.LogInfo($"[CAPTURE] Abilities capture skipped - component type not available");
                
                Plugin.Logger?.LogInfo($"[CAPTURE] Total captured: {snapshot.UnlockedAbilities.Count} learned abilities");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error capturing abilities: {ex.Message}");
            }
        }

        private static void CaptureAchievements(Entity characterEntity, ProgressionSnapshot snapshot)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                
                if (!em.HasComponent<PlayerCharacter>(characterEntity)) return;
                var pc = em.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = pc.UserEntity;
                
                // Capture achievements - TODO: Implement when correct component type is identified
                // if (em.HasComponent<AchievementOwner>(userEntity))
                // {
                //     var achievementBuffer = em.GetBuffer<AchievementOwner>(userEntity);
                //     for (int i = 0; i < achievementBuffer.Length; i++)
                //     {
                //         snapshot.CompletedAchievements.Add(achievementBuffer[i].AchievementGuid);
                //     }
                // }

                Plugin.Logger?.LogInfo($"[CAPTURE] Achievements capture skipped - component type not available");
                
                Plugin.Logger?.LogInfo($"Captured {snapshot.CompletedAchievements.Count} completed achievements");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error capturing achievements: {ex.Message}");
            }
        }

        private static void CaptureMapProgress(Entity userEntity, ProgressionSnapshot snapshot)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                
                // Capture waypoints - TODO: Implement when correct component type is identified
                // if (em.HasComponent<UnlockedWaypointElement>(userEntity))
                // {
                //     var waypointBuffer = em.GetBuffer<UnlockedWaypointElement>(userEntity);
                //     for (int i = 0; i < waypointBuffer.Length; i++)
                //     {
                //         snapshot.UnlockedWaypoints.Add(waypointBuffer[i].Waypoint);
                //     }
                // }
                
                // Capture revealed map chunks - TODO: Implement when correct component type is identified
                // if (em.HasComponent<RevealedChunkElement>(userEntity))
                // {
                //     var chunkBuffer = em.GetBuffer<RevealedChunkElement>(userEntity);
                //     for (int i = 0; i < chunkBuffer.Length; i++)
                //     {
                //         snapshot.RevealedMapChunks.Add(chunkBuffer[i].ChunkCoord);
                //     }
                // }

                Plugin.Logger?.LogInfo($"[CAPTURE] Map progress capture skipped - component types not available");
                
                Plugin.Logger?.LogInfo($"Captured map progress: {snapshot.UnlockedWaypoints.Count} waypoints, {snapshot.RevealedMapChunks.Count} revealed chunks");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error capturing map progress: {ex.Message}");
            }
        }

        public void RestoreTo(Entity userEntity, Entity characterEntity)
        {
            var entityManager = VRisingCore.EntityManager;

            try
            {
                Plugin.Logger?.LogInfo($"[RESTORE] Starting restoration for {CharacterName}");
                Plugin.Logger?.LogInfo($"[RESTORE] Snapshot contains: {UnlockedVBlood.Count} VBloods, {UnlockedResearch.Count} research, {UnlockedAbilities.Count} abilities");
                
                var pc = entityManager.GetComponentData<PlayerCharacter>(characterEntity);
                var userEnt = pc.UserEntity;
                
                // Restore VBloods
                if (entityManager.HasComponent<VBloodConsumed>(userEnt))
                {
                    var vb = entityManager.GetBuffer<VBloodConsumed>(userEnt);
                    Plugin.Logger?.LogInfo($"[RESTORE] Clearing {vb.Length} VBloods, restoring {UnlockedVBlood.Count}");
                    vb.Clear();
                    foreach (var vblood in UnlockedVBlood)
                    {
                        vb.Add(new VBloodConsumed { Source = vblood });
                    }
                }
                
                // Restore research - TODO: Implement when correct component type is identified
                // if (entityManager.TryGetComponentData(userEnt, out ProgressionMapper mapper))
                // {
                //     var progEntity = mapper.ProgressionEntity.GetEntityOnServer();
                //     if (entityManager.HasComponent<UnlockedResearchBuffer>(progEntity))
                //     {
                //         var rb = entityManager.GetBuffer<UnlockedResearchBuffer>(progEntity);
                //         Plugin.Logger?.LogInfo($"[RESTORE] Clearing {rb.Length} research, restoring {UnlockedResearch.Count}");
                //         rb.Clear();
                //         foreach (var research in UnlockedResearch)
                //         {
                //             rb.Add(new UnlockedResearchBuffer { ResearchGuid = research });
                //         }
                //     }
                // }

                Plugin.Logger?.LogInfo($"[RESTORE] Research restore skipped - component type not available");
                
                // Restore abilities - TODO: Implement when correct component type is identified
                // if (entityManager.HasComponent<UnlockedAbilityElement>(characterEntity))
                // {
                //     var ab = entityManager.GetBuffer<UnlockedAbilityElement>(characterEntity);
                //     Plugin.Logger?.LogInfo($"[RESTORE] Clearing {ab.Length} abilities, restoring {UnlockedAbilities.Count}");
                //     ab.Clear();
                //     foreach (var ability in UnlockedAbilities)
                //     {
                //         ab.Add(new UnlockedAbilityElement { Guid = ability });
                //     }
                // }

                Plugin.Logger?.LogInfo($"[RESTORE] Abilities restore skipped - component type not available");

                Plugin.Logger?.LogInfo($"[RESTORE] Restoration complete");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[RESTORE] Error: {ex.Message}");
                Plugin.Logger?.LogError($"[RESTORE] Stack: {ex.StackTrace}");
                throw;
            }
        }

        private void RestoreProgression(Entity progressionEntity)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                
                // Clear and restore research - TODO: Implement when correct component type is identified
                // if (em.HasComponent<UnlockedResearchBuffer>(progressionEntity))
                // {
                //     var researchBuffer = em.GetBuffer<UnlockedResearchBuffer>(progressionEntity);
                //     researchBuffer.Clear();
                //     
                //     foreach (var research in UnlockedResearch)
                //     {
                //         researchBuffer.Add(new UnlockedResearchBuffer { ResearchGuid = research });
                //     }
                // }

                Plugin.Logger?.LogInfo($"Progression restore skipped - component type not available");
                
                Plugin.Logger?.LogInfo($"Restored progression data: {UnlockedResearch.Count} researches");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error restoring progression data: {ex.Message}");
            }
        }

        private void RestoreVBlood(Entity characterEntity)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                if (!em.HasComponent<PlayerCharacter>(characterEntity)) return;
                
                var pc = em.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = pc.UserEntity;
                
                if (userEntity == Entity.Null) return;
                
                // Clear VBlood buffer first
                if (em.HasComponent<VBloodConsumed>(userEntity))
                {
                    var vbloodBuffer = em.GetBuffer<VBloodConsumed>(userEntity);
                    vbloodBuffer.Clear();
                    
                    // Restore original VBloods
                    foreach (var vblood in UnlockedVBlood)
                    {
                        vbloodBuffer.Add(new VBloodConsumed { Source = vblood });
                    }
                }
                
                Plugin.Logger?.LogInfo($"Restored {UnlockedVBlood.Count} VBlood unlocks");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error restoring VBlood unlocks: {ex.Message}");
            }
        }

        private void RestoreAbilities(Entity characterEntity)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                
                // Clear and restore abilities - TODO: Implement when correct component type is identified
                // if (em.HasComponent<UnlockedAbilityElement>(characterEntity))
                // {
                //     var abilityBuffer = em.GetBuffer<UnlockedAbilityElement>(characterEntity);
                //     abilityBuffer.Clear();
                //     
                //     foreach (var ability in UnlockedAbilities)
                //     {
                //         abilityBuffer.Add(new UnlockedAbilityElement { Guid = ability });
                //     }
                // }

                Plugin.Logger?.LogInfo($"Abilities restore skipped - component type not available");
                
                Plugin.Logger?.LogInfo($"Restored {UnlockedAbilities.Count} abilities");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error restoring abilities: {ex.Message}");
            }
        }

        private void RestoreAchievements(Entity characterEntity)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                
                if (!em.HasComponent<PlayerCharacter>(characterEntity)) return;
                var pc = em.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = pc.UserEntity;
                
                // Clear and restore achievements - TODO: Implement when correct component type is identified
                // if (em.HasComponent<AchievementOwner>(userEntity))
                // {
                //     var achievementBuffer = em.GetBuffer<AchievementOwner>(userEntity);
                //     achievementBuffer.Clear();
                //     
                //     foreach (var achievement in CompletedAchievements)
                //     {
                //         achievementBuffer.Add(new AchievementOwner { AchievementGuid = achievement });
                //     }
                // }

                Plugin.Logger?.LogInfo($"Achievements restore skipped - component type not available");
                
                Plugin.Logger?.LogInfo($"Restored {CompletedAchievements.Count} achievements");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error restoring achievements: {ex.Message}");
            }
        }

        private void RestoreMapProgress(Entity userEntity)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                
                // Clear and restore waypoints - TODO: Implement when correct component type is identified
                // if (em.HasComponent<UnlockedWaypointElement>(userEntity))
                // {
                //     var waypointBuffer = em.GetBuffer<UnlockedWaypointElement>(userEntity);
                //     waypointBuffer.Clear();
                //     
                //     foreach (var waypoint in UnlockedWaypoints)
                //     {
                //         waypointBuffer.Add(new UnlockedWaypointElement { Waypoint = waypoint });
                //     }
                // }
                
                // Clear and restore map chunks - TODO: Implement when correct component type is identified
                // if (em.HasComponent<RevealedChunkElement>(userEntity))
                // {
                //     var chunkBuffer = em.GetBuffer<RevealedChunkElement>(userEntity);
                //     chunkBuffer.Clear();
                //     
                //     foreach (var chunk in RevealedMapChunks)
                //     {
                //         chunkBuffer.Add(new RevealedChunkElement { ChunkCoord = chunk });
                //     }
                // }

                Plugin.Logger?.LogInfo($"Map progress restore skipped - component types not available");
                
                Plugin.Logger?.LogInfo($"Restored map progress: {UnlockedWaypoints.Count} waypoints, {RevealedMapChunks.Count} chunks");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error restoring map progress: {ex.Message}");
            }
        }
    }
}