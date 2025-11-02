using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutomationSystem.Models;

namespace AutomationSystem.Services
{
    public static class PlayerSnapshotService
    {
        private static Dictionary<Guid, PlayerSnapshot> _snapshots = new();
        private static Dictionary<ulong, Guid> _playerCurrentSnapshot = new();
        private static readonly string SnapshotsPath = Path.Combine("config", "AutomationSystem_Snapshots.json");

        public static void Initialize()
        {
            var loaded = SnapshotManager.LoadSnapshot<Dictionary<Guid, PlayerSnapshot>>(SnapshotsPath);
            if (loaded != null)
            {
                _snapshots = loaded;
                foreach (var kvp in _snapshots)
                    if (kvp.Value.PlatformId != 0)
                        _playerCurrentSnapshot[kvp.Value.PlatformId] = kvp.Key;
            }
            Plugin.Logger?.LogInfo($"PlayerSnapshotService initialized with {_snapshots.Count} snapshots");
        }

        public static bool IsInArena(ulong platformId) =>
            _playerCurrentSnapshot.TryGetValue(platformId, out var id) && 
            _snapshots.TryGetValue(id, out var s) && s.IsInArena;

        public static bool EnterArena(Entity userEntity, Entity characterEntity, float3 arenaLocation)
        {
            if (!EM.TryGetComponentData(userEntity, out User user)) return false;
            if (IsInArena(user.PlatformId)) return true;

            var snapshot = new PlayerSnapshot { PlatformId = user.PlatformId, IsInArena = true };

            snapshot.OriginalName = user.CharacterName.ToString();
            
            if (EM.TryGetComponentData(characterEntity, out Blood blood))
            {
                snapshot.BloodTypeGuid = blood.BloodType.GuidHash;
                snapshot.BloodQuality = blood.Quality;
            }
            
            if (InventoryUtilities.TryGetInventoryEntity(EM, characterEntity, out var invEntity))
            {
                var buffer = EM.GetBuffer<InventoryBuffer>(invEntity);
                for (int i = 0; i < buffer.Length; i++)
                {
                    var itemType = buffer[i].ItemType;
                    if (itemType.GuidHash == 0 || buffer[i].Amount <= 0) continue;

                    AddInventoryStack(snapshot.InventoryItems, itemType.GuidHash, buffer[i].Amount);
                }
            }
            
            if (EM.TryGetComponentData(characterEntity, out Equipment equipment))
            {
                var equipped = new NativeList<Entity>(Allocator.Temp);
                equipment.GetAllEquipmentEntities(equipped);
                foreach (var e in equipped)
                    if (e != Entity.Null && EM.Exists(e) && EM.TryGetComponentData(e, out PrefabGUID guid))
                        snapshot.EquippedItems.Add(new Models.ItemData { ItemGuidHash = guid.GuidHash, Amount = 1 });
                equipped.Dispose();
            }

            _snapshots[snapshot.SnapshotId] = snapshot;
            _playerCurrentSnapshot[user.PlatformId] = snapshot.SnapshotId;
            SnapshotManager.SaveSnapshot(SnapshotsPath, _snapshots);

            // Add [arena] tag to player name for visual identification
            user.CharacterName = $"[arena] {snapshot.OriginalName}";
            EM.SetComponentData(userEntity, user);

            InventoryHelper.ClearAll(characterEntity);
            ApplyLoadout(characterEntity);
            SetArenaBlood(characterEntity);
            TeleportService.Teleport(characterEntity, arenaLocation);

            // Start tracking equipment for no durability loss
            if (TrackPlayerEquipmentService.Instance != null)
            {
                TrackPlayerEquipmentService.Instance.StartTrackingPlayerForNoDurability(characterEntity);
            }

            Plugin.Logger?.LogInfo($"Player {snapshot.OriginalName} entered arena");

            PlatformProgressionService.UnlockAllProgression(userEntity, new[] { "UnlockAllAchievements" });
            return true;
        }

        public static bool LeaveArena(ulong platformId, Entity userEntity, Entity characterEntity)
        {
            if (!_playerCurrentSnapshot.TryGetValue(platformId, out var id)) return false;
            if (!_snapshots.TryGetValue(id, out var snapshot)) return false;

            if (EM.TryGetComponentData(userEntity, out User user))
            {
                user.CharacterName = snapshot.OriginalName;
                EM.SetComponentData(userEntity, user);
            }

            if (EM.TryGetComponentData(characterEntity, out Blood blood))
            {
                blood.BloodType = new PrefabGUID(snapshot.BloodTypeGuid);
                blood.Quality = snapshot.BloodQuality;
                EM.SetComponentData(characterEntity, blood);
            }

            InventoryHelper.ClearAll(characterEntity);
            foreach (var item in snapshot.InventoryItems.Values)
                VRisingCore.ServerGameManager.TryAddInventoryItem(characterEntity, new PrefabGUID(item.ItemGuidHash), item.Amount);
            foreach (var item in snapshot.EquippedItems)
                VRisingCore.ServerGameManager.TryAddInventoryItem(characterEntity, new PrefabGUID(item.ItemGuidHash), item.Amount);

            // Stop tracking equipment durability (restore normal durability loss)
            if (TrackPlayerEquipmentService.Instance != null)
            {
                TrackPlayerEquipmentService.Instance.StopTrackingPlayerForNoDurability(characterEntity);
            }

            _snapshots.Remove(id);
            _playerCurrentSnapshot.Remove(platformId);
            SnapshotManager.SaveSnapshot(SnapshotsPath, _snapshots);

            PlatformProgressionService.LockAllProgression(userEntity, new[] { "UnlockAllAchievements" });

            Plugin.Logger?.LogInfo($"Player {platformId} left arena");
            return true;
        }

        private static void AddInventoryStack(Dictionary<int, Models.ItemData> inventory, int guidHash, int amount)
        {
            if (amount <= 0) return;

            foreach (var entry in inventory)
            {
                if (entry.Value.ItemGuidHash == guidHash)
                {
                    entry.Value.Amount += amount;
                    return;
                }
            }

            var key = inventory.Count == 0 ? 0 : inventory.Keys.Max() + 1;
            inventory[key] = new Models.ItemData
            {
                ItemGuidHash = guidHash,
                Amount = amount
            };
        }

        private static void ApplyLoadout(Entity characterEntity)
        {
            var config = ConfigService.Config;
            var loadout = config?.Loadouts?.FirstOrDefault(l => l.Enabled);
            if (loadout == null) return;

            foreach (var weaponName in loadout.Weapons ?? new List<string>())
            {
                var weapon = config.Weapons?.FirstOrDefault(w => w.Name == weaponName && w.Enabled);
                if (weapon != null)
                    VRisingCore.ServerGameManager.TryAddInventoryItem(characterEntity, new PrefabGUID(weapon.Guid), 1);
            }

            foreach (var armorName in loadout.ArmorSets ?? new List<string>())
            {
                var armor = config.ArmorSets?.FirstOrDefault(a => a.Name == armorName && a.Enabled);
                if (armor != null)
                {
                    VRisingCore.ServerGameManager.TryAddInventoryItem(characterEntity, new PrefabGUID((int)armor.ChestGuid), 1);
                    VRisingCore.ServerGameManager.TryAddInventoryItem(characterEntity, new PrefabGUID((int)armor.LegsGuid), 1);
                    VRisingCore.ServerGameManager.TryAddInventoryItem(characterEntity, new PrefabGUID((int)armor.BootsGuid), 1);
                    VRisingCore.ServerGameManager.TryAddInventoryItem(characterEntity, new PrefabGUID((int)armor.GlovesGuid), 1);
                }
            }

            foreach (var consumableName in loadout.Consumables ?? new List<string>())
            {
                var consumable = config.Consumables?.FirstOrDefault(c => c.Name == consumableName && c.Enabled);
                if (consumable != null)
                    VRisingCore.ServerGameManager.TryAddInventoryItem(characterEntity, new PrefabGUID((int)consumable.Guid), consumable.DefaultAmount);
            }
        }

        private static void SetArenaBlood(Entity characterEntity)
        {
            if (EM.TryGetComponentData(characterEntity, out Blood blood))
            {
                blood.BloodType = new PrefabGUID(1558171501); // Brute (Warrior)
                blood.Quality = 100f;
                EM.SetComponentData(characterEntity, blood);
            }
        }

        public static int GetSnapshotCount() => _snapshots.Count;
    }
}
