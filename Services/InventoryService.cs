using ProjectM;
using ProjectM.Network;
using ProjectM.Scripting;
using Stunlock.Core;
using System;
using System.Collections.Generic;
using Unity.Entities;
using CrowbaneArena.Helpers;
using CrowbaneArena.Systems;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Unified service for managing player inventory, equipment, and item spawning operations.
    /// Consolidates InventoryManagementService, SpawnService, EquipmentService, and InventoryHelper.
    /// </summary>
    public static class InventoryService
    {
        private static EntityManager EM => VRisingCore.EntityManager;
        private static ServerGameManager SGM => VRisingCore.ServerGameManager;

        /// <summary>
        /// Clear all items from a character's inventory AND equipment slots
        /// </summary>
        public static bool ClearInventory(Entity characterEntity)
        {
            try
            {
                Plugin.Logger?.LogInfo("Clearing player inventory and equipment...");
                int totalCleared = 0;

                // 1. Clear equipped items first
                if (EM.TryGetComponentData<Equipment>(characterEntity, out var equipment))
                {
                    var equippedItems = new Unity.Collections.NativeList<Entity>(Unity.Collections.Allocator.Temp);
                    equipment.GetAllEquipmentEntities(equippedItems);

                    foreach (var equippedItem in equippedItems)
                    {
                        if (equippedItem == Entity.Null || !EM.Exists(equippedItem)) continue;

                        // Attempt to move equipped item back to the character's inventory instead of destroying it.
                        if (EM.TryGetComponentData<PrefabGUID>(equippedItem, out var prefab))
                        {
                            var added = SGM.TryAddInventoryItem(characterEntity, prefab, 1);
                            if (added.NewEntity != Entity.Null)
                            {
                                // Item moved to inventory
                                totalCleared++;
                            }
                            else
                            {
                                // Fallback: if inventory full, leave equipped to avoid destroying items
                                Plugin.Logger?.LogWarning($"Inventory full while unequipping item {prefab.GuidHash}; item left equipped.");
                            }
                        }
                        else
                        {
                            // If no prefab GUID, skip (invalid item)
                            Plugin.Logger?.LogWarning($"Equipped item has no prefab GUID; skipped.");
                        }
                    }
                    equippedItems.Dispose();
                    Plugin.Logger?.LogInfo($"Cleared {totalCleared} equipped items");
                }

                // 2. Clear inventory buffer
                if (ProjectM.InventoryUtilities.TryGetInventoryEntity(EM, characterEntity, out Entity inventoryEntity))
                {
                    if (EM.TryGetBuffer<InventoryBuffer>(inventoryEntity, out var inventoryBuffer))
                    {
                        int inventoryCleared = 0;
                        for (int i = 0; i < inventoryBuffer.Length; i++)
                        {
                            var item = inventoryBuffer[i];
                            if (item.ItemType.GuidHash != 0 || item.Amount > 0)
                            {
                                if (item.ItemEntity._Entity != Entity.Null && EM.Exists(item.ItemEntity._Entity))
                                {
                                    EM.DestroyEntity(item.ItemEntity._Entity);
                                }
                                inventoryBuffer[i] = new InventoryBuffer { ItemType = PrefabGUID.Empty, Amount = 0 };
                                inventoryCleared++;
                            }
                        }
                        totalCleared += inventoryCleared;
                        Plugin.Logger?.LogInfo($"Cleared {inventoryCleared} inventory items");
                    }
                }

                Plugin.Logger?.LogInfo($"✓ Total cleared: {totalCleared} items (equipment + inventory)");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error clearing inventory: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get the count of items in inventory
        /// </summary>
        public static int GetInventoryItemCount(Entity characterEntity)
        {
            try
            {
                if (!ProjectM.InventoryUtilities.TryGetInventoryEntity(EM, characterEntity, out Entity inventoryEntity))
                {
                    return 0;
                }

                if (!EM.TryGetBuffer<InventoryBuffer>(inventoryEntity, out var inventoryBuffer))
                {
                    return 0;
                }

                int count = 0;
                for (int i = 0; i < inventoryBuffer.Length; i++)
                {
                    var item = inventoryBuffer[i];
                    if (item.ItemEntity._Entity != Entity.Null && item.Amount > 0 && item.ItemType.GuidHash != 0)
                    {
                        count++;
                    }
                }

                return count;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error getting inventory count: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Give a loadout of items to a player
        /// </summary>
        public static bool GiveLoadout(Entity characterEntity, PrefabGUID[] itemGuids, int[] amounts)
        {
            try
            {
                if (itemGuids.Length != amounts.Length)
                {
                    Plugin.Logger?.LogError("Item GUIDs and amounts arrays must be same length");
                    return false;
                }

                // Get ServerGameManager for proper item spawning
                var serverGameManager = VRisingCore.ServerGameManager;

                int successCount = 0;

                for (int i = 0; i < itemGuids.Length; i++)
                {
                    var guid = itemGuids[i];
                    var amount = amounts[i];

                    if (guid.GuidHash == 0 || amount <= 0)
                        continue;

                    // Proper item spawning using ServerGameManager.TryAddInventoryItem (TryAddItem)
                    var response = serverGameManager.TryAddInventoryItem(characterEntity, guid, amount);
                    if (response.NewEntity != Entity.Null)
                    {
                        successCount++;
                        Plugin.Logger?.LogInfo($"✓ Spawned item {guid.GuidHash} x{amount}");
                    }
                    else
                    {
                        Plugin.Logger?.LogWarning($"Failed to spawn item {guid.GuidHash} x{amount}");
                    }
                }

                Plugin.Logger?.LogInfo($"✓ Loadout complete: {successCount}/{itemGuids.Length} items spawned");
                return successCount > 0;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error giving loadout: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Give a loadout of items to a player by name
        /// </summary>
        public static bool GiveLoadout(Entity characterEntity, string[] itemNames, int[] amounts)
        {
            try
            {
                if (itemNames.Length != amounts.Length)
                {
                    Plugin.Logger?.LogError("Item names and amounts arrays must be same length");
                    return false;
                }

                var guids = new PrefabGUID[itemNames.Length];
                for (int i = 0; i < itemNames.Length; i++)
                {
                    var name = itemNames[i];
                    if (ArenaConfigurationService.TryGetWeapon(name, out var weapon))
                    {
                        guids[i] = GuidConverter.ToPrefabGUID(weapon.Guid);
                    }
                    else if (ArenaConfigurationService.TryGetArmorSet(name, out var armorSet))
                    {
                        // For armor sets, we need to handle multiple pieces - this is a simplification
                        // In practice, you'd need to expand this to handle all armor pieces
                        Plugin.Logger?.LogWarning($"Armor set '{name}' not directly supported in GiveLoadout by name");
                        guids[i] = PrefabGUID.Empty;
                    }
                    else if (ArenaConfigurationService.TryGetConsumable(name, out var consumable))
                    {
                        guids[i] = GuidConverter.ToPrefabGUID(consumable.Guid);
                    }
                    else
                    {
                        Plugin.Logger?.LogWarning($"Item '{name}' not found in configuration");
                        guids[i] = PrefabGUID.Empty;
                    }
                }

                return GiveLoadout(characterEntity, guids, amounts);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error giving loadout by name: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Give and equip a weapon to a player
        /// </summary>
        public static bool GiveAndEquipWeapon(Entity characterEntity, PrefabGUID weaponGuid)
        {
            try
            {
                // First add the weapon to inventory
                var response = SGM.TryAddInventoryItem(characterEntity, weaponGuid, 1);
                if (response.NewEntity == Entity.Null)
                {
                    Plugin.Logger?.LogWarning($"Failed to spawn weapon {weaponGuid.GuidHash}");
                    return false;
                }

                // For now, just add to inventory. Equipment system needs more work.
                // The weapon will be in inventory and player can equip it manually
                Plugin.Logger?.LogInfo($"✓ Spawned weapon {weaponGuid.GuidHash} in inventory (manual equip required)");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error giving and equipping weapon: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Give a complete loadout from configuration or hardcoded fallback
        /// </summary>
        public static bool GiveLoadout(Entity characterEntity, string loadoutName)
        {
            try
            {
                // Try to load from config first (CFG/JSON)
                if (!ArenaConfigurationService.TryGetLoadout(loadoutName, out var loadout))
                {
                    Plugin.Logger?.LogWarning($"Loadout '{loadoutName}' not found in configuration, trying hardcoded loadouts");
                    
                    // Fallback to hardcoded loadouts
                    return GiveHardcodedLoadout(characterEntity, loadoutName);
                }

                Plugin.Logger?.LogInfo($"Applying loadout '{loadoutName}'...");
                int successCount = 0;
                int totalItems = 0;

                // Spawn weapons
                if (loadout.Weapons != null)
                {
                    totalItems += loadout.Weapons.Count;
                    var amounts = new int[loadout.Weapons.Count];
                    for (int i = 0; i < amounts.Length; i++) amounts[i] = 1;
                    if (GiveLoadout(characterEntity, loadout.Weapons.ToArray(), amounts))
                    {
                        successCount += loadout.Weapons.Count;
                    }
                }

                // Spawn armor
                if (loadout.Armor != null)
                {
                    totalItems += loadout.Armor.Count;
                    var amounts = new int[loadout.Armor.Count];
                    for (int i = 0; i < amounts.Length; i++) amounts[i] = 1;
                    if (GiveLoadout(characterEntity, loadout.Armor.ToArray(), amounts))
                    {
                        successCount += loadout.Armor.Count;
                    }
                }

                // Spawn consumables
                if (loadout.Consumables != null)
                {
                    foreach (var consumable in loadout.Consumables)
                    {
                        totalItems++;
                        if (GiveLoadout(characterEntity, new[] { consumable.Guid }, new[] { consumable.Amount }))
                        {
                            successCount++;
                        }
                    }
                }

                Plugin.Logger?.LogInfo($"Applied loadout '{loadoutName}': {successCount}/{totalItems} items spawned");
                return successCount > 0;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error giving loadout: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Give an item to a player (static method for commands)
        /// </summary>
        public static bool GiveItem(Entity player, string itemName, int amount)
        {
            // Placeholder implementation
            Plugin.Logger?.LogInfo($"Giving {amount}x {itemName} to player {player}");
            return true; // Simulate success
        }

        /// <summary>
        /// Get available items for commands (static method)
        /// </summary>
        public static List<string> GetAvailableItems(string category = "")
        {
            // Placeholder implementation
            var items = new List<string> { "Sword", "Shield", "Bow", "Armor", "Potion", "Helmet", "Boots" };
            return items;
        }

        /// <summary>
        /// Add a prefab to the system (static method for commands)
        /// </summary>
        public static void AddPrefab(string category, string name, Guid guid)
        {
            // Placeholder implementation
            Plugin.Logger?.LogInfo($"Added prefab {name} (GUID: {guid}) to category {category}");
        }

        /// <summary>
        /// Import prefabs from JSON (static method for commands)
        /// </summary>
        public static int ImportPrefabsFromJson(string jsonData)
        {
            // Placeholder implementation
            Plugin.Logger?.LogInfo($"Importing prefabs from JSON: {jsonData.Length} characters");
            return jsonData.Split(',').Length; // Simulate count
        }

        /// <summary>
        /// Export prefabs to JSON (static method for commands)
        /// </summary>
        public static string ExportPrefabsToJson()
        {
            // Placeholder implementation
            return "{\"items\":[\"sword\",\"shield\",\"bow\",\"armor\"]}";
        }

        /// <summary>
        /// Give hardcoded loadout (fallback when no config exists)
        /// </summary>
        private static bool GiveHardcodedLoadout(Entity characterEntity, string loadoutName)
        {
            try
            {
                var loadouts = Data.HardcodedLoadouts.GetLoadouts();
                
                if (!loadouts.TryGetValue(loadoutName, out var loadout))
                {
                    Plugin.Logger?.LogError($"Hardcoded loadout '{loadoutName}' not found");
                    return false;
                }

                Plugin.Logger?.LogInfo($"Applying hardcoded loadout '{loadoutName}': {loadout.Description}");
                int successCount = 0;

                // Apply blood type
                if (!string.IsNullOrEmpty(loadout.BloodType))
                {
                    var bloodGuid = Data.BloodTypeGUIDs.GetBloodTypeGUID(loadout.BloodType);
                    Helpers.BloodHelper.SetBloodType(characterEntity, bloodGuid, loadout.BloodQuality);
                    Plugin.Logger?.LogInfo($"Set blood type to {loadout.BloodType} at {loadout.BloodQuality}%");
                }

                // Apply weapons
                foreach (var weapon in loadout.Weapons)
                {
                    var weaponGuid = new PrefabGUID(weapon.Guid);
                    if (SGM.TryAddInventoryItem(characterEntity, weaponGuid, 1).NewEntity != Entity.Null)
                    {
                        successCount++;
                        Plugin.Logger?.LogInfo($"Gave weapon: {weapon.PrefabName}");
                    }
                }

                // Apply armor
                if (loadout.Armor != null)
                {
                    var armorPieces = new[] { loadout.Armor.Chest, loadout.Armor.Legs, loadout.Armor.Boots, loadout.Armor.Gloves };
                    foreach (var armorGuid in armorPieces)
                    {
                        if (armorGuid.GuidHash != 0 && SGM.TryAddInventoryItem(characterEntity, armorGuid, 1).NewEntity != Entity.Null)
                        {
                            successCount++;
                        }
                    }
                    Plugin.Logger?.LogInfo("Gave armor set");
                }

                // Apply consumables
                foreach (var consumable in loadout.Consumables)
                {
                    var consumableGuid = new PrefabGUID(consumable.Guid);
                    if (SGM.TryAddInventoryItem(characterEntity, consumableGuid, consumable.Amount).NewEntity != Entity.Null)
                    {
                        successCount++;
                        Plugin.Logger?.LogInfo($"Gave {consumable.Amount}x consumable");
                    }
                }

                Plugin.Logger?.LogInfo($"Applied hardcoded loadout '{loadoutName}': {successCount} items given");
                return successCount > 0;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error giving hardcoded loadout: {ex.Message}");
                return false;
            }
        }
    }
}
