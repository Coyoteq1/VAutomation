using CrowbaneArena.Data;
using CrowbaneArena.Models;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using Exception = System.Exception;

namespace CrowbaneArena.Helpers
{

internal static class AbilityHelper
{
    public static void EquipAbilities(Entity character, User user, Models.Abilities abilities)
    {
        var abilityMappings = new (IAbilityData ability, AbilityTypeEnum slot)[]
        {
            (abilities.Travel, AbilityTypeEnum.Travel),
            (abilities.Ability1, AbilityTypeEnum.SpellSlot1),
            (abilities.Ability2, AbilityTypeEnum.SpellSlot2),
            (abilities.Ultimate, AbilityTypeEnum.Ultimate)
        };

        foreach (var (ability, slot) in abilityMappings)
        {
            if (string.IsNullOrEmpty(ability.Name)) continue;
            if (UtilsHelper.TryGetPrefabGuid(ability.Name, out var guid))
            {
                EquipAbility(character, guid, slot);
                if (ability is AbilityData normalAbility)
                {
                    JewelHelper.CreateAndEquip(user, guid, normalAbility.Jewel);
                }
            }
            else
            {
                Plugin.Logger.LogWarning($"Ability guid not found for {ability.Name}.");
            }
        }
    }

    public static void EquipPassiveSpells(Entity character, Models.PassiveSpells passiveSpells)
    {
        var spells = new[]
        {
            passiveSpells.PassiveSpell1,
            passiveSpells.PassiveSpell2,
            passiveSpells.PassiveSpell3,
            passiveSpells.PassiveSpell4,
            passiveSpells.PassiveSpell5
        };

        for (var i = 0; i < spells.Length; i++)
        {
            if (string.IsNullOrEmpty(spells[i])) continue;
            if (UtilsHelper.TryGetPrefabGuid(spells[i], out var spellGuid))
            {
                EquipSpellPassive(character, spellGuid, i);
            }
            else
            {
                Plugin.Logger.LogWarning($"Passive spell guid not found for {spells[i]}.");
            }
        }
    }

    public static void ClearAbilities(Entity character)
    {
        if (VRisingCore.EntityManager.TryGetBuffer<VBloodAbilityBuffEntry>(character, out var buffer))
        {
            foreach (var abilityEntry in buffer)
            {
                VBloodAbilityUtilities.TryRemoveVBloodAbility(
                    VRisingCore.EntityManager,
                    character,
                    abilityEntry.ActiveAbility);
            }
        }
    }

    public static void ClearPassiveSpells(Entity character)
    {
        if (VRisingCore.EntityManager.TryGetBuffer<ActivePassivesBuffer>(character, out var buffer))
        {
            for (var i = 0; i < buffer.Length; ++i)
            {
                buffer[i] = new ActivePassivesBuffer();
            }
        }
    }

    private static void EquipAbility(Entity character, PrefabGUID guid, AbilityTypeEnum slot)
    {
        try
        {
            // TODO: Implement proper ability equipping system
            // The ActivateVBloodAbilitySystem is not available in this version
            Plugin.Logger.LogWarning("Ability equipping not implemented - ability not equipped");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"Error equipping ability: {ex.Message}");
        }
    }

    private static void EquipSpellPassive(Entity character, PrefabGUID guid, int slot)
    {
        if (VRisingCore.EntityManager.TryGetBuffer<ActivePassivesBuffer>(character, out var buffer))
            buffer[slot] = new ActivePassivesBuffer
            {
                Entity = new Entity(),
                PrefabGuid = guid
            };
    }
}
}
