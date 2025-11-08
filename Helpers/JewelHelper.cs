using CrowbaneArena.Data;
using CrowbaneArena.Models;
using ProjectM.Network;
using Stunlock.Core;
using UnityEngine;
using Exception = System.Exception;

namespace CrowbaneArena.Helpers
{
    internal static class JewelHelper
    {
        public static void CreateAndEquip(User user, PrefabGUID abilityGuid, Models.JewelData jewelData)
        {
            var jewelDebugEvent = new CreateJewelDebugEventV2
            {
                AbilityPrefabGuid = abilityGuid,
                Equip = true,
                SpellMod1 = UtilsHelper.GetPrefabGuid(jewelData.SpellMod1) ?? default,
                SpellMod1Power = Mathf.Clamp(jewelData.SpellMod1Power, 0, 1),
                SpellMod2 = UtilsHelper.GetPrefabGuid(jewelData.SpellMod2) ?? default,
                SpellMod2Power = Mathf.Clamp(jewelData.SpellMod2Power, 0, 1),
                SpellMod3 = UtilsHelper.GetPrefabGuid(jewelData.SpellMod3) ?? default,
                SpellMod3Power = Mathf.Clamp(jewelData.SpellMod3Power, 0, 1),
                SpellMod4 = UtilsHelper.GetPrefabGuid(jewelData.SpellMod4) ?? default,
                SpellMod4Power = Mathf.Clamp(jewelData.SpellMod4Power, 0, 1),
                Tier = 3
            };

            // Note: Requires DebugEventsSystem access
            // VRisingCore.DebugEventsSystem.CreateJewelEvent(user.Index, ref jewelDebugEvent);
            Plugin.Logger.LogWarning("CreateAndEquip requires DebugEventsSystem access - not implemented in CrowbaneArena");
        }
    }
}
