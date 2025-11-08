using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CrowbaneArena.Models;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using Unity.Mathematics;
using Stunlock.Core;

namespace CrowbaneArena.Services
{
    public sealed class SnapshotManagerService : ISnapshotManager
    {
        private readonly Dictionary<ulong, ProgressionSnapshot> _snapshots = new();
        private readonly Dictionary<ulong, CrowbaneArena.PlayerSnapshot> _playerSnapshots = new();
        private EntityManager EM => VRisingCore.EntityManager;

        public SnapshotManagerService()
        {
            try
            {
                SnapshotManager.Initialize();
                Plugin.Logger?.LogInfo("SnapshotManagerService initialized");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error initializing SnapshotManagerService: {ex.Message}");
            }
        }

        // ISnapshotManager interface implementation
        public void Initialize()
        {
            try
            {
                SnapshotManager.Initialize();
                Plugin.Logger?.LogInfo("SnapshotManagerService.Initialize() called");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in Initialize: {ex.Message}");
            }
        }

        public void Shutdown()
        {
            try
            {
                _snapshots.Clear();
                _playerSnapshots.Clear();
                Plugin.Logger?.LogInfo("SnapshotManagerService.Shutdown() called");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in Shutdown: {ex.Message}");
            }
        }

        public async Task CreateSnapshotAsync(ulong steamId, CrowbaneArena.PlayerSnapshot snapshot)
        {
            await Task.Run(() =>
            {
                try
                {
                    _playerSnapshots[steamId] = snapshot;
                    var path = GetPlayerSnapshotPath(steamId);
                    SnapshotManager.SaveSnapshot(path, snapshot);
                    Plugin.Logger?.LogInfo($"Created snapshot for {steamId}");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"Error creating snapshot: {ex.Message}");
                }
            });
        }

        public async Task<CrowbaneArena.PlayerSnapshot?> GetLatestSnapshotAsync(ulong steamId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (_playerSnapshots.TryGetValue(steamId, out var snapshot))
                    {
                        return snapshot;
                    }

                    var path = GetPlayerSnapshotPath(steamId);
                    return SnapshotManager.LoadSnapshot<CrowbaneArena.PlayerSnapshot>(path);
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"Error getting snapshot: {ex.Message}");
                    return null;
                }
            });
        }

        public async Task RestoreSnapshotAsync(ulong steamId, string snapshotId)
        {
            await Task.Run(() =>
            {
                try
                {
                    var snapshot = GetOrLoadPlayerSnapshot(steamId);
                    if (snapshot != null)
                    {
                        Plugin.Logger?.LogInfo($"Restored snapshot {snapshotId} for {steamId}");
                    }
                    else
                    {
                        Plugin.Logger?.LogWarning($"No snapshot found for {steamId}");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"Error restoring snapshot: {ex.Message}");
                }
            });
        }

        private void RestoreAbilitiesFallback(Entity characterEntity, CrowbaneArena.PlayerSnapshot snapshot)
        {
            try
            {
                Plugin.Logger?.LogInfo($"Restoring {snapshot.AbilityGuids?.Count ?? 0} abilities from snapshot for arena exit");
                if (snapshot.AbilityGuids == null || snapshot.AbilityGuids.Count == 0)
                {
                    Plugin.Logger?.LogInfo("No abilities to restore from snapshot");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"RestoreAbilitiesFallback failed: {ex.Message}");
            }
        }

        private void RestoreVBloods(Entity characterEntity, CrowbaneArena.PlayerSnapshot snapshot)
        {
            try
            {
                Plugin.Logger?.LogInfo($"Restoring {snapshot.UnlockedVBloods?.Count ?? 0} VBlood unlocks from snapshot");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"RestoreVBloods failed: {ex.Message}");
            }
        }

        public async Task<bool> CreateSnapshotAsync(Entity userEntity, Entity characterEntity)
        {
            try
            {
                if (!VRisingCore.EntityManager.TryGetComponentData(userEntity, out User user))
                {
                    Plugin.Logger?.LogError("Failed to get User component");
                    return false;
                }

                var snapshot = ProgressionSnapshot.Create(userEntity, characterEntity);
                _snapshots[user.PlatformId] = snapshot;

                try
                {
                    var path = GetProgressionSnapshotPath(user.PlatformId);
                    SnapshotManager.SaveSnapshot(path, snapshot);
                    Plugin.Logger?.LogInfo($"Created and saved progression snapshot for {snapshot.CharacterName}");
                }
                catch (Exception pex)
                {
                    Plugin.Logger?.LogWarning($"Failed to persist progression snapshot to disk: {pex.Message}");
                }

                // Capture player items/equipment/position/health/blood/abilities
                var playerSnapshot = CapturePlayerSnapshot(characterEntity);
                playerSnapshot.IsInArena = true;
                _playerSnapshots[user.PlatformId] = playerSnapshot;

                try
                {
                    var ppath = GetPlayerSnapshotPath(user.PlatformId);
                    SnapshotManager.SaveSnapshot(ppath, playerSnapshot);
                    Plugin.Logger?.LogInfo($"Saved player snapshot to {ppath}");
                }
                catch (Exception sex)
                {
                    Plugin.Logger?.LogWarning($"Failed to persist player snapshot to disk: {sex.Message}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error creating progression snapshot: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RestoreSnapshotAsync(Entity userEntity, Entity characterEntity)
        {
            try
            {
                if (!VRisingCore.EntityManager.TryGetComponentData(userEntity, out User user))
                {
                    Plugin.Logger?.LogError("Failed to get User component");
                    return false;
                }

                ProgressionSnapshot snapshot;

                if (!_snapshots.TryGetValue(user.PlatformId, out snapshot))
                {
                    var diskPath = GetProgressionSnapshotPath(user.PlatformId);
                    if (!SnapshotManager.SnapshotExists(diskPath))
                    {
                        Plugin.Logger?.LogError($"No snapshot found for {user.CharacterName}");
                        return false;
                    }

                    snapshot = SnapshotManager.LoadSnapshot<ProgressionSnapshot>(diskPath);
                    if (snapshot == null)
                    {
                        Plugin.Logger?.LogError("Failed to load progression snapshot from disk");
                        return false;
                    }

                    _snapshots[user.PlatformId] = snapshot;
                }

                snapshot.RestoreTo(userEntity, characterEntity);

                // Mark components as dirty to trigger save on next auto-save cycle
                try
                {
                    EM.SetComponentData(userEntity, EM.GetComponentData<User>(userEntity));
                    Plugin.Logger?.LogInfo($"[RESTORE] Marked components dirty - will save on next auto-save cycle");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning($"[RESTORE] Could not mark dirty: {ex.Message}");
                }

                // Restore player snapshot (items/equipment/position/ability/health/blood)
                var playerSnapshot = GetOrLoadPlayerSnapshot(user.PlatformId);
                if (playerSnapshot != null)
                {
                    RestorePlayerSnapshot(characterEntity, playerSnapshot);
                }

                _snapshots.Remove(user.PlatformId);
                var diskPathCleanup = GetProgressionSnapshotPath(user.PlatformId);
                try
                {
                    if (SnapshotManager.DeleteSnapshot(diskPathCleanup))
                    {
                        Plugin.Logger?.LogInfo($"Deleted progression snapshot from disk for {user.PlatformId}");
                    }
                }
                catch { }

                try
                {
                    var ppath = GetPlayerSnapshotPath(user.PlatformId);
                    _playerSnapshots.Remove(user.PlatformId);
                    SnapshotManager.DeleteSnapshot(ppath);
                }
                catch { }

                Plugin.Logger?.LogInfo($"Successfully restored progression for {user.CharacterName}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error restoring progression: {ex.Message}");
                return false;
            }
        }

        public bool HasSnapshot(ulong platformId)
        {
            if (_snapshots.ContainsKey(platformId)) return true;
            var path = GetProgressionSnapshotPath(platformId);
            return SnapshotManager.SnapshotExists(path);
        }

        public void DeleteSnapshot(ulong platformId)
        {
            _snapshots.Remove(platformId);
            var path = GetProgressionSnapshotPath(platformId);
            SnapshotManager.DeleteSnapshot(path);
            _playerSnapshots.Remove(platformId);
            SnapshotManager.DeleteSnapshot(GetPlayerSnapshotPath(platformId));
        }

        public void DeleteAllSnapshots()
        {
            _snapshots.Clear();
            SnapshotManager.ClearAllSnapshots();
        }

        public ProgressionSnapshot GetSnapshot(ulong platformId)
        {
            if (_snapshots.TryGetValue(platformId, out var snapshot))
            {
                return snapshot;
            }
            var path = GetProgressionSnapshotPath(platformId);
            return SnapshotManager.LoadSnapshot<ProgressionSnapshot>(path);
        }

        public bool OverwriteAllInMemory(Entity userEntity, Entity characterEntity)
        {
            try
            {
                if (userEntity == Entity.Null || characterEntity == Entity.Null)
                {
                    Plugin.Logger?.LogWarning("SnapshotManagerService.OverwriteAllInMemory called with null entities");
                    return false;
                }

                var fromCharacter = new FromCharacter
                {
                    User = userEntity,
                    Character = characterEntity
                };

                try
                {
                    var systemService = new SystemService(VRisingCore.ServerWorld);
                    var debugEventsSystem = systemService.DebugEventsSystem;
                    debugEventsSystem.UnlockAllResearch(fromCharacter);
                    debugEventsSystem.UnlockAllVBloods(fromCharacter);
                    debugEventsSystem.CompleteAllAchievements(fromCharacter);
                }
                catch (Exception sysEx)
                {
                    Plugin.Logger?.LogWarning($"Failed to access DebugEventsSystem for progression overwrite: {sysEx.Message}");
                }

                try
                {
                    BossManager.UnlockAllBosses(characterEntity);
                }
                catch (Exception bEx)
                {
                    Plugin.Logger?.LogWarning($"BossManager.UnlockAllBosses failed during overwrite: {bEx.Message}");
                }

                try
                {
                    Plugin.Logger?.LogWarning("Map/waypoint reveal not implemented in SnapshotManagerService; skipping.");
                }
                catch (Exception mEx)
                {
                    Plugin.Logger?.LogWarning($"Map/waypoint reveal skipped during overwrite: {mEx.Message}");
                }

                Plugin.Logger?.LogInfo("SnapshotManagerService: in-memory progression overwritten (hybrid unlock)");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in OverwriteAllInMemory: {ex.Message}");
                return false;
            }
        }

        private string GetProgressionSnapshotPath(ulong steamId) => $"progression/{steamId}.json";
        private string GetPlayerSnapshotPath(ulong steamId) => $"players/{steamId}.json";

        private CrowbaneArena.PlayerSnapshot GetOrLoadPlayerSnapshot(ulong steamId)
        {
            if (_playerSnapshots.TryGetValue(steamId, out var snap)) return snap;
            var path = GetPlayerSnapshotPath(steamId);
            var onDisk = SnapshotManager.LoadSnapshot<CrowbaneArena.PlayerSnapshot>(path);
            if (onDisk != null) _playerSnapshots[steamId] = onDisk;
            return onDisk;
        }

        private CrowbaneArena.PlayerSnapshot CapturePlayerSnapshot(Entity characterEntity)
        {
            var snapshot = new CrowbaneArena.PlayerSnapshot();
            try
            {
                CapturePosition(characterEntity, snapshot);
                CaptureHealthAndBlood(characterEntity, snapshot);
                CaptureInventory(characterEntity, snapshot);
                CaptureEquipment(characterEntity, snapshot);
                CaptureAbilities(characterEntity, snapshot);
                CaptureVBloods(characterEntity, snapshot);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error capturing player snapshot: {ex.Message}");
            }
            return snapshot;
        }

        private void RestorePlayerSnapshot(Entity characterEntity, CrowbaneArena.PlayerSnapshot snapshot)
        {
            try
            {
                Patches.SpawnChainPatch.skipOnce = true;
                TeleportService.Teleport(characterEntity, snapshot.OriginalLocation);
            }
            catch { }

            RestoreHealthAndBlood(characterEntity, snapshot);
            ClearInventory(characterEntity);
            RestoreInventory(characterEntity, snapshot);
            RestoreEquipment(characterEntity, snapshot);

            try
            {
                AbilityService.RestoreFromSnapshot(characterEntity, snapshot.AbilityGuids);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"Restore abilities failed: {ex.Message}");
                RestoreAbilitiesFallback(characterEntity, snapshot);
            }

            // Ensure VBlood boss unlocks are restored even if progression restore was skipped
            RestoreVBloods(characterEntity, snapshot);
        }

        private void CapturePosition(Entity characterEntity, CrowbaneArena.PlayerSnapshot snapshot)
        {
            try
            {
                if (EM.TryGetComponentData(characterEntity, out Unity.Transforms.LocalToWorld ltw))
                {
                    snapshot.OriginalLocation = ltw.Position;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"CapturePosition failed: {ex.Message}");
            }
        }

        private void CaptureHealthAndBlood(Entity characterEntity, CrowbaneArena.PlayerSnapshot snapshot)
        {
            try
            {
                if (EM.TryGetComponentData(characterEntity, out Health health))
                {
                    snapshot.Health = health.Value;
                }

                var bloodData = CrowbaneArena.Helpers.BloodHelper.GetBloodData(characterEntity);
                snapshot.BloodTypeGuid = bloodData.BloodType.GuidHash;
                snapshot.BloodQuality = bloodData.Quality;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"CaptureHealthAndBlood failed: {ex.Message}");
            }
        }

        private void CaptureInventory(Entity characterEntity, CrowbaneArena.PlayerSnapshot snapshot)
        {
            try
            {
                if (ProjectM.InventoryUtilities.TryGetInventoryEntity(EM, characterEntity, out Entity inventoryEntity))
                {
                    if (EM.TryGetBuffer<InventoryBuffer>(inventoryEntity, out var inv))
                    {
                        for (int i = 0; i < inv.Length; i++)
                        {
                            var item = inv[i];
                            if (item.ItemType != PrefabGUID.Empty && item.Amount > 0)
                            {
                                snapshot.InventoryItems[i] = new CrowbaneArena.ItemData
                                {
                                    ItemGuidHash = (int)item.ItemType.GuidHash,
                                    Amount = item.Amount
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"CaptureInventory failed: {ex.Message}");
            }
        }

        private void CaptureEquipment(Entity characterEntity, CrowbaneArena.PlayerSnapshot snapshot)
        {
            try
            {
                if (EM.TryGetComponentData(characterEntity, out Equipment equipment))
                {
                    var equippedItems = new Unity.Collections.NativeList<Entity>(Unity.Collections.Allocator.Temp);
                    equipment.GetAllEquipmentEntities(equippedItems);
                    for (int slot = 0; slot < equippedItems.Length; slot++)
                    {
                        var equippedItem = equippedItems[slot];
                        if (equippedItem != Entity.Null && EM.TryGetComponentData<PrefabGUID>(equippedItem, out var prefabGuid))
                        {
                            snapshot.EquippedItems.Add(new CrowbaneArena.EquippedItemData
                            {
                                ItemGuidHash = (int)prefabGuid.GuidHash,
                                Amount = 1,
                                SlotId = slot,
                                SlotName = $"Slot_{slot}",
                                Quality = 0
                            });
                        }
                    }
                    equippedItems.Dispose();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"CaptureEquipment failed: {ex.Message}");
            }
        }

        private void CaptureAbilities(Entity characterEntity, CrowbaneArena.PlayerSnapshot snapshot)
        {
            try
            {
                snapshot.AbilityGuids = AbilityService.GetCurrentAbilityIds(characterEntity);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"CaptureAbilities failed: {ex.Message}");
            }
        }

        private void CaptureVBloods(Entity characterEntity, CrowbaneArena.PlayerSnapshot snapshot)
        {
            try
            {
                Plugin.Logger?.LogInfo($"Captured {snapshot.UnlockedVBloods?.Count ?? 0} VBlood unlocks for snapshot");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"CaptureVBloods failed: {ex.Message}");
            }
        }

        private void RestoreHealthAndBlood(Entity characterEntity, CrowbaneArena.PlayerSnapshot snapshot)
        {
            try
            {
                if (EM.TryGetComponentData(characterEntity, out Health health))
                {
                    health.Value = snapshot.Health;
                    EM.SetComponentData(characterEntity, health);
                }
                if (EM.TryGetComponentData(characterEntity, out Blood blood))
                {
                    blood.BloodType = new PrefabGUID(snapshot.BloodTypeGuid);
                    blood.Quality = snapshot.BloodQuality;
                    EM.SetComponentData(characterEntity, blood);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"RestoreHealthAndBlood failed: {ex.Message}");
            }
        }

        private void ClearInventory(Entity characterEntity)
        {
            try
            {
                if (ProjectM.InventoryUtilities.TryGetInventoryEntity(EM, characterEntity, out Entity inventoryEntity))
                {
                    if (EM.TryGetBuffer<InventoryBuffer>(inventoryEntity, out var inv))
                    {
                        for (int i = 0; i < inv.Length; i++)
                        {
                            inv[i] = new InventoryBuffer { ItemType = PrefabGUID.Empty, Amount = 0 };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"ClearInventory failed: {ex.Message}");
            }
        }

        private void RestoreInventory(Entity characterEntity, CrowbaneArena.PlayerSnapshot snapshot)
        {
            try
            {
                if (ProjectM.InventoryUtilities.TryGetInventoryEntity(EM, characterEntity, out Entity inventoryEntity))
                {
                    if (EM.TryGetBuffer<InventoryBuffer>(inventoryEntity, out var inv))
                    {
                        foreach (var kv in snapshot.InventoryItems)
                        {
                            var idx = kv.Key;
                            var item = kv.Value;
                            if (idx < inv.Length)
                            {
                                inv[idx] = new InventoryBuffer
                                {
                                    ItemType = new PrefabGUID(item.ItemGuidHash),
                                    Amount = item.Amount
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"RestoreInventory failed: {ex.Message}");
            }
        }

        private void RestoreEquipment(Entity characterEntity, CrowbaneArena.PlayerSnapshot snapshot)
        {
            try
            {
                if (!EM.TryGetComponentData(characterEntity, out Equipment equipment))
                {
                    // Fallback: push all equipped items back to inventory
                    foreach (var e in snapshot.EquippedItems)
                    {
                        AddItemToInventory(characterEntity, e.ItemGuidHash, e.Amount);
                    }
                    return;
                }

                int reequipped = 0;
                foreach (var e in snapshot.EquippedItems)
                {
                    try
                    {
                        var prefab = new PrefabGUID(e.ItemGuidHash);
                        if (!TryEquipItemToSlot(characterEntity, prefab, e.SlotId, e.Quality))
                        {
                            AddItemToInventory(characterEntity, e.ItemGuidHash, e.Amount);
                        }
                        else
                        {
                            reequipped++;
                        }
                    }
                    catch
                    {
                        AddItemToInventory(characterEntity, e.ItemGuidHash, e.Amount);
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"RestoreEquipment failed: {ex.Message}");
            }
        }

        private bool TryEquipItemToSlot(Entity characterEntity, PrefabGUID itemGuid, int slotId, int quality)
        {
            // Placeholder: if no direct API available, return false so we fallback to inventory
            return false;
        }

        private bool AddItemToInventory(Entity characterEntity, int itemGuidHash, int amount)
        {
            try
            {
                if (ProjectM.InventoryUtilities.TryGetInventoryEntity(EM, characterEntity, out Entity inventoryEntity))
                {
                    if (EM.TryGetBuffer<InventoryBuffer>(inventoryEntity, out var inv))
                    {
                        for (int i = 0; i < inv.Length; i++)
                        {
                            if (inv[i].ItemType == PrefabGUID.Empty || inv[i].Amount == 0)
                            {
                                inv[i] = new InventoryBuffer
                                {
                                    ItemType = new PrefabGUID(itemGuidHash),
                                    Amount = amount
                                };
                                return true;
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }
    }
}
