using System.Collections.Generic;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using UnityEngine;
using Exception = System.Exception;

namespace CrowbaneArena.Helpers;

internal static class BloodHelper
{
    public static void SetBloodType(Entity character, PrefabGUID bloodTypeGuid, float quality = 100f)
    {
        try
        {
            if (!VRisingCore.EntityManager.HasComponent<Blood>(character))
            {
                Plugin.Logger.LogWarning("Character does not have Blood component");
                return;
            }

            var blood = VRisingCore.EntityManager.GetComponentData<Blood>(character);
            blood.BloodType = bloodTypeGuid;
            blood.Quality = Mathf.Clamp(quality, 0f, 100f);

            VRisingCore.EntityManager.SetComponentData(character, blood);
            Plugin.Logger.LogInfo($"Set blood type {bloodTypeGuid} with quality {quality}%");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"Error setting blood type: {ex.Message}");
        }
    }

    public static void SetBloodQuality(Entity character, float quality)
    {
        try
        {
            if (!VRisingCore.EntityManager.HasComponent<Blood>(character))
            {
                Plugin.Logger.LogWarning("Character does not have Blood component");
                return;
            }

            var blood = VRisingCore.EntityManager.GetComponentData<Blood>(character);
            blood.Quality = Mathf.Clamp(quality, 0f, 100f);

            VRisingCore.EntityManager.SetComponentData(character, blood);
            Plugin.Logger.LogInfo($"Set blood quality to {quality}%");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"Error setting blood quality: {ex.Message}");
        }
    }

    public static BloodData GetBloodData(Entity character)
    {
        try
        {
            if (!VRisingCore.EntityManager.HasComponent<Blood>(character))
            {
                return new BloodData { BloodType = PrefabGUID.Empty, Quality = 0f };
            }

            var blood = VRisingCore.EntityManager.GetComponentData<Blood>(character);
            return new BloodData
            {
                BloodType = blood.BloodType,
                Quality = blood.Quality
            };
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"Error getting blood data: {ex.Message}");
            return new BloodData { BloodType = PrefabGUID.Empty, Quality = 0f };
        }
    }

    public static bool TryGetBloodTypeGuid(string bloodTypeName, out PrefabGUID bloodGuid)
    {
        // Common blood type mappings - you may need to adjust these GUIDs
        var bloodTypeMap = new Dictionary<string, PrefabGUID>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["warrior"] = new PrefabGUID(-123456789), // Replace with actual GUIDs
            ["rogue"] = new PrefabGUID(-123456788),
            ["brute"] = new PrefabGUID(-123456787),
            ["scholar"] = new PrefabGUID(-123456786),
            ["creature"] = new PrefabGUID(-123456785),
            ["worker"] = new PrefabGUID(-123456784),
            ["mutated"] = new PrefabGUID(-123456783),
            ["default"] = new PrefabGUID(-123456782)
        };

        if (bloodTypeMap.TryGetValue(bloodTypeName, out bloodGuid))
        {
            return true;
        }

        // Fallback to UtilsHelper for dynamic lookup
        return UtilsHelper.TryGetPrefabGuid(bloodTypeName, out bloodGuid);
    }

    public static void ApplyBloodStats(Entity character, string bloodType, string statFocus, float quality = 100f)
    {
        try
        {
            if (!TryGetBloodTypeGuid(bloodType, out var bloodGuid))
            {
                Plugin.Logger.LogWarning($"Unknown blood type: {bloodType}");
                return;
            }

            SetBloodType(character, bloodGuid, quality);

            // Apply stat modifications based on focus
            // This would require additional stat modification logic
            // For now, just log the intent
            Plugin.Logger.LogInfo($"Applied {bloodType} blood with {statFocus} focus at {quality}% quality");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"Error applying blood stats: {ex.Message}");
        }
    }

    public static void ResetBloodToDefault(Entity character)
    {
        try
        {
            // Set to default blood type with 0 quality
            var defaultBloodGuid = new PrefabGUID(0); // Default blood GUID
            SetBloodType(character, defaultBloodGuid, 0f);
            Plugin.Logger.LogInfo("Reset blood to default");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError($"Error resetting blood: {ex.Message}");
        }
    }
}

public struct BloodData
{
    public PrefabGUID BloodType;
    public float Quality;
}
