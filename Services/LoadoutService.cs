using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProjectM;
using Unity.Entities;
using CrowbaneArena.Core;
using CrowbaneArena.Helpers;
using Stunlock.Core;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// LoadoutService: save, list and apply loadouts.
    /// Sources:
    /// - ArenaConfigurationService (CFG/JSON loadouts)
    /// - JSON files: BepInEx/config/CrowbaneArena/Loadouts/{name}.json
    /// </summary>
    public static class LoadoutService
    {
        private static readonly string LoadoutsDir = Path.Combine(BepInEx.Paths.ConfigPath, "CrowbaneArena", "Loadouts");

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static void Initialize()
        {
            try
            {
                if (!Directory.Exists(LoadoutsDir)) Directory.CreateDirectory(LoadoutsDir);
                Plugin.Logger?.LogInfo($"[LoadoutService] Ready. Dir: {LoadoutsDir}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[LoadoutService] Init error: {ex.Message}");
            }
        }

        /// <summary>
        /// Save an existing config loadout (from ArenaConfigurationService) to a JSON file.
        /// </summary>
        public static bool SaveConfigLoadoutToFile(string configLoadoutName, string saveAsName = null)
        {
            try
            {
                if (!ArenaConfigurationService.TryGetLoadout(configLoadoutName, out var loadout))
                {
                    Plugin.Logger?.LogWarning($"[LoadoutService] Config loadout not found: {configLoadoutName}");
                    return false;
                }

                var model = new SavedLoadout
                {
                    Name = saveAsName ?? loadout.Name,
                    Weapons = GuidConverter.ToStrings(loadout.Weapons),
                    Armor = GuidConverter.ToStrings(loadout.Armor),
                    Consumables = loadout.Consumables?.Select(i => new SavedItem
                    {
                        PrefabGUID = GuidConverter.ToString(i.Guid),
                        Amount = i.Amount
                    }).ToList() ?? new List<SavedItem>(),
                    // Also keep name references if available in config by resolving back from data tables
                    WeaponNames = ResolveWeaponNames(loadout.Weapons),
                    ArmorNames = ResolveArmorNames(loadout.Armor),
                    ConsumableNames = ResolveConsumableNames(loadout.Consumables)
                };

                var path = Path.Combine(LoadoutsDir, $"{model.Name}.json");
                var json = JsonSerializer.Serialize(model, JsonOpts);
                File.WriteAllText(path, json);
                Plugin.Logger?.LogInfo($"[LoadoutService] Saved loadout to {path}");
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[LoadoutService] SaveConfigLoadoutToFile error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply a loadout to player. Priority: file loadout -> ArenaConfigurationService loadout -> LoadoutApplicationService build.
        /// </summary>
        public static bool ApplyLoadout(Entity player, string loadoutName)
        {
            try
            {
                // 1) Try file-based
                if (TryReadSaved(loadoutName, out var saved))
                {
                    return ApplySavedModel(player, saved);
                }

                // 2) Fallback to config-based
                if (ArenaConfigurationService.TryGetLoadout(loadoutName, out var loadout))
                {
                    return ApplyConfigModel(player, loadout);
                }

                // 3) Fallback to complete build from LoadoutApplicationService
                if (CrowbaneArena.Services.LoadoutApplicationService.BuildNames.Contains(loadoutName))
                {
                    Plugin.Logger?.LogInfo($"[LoadoutService] Falling back to complete build '{loadoutName}' from LoadoutApplicationService");
                    return CrowbaneArena.Services.LoadoutApplicationService.ApplyLoadout(player, loadoutName).Result;
                }

                Plugin.Logger?.LogWarning($"[LoadoutService] Loadout not found: {loadoutName}");
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[LoadoutService] ApplyLoadout error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// List all available loadouts from files, enabled config loadouts, and LoadoutApplicationService builds.
        /// </summary>
        public static List<string> GetAvailableLoadouts()
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                // Files
                if (Directory.Exists(LoadoutsDir))
                {
                    foreach (var file in Directory.GetFiles(LoadoutsDir, "*.json"))
                    {
                        names.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
                // Config
                foreach (var l in ArenaConfigurationService.GetEnabledLoadouts())
                {
                    names.Add(l.Name);
                }
                // Complete builds from LoadoutApplicationService
                foreach (var buildName in CrowbaneArena.Services.LoadoutApplicationService.BuildNames)
                {
                    names.Add(buildName);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[LoadoutService] GetAvailableLoadouts error: {ex.Message}");
            }
            return names.ToList();
        }

        // ==========================
        // Internals
        // ==========================

        private static bool TryReadSaved(string name, out SavedLoadout model)
        {
            try
            {
                var path = Path.Combine(LoadoutsDir, $"{name}.json");
                if (!File.Exists(path)) { model = null; return false; }
                var json = File.ReadAllText(path);
                model = JsonSerializer.Deserialize<SavedLoadout>(json, JsonOpts);
                if (model != null) return true;

                // Fallback: try alternate schema (arena_config-like)
                return TryConvertAlternateSchema(json, out model);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[LoadoutService] TryReadSaved error: {ex.Message}");
                model = null;
                return false;
            }
        }

        private static bool ApplyConfigModel(Entity player, ArenaLoadout loadout)
        {
            try
            {
                // Use existing InventoryService which already handles config translation & spawning
                return InventoryService.GiveLoadout(player, loadout.Name);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[LoadoutService] ApplyConfigModel error: {ex.Message}");
                return false;
            }
        }

        private static bool ApplySavedModel(Entity player, SavedLoadout saved)
        {
            try
            {
                int success = 0; int total = 0;

                // Weapons from GUIDs - equip them properly
                if (saved.Weapons != null)
                {
                    foreach (var g in saved.Weapons)
                    {
                        if (int.TryParse(g, out var guidHash))
                        {
                            total++;
                            // Use the new GiveAndEquipWeapon method for weapons
                            if (InventoryService.GiveAndEquipWeapon(player, new PrefabGUID(guidHash))) success++;
                        }
                    }
                }

                // Armor from GUIDs
                if (saved.Armor != null)
                {
                    foreach (var g in saved.Armor)
                    {
                        if (int.TryParse(g, out var guidHash))
                        {
                            total++;
                            if (InventoryService.GiveLoadout(player, new[] { new PrefabGUID(guidHash) }, new[] { 1 })) success++;
                        }
                    }
                }

                // Consumables from GUIDs
                if (saved.Consumables != null)
                {
                    foreach (var item in saved.Consumables)
                    {
                        if (item != null && int.TryParse(item.PrefabGUID, out var guidHash))
                        {
                            total++;
                            if (InventoryService.GiveLoadout(player, new[] { new PrefabGUID(guidHash) }, new[] { Math.Max(1, item.Amount) })) success++;
                        }
                    }
                }

                // Also resolve by names if present
                success += ResolveAndApplyByNames(player, saved, ref total);

                Plugin.Logger?.LogInfo($"[LoadoutService] Applied saved loadout '{saved.Name}': {success}/{total} items");
                return success > 0;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[LoadoutService] ApplySavedModel error: {ex.Message}");
                return false;
            }
        }

        private static int ResolveAndApplyByNames(Entity player, SavedLoadout saved, ref int total)
        {
            int success = 0;
            try
            {
                // Weapons by name via ArenaConfigurationService or UtilsHelper
                if (saved.WeaponNames != null)
                {
                    foreach (var name in saved.WeaponNames)
                    {
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        total++;
                        if (ArenaConfigurationService.TryGetWeapon(name, out var weaponData))
                        {
                            if (InventoryService.GiveLoadout(player, new[] { new PrefabGUID((int)weaponData.Guid) }, new[] { 1 })) success++;
                        }
                        else if (CrowbaneArena.Helpers.UtilsHelper.TryGetPrefabGuid(name, out var guid))
                        {
                            if (InventoryService.GiveLoadout(player, new[] { guid }, new[] { 1 })) success++;
                        }
                    }
                }

                // Armor by name
                if (saved.ArmorNames != null)
                {
                    foreach (var name in saved.ArmorNames)
                    {
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        total++;
                        if (ArenaConfigurationService.TryGetArmorSet(name, out var armorSet))
                        {
                            var armorGuids = new[]
                            {
                                new PrefabGUID((int)armorSet.BootsGuid),
                                new PrefabGUID((int)armorSet.GlovesGuid),
                                new PrefabGUID((int)armorSet.ChestGuid),
                                new PrefabGUID((int)armorSet.LegsGuid)
                            };
                            if (InventoryService.GiveLoadout(player, armorGuids, new[] { 1, 1, 1, 1 })) success++;
                        }
                        else if (CrowbaneArena.Helpers.UtilsHelper.TryGetPrefabGuid(name, out var guid))
                        {
                            if (InventoryService.GiveLoadout(player, new[] { guid }, new[] { 1 })) success++;
                        }
                    }
                }

                // Consumables by name
                if (saved.ConsumableNames != null)
                {
                    foreach (var name in saved.ConsumableNames)
                    {
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        total++;
                        if (ArenaConfigurationService.TryGetConsumable(name, out var cons))
                        {
                            if (InventoryService.GiveLoadout(player, new[] { new PrefabGUID((int)cons.Guid) }, new[] { Math.Max(1, cons.DefaultAmount) })) success++;
                        }
                        else if (CrowbaneArena.Helpers.UtilsHelper.TryGetPrefabGuid(name, out var guid))
                        {
                            if (InventoryService.GiveLoadout(player, new[] { guid }, new[] { 1 })) success++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[LoadoutService] Name resolution apply warning: {ex.Message}");
            }
            return success;
        }

        private static List<string> ResolveWeaponNames(List<PrefabGUID> guids)
        {
            var names = new List<string>();
            if (guids == null || guids.Count == 0) return names;
            foreach (var g in guids)
            {
                var match = ArenaConfigurationService.GetWeapons().FirstOrDefault(w => w.Guid == GuidConverter.GetGuidHash(g));
                if (match != null) names.Add(match.Name);
            }
            return names;
        }

        private static List<string> ResolveArmorNames(List<PrefabGUID> guids)
        {
            var names = new List<string>();
            if (guids == null || guids.Count == 0) return names;
            // Best-effort: map any piece guid back to its set name
            foreach (var set in ArenaConfigurationService.GetArmorSets())
            {
                if (guids.Any(g => GuidConverter.GetGuidHash(g) == set.BootsGuid || GuidConverter.GetGuidHash(g) == set.GlovesGuid || GuidConverter.GetGuidHash(g) == set.ChestGuid || GuidConverter.GetGuidHash(g) == set.LegsGuid))
                {
                    if (!names.Contains(set.Name, StringComparer.OrdinalIgnoreCase)) names.Add(set.Name);
                }
            }
            return names;
        }

        private static List<string> ResolveConsumableNames(List<ArenaItem> items)
        {
            var names = new List<string>();
            if (items == null || items.Count == 0) return names;
            foreach (var it in items)
            {
                var match = ArenaConfigurationService.GetConsumables().FirstOrDefault(c => c.Guid == GuidConverter.GetGuidHash(it.Guid));
                if (match != null) names.Add(match.Name);
            }
            return names;
        }

        // ==========================
        // Models
        // ==========================
        private class SavedLoadout
        {
            public string Name { get; set; }
            public List<string> Weapons { get; set; }
            public List<string> Armor { get; set; }
            public List<SavedItem> Consumables { get; set; }
            public List<string> WeaponNames { get; set; }
            public List<string> ArmorNames { get; set; }
            public List<string> ConsumableNames { get; set; }
        }

        private class SavedItem
        {
            public string PrefabGUID { get; set; }
            public int Amount { get; set; }
        }

        // ==========================
        // Alternate schema support
        // ==========================
        private class AltWeapon
        {
            public string Name { get; set; }
        }

        private class AltArmors
        {
            public string Boots { get; set; }
            public string Chest { get; set; }
            public string Gloves { get; set; }
            public string Legs { get; set; }
            public string MagicSource { get; set; } // amulet
        }

        private class AltLoadout
        {
            public AltArmors Armors { get; set; }
            public List<AltWeapon> Weapons { get; set; }
            public List<AltItem> Items { get; set; }
            public AltAbilities Abilities { get; set; }
        }

        private class AltItem
        {
            public string Name { get; set; }
            public int Amount { get; set; }
        }

        private class AltAbilities
        {
            public AltAbility Travel { get; set; }
            public AltAbility Ability1 { get; set; }
            public AltAbility Ability2 { get; set; }
            public AltAbility Ultimate { get; set; }
        }

        private class AltAbility
        {
            public string Name { get; set; }
            public object Jewel { get; set; }
        }

        private static bool TryConvertAlternateSchema(string json, out SavedLoadout model)
        {
            model = null;
            try
            {
                // arena_config.json top-level may have categories like "offensive"; pick the first object
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;

                JsonElement payload = default;
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        payload = prop.Value;
                        break;
                    }
                }
                if (payload.ValueKind != JsonValueKind.Object) return false;

                var alt = JsonSerializer.Deserialize<AltLoadout>(payload.GetRawText(), JsonOpts);
                if (alt == null) return false;

                var saved = new SavedLoadout
                {
                    Name = "converted",
                    Weapons = new List<string>(),
                    Armor = new List<string>(),
                    Consumables = new List<SavedItem>(),
                    WeaponNames = new List<string>(),
                    ArmorNames = new List<string>(),
                    ConsumableNames = new List<string>()
                };

                // Weapons: up to 13
                if (alt.Weapons != null)
                {
                    int count = 0;
                    foreach (var w in alt.Weapons)
                    {
                        if (count >= 13) break;
                        if (w == null || string.IsNullOrWhiteSpace(w.Name)) continue;
                        saved.WeaponNames.Add(w.Name);
                        count++;
                    }
                }

                // Armor: four core pieces by name; MagicSource as amulet (treat like item to give one)
                if (alt.Armors != null)
                {
                    void addArmorName(string n)
                    {
                        if (!string.IsNullOrWhiteSpace(n) && !saved.ArmorNames.Contains(n, StringComparer.OrdinalIgnoreCase))
                            saved.ArmorNames.Add(n);
                    }
                    addArmorName(alt.Armors.Boots);
                    addArmorName(alt.Armors.Gloves);
                    addArmorName(alt.Armors.Chest);
                    addArmorName(alt.Armors.Legs);

                    if (!string.IsNullOrWhiteSpace(alt.Armors.MagicSource))
                    {
                        saved.ConsumableNames.Add(alt.Armors.MagicSource); // amulet/jewelry
                    }
                }

                // Items
                if (alt.Items != null)
                {
                    foreach (var it in alt.Items)
                    {
                        if (it == null || string.IsNullOrWhiteSpace(it.Name)) continue;
                        saved.ConsumableNames.Add(it.Name);
                    }
                }

                // Jewels present?
                if (alt.Abilities != null)
                {
                    if (alt.Abilities.Travel?.Jewel != null || alt.Abilities.Ability1?.Jewel != null || alt.Abilities.Ability2?.Jewel != null)
                    {
                        Plugin.Logger?.LogInfo("[LoadoutService] Jewel data detected in alt schema; equipping jewels is not supported in this build.");
                    }
                }

                model = saved;
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[LoadoutService] TryConvertAlternateSchema warning: {ex.Message}");
                model = null;
                return false;
            }
        }
    }
}
