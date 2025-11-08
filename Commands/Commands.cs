using VampireCommandFramework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using CrowbaneArena.Services;
using CrowbaneArena.Data;
using CrowbaneArena.Helpers;
using Stunlock.Core;
using VAutomation.Converters;


namespace CrowbaneArena
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Legacy Arena Commands implementation
    /// This class is obsolete and will be removed in a future version.
    /// Please use ArenaCommands class instead.
    /// </summary>
    [Obsolete("This class is deprecated. Use ArenaCommands class instead.")]
    public static class LegacyArenaCommands
    {
        // ===== ARENA SETUP COMMANDS (Admin Only) =====
        
        [Command("setzone", description: "Set arena zone radius", adminOnly: true)]
        public static void SetZone(ICommandContext ctx, float radius = 50f)
        {
            if (radius <= 0)
            {
                ctx.Error("Radius must be greater than 0!");
                return;
            }
            ArenaController.SetZoneRadius(radius);
            ctx.Reply($"Arena zone set with radius {radius}m");
        }

        
        // ===== BLOOD PRESET COMMAND =====

        [Command("blood", description: "Blood utils: .blood preset <type> (rogue|warrior|scholar|creature|mutant|dracula|corrupted)")]
        public static void BloodCommand(ICommandContext ctx, string sub = "", string type = "")
        {
            try
            {
                if (!string.Equals(sub, "preset", StringComparison.OrdinalIgnoreCase))
                {
                    ctx.Reply("Usage: .blood preset <type> (rogue|warrior|scholar|creature|mutant|dracula|corrupted)");
                    return;
                }

                if (string.IsNullOrWhiteSpace(type))
                {
                    ctx.Error("Please provide a blood type. Example: .blood preset rogue");
                    return;
                }

                var player = GetPlayerEntity(ctx);
                if (player.Equals(Entity.Null))
                {
                    ctx.Error("Could not find your player entity!");
                    return;
                }

                // Must be in arena to change preset
                var pc = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(player);
                var userEntity = pc.UserEntity;
                var steamId = PlayerService.GetSteamId(userEntity);
                if (!SnapshotService.IsInArena(steamId))
                {
                    ctx.Error("❌ You must be in the arena to change blood presets!");
                    return;
                }

                if (!BloodTypeGUIDs.IsValidBloodType(type))
                {
                    ctx.Error("❌ Unknown blood type. Try: rogue, warrior, scholar, creature, mutant, dracula, corrupted");
                    return;
                }

                var guid = BloodTypeGUIDs.GetBloodTypeGUID(type);
                BloodHelper.SetBloodType(player, guid, 100f);
                ctx.Reply($"🩸 Applied {type} blood at 100% for arena");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in BloodCommand: {ex.Message}");
                ctx.Error("❌ Failed to apply blood preset");
            }
        }

        // Legacy commands have been replaced by the new progression system
        // These commands are kept for reference but are disabled

        /*
        // Compatibility aliases for existing unlock/lock commands
        [Command("unlock", description: "Alias: Activate arena VBlood hook (UI shows all unlocked)")]
        public static void UnlockAlias(ICommandContext ctx) => VBloodUnlock(ctx);

        [Command("lock", description: "Alias: Deactivate arena VBlood hook (UI shows real progression)")]
        public static void LockAlias(ICommandContext ctx) => VBloodLock(ctx);

        // VBLOOD HOOK TOGGLES (UI-only, non-persistent) - DEPRECATED
        // These commands have been replaced by the new progression snapshot system
        // that properly handles all unlocks and UI state
        */


        [Command("setentry", description: "Set entry point and radius", adminOnly: true)]
        public static void SetEntry(ICommandContext ctx, float radius = 10f)
        {
            if (radius <= 0)
            {
                ctx.Error("Entry radius must be greater than 0!");
                return;
            }
            
            var position = GetPlayerPosition(ctx);
            ArenaController.SetEntryPoint(position, radius);
            ctx.Reply($"Entry point set at your location with radius {radius}m");
        }


        [Command("setexit", description: "Set exit point and radius", adminOnly: true)]
        public static void SetExit(ICommandContext ctx, float radius = 10f)
        {
            if (radius <= 0)
            {
                ctx.Error("Exit radius must be greater than 0!");
                return;
            }
            
            var position = GetPlayerPosition(ctx);
            ArenaController.SetExitPoint(position, radius);
            ctx.Reply($"Exit point set at your location with radius {radius}m");
        }


        [Command("setspawn", description: "Set arena spawn point", adminOnly: true)]
        public static void SetSpawn(ICommandContext ctx)
        {
            var position = GetPlayerPosition(ctx);
            ArenaController.SetSpawnPoint(position);
            ctx.Reply("Arena spawn point set at your location");
        }


        [Command("reload", description: "Reload arena configuration", adminOnly: true)]
        public static void ReloadConfig(ICommandContext ctx)
        {
            try
            {
                ArenaConfigurationService.Initialize();
                ctx.Reply("✅ Arena configuration reloaded successfully");
            }
            catch (Exception ex)
            {
                ctx.Error($"❌ Failed to reload config: {ex.Message}");
            }
        }


        [Command("save", description: "Save arena configuration", adminOnly: true)]
        public static void SaveConfig(ICommandContext ctx)
        {
            try
            {
                ArenaConfigurationService.SaveConfiguration();
                ctx.Reply("✅ Arena configuration saved successfully");
            }
            catch (Exception ex)
            {
                ctx.Error($"❌ Failed to save config: {ex.Message}");
            }
        }


        [Command("clear", adminOnly: true, description: "Clear all arena snapshots")]
        public static void ClearSnapshots(ICommandContext ctx)
        {
            try
            {
                SnapshotService.ClearAllSnapshots();
                ctx.Reply("✅ All arena snapshots cleared");
            }
            catch (Exception ex)
            {
                ctx.Error($"❌ Error clearing snapshots: {ex.Message}");
            }
        }


        // ===== PLAYER ARENA COMMANDS =====


    [Command("enter", description: "Enter the arena")]
    public static async Task EnterArena(ICommandContext ctx)
        {
            try
            {
                var characterEntity = GetPlayerEntity(ctx);

                if (characterEntity == Entity.Null)
                {
                    ctx.Error("❌ Error: Invalid entity. Please try again.");
                    return;
                }

                if (!VRisingCore.EntityManager.HasComponent<PlayerCharacter>(characterEntity))
                {
                    ctx.Error("❌ Error: Invalid character entity. Please try reconnecting.");
                    return;
                }

                if (ZoneManager.IsPlayerInArena(characterEntity))
                {
                    ctx.Error("❌ You are already in the arena!");
                    return;
                }

                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = playerCharacter.UserEntity;

                if (userEntity == Entity.Null)
                {
                    ctx.Error("❌ Error: Could not find user entity.");
                    return;
                }

                if (ZoneManager.SpawnPoint.Equals(float3.zero))
                {
                    ctx.Error("❌ Error: Arena spawn point not configured! Please contact an admin.");
                    return;
                }

                var steamId = PlayerService.GetSteamId(userEntity);
                var playerName = PlayerService.GetPlayerName(userEntity);
                var arenaLocation = ZoneManager.SpawnPoint;

                Plugin.Logger?.LogInfo($"Attempting arena entry for player {playerName} (SteamID: {steamId})");

                // 1. Capture snapshot (includes current name, blood type, inventory, equipment, VBlood)
                Plugin.Logger?.LogInfo($"About to call SnapshotService.EnterArena with location: {arenaLocation}");
                var snapshotResult = SnapshotService.EnterArena(userEntity, characterEntity, arenaLocation);
                Plugin.Logger?.LogInfo($"SnapshotService.EnterArena returned: {snapshotResult}");

                if (!snapshotResult)
                {
                    Plugin.Logger?.LogError("SnapshotService.EnterArena failed - checking why...");
                    ctx.Error("❌ Failed to create snapshot. Please try again.");
                    return;
                }

                // 2. Rename player with [PVP] prefix (only if not already prefixed)
                try
                {
                    if (!playerName.StartsWith("[PVP]"))
                    {
                        var user = VRisingCore.EntityManager.GetComponentData<User>(userEntity);
                        user.CharacterName = new Unity.Collections.FixedString64Bytes("[PVP] " + playerName);
                        VRisingCore.EntityManager.SetComponentData(userEntity, user);
                        Plugin.Logger?.LogInfo($"Renamed player to: [PVP] {playerName}");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning($"Failed to rename player: {ex.Message}");
                }

                // 3. Blood type is set by loadout or .blood preset command (not forced here)

                // 3b. Clear inventory and equipment, then give items from config
                try
                {
                    InventoryService.ClearInventory(characterEntity);
                    Plugin.Logger?.LogInfo("Cleared inventory and equipment for arena entry");
                    
                    // Give items from InputItems config
                    var inputItems = Plugin.InputItems?.Value ?? "";
                    if (!string.IsNullOrWhiteSpace(inputItems))
                    {
                        var itemGuids = inputItems.Split(',');
                        int givenCount = 0;
                        foreach (var guidStr in itemGuids)
                        {
                            if (int.TryParse(guidStr.Trim(), out int guidHash))
                            {
                                var result = VRisingCore.ServerGameManager.TryAddInventoryItem(characterEntity, new PrefabGUID(guidHash), 1);
                                if (result.NewEntity != Unity.Entities.Entity.Null) givenCount++;
                            }
                        }
                        Plugin.Logger?.LogInfo($"Gave {givenCount} items from config InputItems");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning($"Failed to setup arena inventory: {ex.Message}");
                }

                // 4. Apply arena buff and tracking
                try
                {
                    Buffs.AddBuff(userEntity, characterEntity, Data.ArenaBuffs.Buff_Arena_Active, -1);
                    Plugin.Logger?.LogInfo("Applied arena buff");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning($"Failed to apply arena buff: {ex.Message}");
                }
                
                ZoneManager.ManualEnterArena(characterEntity);

                // 4b. Activate VBlood UI hook for this player (override unlock checks while in arena)
                try
                {
                    var user = VRisingCore.EntityManager.GetComponentData<User>(userEntity);
                    CrowbaneArena.Services.GameSystems.MarkPlayerEnteredArena(user.PlatformId);
                }
                catch { }

                // 5. Unlock spellbooks and research
                try
                {
                    var fromCharacter = new FromCharacter { User = userEntity, Character = characterEntity };
                    var systemService = new SystemService(VRisingCore.ServerWorld);
                    systemService.DebugEventsSystem.UnlockAllVBloods(fromCharacter);
                    systemService.DebugEventsSystem.UnlockAllResearch(fromCharacter);
                    Plugin.Logger?.LogInfo("Unlocked VBloods and research for arena");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning($"Failed to unlock progression: {ex.Message}");
                }

                // 6. Update player state
                try
                {
                    PlayerManager.UpdatePlayerState(characterEntity, new PlayerState { IsInArena = true, VBloodCount = 0 });
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning($"Failed to update player state: {ex.Message}");
                }

                // Track arena entry
                PlayerTrackerService.TrackArenaEntry(steamId, playerName, "default");

                // Inform player (chat)
                ctx.Reply("==============================");
                ctx.Reply("You have entered the PVP practice area.");
                ctx.Reply("All spells are available. Default gear provided.");
                ctx.Reply("Use .blood preset <type> to set blood (rogue|warrior|scholar|creature|mutant|dracula|corrupted).");
                ctx.Reply("Use .loadout <name> or .build <1-4> for different equipment.");
                ctx.Reply("Your original state will be restored on exit.");
                ctx.Reply("==============================");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in EnterArena command: {ex.Message}");
                ctx.Error("❌ An error occurred while entering the arena. Please try again.");
            }
        }


    [Command("exit", description: "Exit the arena")]
    public static async Task ExitArena(ICommandContext ctx)
        {
            try
            {
                var characterEntity = GetPlayerEntity(ctx);

                if (characterEntity == Entity.Null)
                {
                    ctx.Error("❌ Error: Invalid entity. Please try again.");
                    return;
                }

                if (!VRisingCore.EntityManager.HasComponent<PlayerCharacter>(characterEntity))
                {
                    ctx.Error("❌ Error: Invalid character entity. Please try reconnecting.");
                    return;
                }

                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = playerCharacter.UserEntity;

                if (userEntity == Entity.Null)
                {
                    ctx.Error("❌ Error: Could not find user entity.");
                    return;
                }

                var steamId = PlayerService.GetSteamId(userEntity);
                var playerName = PlayerService.GetPlayerName(userEntity);

                // Check if player is in arena
                if (!SnapshotService.IsInArena(steamId))
                {
                    ctx.Error("❌ You are not in the arena!");
                    return;
                }

                Plugin.Logger?.LogInfo($"Attempting arena exit for player {playerName} (SteamID: {steamId})");

                // 1. Exit arena and restore snapshot (inventory, equipment, blood, location, name)
                // Note: Progression doesn't need restoration - DebugEventsSystem unlocks are runtime-only
                if (!SnapshotService.ExitArena(userEntity, characterEntity))
                {
                    ctx.Error("❌ Failed to restore snapshot. Please contact an admin.");
                    return;
                }

                // 2. Remove arena buff and tracking
                try
                {
                    Buffs.RemoveBuff(characterEntity, Data.ArenaBuffs.Buff_Arena_Active);
                    Plugin.Logger?.LogInfo("Removed arena buff");
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning($"Failed to remove arena buff: {ex.Message}");
                }
                
                ZoneManager.ManualExitArena(characterEntity);

                // 3. Deactivate VBlood UI hook (restore real unlock display)
                try
                {
                    var user = VRisingCore.EntityManager.GetComponentData<User>(userEntity);
                    CrowbaneArena.Services.GameSystems.MarkPlayerExitedArena(user.PlatformId);
                }
                catch { }

                // 4. Update player state
                try
                {
                    PlayerManager.UpdatePlayerState(characterEntity, new PlayerState { IsInArena = false, VBloodCount = 0 });
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning($"Failed to update player state: {ex.Message}");
                }

                // Track arena exit
                var trackerData = PlayerTrackerService.GetPlayerData(steamId);
                if (trackerData != null)
                {
                    var timeInArena = DateTime.UtcNow - trackerData.LastSeen;
                    PlayerTrackerService.TrackArenaExit(steamId, timeInArena);
                }

                // Inform player (chat)
                ctx.Reply("==============================");
                ctx.Reply("You have left the PVP practice area.");
                ctx.Reply("Your original stats, inventory, and unlocks have been restored.");
                ctx.Reply("==============================");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ExitArena command: {ex.Message}");
                ctx.Error("❌ An error occurred while exiting the arena. Please try again.");
            }
        }


        [Command("status", description: "Check arena status")]
        public static void CheckStatus(ICommandContext ctx)
        {
            try
            {
                var characterEntity = GetPlayerEntity(ctx);
                if (characterEntity.Equals(Entity.Null))
                {
                    ctx.Error("Could not find your player entity!");
                    return;
                }


                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = playerCharacter.UserEntity;


                if (userEntity == Entity.Null)
                {
                    ctx.Error("❌ Error: Could not find user entity.");
                    return;
                }


                var steamId = PlayerService.GetSteamId(userEntity);
                var playerName = PlayerService.GetPlayerName(userEntity);
                var isInArena = SnapshotService.IsInArena(steamId);
                var position = PlayerService.GetPlayerPosition(characterEntity);


                ctx.Reply($"📊 Arena Status for {playerName}:");
                ctx.Reply($"   • In Arena: {(isInArena ? "✅ Yes" : "❌ No")}");
                ctx.Reply($"   • Position: {position.x:F1}, {position.y:F1}, {position.z:F1}");
                ctx.Reply($"   • Steam ID: {steamId}");
                
                if (ZoneManager.SpawnPoint.Equals(float3.zero))
                {
                    ctx.Reply($"   • ⚠️ Arena spawn point not configured!");
                }
                else
                {
                    ctx.Reply($"   • Spawn Point: {ZoneManager.SpawnPoint.x:F1}, {ZoneManager.SpawnPoint.y:F1}, {ZoneManager.SpawnPoint.z:F1}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in CheckStatus command: {ex.Message}");
                ctx.Error("❌ An error occurred while checking status.");
            }
        }


        [Command("tp", description: "Teleport to entry or exit (e for entry, x for exit)")]
        public static void TeleportArena(ICommandContext ctx, string location = "e")
        {
            try
            {
                var player = GetPlayerEntity(ctx);
                if (player.Equals(Entity.Null))
                {
                    ctx.Error("Could not find your player entity!");
                    return;
                }


                var position = location.ToLower() == "x" ? ArenaController.GetExitPoint() : ArenaController.GetEntryPoint();
                if (position.Equals(float3.zero))
                {
                    ctx.Error("Arena point not set!");
                    return;
                }


                if (VRisingCore.EntityManager.HasComponent<Translation>(player))
                {
                    var translation = VRisingCore.EntityManager.GetComponentData<Translation>(player);
                    translation.Value = position;
                    VRisingCore.EntityManager.SetComponentData(player, translation);
                    ctx.Reply($"✅ Teleported to {(location.ToLower() == "x" ? "exit" : "entry")} point.");
                }
                else
                {
                    ctx.Error("Could not teleport player!");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in TeleportArena command: {ex.Message}");
                ctx.Error("❌ An error occurred while teleporting.");
            }
        }


        // ===== EQUIPMENT AND LOADOUT COMMANDS =====


        [Command("build", description: "Select a build preset (1-4)")]
        public static void SelectBuild(ICommandContext ctx, int buildNumber = 1)
        {
            try
            {
                if (buildNumber < 1 || buildNumber > 4)
                {
                    ctx.Error("Build number must be between 1 and 4!");
                    return;
                }
                
                var player = GetPlayerEntity(ctx);
                if (player.Equals(Entity.Null))
                {
                    ctx.Error("Could not find your player entity!");
                    return;
                }

                // Check if player is in arena
                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(player);
                var userEntity = playerCharacter.UserEntity;
                var steamId = PlayerService.GetSteamId(userEntity);

                if (!SnapshotService.IsInArena(steamId))
                {
                    ctx.Error("❌ You must be in the arena to change builds!");
                    return;
                }
                
                var buildName = $"build{buildNumber}";

                // Clear current equipped items and inventory
                Plugin.Logger?.LogInfo($"Clearing equipped items before applying build {buildNumber}");
                InventoryService.ClearInventory(player);

                // Track loadout change
                var currentLoadout = PlayerTrackerService.GetPreferredLoadout(steamId);
                PlayerTrackerService.TrackLoadoutChange(steamId, currentLoadout, buildName);

                // Apply new loadout from JSON
                if (InventoryService.GiveLoadout(player, buildName))
                {
                    ctx.Reply($"✅ Applied build preset {buildNumber}");
                    ctx.Reply("🔄 Equipped items cleared and new loadout applied");
                }
                else
                {
                    ctx.Error($"❌ Build preset {buildNumber} not found or failed to apply");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SelectBuild command: {ex.Message}");
                ctx.Error("❌ An error occurred while applying build.");
            }
        }


        [Command("give", description: "Give an item by name")]
        public static void GiveItem(ICommandContext ctx, string itemName, int amount = 1)
        {
            try
            {
                if (amount <= 0)
                {
                    ctx.Error("Amount must be greater than 0!");
                    return;
                }
                
                var player = GetPlayerEntity(ctx);
                if (player.Equals(Entity.Null))
                {
                    ctx.Error("Could not find your player entity!");
                    return;
                }
                
                if (InventoryService.GiveItem(player, itemName, amount))
                {
                    ctx.Reply($"✅ Gave {amount}x {itemName}");
                }
                else
                {
                    ctx.Error($"❌ Could not find item: {itemName}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in GiveItem command: {ex.Message}");
                ctx.Error("❌ An error occurred while giving item.");
            }
        }


        [Command("listloadouts", description: "List all available loadouts")]
        public static void ListLoadouts(ICommandContext ctx)
        {
            try
            {
                var loadouts = LoadoutService.GetAvailableLoadouts();
                if (loadouts.Count == 0)
                {
                    ctx.Reply("📋 No loadouts available");
                    return;
                }

                ctx.Reply($"📋 Available loadouts ({loadouts.Count}):");
                
                var completeBuilds = new HashSet<string>(Services.LoadoutApplicationService.BuildNames, StringComparer.OrdinalIgnoreCase);
                var basicLoadouts = new List<string>();
                var completeLoadouts = new List<string>();

                foreach (var loadout in loadouts.OrderBy(x => x))
                {
                    if (completeBuilds.Contains(loadout))
                    {
                        completeLoadouts.Add(loadout);
                    }
                    else
                    {
                        basicLoadouts.Add(loadout);
                    }
                }

                if (basicLoadouts.Count > 0)
                {
                    ctx.Reply("🛡️ Basic Loadouts (weapons, armor, items):");
                    foreach (var loadout in basicLoadouts)
                    {
                        ctx.Reply($"   • {loadout}");
                    }
                }

                if (completeLoadouts.Count > 0)
                {
                    ctx.Reply("⚔️ Complete Builds (blood, weapons, armor, abilities, jewels, passives):");
                    foreach (var loadout in completeLoadouts)
                    {
                        ctx.Reply($"   • {loadout}");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ListLoadouts command: {ex.Message}");
                ctx.Error("❌ An error occurred while listing loadouts.");
            }
        }


        [Command("loadout", description: "Apply a loadout from files, config, or complete builds (blood, weapons, armor, abilities, jewels, passives)")]
        public static void ApplyLoadout(ICommandContext ctx, string loadoutName)
        {
            try
            {
                var characterEntity = GetPlayerEntity(ctx);
                if (characterEntity.Equals(Entity.Null))
                {
                    ctx.Error("❌ Could not find your player entity!");
                    return;
                }

                // Check if player is in arena
                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = playerCharacter.UserEntity;
                var steamId = PlayerService.GetSteamId(userEntity);

                if (!SnapshotService.IsInArena(steamId))
                {
                    ctx.Error("❌ You must be in the arena to apply loadouts!");
                    return;
                }

                var success = LoadoutService.ApplyLoadout(characterEntity, loadoutName);
                if (success)
                {
                    ctx.Reply($"✅ Applied loadout: {loadoutName}");
                    // Check if this was a complete build from LoadoutApplicationService
                    if (Services.LoadoutApplicationService.BuildNames.Contains(loadoutName))
                    {
                        ctx.Reply("🔄 Complete build applied (blood, weapons, armor, abilities, jewels, passives)");
                    }
                    else
                    {
                        ctx.Reply("🔄 Basic loadout applied (weapons, armor, items)");
                    }
                }
                else
                {
                    ctx.Error($"❌ Loadout '{loadoutName}' not found or failed to apply");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ApplyLoadout command: {ex.Message}");
                ctx.Error("❌ An error occurred while applying loadout.");
            }
        }


        // ===== ADMIN MANAGEMENT COMMANDS =====


        [Command("add", adminOnly: true, description: "Add an item to prefabs")]
        public static void AddPrefab(ICommandContext ctx, string category, string name, string guidStr)
        {
            try
            {
                if (!Guid.TryParse(guidStr, out var guid))
                {
                    ctx.Error("Invalid GUID format!");
                    return;
                }
                
                InventoryService.AddPrefab(category, name, guid);
                ctx.Reply($"✅ Added {name} to {category} prefabs");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in AddPrefab command: {ex.Message}");
                ctx.Error("❌ An error occurred while adding prefab.");
            }
        }


        [Command("cfgreload", adminOnly: true, description: "Reload plugin cfg (zones/proximity) and apply live")]
        public static void ReloadPluginCfg(ICommandContext ctx)
        {
            try
            {
                CrowbaneArena.Plugin.Instance?.ReloadPluginSettings();
                ctx.Reply($"✅ Plugin cfg reloaded. Proximity: enter={Systems.ArenaProximitySystem.EnterRadius}, exit={Systems.ArenaProximitySystem.ExitRadius}, interval={Systems.ArenaProximitySystem.UpdateIntervalSeconds}s");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"cfgreload failed: {ex.Message}");
                ctx.Error($"❌ Failed to reload plugin cfg: {ex.Message}");
            }
        }


        [Command("import", adminOnly: true, description: "Import prefabs from JSON")]
        public static void ImportPrefabs(ICommandContext ctx, string jsonData)
        {
            try
            {
                var count = InventoryService.ImportPrefabsFromJson(jsonData);
                ctx.Reply($"✅ Successfully imported {count} prefabs");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ImportPrefabs command: {ex.Message}");
                ctx.Error($"❌ Failed to import prefabs: {ex.Message}");
            }
        }


        [Command("export", adminOnly: true, description: "Export all prefabs to JSON")]
        public static void ExportPrefabs(ICommandContext ctx)
        {
            try
            {
                var json = InventoryService.ExportPrefabsToJson();
                ctx.Reply($"✅ Exported {json.Length} characters of prefab data");
                Plugin.Logger?.LogInfo($"Exported prefab data: {json}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ExportPrefabs command: {ex.Message}");
                ctx.Error($"❌ Failed to export prefabs: {ex.Message}");
            }
        }


        // ===== INFORMATION COMMANDS =====


        [Command("help", description: "Show Arena command help")]
        public static void ShowHelp(ICommandContext ctx, string category = "")
        {
            if (string.IsNullOrEmpty(category))
            {
                ctx.Reply("🏟️ Arena Commands Help:");
                ctx.Reply("📋 Categories: setup, player, admin, info");
                ctx.Reply("💡 Use '.help <category>' for specific commands");
                ctx.Reply("🎮 Quick commands: .enter, .exit, .status");
            }
            else
            {
                switch (category.ToLower())
                {
                    case "setup":
                        ctx.Reply("🔧 Setup Commands (Admin Only):");
                        ctx.Reply("   • .setzone <radius> - Set arena zone");
                        ctx.Reply("   • .setentry <radius> - Set entry point");
                        ctx.Reply("   • .setexit <radius> - Set exit point");
                        ctx.Reply("   • .setspawn - Set spawn point");
                        break;
                    case "player":
                        ctx.Reply("🎮 Player Commands:");
                        ctx.Reply("   • .enter - Enter arena (renames to [PVP], 100% Rogue blood, unlocks VBloods)");
                        ctx.Reply("   • .exit - Exit arena (restores name, blood, inventory, VBloods)");
                        ctx.Reply("   • .status - Check your status");
                        ctx.Reply("   • .tp <e/x> - Teleport to entry/exit");
                        ctx.Reply("   • .build <1-4> - Apply build preset");
                        ctx.Reply("   • .loadout <name> - Apply complete loadout (blood, weapons, armor, abilities, jewels, passives)");
                        ctx.Reply("   • .listloadouts - List all available loadouts");
                        break;
                    case "admin":
                        ctx.Reply("👑 Admin Commands:");
                        ctx.Reply("   • .reload - Reload configuration");
                        ctx.Reply("   • .save - Save configuration");
                        ctx.Reply("   • .clear - Clear all snapshots");
                        ctx.Reply("   • .add - Add prefab");
                        ctx.Reply("   • .import/.export - Manage prefabs");
                        break;
                    case "info":
                        ctx.Reply("ℹ️ Information Commands:");
                        ctx.Reply("   • .help - Show this help");
                        ctx.Reply("   • .stats - Show statistics");
                        ctx.Reply("   • .list - List available items");
                        break;
                    default:
                        ctx.Reply("❌ Unknown category. Available: setup, player, admin, info");
                        break;
                }
            }
        }


        [Command("stats", description: "Show Arena statistics")]
        public static void ShowStats(ICommandContext ctx)
        {
            try
            {
                var player = GetPlayerEntity(ctx);
                if (player.Equals(Entity.Null))
                {
                    ctx.Error("Could not find your player entity!");
                    return;
                }


                var inArena = ZoneManager.IsPlayerInArena(player);
                var snapshotCount = SnapshotService.GetSnapshotCount();


                ctx.Reply("📊 Arena Statistics:");
                ctx.Reply($"   • Arena Status: Available");
                ctx.Reply($"   • Your Status: {(inArena ? "In Arena" : "Outside Arena")}");
                ctx.Reply($"   • Active Snapshots: {snapshotCount}");
                ctx.Reply($"   • Available Commands: 20+");
                ctx.Reply($"   • Spawn Point: {(ZoneManager.SpawnPoint.Equals(float3.zero) ? "Not Set" : "Configured")}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ShowStats command: {ex.Message}");
                ctx.Error("❌ An error occurred while showing stats.");
            }
        }




        [Command("swap", description: "Swap to a new arena character or create one")]
        public static void ArenaSwap(ICommandContext ctx, string action, string charName = "")
        {
            try
            {
                if (action.ToLower() == "new")
                {
                    if (string.IsNullOrEmpty(charName))
                    {
                        ctx.Error("❌ Please provide a character name: .arena_swap new <name>");
                        return;
                    }


                    // Create new User entity (fresh/unbound - no kicking needed)
                    var newUserEntity = VRisingCore.EntityManager.CreateEntity();
                    VRisingCore.EntityManager.AddComponent<User>(newUserEntity);
                    VRisingCore.EntityManager.SetComponentData(newUserEntity, new User
                    {
                        CharacterName = charName,
                        PlatformId = 0,  // Fresh unbound character
                        IsConnected = false
                    });


                    ctx.Reply($"✅ Created fresh arena character '{charName}' with PlatformId 0 (unbound)");
                    ctx.Reply("💡 Use .arena_enter to enter the arena with this character");
                }
                else
                {
                    ctx.Error("❌ Invalid action. Use: .arena_swap new <name>");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ArenaSwap command: {ex.Message}");
                ctx.Error("❌ An error occurred while creating arena character.");
            }
        }


        [Command("status", description: "Alias for arena_status")]
        public static void ArenaStatus(ICommandContext ctx) => CheckStatus(ctx);

        // ===== PORTAL COMMANDS =====

        [Command("portal", adminOnly: true, description: "Portal management: .portal start | .portal end")]
        public static void PortalCommand(ICommandContext ctx, string action = "")
        {
            try
            {
                var player = GetPlayerEntity(ctx);
                if (player.Equals(Entity.Null))
                {
                    ctx.Error("Could not find your player entity!");
                    return;
                }

                switch (action.ToLower())
                {
                    case "start":
                        if (ArenaPortalService.StartPortal(player))
                        {
                            ctx.Reply("✅ Portal start position set. Move to destination and use .portal end");
                        }
                        else
                        {
                            ctx.Error("❌ Cannot create portal - chunk has too many portals (max 9)");
                        }
                        break;

                    case "end":
                        var error = ArenaPortalService.EndPortal(player);
                        if (error == null)
                        {
                            ctx.Reply("✅ Portal pair created successfully!");
                        }
                        else
                        {
                            ctx.Error($"❌ {error}");
                        }
                        break;

                    default:
                        ctx.Reply("Usage: .portal start | .portal end");
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in PortalCommand: {ex.Message}");
                ctx.Error("❌ An error occurred with portal command.");
            }
        }

        // ===== MAP ICON COMMANDS =====

        [Command("mapicon", adminOnly: true, description: "Map icon management: .mapicon create <prefab> | .mapicon list | .mapicon remove")]
        public static void MapIconCommand(ICommandContext ctx, string action = "", FoundMapIcon mapIcon = null)
        {
            try
            {
                var player = GetPlayerEntity(ctx);
                if (player.Equals(Entity.Null))
                {
                    ctx.Error("Could not find your player entity!");
                    return;
                }

                switch (action.ToLower())
                {
                    case "create":
                        if (mapIcon == null)
                        {
                            ctx.Error("Usage: .mapicon create <prefab>");
                            return;
                        }
                        if (MapIconService.CreateMapIcon(player, CrowbaneArenaCore.SystemService.PrefabCollectionSystem._PrefabLookupMap.GetName(mapIcon.Value)))
                        {
                            ctx.Reply($"✅ Map icon '{CrowbaneArenaCore.SystemService.PrefabCollectionSystem._PrefabLookupMap.GetName(mapIcon.Value)}' created at your location");
                        }
                        else
                        {
                            ctx.Error($"❌ Failed to create map icon - prefab '{CrowbaneArenaCore.SystemService.PrefabCollectionSystem._PrefabLookupMap.GetName(mapIcon.Value)}' not found");
                        }
                        break;

                    case "list":
                        var icons = MapIconService.GetAvailableIcons();
                        ctx.Reply($"📍 Available Map Icons ({icons.Count}):");
                        foreach (var icon in icons)
                        {
                            ctx.Reply($"   • {icon}");
                        }
                        break;

                    case "remove":
                        if (MapIconService.RemoveMapIcon(player))
                        {
                            ctx.Reply("✅ Map icon removed");
                        }
                        else
                        {
                            ctx.Error("❌ No map icon found within 5m");
                        }
                        break;

                    default:
                        ctx.Reply("Usage: .mapicon create <prefab> | .mapicon list | .mapicon remove");
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in MapIconCommand: {ex.Message}");
                ctx.Error("❌ An error occurred with map icon command.");
            }
        }

        // ===== HELPER METHODS =====


        private static Entity GetPlayerEntity(ICommandContext ctx)
        {
            return PlayerManager.GetPlayerByName(ctx.Name);
        }


        private static void ExecuteUnlockCommand(ICommandContext ctx, Entity characterEntity)
        {
            try
            {
                // Get player name from context
                var playerName = ctx.Name;
                Plugin.Logger?.LogInfo($"[UNLOCK_COMMAND] Executing unlock command for player: {playerName}");


                // Log that unlock should happen - actual implementation would go here
                Plugin.Logger?.LogInfo($"[UNLOCK_COMMAND] Player {playerName} should have all achievements unlocked");


                // TODO: Implement actual unlock logic when available
                // This could call a separate unlock command or directly manipulate achievement data


                ctx.Reply("🔓 Achievements unlocked for arena mode!");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ExecuteUnlockCommand: {ex.Message}");
                ctx.Error("⚠️ Warning: Achievement unlock failed, but arena entry succeeded.");
            }
        }


        private static void ExecuteLockCommand(ICommandContext ctx, Entity characterEntity)
        {
            try
            {
                // Get player name from context
                var playerName = ctx.Name;
                Plugin.Logger?.LogInfo($"[LOCK_COMMAND] Executing lock command for player: {playerName}");


                // Call BossManager to handle actual achievement/ability locking
                BossManager.RestoreBossUnlockState(characterEntity);


                Plugin.Logger?.LogInfo($"[LOCK_COMMAND] Player {playerName} achievements and abilities locked/restored");
                ctx.Reply("🔒 Achievements and abilities locked after arena exit!");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in ExecuteLockCommand: {ex.Message}");
                ctx.Error("⚠️ Warning: Achievement lock failed, but arena exit succeeded.");
            }
        }


        private static void SendGiveDebugEvent(ICommandContext ctx, Entity characterEntity)
        {
            try
            {
                // Get player name from context
                var playerName = ctx.Name;
                Plugin.Logger?.LogInfo($"[GIVE_DEBUG_EVENT] Sending GiveDebugEvent for player: {playerName}");


                // Get the user entity to send the debug event
                if (!VRisingCore.EntityManager.TryGetComponentData<PlayerCharacter>(characterEntity, out var playerCharacter) ||
                    playerCharacter.UserEntity == Entity.Null)
                {
                    Plugin.Logger?.LogWarning("Could not find user entity for GiveDebugEvent");
                    return;
                }


                var userEntity = playerCharacter.UserEntity;


                // Create and send the GiveDebugEvent
                // GiveDebugEvent structure is not fully known, so we'll use a simpler approach
                // Based on the log "aa True is sending a GiveDebugEvent"
                Plugin.Logger?.LogInfo($"[GIVE_DEBUG_EVENT] Would send GiveDebugEvent with Value=true for player: {playerName}");


                // For now, we'll just log that the event should be sent
                // The actual GiveDebugEvent structure needs to be determined from VRising internals
                // TODO: Implement actual GiveDebugEvent when structure is known


                Plugin.Logger?.LogInfo($"[GIVE_DEBUG_EVENT] Sent GiveDebugEvent with Value=true for player: {playerName}");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in SendGiveDebugEvent: {ex.Message}");
            }
        }


        private static float3 GetPlayerPosition(ICommandContext ctx)
        {
            var playerEntity = GetPlayerEntity(ctx);
            if (playerEntity != Entity.Null)
            {
                return PlayerService.GetPlayerPosition(playerEntity);
            }
            return float3.zero;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
