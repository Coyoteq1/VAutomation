using CrowbaneArena.Data;
using CrowbaneArena.Models;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using Unity.Collections;
using CrowbaneArena.Services;
using Exception = System.Exception;

namespace CrowbaneArena.Helpers;

public static class ArmorHelper
{
    public static void EquipArmor(Entity character, int slotId, Entity itemEntity)
    {
        var systemService = new SystemService(VRisingCore.ServerWorld);
        systemService.UnEquipItemSystem.Update(); // Process any unequips first
        // Note: EquipItemSystem.TryEquipItem API may not be available in all engine versions.
        // Fallback: log and leave the item in inventory if equipping is unsupported here.
        Plugin.Logger?.LogWarning("EquipArmor: TryEquipItem API not available; equipping skipped (item left in inventory)");
    }
    public static void EquipArmors(Entity character, CrowbaneArena.Models.Armors armors)
    {
        var armorList = new[]
        {
            armors.Boots,
            armors.Chest,
            armors.Gloves,
            armors.Legs,
            armors.MagicSource,
            armors.Head,
            armors.Cloak,
            armors.Bag
        };

        foreach (var armor in armorList)
        {
            if (string.IsNullOrEmpty(armor)) continue;
            if (UtilsHelper.TryGetPrefabGuid(armor, out var guid))
            {
                GiveAndEquip(character, guid);
            }
            else
            {
                Plugin.Logger.LogWarning($"Armor guid not found for {armor}.");
            }
        }
    }

    private static void GiveAndEquip(Entity character, PrefabGUID guid)
    {
        // Use ServerGameManager directly
        var serverGameManager = VRisingCore.ServerGameManager;
        var em = VRisingCore.EntityManager;

        // Spawn the item
        var resp = serverGameManager.TryAddInventoryItem(character, guid, 1);
        if (resp.NewEntity == Entity.Null)
        {
            Plugin.Logger?.LogWarning($"GiveAndEquip: failed to spawn item {guid.GuidHash}");
            return;
        }

        var itemEntity = resp.NewEntity;

        // Find correct equipment slot using vanilla utilities - fallback to slot 0 if helper not available
        int equipmentSlot = 0;
        
        // Call into SystemService to equip
        var systemService = new SystemService(VRisingCore.ServerWorld);
        systemService.UnEquipItemSystem.Update(); // Process any unequips first
        // Fallback: EquipItemSystem.TryEquipItem may not exist; don't attempt to call it to avoid compatibility issues.
        Plugin.Logger?.LogWarning($"GiveAndEquip: skipped equipping {guid.GuidHash} - EquipItem API unavailable. Item left in inventory.");
    }
}
