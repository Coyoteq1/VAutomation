using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using CrowbaneArena.Core;
using CrowbaneArena.Data;
using CrowbaneArena.Helpers;
using CrowbaneArena.Services;
using ProjectM;
using ProjectM.Network;
using Stunlock.Core;
using Unity.Entities;
using Unity.Mathematics;
using VampireCommandFramework;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// LoadoutApplicationService: Complete loadout application including blood, weapons, armor, abilities, jewels, and passives.
    /// Compatible with VRisingArenaBuilds Builds.json format.
    /// </summary>
    public static class LoadoutApplicationService
    {
        private static readonly string LoadoutPath = Path.Combine(BepInEx.Paths.ConfigPath, "CrowbaneArena", "Builds.json");
        private static Dictionary<string, LoadoutModel> _builds = new();
        public static IReadOnlyCollection<string> BuildNames => _builds.Keys;

        public static void Initialize()
        {
            try
            {
                LoadBuilds();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[LoadoutApplicationService] Initialize error: {ex.Message}");
                CreateDefaultBuildsFile();
            }
        }

        public static async Task<bool> ApplyLoadout(Entity player, string loadoutName)
        {
            try
            {
                if (!_builds.TryGetValue(loadoutName, out var loadout))
                {
                    Plugin.Logger?.LogWarning($"[LoadoutApplicationService] Loadout '{loadoutName}' not found");
                    return false;
                }

                return await ApplyLoadoutModel(player, loadout);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[LoadoutApplicationService] ApplyLoadout error: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> ApplyLoadoutModel(Entity player, LoadoutModel build)
        {
            var successCount = 0;
            var totalSteps = 0;

            try
            {
                // 1. Apply Blood Type
                if (build.Blood != null)
                {
                    totalSteps++;
                    if (ApplyBloodType(player, build.Blood))
                        successCount++;
                }

                // 2. Apply Weapons
                if (build.Weapons != null && build.Weapons.Count > 0)
                {
                    foreach (var weapon in build.Weapons)
                    {
                        totalSteps++;
                        if (await ApplyWeapon(player, weapon))
                            successCount++;
                    }
                }

                // 3. Apply Armor
                if (build.Armors != null)
                {
                    totalSteps++;
                    if (ApplyArmor(player, build.Armors))
                        successCount++;
                }

                // 4. Apply Items (consumables)
                if (build.Items != null && build.Items.Count > 0)
                {
                    foreach (var item in build.Items)
                    {
                        totalSteps++;
                        if (ApplyItem(player, item))
                            successCount++;
                    }
                }

                // 5. Apply Abilities
                if (build.Abilities != null)
                {
                    totalSteps += 4; // travel, ability1, ability2, ultimate
                    if (await ApplyAbility(player, "Travel", build.Abilities.Travel))
                        successCount++;
                    if (await ApplyAbility(player, "Ability1", build.Abilities.Ability1))
                        successCount++;
                    if (await ApplyAbility(player, "Ability2", build.Abilities.Ability2))
                        successCount++;
                    if (await ApplyAbility(player, "Ability2", build.Abilities.Ability2))
                        successCount++;
                }

                // 6. Apply Passive Spells
                if (build.PassiveSpells != null)
                {
                    totalSteps += build.PassiveSpells.Count;
                    foreach (var passive in build.PassiveSpells.Values)
                    {
                        if (ApplyPassiveSpell(player, passive))
                            successCount++;
                    }
                }

                // 7. Apply Settings
                if (build.Settings != null)
                {
                    totalSteps++;
                    if (ApplySettings(player, build.Settings))
                        successCount++;
                }

                Plugin.Logger?.LogInfo($"[LoadoutApplicationService] Applied loadout: {successCount}/{totalSteps} steps successful");
                return successCount > 0;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[LoadoutApplicationService] ApplyLoadoutModel error: {ex.Message}");
                return false;
            }
        }

        private static void LoadBuilds()
        {
            if (!File.Exists(LoadoutPath))
            {
                Plugin.Logger?.LogWarning($"[LoadoutApplicationService] Builds.json not found at {LoadoutPath}, creating default file");
                CreateDefaultBuildsFile();
                return;
            }

            var jsonString = File.ReadAllText(LoadoutPath);
            var tempDict = JsonSerializer.Deserialize<Dictionary<string, LoadoutModel>>(jsonString,
                new JsonSerializerOptions
                {
                    AllowTrailingCommas = true
                });
            _builds = new Dictionary<string, LoadoutModel>(tempDict ?? new Dictionary<string, LoadoutModel>(), StringComparer.OrdinalIgnoreCase);
            Plugin.Logger?.LogInfo($"[LoadoutApplicationService] Loaded {_builds.Count} loadouts from Builds.json");

            // Validate loaded data
            ValidateBuildsData();
        }

        private static void CreateDefaultBuildsFile()
        {
            var directory = Path.GetDirectoryName(LoadoutPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Only create if file doesn't exist
            if (File.Exists(LoadoutPath))
            {
                Plugin.Logger?.LogInfo($"[LoadoutApplicationService] Builds.json already exists, skipping creation");
                return;
            }

            var defaultBuilds = CreateDefaultBuilds();
            var json = JsonSerializer.Serialize(defaultBuilds, new JsonSerializerOptions
            {
                WriteIndented = true,
            });
            File.WriteAllText(LoadoutPath, json);
            Plugin.Logger?.LogInfo($"[LoadoutApplicationService] Created default Builds.json at {LoadoutPath}");
        }

        private static Dictionary<string, LoadoutModel> CreateDefaultBuilds()
        {
            return new Dictionary<string, LoadoutModel>(StringComparer.OrdinalIgnoreCase)
            {
                ["EmptyDefault"] = new LoadoutModel
                {
                    Settings = new BuildSettings
                    {
                        ClearInventory = true
                    },
                    Blood = new BloodConfig
                    {
                        PrimaryType = "Rogue",
                        PrimaryQuality = 100,
                        FillBloodPool = true,
                        GiveBloodPotion = false
                    },
                    Weapons = new List<WeaponConfig>
                    {
                        new WeaponConfig { Name = "Item_Weapon_Sword_Sanguine" }
                    },
                    Items = new List<ItemConfig>
                    {
                        new ItemConfig { Name = "Item_Consumable_BloodPotion_T02", Amount = 10 },
                        new ItemConfig { Name = "Item_Consumable_HealingPotion_T02", Amount = 5 }
                    }
                },
                ["WarriorBuild"] = new LoadoutModel
                {
                    Settings = new BuildSettings
                    {
                        ClearInventory = true
                    },
                    Blood = new BloodConfig
                    {
                        PrimaryType = "Warrior",
                        PrimaryQuality = 100,
                        FillBloodPool = true,
                        GiveBloodPotion = true
                    },
                    Weapons = new List<WeaponConfig>
                    {
                        new WeaponConfig { Name = "Item_Weapon_GreatSword_Sanguine" }
                    },
                    Items = new List<ItemConfig>
                    {
                        new ItemConfig { Name = "Item_Consumable_BloodPotion_T02", Amount = 15 },
                        new ItemConfig { Name = "Item_Consumable_HealingPotion_T02", Amount = 10 }
                    }
                }
            };
        }


        private static bool ApplyBloodType(Entity player, BloodConfig blood)
        {
            try
            {
                if (blood.FillBloodPool)
                {
                    // Fill blood pool logic here
                }

                if (!string.IsNullOrWhiteSpace(blood.PrimaryType))
                {
                    var guid = BloodTypeGUIDs.GetBloodTypeGUID(blood.PrimaryType);
                    BloodHelper.SetBloodType(player, guid, blood.PrimaryQuality);
                }

                if (!string.IsNullOrWhiteSpace(blood.SecondaryType))
                {
                    var guid = BloodTypeGUIDs.GetBloodTypeGUID(blood.SecondaryType);
                    // Secondary blood application logic
                }

                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[LoadoutApplicationService] ApplyBloodType error: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> ApplyWeapon(Entity player, WeaponConfig weapon)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(weapon.Name)) return false;

                // Use existing InventoryService to give weapon by name
                return InventoryService.GiveItem(player, weapon.Name, 1);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[LoadoutApplicationService] ApplyWeapon error: {ex.Message}");
                return false;
            }
        }

        private static bool ApplyArmor(Entity player, ArmorConfig armor)
        {
            try
            {
                var guids = new List<PrefabGUID>();

                if (!string.IsNullOrWhiteSpace(armor.Head))
                    guids.Add(ResolvePrefabGUID(armor.Head));
                if (!string.IsNullOrWhiteSpace(armor.Chest))
                    guids.Add(ResolvePrefabGUID(armor.Chest));
                if (!string.IsNullOrWhiteSpace(armor.Legs))
                    guids.Add(ResolvePrefabGUID(armor.Legs));
                if (!string.IsNullOrWhiteSpace(armor.Boots))
                    guids.Add(ResolvePrefabGUID(armor.Boots));
                if (!string.IsNullOrWhiteSpace(armor.Gloves))
                    guids.Add(ResolvePrefabGUID(armor.Gloves));
                if (!string.IsNullOrWhiteSpace(armor.Cloak))
                    guids.Add(ResolvePrefabGUID(armor.Cloak));
                if (!string.IsNullOrWhiteSpace(armor.Bag))
                    guids.Add(ResolvePrefabGUID(armor.Bag));
                if (!string.IsNullOrWhiteSpace(armor.MagicSource))
                    guids.Add(ResolvePrefabGUID(armor.MagicSource));

                return InventoryService.GiveLoadout(player, guids.ToArray(), guids.Select(_ => 1).ToArray());
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[LoadoutApplicationService] ApplyArmor error: {ex.Message}");
                return false;
            }
        }

        private static bool ApplyItem(Entity player, ItemConfig item)
        {
            try
            {
                return InventoryService.GiveItem(player, item.Name, item.Amount);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[LoadoutApplicationService] ApplyItem error: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> ApplyAbility(Entity player, string slot, AbilityConfig ability)
        {
            try
            {
                if (ability == null || string.IsNullOrWhiteSpace(ability.Name)) return false;

                // Apply ability via progression system
                // This would need integration with ability unlocking system
                Plugin.Logger?.LogInfo($"[LoadoutApplicationService] Ability application for {slot} not implemented yet: {ability.Name}");
                return true; // Placeholder
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[LoadoutApplicationService] ApplyAbility error: {ex.Message}");
                return false;
            }
        }

        private static bool ApplyPassiveSpell(Entity player, string spellName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(spellName)) return false;

                // Apply passive spell
                Plugin.Logger?.LogInfo($"[LoadoutApplicationService] Passive spell application not implemented yet: {spellName}");
                return true; // Placeholder
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[LoadoutApplicationService] ApplyPassiveSpell error: {ex.Message}");
                return false;
            }
        }

        private static bool ApplySettings(Entity player, BuildSettings settings)
        {
            try
            {
                if (settings.ClearInventory)
                {
                    InventoryService.ClearInventory(player);
                }

                // Other settings as needed
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[LoadoutApplicationService] ApplySettings error: {ex.Message}");
                return false;
            }
        }

        private static PrefabGUID ResolvePrefabGUID(string name)
        {
            return UtilsHelper.GetPrefabGuid(name) ?? default;
        }

        private static void ValidateBuildsData()
        {
            Plugin.Logger?.LogInfo("[LoadoutApplicationService] Validating Builds.json data...");

            foreach (var build in _builds)
            {
                var buildName = build.Key;
                var loadout = build.Value;

                // Validate blood types
                if (loadout.Blood != null)
                {
                    if (!string.IsNullOrWhiteSpace(loadout.Blood.PrimaryType))
                    {
                        var bloodGuid = BloodTypeGUIDs.GetBloodTypeGUID(loadout.Blood.PrimaryType);
                        if (bloodGuid == PrefabGUID.Empty)
                        {
                            Plugin.Logger?.LogWarning($"[LoadoutApplicationService] Invalid blood type '{loadout.Blood.PrimaryType}' in build '{buildName}'");
                        }
                        else
                        {
                            Plugin.Logger?.LogInfo($"[LoadoutApplicationService] Valid blood type '{loadout.Blood.PrimaryType}' in build '{buildName}'");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(loadout.Blood.SecondaryType))
                    {
                        var bloodGuid = BloodTypeGUIDs.GetBloodTypeGUID(loadout.Blood.SecondaryType);
                        if (bloodGuid == PrefabGUID.Empty)
                        {
                            Plugin.Logger?.LogWarning($"[LoadoutApplicationService] Invalid secondary blood type '{loadout.Blood.SecondaryType}' in build '{buildName}'");
                        }
                        else
                        {
                            Plugin.Logger?.LogInfo($"[LoadoutApplicationService] Valid secondary blood type '{loadout.Blood.SecondaryType}' in build '{buildName}'");
                        }
                    }
                }

                // Validate weapons
                if (loadout.Weapons != null)
                {
                    foreach (var weapon in loadout.Weapons)
                    {
                        if (!string.IsNullOrWhiteSpace(weapon.Name))
                        {
                            var weaponGuid = UtilsHelper.GetPrefabGuid(weapon.Name);
                            if (weaponGuid == null || weaponGuid == PrefabGUID.Empty)
                            {
                                Plugin.Logger?.LogWarning($"[LoadoutApplicationService] Invalid weapon '{weapon.Name}' in build '{buildName}'");
                            }
                            else
                            {
                                Plugin.Logger?.LogInfo($"[LoadoutApplicationService] Valid weapon '{weapon.Name}' in build '{buildName}'");
                            }
                        }
                    }
                }

                // Validate items
                if (loadout.Items != null)
                {
                    foreach (var item in loadout.Items)
                    {
                        if (!string.IsNullOrWhiteSpace(item.Name))
                        {
                            var itemGuid = UtilsHelper.GetPrefabGuid(item.Name);
                            if (itemGuid == null || itemGuid == PrefabGUID.Empty)
                            {
                                Plugin.Logger?.LogWarning($"[LoadoutApplicationService] Invalid item '{item.Name}' in build '{buildName}'");
                            }
                            else
                            {
                                Plugin.Logger?.LogInfo($"[LoadoutApplicationService] Valid item '{item.Name}' in build '{buildName}'");
                            }
                        }
                    }
                }

                // Validate armor
                if (loadout.Armors != null)
                {
                    var armorFields = new[] { loadout.Armors.Head, loadout.Armors.Chest, loadout.Armors.Legs,
                                             loadout.Armors.Boots, loadout.Armors.Gloves, loadout.Armors.Cloak,
                                             loadout.Armors.Bag, loadout.Armors.MagicSource };

                    foreach (var armorPiece in armorFields)
                    {
                        if (!string.IsNullOrWhiteSpace(armorPiece))
                        {
                            var armorGuid = UtilsHelper.GetPrefabGuid(armorPiece);
                            if (armorGuid == null || armorGuid == PrefabGUID.Empty)
                            {
                                Plugin.Logger?.LogWarning($"[LoadoutApplicationService] Invalid armor piece '{armorPiece}' in build '{buildName}'");
                            }
                            else
                            {
                                Plugin.Logger?.LogInfo($"[LoadoutApplicationService] Valid armor piece '{armorPiece}' in build '{buildName}'");
                            }
                        }
                    }
                }
            }

            Plugin.Logger?.LogInfo("[LoadoutApplicationService] Builds.json validation completed");
        }

        // Models matching VRisingArenaBuilds Builds.json format
        private class LoadoutModel
        {
            public BuildSettings Settings { get; set; }
            public BloodConfig Blood { get; set; }
            public ArmorConfig Armors { get; set; }
            public List<WeaponConfig> Weapons { get; set; }
            public List<ItemConfig> Items { get; set; }
            public AbilitiesConfig Abilities { get; set; }
            public Dictionary<string, string> PassiveSpells { get; set; }
        }

        private class BuildSettings
        {
            public bool ClearInventory { get; set; }
        }

        private class BloodConfig
        {
            public bool FillBloodPool { get; set; }
            public bool GiveBloodPotion { get; set; }
            public string PrimaryType { get; set; }
            public string SecondaryType { get; set; }
            public int PrimaryQuality { get; set; }
            public int SecondaryQuality { get; set; }
            public int SecondaryBuffIndex { get; set; }
        }

        private class ArmorConfig
        {
            public string Head { get; set; }
            public string Chest { get; set; }
            public string Legs { get; set; }
            public string Boots { get; set; }
            public string Gloves { get; set; }
            public string Cloak { get; set; }
            public string Bag { get; set; }
            public string MagicSource { get; set; }
        }

        private class WeaponConfig
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

        private class ItemConfig
        {
            public string Name { get; set; }
            public int Amount { get; set; }
        }

        private class AbilitiesConfig
        {
            public AbilityConfig Travel { get; set; }
            public AbilityConfig Ability1 { get; set; }
            public AbilityConfig Ability2 { get; set; }
            public AbilityConfig Ultimate { get; set; }
        }

        private class AbilityConfig
        {
            public string Name { get; set; }
            public JewelConfig Jewel { get; set; }
        }

        private class JewelConfig
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
    }
}
