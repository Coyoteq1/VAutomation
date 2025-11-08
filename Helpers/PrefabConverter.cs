using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Stunlock.Core;

namespace CrowbaneArena.Utilities
{
    /// <summary>
    /// Utility for converting and importing VRising prefab data into the CrowbaneArena system.
    /// Supports importing data from various formats and adding items with single-word commands.
    /// </summary>
    public static class PrefabConverter
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        public static class ImportFormats
        {
            /// <summary>
            /// Simple format: { "Name": GUID }
            /// </summary>
            public static Dictionary<string, int> FromSimpleDictionary(string jsonText)
            {
                try
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(jsonText);
                    return dict ?? new();
                }
                catch
                {
                    return new();
                }
            }

            /// <summary>
            /// Array format: [{ "Name": "", "Guid": 0, "Category": "" }, ...]
            /// </summary>
            public static void FromArrayFormat(string jsonText)
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<ImportItem>>(jsonText);
                    if (items == null) return;

                    foreach (var item in items)
                    {
                        if (string.IsNullOrEmpty(item.Name)) continue;

                        var guid = new PrefabGUID(item.Guid);
                        AddItemToCategory(item.Name, guid, item.Category);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error importing array format: {ex.Message}");
                }
            }

            /// <summary>
            /// Category-based format: { "category": { "name": guid, ... }, ... }
            /// </summary>
            public static void FromCategoryFormat(string jsonText)
            {
                try
                {
                    var categories = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(jsonText);
                    if (categories == null) return;

                    foreach (var category in categories)
                    {
                        foreach (var item in category.Value)
                        {
                            var guid = new PrefabGUID(item.Value);
                            AddItemToCategory(item.Key, guid, category.Key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error importing category format: {ex.Message}");
                }
            }

            private static void AddItemToCategory(string name, PrefabGUID guid, string category)
            {
                switch (category.ToLowerInvariant())
                {
                    case "weapons":
                    case "weapon":
                        if (!Data.Prefabs.Weapons.ContainsKey(name.ToLowerInvariant()))
                            Data.Prefabs.Weapons[name.ToLowerInvariant()] = guid;
                        else
                            Console.WriteLine($"Warning: Weapon '{name}' already exists");
                        break;

                    case "consumables":
                    case "consumable":
                        if (!Data.Prefabs.Consumables.ContainsKey(name.ToLowerInvariant()))
                            Data.Prefabs.Consumables[name.ToLowerInvariant()] = guid;
                        else
                            Console.WriteLine($"Warning: Consumable '{name}' already exists");
                        break;

                    case "spells":
                    case "spell":
                    case "abilities":
                    case "ability":
                        if (!Data.Prefabs.Spells.ContainsKey(name.ToLowerInvariant()))
                            Data.Prefabs.Spells[name.ToLowerInvariant()] = guid;
                        else
                            Console.WriteLine($"Warning: Spell '{name}' already exists");
                        break;

                    case "units":
                    case "unit":
                        if (!Data.Prefabs.Units.ContainsKey(name.ToLowerInvariant()))
                            Data.Prefabs.Units[name.ToLowerInvariant()] = guid;
                        else
                            Console.WriteLine($"Warning: Unit '{name}' already exists");
                        break;

                    default:
                        // Try auto-add to weapons as fallback
                        if (!Data.Prefabs.Weapons.ContainsKey(name.ToLowerInvariant()))
                        {
                            Data.Prefabs.Weapons[name.ToLowerInvariant()] = guid;
                            Console.WriteLine($"Added '{name}' to weapons (unknown category: {category})");
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Item '{name}' already exists in weapons");
                        }
                        break;
                }
            }
        }

        public static class Commands
        {
            /// <summary>
            /// Single-word command to add any item: .add weapon/sword/1234567890
            /// Format: .add [category]/[name]/[guid]
            /// </summary>
            public static string AddItem(string command)
            {
                var parts = command.Split('/');
                if (parts.Length != 3)
                    return "Format: .add category/name/guid";

                var category = parts[0].Trim();
                var name = parts[1].Trim().ToLowerInvariant();
                var guidStr = parts[2].Trim();

                if (!int.TryParse(guidStr, out var guidInt))
                    return $"Invalid GUID: {guidStr}";

                var guid = new PrefabGUID(guidInt);

                switch (category.ToLowerInvariant())
                {
                    case "w":
                    case "weapon":
                        if (!Data.Prefabs.Weapons.ContainsKey(name))
                        {
                            Data.Prefabs.Weapons[name] = guid;
                            return $"Added weapon: {name} ({guidInt})";
                        }
                        else
                            return $"Weapon '{name}' already exists";

                    case "c":
                    case "consumable":
                        if (!Data.Prefabs.Consumables.ContainsKey(name))
                        {
                            Data.Prefabs.Consumables[name] = guid;
                            return $"Added consumable: {name} ({guidInt})";
                        }
                        else
                            return $"Consumable '{name}' already exists";

                    case "s":
                    case "spell":
                        if (!Data.Prefabs.Spells.ContainsKey(name))
                        {
                            Data.Prefabs.Spells[name] = guid;
                            return $"Added spell: {name} ({guidInt})";
                        }
                        else
                            return $"Spell '{name}' already exists";

                    case "u":
                    case "unit":
                        if (!Data.Prefabs.Units.ContainsKey(name))
                        {
                            Data.Prefabs.Units[name] = guid;
                            return $"Added unit: {name} ({guidInt})";
                        }
                        else
                            return $"Unit '{name}' already exists";

                    default:
                        return $"Unknown category: {category}. Use: weapon/consumable/spell/unit";
                }
            }

            /// <summary>
            /// Import items from a JSON string: .import json:{...}
            /// Supports multiple formats automatically detected
            /// </summary>
            public static string ImportJson(string jsonData)
            {
                if (string.IsNullOrWhiteSpace(jsonData))
                    return "No JSON data provided";

                try
                {
                    // Try array format first (most common)
                    if (jsonData.TrimStart().StartsWith("["))
                    {
                        ImportFormats.FromArrayFormat(jsonData);
                        return "Imported array format successfully";
                    }
                    // Try category format
                    else if (jsonData.Contains('"') && jsonData.Contains(':') && jsonData.Contains('{'))
                    {
                        ImportFormats.FromCategoryFormat(jsonData);
                        return "Imported category format successfully";
                    }
                    // Try simple dictionary format
                    else
                    {
                        var dict = ImportFormats.FromSimpleDictionary(jsonData);
                        if (dict.Count > 0)
                        {
                            foreach (var kvp in dict)
                            {
                                var guid = new PrefabGUID(kvp.Value);
                                if (!Data.Prefabs.Weapons.ContainsKey(kvp.Key.ToLowerInvariant()))
                                {
                                    Data.Prefabs.Weapons[kvp.Key.ToLowerInvariant()] = guid;
                                }
                            }
                            return $"Imported {dict.Count} items as weapons";
                        }
                        else
                        {
                            return "Failed to parse JSON format";
                        }
                    }
                }
                catch (Exception ex)
                {
                    return $"Import failed: {ex.Message}";
                }
            }
        }

        public static class Export
        {
            /// <summary>
            /// Export all prefabs to JSON format for backup/sharing
            /// </summary>
            public static string ToJson()
            {
                var export = new
                {
                    Weapons = Data.Prefabs.Weapons.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GuidHash),
                    ArmorSets = Data.Prefabs.ArmorSets.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToDictionary(inner => inner.Key, inner => inner.Value.GuidHash)),
                    Consumables = Data.Prefabs.Consumables.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GuidHash),
                    Spells = Data.Prefabs.Spells.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GuidHash),
                    Units = Data.Prefabs.Units.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.GuidHash)
                };

                return JsonSerializer.Serialize(export, _jsonOptions);
            }
        }

        /// <summary>
        /// Data structure for importing items from JSON arrays
        /// </summary>
        public class ImportItem
        {
            public string Name { get; set; } = "";
            public int Guid { get; set; }
            public string Category { get; set; } = "weapons";
        }
    }
}
