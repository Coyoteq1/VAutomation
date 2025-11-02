using System.Collections.Generic;
using System.Linq;
using CrowbaneArena.Data;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using Exception = System.Exception;

namespace CrowbaneArena.Helpers;

// Commented out - this file appears to be from a different project and has many missing dependencies
/*
internal static class InventoryHelper
{
    public static void GiveItems(Entity character, List<BuildItemData> items)
    {
        foreach (var item in items)
        {
            if (string.IsNullOrEmpty(item.Name)) continue;
            if (UtilsHelper.TryGetPrefabGuid(item.Name, out var itemGuid))
            {
                AddItemToInventory(character, itemGuid, item.Quantity);
            }
            else
            {
                Plugin.LogSource.LogWarning($"Item guid not found for {item.Name}.");
            }
        }
    }

    public static Entity AddItemToInventory(Entity recipient, PrefabGUID guid, int amount)
    {
        try
        {
            var inventoryResponse = CrowbaneArenaCore.EntityManager.TryAddInventoryItem(recipient, guid, amount);
            return inventoryResponse.NewEntity;
        }
        catch (Exception e)
        {
            Plugin.LogSource.LogFatal(e);
        }

        return new Entity();
    }

    public static void EquipEquipment(Entity character, int slot)
    {
        // UtilsHelper.CreateEventFromCharacter(character, new EquipItemEvent { SlotIndex = slot });
    }

    public static void ClearInventory(Entity character)
    {
        var equipment = CrowbaneArenaCore.EntityManager.GetComponentData<Equipment>(character);
        List<Entity> equippedEntity =
        [
            equipment.GetEquipmentEntity(EquipmentType.Headgear).GetEntityOnServer(),
            equipment.GetEquipmentEntity(EquipmentType.Chest).GetEntityOnServer(),
            equipment.GetEquipmentEntity(EquipmentType.Legs).GetEntityOnServer(),
            equipment.GetEquipmentEntity(EquipmentType.Footgear).GetEntityOnServer(),
            equipment.GetEquipmentEntity(EquipmentType.Gloves).GetEntityOnServer(),
            equipment.GetEquipmentEntity(EquipmentType.Cloak).GetEntityOnServer(),
            equipment.GetEquipmentEntity(EquipmentType.MagicSource).GetEntityOnServer(),
            equipment.GetEquipmentEntity(EquipmentType.Bag).GetEntityOnServer(),
            equipment.GetEquipmentEntity(EquipmentType.Weapon).GetEntityOnServer()
        ];

        foreach (var entity in equippedEntity.Where(entity => entity != new Entity()))
        {
            InventoryUtilitiesServer.TryUnEquipItem(CrowbaneArenaCore.EntityManager, character, entity);
        }

        InventoryUtilitiesServer.ClearInventory(CrowbaneArenaCore.EntityManager, character);
        // TODO Clear Jewels
    }
}
*/
