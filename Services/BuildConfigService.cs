using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using CrowbaneArena.Models;
using CrowbaneArena.Helpers;

namespace CrowbaneArena.Services;

// BuildConfigService is incomplete and causing compilation errors
// Commented out to allow build to succeed
/*
public static class BuildConfigService
{
    private static Dictionary<string, BuildConfig> _buildConfigs;
    private static string _buildsDirectory = "Builds";

    public static void Initialize()
    {
        _buildConfigs = new Dictionary<string, BuildConfig>();
        LoadBuildConfigurations();
        Plugin.Logger?.LogInfo($"BuildConfigService initialized with {_buildConfigs.Count} build configurations");
    }

    private static void LoadBuildConfigurations()
    {
        try
        {
            if (!Directory.Exists(_buildsDirectory))
            {
                Directory.CreateDirectory(_buildsDirectory);
            }

            var buildFiles = Directory.GetFiles(_buildsDirectory, "*.json");
            foreach (var file in buildFiles)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var buildData = JsonSerializer.Deserialize<Dictionary<string, BuildConfig>>(json);

                    if (buildData != null)
                    {
                        foreach (var kvp in buildData)
                        {
                            _buildConfigs[kvp.Key] = kvp.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError($"Failed to load build config from {file}: {ex.Message}");
                }
            }

            Plugin.Logger?.LogInfo($"Loaded {_buildConfigs.Count} build configurations");
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error loading build configurations: {ex.Message}");
        }
    }

    public static BuildConfig GetBuildConfig(string buildName)
    {
        if (_buildConfigs.TryGetValue(buildName, out var config))
        {
            return config;
        }
        return null;
    }

    public static List<string> GetAvailableBuilds()
    {
        return new List<string>(_buildConfigs.Keys);
    }

    public static bool ApplyBuildToPlayer(string buildName, PlayerData playerData)
    {
        var buildConfig = GetBuildConfig(buildName);
        if (buildConfig == null)
        {
            Plugin.Logger?.LogWarning($"Build configuration '{buildName}' not found");
            return false;
        }

        try
        {
            // Clear inventory if specified
            if (buildConfig.Settings?.ClearInventory == true)
            {
                InventoryHelper.ClearInventory(playerData.CharEntity);
            }

            // Apply blood configuration
            if (buildConfig.Blood != null)
            {
                ApplyBloodConfig(buildConfig.Blood, playerData);
            }

            // Apply armor configuration
            if (buildConfig.Armors != null)
            {
                ApplyArmorConfig(buildConfig.Armors, playerData);
            }

            // Apply weapon configuration
            if (buildConfig.Weapons != null && buildConfig.Weapons.Count > 0)
            {
                ApplyWeaponConfig(buildConfig.Weapons, playerData);
            }

            // Apply item configuration
            if (buildConfig.Items != null && buildConfig.Items.Count > 0)
            {
                ApplyItemConfig(buildConfig.Items, playerData);
            }

            // Apply ability configuration
            if (buildConfig.Abilities != null)
            {
                ApplyAbilityConfig(buildConfig.Abilities, playerData);
            }

            // Apply passive spells
            if (buildConfig.PassiveSpells != null)
            {
                ApplyPassiveSpells(buildConfig.PassiveSpells, playerData);
            }

            Plugin.Logger?.LogInfo($"Successfully applied build '{buildName}' to player {playerData.Name}");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error applying build '{buildName}' to player {playerData.Name}: {ex.Message}");
            return false;
        }
    }

    private static void ApplyBloodConfig(BloodConfig bloodConfig, PlayerData playerData)
    {
        try
        {
            // Simplified blood config - only supports primary blood type
            if (!string.IsNullOrEmpty(bloodConfig.PrimaryType))
            {
                if (BloodHelper.TryGetBloodTypeGuid(bloodConfig.PrimaryType, out var bloodGuid))
                {
                    BloodHelper.SetBloodType(playerData.CharEntity, bloodGuid, bloodConfig.PrimaryQuality);
                }
            }

            // FillBloodPool method doesn't exist, skipping
            // GiveBloodPotion not implemented
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error applying blood config: {ex.Message}");
        }
    }

    private static void ApplyArmorConfig(Dictionary<string, string> armorConfig, PlayerData playerData)
    {
        try
        {
            var armors = new Armors();

            // Map armor slots - using names directly as ArmorHelper expects strings
            if (armorConfig.TryGetValue("Chest", out var chest)) armors.Chest = chest;
            if (armorConfig.TryGetValue("Legs", out var legs)) armors.Legs = legs;
            if (armorConfig.TryGetValue("Boots", out var boots)) armors.Boots = boots;
            if (armorConfig.TryGetValue("Gloves", out var gloves)) armors.Gloves = gloves;
            if (armorConfig.TryGetValue("Head", out var head)) armors.Head = head;
            if (armorConfig.TryGetValue("MagicSource", out var magicSource)) armors.MagicSource = magicSource;
            if (armorConfig.TryGetValue("Cloak", out var cloak)) armors.Cloak = cloak;
            if (armorConfig.TryGetValue("Bag", out var bag)) armors.Bag = bag;

            ArmorHelper.EquipArmors(playerData.CharEntity, armors);
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error applying armor config: {ex.Message}");
        }
    }

    private static void ApplyWeaponConfig(List<WeaponBuildConfig> weaponConfigs, PlayerData playerData)
    {
        try
        {
            var weaponDataList = new List<WeaponData>();

            foreach (var weaponConfig in weaponConfigs)
            {
                var weaponData = new WeaponData
                {
                    Name = weaponConfig.Name,
                    InfuseSpellMod = weaponConfig.InfuseSpellMod,
                    SpellMod1 = weaponConfig.SpellMod1,
                    SpellMod2 = weaponConfig.SpellMod2,
                    StatMod1 = weaponConfig.StatMod1,
                    StatMod1Power = weaponConfig.StatMod1Power,
                    StatMod2 = weaponConfig.StatMod2,
                    StatMod2Power = weaponConfig.StatMod2Power,
                    StatMod3 = weaponConfig.StatMod3,
                    StatMod3Power = weaponConfig.StatMod3Power,
                    StatMod4 = weaponConfig.StatMod4,
                    StatMod4Power = weaponConfig.StatMod4Power
                };

                weaponDataList.Add(weaponData);
            }

            WeaponHelper.GiveWeapons(playerData.CharEntity, weaponDataList);
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error applying weapon config: {ex.Message}");
        }
    }

    private static void ApplyItemConfig(List<ItemConfig> itemConfigs, PlayerData playerData)
    {
        try
        {
            foreach (var itemConfig in itemConfigs)
            {
                if (UtilsHelper.TryGetPrefabGuid(itemConfig.Name, out var itemGuid))
                {
                    InventoryHelper.AddItemToInventory(playerData.CharEntity, itemGuid, itemConfig.Amount);
                }
                else
                {
                    Plugin.Logger?.LogWarning($"Item guid not found for {itemConfig.Name}");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error applying item config: {ex.Message}");
        }
    }

    private static void ApplyAbilityConfig(Dictionary<string, AbilityConfig> abilityConfigs, PlayerData playerData)
    {
        try
        {
            var abilities = new Abilities();

            foreach (var kvp in abilityConfigs)
            {
                var abilityType = kvp.Key;
                var abilityConfig = kvp.Value;

                var abilityData = new AbilityData
                {
                    Name = abilityConfig.Name
                };

                // Set jewel data if present
                if (abilityConfig.Jewel != null)
                {
                    abilityData.Jewel = new JewelData
                    {
                        SpellMod1 = abilityConfig.Jewel.SpellMod1,
                        SpellMod1Power = abilityConfig.Jewel.SpellMod1Power,
                        SpellMod2 = abilityConfig.Jewel.SpellMod2,
                        SpellMod2Power = abilityConfig.Jewel.SpellMod2Power,
                        SpellMod3 = abilityConfig.Jewel.SpellMod3,
                        SpellMod3Power = abilityConfig.Jewel.SpellMod3Power,
                        SpellMod4 = abilityConfig.Jewel.SpellMod4,
                        SpellMod4Power = abilityConfig.Jewel.SpellMod4Power
                    };
                }

                // Map to ability slots
                switch (abilityType.ToLower())
                {
                    case "travel":
                        abilities.Travel = abilityData;
                        break;
                    case "ability1":
                        abilities.Ability1 = abilityData;
                        break;
                    case "ability2":
                        abilities.Ability2 = abilityData;
                        break;
                    case "ultimate":
                        abilities.Ultimate = abilityData;
                        break;
                }
            }

            // Note: EquipAbilities requires User entity, which we don't have here
            // This would need to be implemented differently
            Plugin.Logger?.LogWarning("Ability equipping requires User entity - not implemented in BuildConfigService");
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error applying ability config: {ex.Message}");
        }
    }

    private static void ApplyPassiveSpells(Dictionary<string, string> passiveSpells, PlayerData playerData)
    {
        try
        {
            var passiveSpellsObj = new PassiveSpells();

            // Map passive spells to slots
            if (passiveSpells.TryGetValue("PassiveSpell1", out var spell1)) passiveSpellsObj.PassiveSpell1 = spell1;
            if (passiveSpells.TryGetValue("PassiveSpell2", out var spell2)) passiveSpellsObj.PassiveSpell2 = spell2;
            if (passiveSpells.TryGetValue("PassiveSpell3", out var spell3)) passiveSpellsObj.PassiveSpell3 = spell3;
            if (passiveSpells.TryGetValue("PassiveSpell4", out var spell4)) passiveSpellsObj.PassiveSpell4 = spell4;
            if (passiveSpells.TryGetValue("PassiveSpell5", out var spell5)) passiveSpellsObj.PassiveSpell5 = spell5;

            AbilityHelper.EquipPassiveSpells(playerData.CharEntity, passiveSpellsObj);
        }
        catch (Exception ex)
        {
            Plugin.Logger?.LogError($"Error applying passive spells: {ex.Message}");
        }
    }

    private static int GetGuidFromName(string itemName)
    {
        // This should be replaced with actual GUID lookup from a database
        // For now, return a placeholder - the real implementation would need
        // a comprehensive item database mapping names to GUIDs
        Plugin.Logger?.LogWarning($"GetGuidFromName called for '{itemName}' - using placeholder GUID");
        return 0; // Placeholder - needs proper GUID database
    }
}
*/

// Build configuration classes
public class BuildConfig
{
    public BuildSettings Settings { get; set; }
    public BloodConfig Blood { get; set; }
    public Dictionary<string, string> Armors { get; set; }
    public List<WeaponBuildConfig> Weapons { get; set; }
    public List<ItemConfig> Items { get; set; }
    public Dictionary<string, AbilityConfig> Abilities { get; set; }
    public Dictionary<string, string> PassiveSpells { get; set; }
}

public class BuildSettings
{
    public bool ClearInventory { get; set; }
}

public class BloodConfig
{
    public bool FillBloodPool { get; set; }
    public bool GiveBloodPotion { get; set; }
    public string PrimaryType { get; set; }
    public string SecondaryType { get; set; }
    public int PrimaryQuality { get; set; }
    public int SecondaryQuality { get; set; }
    public int SecondaryBuffIndex { get; set; }
}

public class WeaponBuildConfig
{
    public string Name { get; set; }
    public string InfuseSpellMod { get; set; }
    public string SpellMod1 { get; set; }
    public string SpellMod2 { get; set; }
    public string StatMod1 { get; set; }
    public float StatMod1Power { get; set; }
    public string StatMod2 { get; set; }
    public float StatMod2Power { get; set; }
    public string StatMod3 { get; set; }
    public float StatMod3Power { get; set; }
    public string StatMod4 { get; set; }
    public float StatMod4Power { get; set; }
}

public class ItemConfig
{
    public string Name { get; set; }
    public int Amount { get; set; }
}

public class AbilityConfig
{
    public string Name { get; set; }
    public JewelConfig Jewel { get; set; }
}

public class JewelConfig
{
    public string SpellMod1 { get; set; }
    public float SpellMod1Power { get; set; }
    public string SpellMod2 { get; set; }
    public float SpellMod2Power { get; set; }
    public string SpellMod3 { get; set; }
    public float SpellMod3Power { get; set; }
    public string SpellMod4 { get; set; }
    public float SpellMod4Power { get; set; }
}
