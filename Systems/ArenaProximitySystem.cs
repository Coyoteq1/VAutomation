using System;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using CrowbaneArena.Services;
using CrowbaneArena.Data;
using CrowbaneArena.Helpers;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;

namespace CrowbaneArena.Systems
{
    /// <summary>
    /// Periodically checks player distance to the arena center and triggers enter/exit actions.
    /// </summary>
    internal class ArenaProximitySystem : SystemBase
    {
        public static float3 ArenaCenter { get; set; } = new float3(0, 0, 0);
        public static float EnterRadius { get; set; } = 50f;
        public static float ExitRadius { get; set; } = 75f;

        private double _lastUpdate;
        public static double UpdateIntervalSeconds { get; set; } = 2.0;

        public override void OnCreate()
        {
            base.OnCreate();
            _lastUpdate = 0;
        }

        public override void OnUpdate()
        {
            var now = Time.ElapsedTime;
            if (now - _lastUpdate < UpdateIntervalSeconds) return;
            _lastUpdate = now;

            var center = ArenaCenter;
            var enterSqr = EnterRadius * EnterRadius;
            var exitSqr = ExitRadius * ExitRadius;

            var query = GetEntityQuery(ComponentType.ReadOnly<ProjectM.PlayerCharacter>());
            var entities = query.ToEntityArray(Allocator.TempJob);
            
            foreach (var entity in entities)
            {
                if (!EntityManager.HasComponent<ProjectM.PlayerCharacter>(entity)) continue;
                var pc = EntityManager.GetComponentData<ProjectM.PlayerCharacter>(entity);
                
                var pos = EntityManager.GetComponentData<Translation>(entity).Value;
                var distSqr = math.distancesq(pos, center);
                var userEntity = pc.UserEntity;
                if (userEntity == Entity.Null) continue;
                var user = EntityManager.GetComponentData<User>(userEntity);
                var pid = user.PlatformId;

                bool inArena = GameSystems.IsPlayerInArena(pid);

                if (!inArena && distSqr <= enterSqr)
                {
                    try
                    {
                        // Execute full enter flow
                        var spawn = ZoneManager.SpawnPoint;
                        // Resolve loadout: per-player AutoEnterService -> first enabled config loadout -> "default"
                        var loadoutName = ArenaConfigurationService.GetEnabledLoadouts().FirstOrDefault()?.Name ?? "default";

                        // Create snapshot and teleport in
                        if (SnapshotService.EnterArena(userEntity, entity, spawn, loadoutName))
                        {
                            // Prepare character identity/blood
                            try
                            {
                                // Prefix name with [PVP] (do not duplicate if already tagged)
                                var userData = EntityManager.GetComponentData<User>(userEntity);
                                var currentName = userData.CharacterName.ToString();
                                if (!currentName.StartsWith("[PVP]", StringComparison.Ordinal))
                                {
                                    userData.CharacterName = new FixedString64Bytes($"[PVP] {currentName}");
                                    EntityManager.SetComponentData(userEntity, userData);
                                }

                                // Set Rogue 100% blood
                                var rogueGuid = BloodTypeGUIDs.GetBloodTypeGUID("rogue");
                                BloodHelper.SetBloodType(entity, rogueGuid, 100f);
                            }
                            catch { }

                            ZoneManager.ManualEnterArena(entity);
                            GameSystems.MarkPlayerEnteredArena(pid);

                            // Send chat messages
                            try
                            {
                                ChatHelper.SendSystemMessages(EntityManager, user,
                                    "==============================",
                                    "You have entered the PVP practice area.",
                                    "All spells are available. Your blood is set to 100% (Rogue by default).",
                                    "Your progression will not be affected. Your real state will be restored on exit.",
                                    "Use .blood preset <type> to switch blood (rogue|warrior|scholar|creature|mutant|dracula|corrupted).",
                                    "=============================="
                                );
                            }
                            catch { }

                            Plugin.Logger?.LogInfo($"[Proximity] Auto-entered arena for {user.CharacterName} (within {EnterRadius}m)");
                        }
                        else
                        {
                            Plugin.Logger?.LogWarning($"[Proximity] EnterArena failed for {user.CharacterName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger?.LogError($"[Proximity] Auto-enter error: {ex.Message}");
                    }
                }
                else if (inArena && distSqr >= exitSqr)
                {
                    try
                    {
                        if (SnapshotService.ExitArena(userEntity, entity))
                        {
                            ZoneManager.ManualExitArena(entity);
                            GameSystems.MarkPlayerExitedArena(pid);

                            // Send chat messages
                            try
                            {
                                ChatHelper.SendSystemMessages(EntityManager, user,
                                    "==============================",
                                    "You have left the PVP practice area.",
                                    "Your original stats, inventory, and unlocks have been restored.",
                                    "=============================="
                                );
                            }
                            catch { }

                            Plugin.Logger?.LogInfo($"[Proximity] Auto-exited arena for {user.CharacterName} (beyond {ExitRadius}m)");
                        }
                        else
                        {
                            Plugin.Logger?.LogWarning($"[Proximity] ExitArena failed for {user.CharacterName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger?.LogError($"[Proximity] Auto-exit error: {ex.Message}");
                    }
                }
            }
            
            entities.Dispose();
        }
    }
}
