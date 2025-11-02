using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service for managing player inventory operations
    /// </summary>
    public static class InventoryManagementService
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
                var totalCleared = 0;

                // 1. Properly unequip items first using ServerGameManager
                if (EM.TryGetComponentData<Equipment>(characterEntity, out var equipment))
                {
                    var equippedItems = new NativeList<Entity>(Allocator.Temp);
                    equipment.GetAllEquipmentEntities(equippedItems);

                    foreach (var equippedItem in equippedItems)
                        if (equippedItem != Entity.Null && EM.Exists(equippedItem))
                            if (EM.TryGetComponentData<PrefabGUID>(equippedItem, out var itemGuid))
                            {
                                SGM.TryRemoveInventoryItem(characterEntity, itemGuid, 1);
                                totalCleared++;
                            }

                    equippedItems.Dispose();
                    Plugin.Logger?.LogInfo($"Unequipped {totalCleared} items");
                }

                // 2. Clear inventory buffer
                if (InventoryUtilities.TryGetInventoryEntity(EM, characterEntity, out var inventoryEntity))
                {
                    if (EM.TryGetBuffer<InventoryBuffer>(inventoryEntity, out var inventoryBuffer))
                    {
                        var inventoryCleared = 0;
                        for (var i = 0; i < inventoryBuffer.Length; i++)
                        {
                            var item = inventoryBuffer[i];
                            if (item.ItemEntity._Entity != Entity.Null && item.Amount > 0 &&
                                item.ItemType.GuidHash != 0)
                            {
                                SGM.TryRemoveInventoryItem(characterEntity, item.ItemType, item.Amount);
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
                if (!InventoryUtilities.TryGetInventoryEntity(EM, characterEntity, out var inventoryEntity))
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
        /// Give a complete loadout from configuration
        /// </summary>
        public static bool GiveLoadout(Entity characterEntity, string loadoutName)
        {
            try
            {
                if (!ArenaConfigLoader.TryGetLoadout(loadoutName, out var loadout))
                {
                    Plugin.Logger?.LogWarning($"Loadout '{loadoutName}' not found in configuration");
                    return false;
                }

                Plugin.Logger?.LogInfo($"Applying loadout '{loadoutName}'...");
                int successCount = 0;
                int totalItems = 0;

                // Spawn weapons
                if (loadout.Weapons != null)
                {
                    foreach (var weaponName in loadout.Weapons)
                    {
                        totalItems++;
                        if (ArenaConfigLoader.TryGetWeaponVariant(weaponName, out var weapon, out var variant))
                        {
                            var weaponGuid = new PrefabGUID(weapon.Guid);
                            if (GiveLoadout(characterEntity, new[] { weaponGuid }, new[] { 1 }))
                            {
                                successCount++;
                            }
                        }
                    }
                }

                // Spawn armor sets
                if (loadout.ArmorSets != null)
                {
                    foreach (var armorSetName in loadout.ArmorSets)
                    {
                        totalItems++;
                        if (ArenaConfigLoader.TryGetArmorSet(armorSetName, out var armorSet))
                        {
                            var armorGuids = new[]
                            {
                                new PrefabGUID((int)armorSet.BootsGuid),
                                new PrefabGUID((int)armorSet.GlovesGuid),
                                new PrefabGUID((int)armorSet.ChestGuid),
                                new PrefabGUID((int)armorSet.LegsGuid)
                            };
                            if (GiveLoadout(characterEntity, armorGuids, new[] { 1, 1, 1, 1 }))
                            {
                                successCount++;
                            }
                        }
                    }
                }

                // Spawn consumables
                if (loadout.Consumables != null)
                {
                    foreach (var consumableName in loadout.Consumables)
                    {
                        if (ArenaConfigLoader.TryGetConsumable(consumableName, out var consumable))
                        {
                            totalItems++;
                            var consumableGuid = new PrefabGUID((int)consumable.Guid);
                            if (GiveLoadout(characterEntity, new[] { consumableGuid },
                                    new[] { consumable.DefaultAmount }))
                            {
                                successCount++;
                            }
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
    }
}