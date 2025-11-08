using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrowbaneArena.Data;
using CrowbaneArena.Services;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;
using VampireCommandFramework;

namespace CrowbaneArena.Commands
{
    /// <summary>
    /// Loadout management commands for VRising arena system.
    /// Provides chat commands to list, apply, and manage loadouts.
    /// </summary>
    internal static class LoadoutCommands
    {
        /// <summary>
        /// List all available loadouts (custom + default)
        /// Usage: .loadouts
        /// </summary>
        [Command("loadouts", description: "List all available loadouts")]
        public static void ListLoadouts(ChatCommandContext ctx)
        {
            try
            {
                string loadoutList = LoadoutManager.GetLoadoutList();
                if (string.IsNullOrEmpty(loadoutList))
                {
                    ctx.Reply("No loadouts available");
                    return;
                }

                var (customCount, defaultCount, totalCount) = LoadoutManager.GetLoadoutStats();
                ctx.Reply($"📋 Available Loadouts ({totalCount} total - {customCount} custom, {defaultCount} default):\n{loadoutList}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ListLoadouts: {ex.Message}");
                ctx.Error("Failed to list loadouts");
            }
        }

        /// <summary>
        /// List only custom loadouts
        /// Usage: .loadouts custom
        /// </summary>
        [Command("loadouts custom", description: "List custom loadouts only")]
        public static void ListCustomLoadouts(ChatCommandContext ctx)
        {
            try
            {
                string loadoutList = LoadoutManager.GetCustomLoadoutList();
                if (string.IsNullOrEmpty(loadoutList) || loadoutList == "No custom loadouts available")
                {
                    ctx.Reply("No custom loadouts available. Use .loadout save <name> to create one.");
                    return;
                }

                ctx.Reply($"🎨 Custom Loadouts ({LoadoutManager.CustomLoadouts.Count}):\n{loadoutList}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ListCustomLoadouts: {ex.Message}");
                ctx.Error("Failed to list custom loadouts");
            }
        }

        /// <summary>
        /// Apply a loadout to yourself
        /// Usage: .loadout <name>
        /// </summary>
        [Command("loadout", description: "Apply a loadout - Usage: .loadout <name>")]
        public static async Task ApplyLoadout(ChatCommandContext ctx, string loadoutName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loadoutName))
                {
                    ctx.Error("Usage: .loadout <name> - Use .loadouts to see available loadouts");
                    return;
                }

                var loadout = LoadoutManager.GetLoadout(loadoutName);
                if (loadout == null)
                {
                    ctx.Error($"Loadout '{loadoutName}' not found. Use .loadouts to see available options.");
                    return;
                }

                // Check if player is in arena (if arena restrictions are enabled)
                var characterEntity = ctx.Event.SenderCharacterEntity;
                var userEntity = ctx.Event.SenderUserEntity;

                // Apply the loadout
                var success = LoadoutService.ApplyLoadout(characterEntity, loadoutName);
                if (success)
                {
                    ctx.Reply($"✅ Applied loadout '{loadoutName}'");
                }
                else
                {
                    ctx.Error("❌ Failed to apply loadout");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ApplyLoadout: {ex.Message}");
                ctx.Error("Failed to apply loadout");
            }
        }

        /// <summary>
        /// Save current equipment as a custom loadout
        /// Usage: .loadout save <name>
        /// </summary>
        [Command("loadout save", description: "Save current equipment as custom loadout - Usage: .loadout save <name>")]
        public static void SaveCurrentLoadout(ChatCommandContext ctx, string loadoutName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loadoutName))
                {
                    ctx.Error("Usage: .loadout save <name> - Provide a name for your loadout");
                    return;
                }

                if (loadoutName.Length > 50)
                {
                    ctx.Error("Loadout name too long (max 50 characters)");
                    return;
                }

                var characterEntity = ctx.Event.SenderCharacterEntity;
                var userEntity = ctx.Event.SenderUserEntity;

                // Create loadout from current equipment
                var loadout = new LoadoutDefinition
                {
                    Name = loadoutName,
                    Description = $"Custom loadout saved by {ctx.Name}",
                    BloodType = "Rogue", // Placeholder
                    BloodQuality = 100f,
                    Weapons = new List<WeaponDefinition>(),
                    Armor = new ArmorDefinition(),
                    Consumables = new List<ConsumableDefinition>(),
                    Abilities = new List<string>()
                };

                LoadoutManager.AddOrUpdateLoadout(loadoutName, loadout);
                LoadoutManager.SaveData();

                ctx.Reply($"💾 Saved current equipment as loadout '{loadoutName}'");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SaveCurrentLoadout: {ex.Message}");
                ctx.Error("Failed to save loadout");
            }
        }

        /// <summary>
        /// Delete a custom loadout
        /// Usage: .loadout delete <name>
        /// </summary>
        [Command("loadout delete", description: "Delete a custom loadout - Usage: .loadout delete <name>")]
        public static void DeleteLoadout(ChatCommandContext ctx, string loadoutName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loadoutName))
                {
                    ctx.Error("Usage: .loadout delete <name>");
                    return;
                }

                // Check if it's a custom loadout (don't allow deleting defaults)
                if (!LoadoutManager.CustomLoadouts.ContainsKey(loadoutName))
                {
                    ctx.Error($"Cannot delete '{loadoutName}' - only custom loadouts can be deleted");
                    return;
                }

                if (LoadoutManager.RemoveLoadout(loadoutName))
                {
                    LoadoutManager.SaveData();
                    ctx.Reply($"🗑️ Deleted custom loadout '{loadoutName}'");
                }
                else
                {
                    ctx.Error($"Failed to delete loadout '{loadoutName}'");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in DeleteLoadout: {ex.Message}");
                ctx.Error("Failed to delete loadout");
            }
        }

        /// <summary>
        /// Show details of a specific loadout
        /// Usage: .loadout info <name>
        /// </summary>
        [Command("loadout info", description: "Show loadout details - Usage: .loadout info <name>")]
        public static void LoadoutInfo(ChatCommandContext ctx, string loadoutName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loadoutName))
                {
                    ctx.Error("Usage: .loadout info <name>");
                    return;
                }

                var loadout = LoadoutManager.GetLoadout(loadoutName);
                if (loadout == null)
                {
                    ctx.Error($"Loadout '{loadoutName}' not found");
                    return;
                }

                string info = $"📋 Loadout: {loadout.Name}\n" +
                             $"📝 Description: {loadout.Description}\n" +
                             $"🩸 Blood: {loadout.BloodType} ({loadout.BloodQuality}%)\n" +
                             $"⚔️ Weapons: {loadout.Weapons?.Count ?? 0}\n" +
                             $"🛡️ Armor: {(loadout.Armor != null ? "Set" : "None")}\n" +
                             $"🧪 Consumables: {loadout.Consumables?.Count ?? 0}\n" +
                             $"✨ Abilities: {loadout.Abilities?.Count ?? 0}";

                ctx.Reply(info);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in LoadoutInfo: {ex.Message}");
                ctx.Error("Failed to get loadout info");
            }
        }

        /// <summary>
        /// Admin command to reload loadouts from disk
        /// Usage: .loadout reload
        /// </summary>
        [Command("loadout reload", description: "Reload loadouts from disk (admin only)", adminOnly: true)]
        public static void ReloadLoadouts(ChatCommandContext ctx)
        {
            try
            {
                LoadoutManager.LoadData();
                var (customCount, defaultCount, totalCount) = LoadoutManager.GetLoadoutStats();
                ctx.Reply($"🔄 Reloaded loadouts: {totalCount} total ({customCount} custom, {defaultCount} default)");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ReloadLoadouts: {ex.Message}");
                ctx.Error("Failed to reload loadouts");
            }
        }

        /// <summary>
        /// Show loadout statistics
        /// Usage: .loadout stats
        /// </summary>
        [Command("loadout stats", description: "Show loadout statistics")]
        public static void LoadoutStats(ChatCommandContext ctx)
        {
            try
            {
                var (customCount, defaultCount, totalCount) = LoadoutManager.GetLoadoutStats();
                var dataSummary = DefaultDataService.GetDataSummary();

                string stats = $"📊 Loadout Statistics:\n" +
                              $"Loadouts: {totalCount} total ({customCount} custom, {defaultCount} default)\n" +
                              $"📊 Data Sources:\n" +
                              $"- Weapons: {dataSummary.WeaponsCount}\n" +
                              $"- Armor Sets: {dataSummary.ArmorSetsCount}\n" +
                              $"- Consumables: {dataSummary.ConsumablesCount}\n" +
                              $"- Spells: {dataSummary.SpellsCount}\n" +
                              $"- Blood Types: {dataSummary.BloodTypesCount}\n" +
                              $"- Spell Schools: {dataSummary.SpellSchoolsCount}";

                ctx.Reply(stats);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in LoadoutStats: {ex.Message}");
                ctx.Error("Failed to get loadout statistics");
            }
        }
    }
}
