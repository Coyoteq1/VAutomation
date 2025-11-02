using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service for managing player equipment and loadouts
    /// </summary>
    public static class EquipmentService
    {
        private static EntityManager EM => VRisingCore.EntityManager;
        private static ServerGameManager SGM => VRisingCore.ServerGameManager;

        /// <summary>
        ///     Apply a complete loadout to a player
        /// </summary>
        public static bool ApplyLoadout(Entity player, string loadoutName)
        {
            try
            {
                if (!ValidatePlayer(player))
                    return false;

                var loadout = ArenaConfigLoader.GetLoadout(loadoutName);
                if (loadout == null)
                {
                    Plugin.Logger?.LogWarning($"Loadout {loadoutName} not found");
                    return false;
                }

                // Clear existing equipment first
                RemoveAllEquipment(player);

                // Process weapons
                if (loadout.Weapons != null)
                    foreach (var weaponName in loadout.Weapons)
                        if (ArenaConfigLoader.TryGetWeaponVariant(weaponName, out var weapon, out var variant))
                        {
                            if (variant != null)
                                GiveItem(player, new PrefabGUID(variant.VariantGuid));
                            else if (weapon != null && weapon.Guid != 0)
                                GiveItem(player, new PrefabGUID(weapon.Guid));
                            else
                                Plugin.Logger?.LogWarning($"Weapon '{weaponName}' has no valid GUID");
                        }
                        else
                        {
                            Plugin.Logger?.LogWarning($"Weapon '{weaponName}' not found");
                        }

                // Process armor sets
                if (loadout.ArmorSets != null)
                    foreach (var armorSetName in loadout.ArmorSets)
                        if (ArenaConfigLoader.TryGetArmorSet(armorSetName, out var armorSet) && armorSet != null)
                        {
                            // Give each piece of the armor set
                            if (armorSet.ChestGuid != 0)
                                GiveItem(player, new PrefabGUID((int)armorSet.ChestGuid));
                            if (armorSet.LegsGuid != 0)
                                GiveItem(player, new PrefabGUID((int)armorSet.LegsGuid));
                            if (armorSet.GlovesGuid != 0)
                                GiveItem(player, new PrefabGUID((int)armorSet.GlovesGuid));
                            if (armorSet.BootsGuid != 0)
                                GiveItem(player, new PrefabGUID((int)armorSet.BootsGuid));
                        }
                        else
                        {
                            Plugin.Logger?.LogWarning($"Armor set '{armorSetName}' not found or invalid");
                        }

                // Process consumables
                if (loadout.Consumables != null)
                    foreach (var consumableName in loadout.Consumables)
                        if (ArenaConfigLoader.TryGetConsumable(consumableName, out var consumable) &&
                            consumable != null)
                            GiveItem(player, new PrefabGUID((int)consumable.Guid), consumable.DefaultAmount);
                        else
                            Plugin.Logger?.LogWarning($"Consumable '{consumableName}' not found");

                Plugin.Logger?.LogInfo($"Successfully applied loadout {loadoutName} to player");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error applying loadout: {ex.Message}\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        ///     Give a specific item to the player
        /// </summary>
        public static bool GiveItem(Entity player, PrefabGUID itemGuid, int amount = 1)
        {
            try
            {
                if (!ValidatePlayer(player))
                    return false;

                // Use ServerGameManager to add item properly
                var response = SGM.TryAddInventoryItem(player, itemGuid, amount);

                if (response.NewEntity != Entity.Null)
                {
                    Plugin.Logger?.LogInfo($"Gave item {itemGuid} x{amount} to player");
                    return true;
                }
                else
                {
                    Plugin.Logger?.LogWarning($"Failed to give item {itemGuid} to player");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error giving item: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Remove all equipment from a player
        /// </summary>
        public static bool RemoveAllEquipment(Entity player)
        {
            try
            {
                if (!ValidatePlayer(player))
                    return false;

                // Get inventory entity
                if (!InventoryUtilities.TryGetInventoryEntity(EM, player, out var inventoryEntity))
                {
                    Plugin.Logger?.LogWarning("Could not get inventory entity for clearing");
                    return false;
                }

                if (!EM.TryGetBuffer<InventoryBuffer>(inventoryEntity, out var inventoryBuffer))
                {
                    Plugin.Logger?.LogWarning("Could not get inventory buffer for clearing");
                    return false;
                }

                // Clear each slot
                var clearedCount = 0;
                for (var i = 0; i < inventoryBuffer.Length; i++)
                {
                    var item = inventoryBuffer[i];
                    if (item.ItemEntity._Entity != Entity.Null && item.Amount > 0)
                        if (EM.HasComponent<PrefabGUID>(item.ItemEntity._Entity))
                        {
                            EM.DestroyEntity(item.ItemEntity._Entity);
                            clearedCount++;
                        }
                }

                Plugin.Logger?.LogInfo($"Removed {clearedCount} items from player inventory/equipment");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error removing equipment: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Equip a specific weapon from the JSON configuration by name
        /// </summary>
        public static bool EquipWeapon(Entity player, string weaponName)
        {
            try
            {
                if (!ValidatePlayer(player))
                    return false;

                if (ArenaConfigLoader.TryGetWeaponVariant(weaponName, out var weapon, out var variant))
                {
                    PrefabGUID itemGuid;
                    if (variant != null)
                        itemGuid = new PrefabGUID(variant.VariantGuid);
                    else if (weapon != null && weapon.Guid != 0)
                        itemGuid = new PrefabGUID(weapon.Guid);
                    else
                    {
                        Plugin.Logger?.LogWarning($"Weapon '{weaponName}' has no valid GUID");
                        return false;
                    }

                    return GiveItem(player, itemGuid);
                }
                else
                {
                    Plugin.Logger?.LogWarning($"Weapon '{weaponName}' not found in configuration");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error equipping weapon '{weaponName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Equip a specific armor set from the JSON configuration by name
        /// </summary>
        public static bool EquipArmorSet(Entity player, string armorSetName)
        {
            try
            {
                if (!ValidatePlayer(player))
                    return false;

                if (ArenaConfigLoader.TryGetArmorSet(armorSetName, out var armorSet) && armorSet != null)
                {
                    bool success = true;

                    // Equip each piece of the armor set
                    if (armorSet.ChestGuid != 0)
                        success &= GiveItem(player, new PrefabGUID((int)armorSet.ChestGuid));
                    if (armorSet.LegsGuid != 0)
                        success &= GiveItem(player, new PrefabGUID((int)armorSet.LegsGuid));
                    if (armorSet.GlovesGuid != 0)
                        success &= GiveItem(player, new PrefabGUID((int)armorSet.GlovesGuid));
                    if (armorSet.BootsGuid != 0)
                        success &= GiveItem(player, new PrefabGUID((int)armorSet.BootsGuid));

                    if (success)
                        Plugin.Logger?.LogInfo($"Successfully equipped armor set '{armorSetName}' to player");
                    else
                        Plugin.Logger?.LogWarning($"Failed to equip some pieces of armor set '{armorSetName}'");

                    return success;
                }
                else
                {
                    Plugin.Logger?.LogWarning($"Armor set '{armorSetName}' not found in configuration");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error equipping armor set '{armorSetName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Equip a specific consumable from the JSON configuration by name
        /// </summary>
        public static bool EquipConsumable(Entity player, string consumableName)
        {
            try
            {
                if (!ValidatePlayer(player))
                    return false;

                if (ArenaConfigLoader.TryGetConsumable(consumableName, out var consumable) && consumable != null)
                {
                    return GiveItem(player, new PrefabGUID((int)consumable.Guid), consumable.DefaultAmount);
                }
                else
                {
                    Plugin.Logger?.LogWarning($"Consumable '{consumableName}' not found in configuration");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error equipping consumable '{consumableName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     Validate that an entity is a valid player
        /// </summary>
        private static bool ValidatePlayer(Entity player)
        {
            if (player == Entity.Null)
            {
                Plugin.Logger?.LogWarning("Invalid player entity: Null");
                return false;
            }

            if (!EM.Exists(player))
            {
                Plugin.Logger?.LogWarning("Invalid player entity: Does not exist");
                return false;
            }

            if (!EM.HasComponent<PlayerCharacter>(player))
            {
                Plugin.Logger?.LogWarning("Invalid player entity: Not a player character");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    ///     Defines the equipment slot indexes used in the game
    /// </summary>
    public static class EquipmentSlots
    {
        /// <summary>Main weapon slot</summary>
        public const int Weapon = 0;

        /// <summary>Helmet armor slot</summary>
        public const int Helmet = 1;

        /// <summary>Chest armor slot</summary>
        public const int Chest = 2;

        /// <summary>Leg armor slot</summary>
        public const int Legs = 3;

        /// <summary>Gloves armor slot</summary>
        public const int Gloves = 4;

        /// <summary>Boots armor slot</summary>
        public const int Boots = 5;

        /// <summary>Cape slot</summary>
        public const int Cape = 6;

        /// <summary>First accessory slot</summary>
        public const int Accessory1 = 7;

        /// <summary>Second accessory slot</summary>
        public const int Accessory2 = 8;
    }
}