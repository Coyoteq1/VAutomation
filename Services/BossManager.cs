using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using CrowbaneArena.Data;
using CrowbaneArena.Services;

namespace CrowbaneArena
{
    /// <summary>
    /// Manages boss encounters, tracking, and rewards for players
    /// </summary>
    public static class BossManager
    {
        private static bool _initialized = false;
        private static readonly Dictionary<ulong, DateTime> _lastBossKill = new();
        private static readonly Dictionary<ulong, string> _currentBossTarget = new();
        private static readonly Dictionary<ulong, List<PrefabGUID>> _originalVBloods = new();
        private static readonly Dictionary<ulong, List<PrefabGUID>> _bossUnlockSnapshots = new();

        // Cooldown between boss kills (in minutes)
        private const int BOSS_KILL_COOLDOWN = 30;


        /// <summary>
        /// Initialize the boss manager
        /// </summary>
        public static bool Initialize()
        {
            try
            {
                if (_initialized)
                {
                    Plugin.Logger?.LogInfo("BossManager already initialized");
                    return true;
                }

                if (!BossService.Initialize())
                {
                    Plugin.Logger?.LogError("Failed to initialize BossService");
                    return false;
                }

                _initialized = true;
                Plugin.Logger?.LogInfo("BossManager initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error initializing BossManager: {ex.Message}");
                Plugin.Logger?.LogError(ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Handle boss kill event
        /// </summary>
        public static void OnBossKilled(Entity killer, Entity bossEntity, PrefabGUID bossPrefab)
        {
            try
            {
                if (!_initialized && !Initialize())
                {
                    Plugin.Logger?.LogError("Cannot process boss kill: BossManager not initialized");
                    return;
                }

                var em = VRisingCore.EntityManager;
                
                // Get killer's Steam ID
                if (!em.TryGetComponentData<PlayerCharacter>(killer, out var playerCharacter) ||
                    !em.TryGetComponentData<User>(playerCharacter.UserEntity, out var user))
                {
                    Plugin.Logger?.LogWarning("Boss killed by non-player entity");
                    return;
                }

                ulong steamId = user.PlatformId;
                string characterName = user.CharacterName.ToString();
                string bossName = bossPrefab.ToString();

                // Check cooldown
                if (IsOnCooldown(steamId))
                {
                    Plugin.Logger?.LogInfo($"Boss kill ignored for {characterName}: On cooldown");
                    return;
                }

                // Update last kill time
                _lastBossKill[steamId] = DateTime.UtcNow;

                // Mark boss as defeated
                if (BossService.DefeatBoss(steamId, bossName))
                {
                    Plugin.Logger?.LogInfo($"{characterName} defeated boss: {bossName}");
                    
                    // Grant rewards (implement your reward system here)
                    GrantBossRewards(killer, bossName);
                    
                    // Update quest progress if needed
                    UpdateQuests(steamId, bossName);
                }

                // Clear current target if it was this boss
                if (_currentBossTarget.TryGetValue(steamId, out var currentTarget) && 
                    string.Equals(currentTarget, bossName, StringComparison.OrdinalIgnoreCase))
                {
                    _currentBossTarget.Remove(steamId);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in OnBossKilled: {ex.Message}");
                Plugin.Logger?.LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Set a boss as the player's current target
        /// </summary>
        public static bool SetBossTarget(ulong steamId, string bossName)
        {
            if (!_initialized && !Initialize()) return false;

            if (!BossService.GetAllBosses().Any(b => 
                string.Equals(b.Name, bossName, StringComparison.OrdinalIgnoreCase)))
            {
                Plugin.Logger?.LogWarning($"Unknown boss: {bossName}");
                return false;
            }

            _currentBossTarget[steamId] = bossName;
            return true;
        }

        /// <summary>
        /// Get the player's current boss target
        /// </summary>
        public static string GetBossTarget(ulong steamId)
        {
            _currentBossTarget.TryGetValue(steamId, out var target);
            return target;
        }

        /// <summary>
        /// Check if a player can fight a boss (cooldown, requirements, etc.)
        /// </summary>
        public static bool CanFightBoss(ulong steamId, string bossName)
        {
            if (!_initialized && !Initialize()) return false;

            // Check cooldown
            if (IsOnCooldown(steamId))
            {
                return false;
            }

            // Check if already defeated (if applicable)
            if (BossService.HasDefeatedBoss(steamId, bossName))
            {
                // Option: Allow re-fighting bosses
                // return false; // If bosses can only be defeated once
            }

            // Add any additional requirements here
            return true;
        }

        private static bool IsOnCooldown(ulong steamId)
        {
            if (_lastBossKill.TryGetValue(steamId, out var lastKill))
            {
                var cooldownEnd = lastKill.AddMinutes(BOSS_KILL_COOLDOWN);
                if (DateTime.UtcNow < cooldownEnd)
                {
                    return true;
                }
            }
            return false;
        }

        private static void GrantBossRewards(Entity playerEntity, string bossName)
        {
            // Implement your reward system here
            // This could include items, currency, achievements, etc.
            var bossInfo = BossService.GetBossInfo(bossName);
            if (bossInfo == null) return;

            // Example: Grant rewards based on boss level/difficulty
            // RewardService.GrantRewards(playerEntity, bossInfo.Rewards);
        }

        private static void UpdateQuests(ulong steamId, string bossName)
        {
            // Update any quests or achievements related to boss kills
            // QuestService.UpdateBossKillQuest(steamId, bossName);
        }

        /// <summary>
        /// Save player's original VBloods
        /// </summary>
        private static void SaveOriginalVBloods(Entity playerEntity, ulong steamId)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                var originalVBloods = new List<PrefabGUID>();

                // COMMENTED OUT: VBlood consumed buffer functionality disabled
                /*
                if (em.HasBuffer<VBloodConsumed>(playerEntity))
                {
                    var vbloodBuffer = em.GetBuffer<VBloodConsumed>(playerEntity);
                    foreach (var vbloodConsumed in vbloodBuffer)
                    {
                        // Access the Source field directly (the VBlood prefab that was consumed)
                        var vbloodGuid = vbloodConsumed.Source;
                        if (vbloodGuid.GuidHash != 0) // Only add valid GUIDs
                        {
                            originalVBloods.Add(vbloodGuid);
                        }
                    }
                }
                */

                _originalVBloods[steamId] = originalVBloods;
                Plugin.Logger?.LogInfo($"Saved {originalVBloods.Count} original VBloods for player {steamId}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error saving original VBloods: {ex.Message}");
            }
        }

        // Removed duplicate RestoreOriginalVBloods method

        /// <summary>
        /// Get all VBlood prefabs
        /// </summary>
        public static List<PrefabGUID> GetVBloodPrefabs()
        {
            try
            {
                var em = VRisingCore.EntityManager;
                var query = em.CreateEntityQuery(ComponentType.ReadOnly<VBloodUnit>());
                var entities = query.ToEntityArray(Allocator.Temp);
                var vbloods = new List<PrefabGUID>();

                foreach (var entity in entities)
                {
                    if (em.HasComponent<PrefabGUID>(entity))
                    {
                        var prefab = em.GetComponentData<PrefabGUID>(entity);
                        vbloods.Add(prefab);
                    }
                }

                entities.Dispose();
                return vbloods;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error getting VBlood prefabs: {ex.Message}");
                return new List<PrefabGUID>();
            }
        }

        /// <summary>
        /// Check if player already has a specific VBlood
        /// </summary>
        private static bool HasVBlood(DynamicBuffer<VBloodConsumed> vbloodBuffer, PrefabGUID vblood)
        {
            try
            {
                foreach (var consumed in vbloodBuffer)
                {
                    // Check the Source field directly (the VBlood prefab that was consumed)
                    if (consumed.Source.GuidHash == vblood.GuidHash)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in HasVBlood: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Restore player's original VBloods when leaving arena
        /// </summary>
        public static void RestoreOriginalVBloods(Entity playerEntity)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                if (!em.Exists(playerEntity) || !em.HasComponent<PlayerCharacter>(playerEntity))
                {
                    Plugin.Logger?.LogError("Cannot restore VBloods: Invalid player entity");
                    return;
                }

                // Get user entity and Steam ID
                var userEntity = em.GetComponentData<PlayerCharacter>(playerEntity).UserEntity;
                if (userEntity == Entity.Null)
                {
                    Plugin.Logger?.LogError("Cannot restore VBloods: User entity not found");
                    return;
                }

                var user = em.GetComponentData<User>(userEntity);
                ulong steamId = user.PlatformId;
                
                if (!_originalVBloods.TryGetValue(steamId, out var originalVBloods))
                {
                    Plugin.Logger?.LogWarning($"No original VBloods found for player {user.CharacterName}");
                    return;
                }

                // Clear current VBloods and restore originals
                // COMMENTED OUT: VBlood consumed buffer functionality disabled
                /*
                if (em.HasBuffer<VBloodConsumed>(playerEntity))
                {
                    var vbloodBuffer = em.GetBuffer<VBloodConsumed>(playerEntity);
                    vbloodBuffer.Clear();

                    // Add back original VBloods
                    foreach (var vblood in originalVBloods)
                    {
                        var consumed = new VBloodConsumed
                        {
                            Source = vblood, // The VBlood prefab that was consumed
                            Target = playerEntity // The player who consumed it
                        };
                        vbloodBuffer.Add(consumed);
                    }

                    Plugin.Logger?.LogInfo($"Restored {originalVBloods.Count} VBloods for player {user.CharacterName}");
                }
                */
                
                // Remove from tracking after restoration
                _originalVBloods.Remove(steamId);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in RestoreOriginalVBloods: {ex.Message}");
                Plugin.Logger?.LogError(ex.StackTrace);
            }

            // Clean up any remaining references
            try
            {
                var em = VRisingCore.EntityManager;
                if (playerEntity != Entity.Null && em.Exists(playerEntity))
                {
                    var userEntity = em.GetComponentData<PlayerCharacter>(playerEntity).UserEntity;
                    if (userEntity != Entity.Null && em.Exists(userEntity))
                    {
                        var user = em.GetComponentData<User>(userEntity);
                        Plugin.Logger?.LogInfo($"VBlood restoration completed for player {user.CharacterName}");
                        return;
                    }
                }
                Plugin.Logger?.LogInfo("VBlood restoration completed");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error during VBlood restoration cleanup: {ex.Message}");
                Plugin.Logger?.LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Take a snapshot of the player's current boss unlock state
        /// </summary>
        public static void SnapshotBossUnlockState(Entity playerEntity)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                if (!em.Exists(playerEntity) || !em.HasComponent<PlayerCharacter>(playerEntity))
                {
                    Plugin.Logger?.LogError("Cannot snapshot boss unlocks: Invalid player entity");
                    return;
                }

                // Get user entity and Steam ID
                var userEntity = em.GetComponentData<PlayerCharacter>(playerEntity).UserEntity;
                if (userEntity == Entity.Null)
                {
                    Plugin.Logger?.LogError("Cannot snapshot boss unlocks: User entity not found");
                    return;
                }

                var user = em.GetComponentData<User>(userEntity);
                ulong steamId = user.PlatformId;

                // Get current unlocked bosses
                var currentUnlocks = new List<PrefabGUID>();
                if (em.HasBuffer<VBloodConsumed>(playerEntity))
                {
                    var vbloodBuffer = em.GetBuffer<VBloodConsumed>(playerEntity);
                    foreach (var vbloodConsumed in vbloodBuffer)
                    {
                        var vbloodGuid = vbloodConsumed.Source;
                        if (vbloodGuid.GuidHash != 0)
                        {
                            currentUnlocks.Add(vbloodGuid);
                        }
                    }
                }

                // Store snapshot
                _bossUnlockSnapshots[steamId] = currentUnlocks;
                Plugin.Logger?.LogInfo($"Snapped {currentUnlocks.Count} boss unlocks for player {user.CharacterName} (SteamID: {steamId})");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SnapshotBossUnlockState: {ex.Message}");
                Plugin.Logger?.LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Unlock all bosses for arena gameplay
        /// </summary>
        public static void UnlockAllBosses(Entity playerEntity)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                if (!em.Exists(playerEntity) || !em.HasComponent<PlayerCharacter>(playerEntity))
                {
                    Plugin.Logger?.LogError("Cannot unlock bosses: Invalid player entity");
                    return;
                }

                // Get user entity and Steam ID
                var userEntity = em.GetComponentData<PlayerCharacter>(playerEntity).UserEntity;
                if (userEntity == Entity.Null)
                {
                    Plugin.Logger?.LogError("Cannot unlock bosses: User entity not found");
                    return;
                }

                var user = em.GetComponentData<User>(userEntity);
                ulong steamId = user.PlatformId;

                Plugin.Logger?.LogInfo($"Unlocking all bosses for arena gameplay: {user.CharacterName} (SteamID: {steamId})");

                // Get all VBlood prefabs
                var allBosses = GetVBloodPrefabs();
                if (allBosses == null || allBosses.Count == 0)
                {
                    Plugin.Logger?.LogError("No boss prefabs found to unlock");
                    return;
                }

                // Add all bosses to VBloodConsumed buffer
                if (em.HasBuffer<VBloodConsumed>(playerEntity))
                {
                    var vbloodBuffer = em.GetBuffer<VBloodConsumed>(playerEntity);

                    foreach (var boss in allBosses)
                    {
                        if (!HasVBlood(vbloodBuffer, boss))
                        {
                            try
                            {
                                var consumed = new VBloodConsumed
                                {
                                    Source = boss,
                                    Target = playerEntity
                                };
                                vbloodBuffer.Add(consumed);
                                Plugin.Logger?.LogDebug($"Unlocked boss {boss.GuidHash} for arena gameplay");
                            }
                            catch (Exception ex)
                            {
                                Plugin.Logger?.LogError($"Error unlocking boss {boss.GuidHash}: {ex.Message}");
                            }
                        }
                    }

                    Plugin.Logger?.LogInfo($"Unlocked {allBosses.Count} bosses for arena gameplay: {user.CharacterName}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in UnlockAllBosses: {ex.Message}");
                Plugin.Logger?.LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Restore boss unlock state from snapshot
        /// </summary>
        public static void RestoreBossUnlockState(Entity playerEntity)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                if (!em.Exists(playerEntity) || !em.HasComponent<PlayerCharacter>(playerEntity))
                {
                    Plugin.Logger?.LogError("Cannot restore boss unlocks: Invalid player entity");
                    return;
                }

                // Get user entity and Steam ID
                var userEntity = em.GetComponentData<PlayerCharacter>(playerEntity).UserEntity;
                if (userEntity == Entity.Null)
                {
                    Plugin.Logger?.LogError("Cannot restore boss unlocks: User entity not found");
                    return;
                }

                var user = em.GetComponentData<User>(userEntity);
                ulong steamId = user.PlatformId;

                // Get snapshot
                if (!_bossUnlockSnapshots.TryGetValue(steamId, out var originalUnlocks))
                {
                    Plugin.Logger?.LogWarning($"No boss unlock snapshot found for player {user.CharacterName}");
                    return;
                }

                Plugin.Logger?.LogInfo($"Restoring {originalUnlocks.Count} boss unlocks from snapshot for {user.CharacterName} (SteamID: {steamId})");

                // Clear current VBlood buffer and restore from snapshot
                if (em.HasBuffer<VBloodConsumed>(playerEntity))
                {
                    var vbloodBuffer = em.GetBuffer<VBloodConsumed>(playerEntity);
                    vbloodBuffer.Clear();

                    // Add back only the originally unlocked bosses
                    foreach (var boss in originalUnlocks)
                    {
                        try
                        {
                            var consumed = new VBloodConsumed
                            {
                                Source = boss,
                                Target = playerEntity
                            };
                            vbloodBuffer.Add(consumed);
                            Plugin.Logger?.LogDebug($"Restored boss unlock {boss.GuidHash}");
                        }
                        catch (Exception ex)
                        {
                            Plugin.Logger?.LogError($"Error restoring boss {boss.GuidHash}: {ex.Message}");
                        }
                    }

                    Plugin.Logger?.LogInfo($"Restored {originalUnlocks.Count} boss unlocks for {user.CharacterName}");
                }

                // Clean up snapshot
                _bossUnlockSnapshots.Remove(steamId);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in RestoreBossUnlockState: {ex.Message}");
                Plugin.Logger?.LogError(ex.StackTrace);
            }
        }

        /// <summary>
        /// Get the VBlood service (placeholder for integration)
        /// </summary>
        private static class VBloodManager
        {
            public static void SnapshotBossUnlockState(Entity playerEntity)
            {
                // Placeholder - integrate with actual VBlood service
                Plugin.Logger?.LogInfo("VBloodManager.SnapshotBossUnlockState called (placeholder)");
            }
        }

        private static List<PrefabGUID> GetAllVBloodGUIDs()
        {
            return new List<PrefabGUID>
            {
                new PrefabGUID(-1905691330), new PrefabGUID(-1342764880), new PrefabGUID(1699865363),
                new PrefabGUID(-2025101517), new PrefabGUID(1362041468), new PrefabGUID(-1065970933),
                new PrefabGUID(435934037), new PrefabGUID(-1208888966), new PrefabGUID(1124739990),
                new PrefabGUID(2054432370), new PrefabGUID(-1449631170), new PrefabGUID(1106458752),
                new PrefabGUID(-1347412392), new PrefabGUID(1896428751), new PrefabGUID(-484556888),
                new PrefabGUID(2089106511), new PrefabGUID(-2137261854), new PrefabGUID(1233988687),
                new PrefabGUID(-1391546313), new PrefabGUID(-680831417), new PrefabGUID(114912615),
                new PrefabGUID(-1659822956)
            };
        }
    }
}
