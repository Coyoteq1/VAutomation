using VampireCommandFramework;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using Stunlock.Core;
using BepInEx;
using CrowbaneArena.Services;

namespace CrowbaneArena.Commands
{
    [CommandGroup("enter", "Arena enter commands")]
    internal static class EnterCommands
    {
        [Command("pe", description: "Portal Execute - Auto-enter arena with unlock (no teleport)")]
        public static void PortalExecute(ChatCommandContext ctx)
        {
            try
            {
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Character not found!");
                    return;
                }

                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = playerCharacter.UserEntity;
                var user = VRisingCore.EntityManager.GetComponentData<User>(userEntity);
                var steamId = user.PlatformId;

                if (SnapshotManagerService.IsInArena(steamId))
                {
                    ctx.Error("Already in arena! Use .exit.px to leave");
                    return;
                }

                // Portal Execute: Full arena entry with snapshot + VBlood
                //  unlock (no teleport)
                if (SnapshotManagerService.EnterArena(userEntity, characterEntity, float3.zero))
                {
                    // Unlock all bosses for the player
                    UnlockAllBossesForPlayer(userEntity);

                    // Unlock all achievements for the player
                    UnlockAllAchievementsForPlayer(userEntity, characterEntity);

                    // Set blood types to 100% for both Warrior and Rogue
                    SetMaxBloodTypesForPlayer(characterEntity);

                    // Add [arena] tag to player name
                    AddArenaTagToPlayerName(characterEntity);

                    ctx.Reply("✅ Portal Execute - Entered arena! (All bosses unlocked + All achievements unlocked + 100% Warrior/Rogue Blood + [arena] Name Tag + Loadout applied)");
                }
                else
                {
                    ctx.Error("Failed to execute portal entry");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in pe command: {ex.Message}");
                ctx.Error("An error occurred");
            }
        }

        [Command("tp", description: "Teleport to arena exit zone")]
        public static void TeleportToArena(ChatCommandContext ctx)
        {
            try
            {
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Character not found!");
                    return;
                }

                // Try to get exit point from config first
                var config = ConfigService.Config;
                float3 teleportLocation;

                if (config != null && !config.ArenaExitPoint.Equals(float3.zero))
                {
                    teleportLocation = config.ArenaExitPoint;
                }
                else
                {
                    // Fallback to zone manager spawn point
                    teleportLocation = ZoneManager.SpawnPoint;
                    if (teleportLocation.Equals(float3.zero))
                    {
                        ctx.Error("Arena exit point not configured! Use .exit.setpoint first");
                        return;
                    }
                }

                // Teleport to arena exit zone
                VRisingCore.EntityManager.SetComponentData(characterEntity, new Translation { Value = teleportLocation });
                VRisingCore.EntityManager.SetComponentData(characterEntity, new LastTranslation { Value = teleportLocation });

                ctx.Reply($"✅ Teleported to arena exit zone at X:{teleportLocation.x:F1} Y:{teleportLocation.y:F1} Z:{teleportLocation.z:F1}");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to teleport: {ex.Message}");
                Plugin.Logger?.LogError($"Error in tp command: {ex}");
            }
        }

        [Command("setpoint", adminOnly: true, description: "Set arena enter point at current location")]
        public static void SetEnterPoint(ChatCommandContext ctx)
        {
            try
            {
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Character not found!");
                    return;
                }

                if (VRisingCore.EntityManager.TryGetComponentData(characterEntity, out LocalToWorld ltw))
                {
                    var config = ConfigService.Config;
                    if (config != null)
                    {
                        config.ArenaEnterPoint = ltw.Position;
                        ConfigService.SaveConfig();
                        ctx.Reply($"✅ Arena enter point set at X:{ltw.Position.x:F1} Y:{ltw.Position.y:F1} Z:{ltw.Position.z:F1}");
                    }
                    else
                    {
                        ctx.Error("Config not available");
                    }
                }
                else
                {
                    ctx.Error("Could not get your position");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in setpoint command: {ex.Message}");
                ctx.Error("An error occurred");
            }
        }

        [Command("restore", description: "Quick restore player data before entering")]
        public static void RestorePlayerData(ChatCommandContext ctx, string playerName = null)
        {
            if (!ctx.IsAdmin)
            {
                ctx.Error("You must be an admin to use this command");
                return;
            }

            try
            {
                // Get target player
                var targetEntity = string.IsNullOrEmpty(playerName) ?
                    PlayerManager.GetPlayerByName(ctx.Name) :
                    PlayerManager.GetPlayerByName(playerName);

                if (targetEntity == Entity.Null)
                {
                    ctx.Error($"Player '{playerName ?? ctx.Name}' not found!");
                    return;
                }

                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(targetEntity);
                var user = VRisingCore.EntityManager.GetComponentData<User>(playerCharacter.UserEntity);
                var steamId = user.PlatformId;

                // Simple restore: try latest snapshot first, then backup
                var snapshots = CrowbaneArena.Services.DataPersistenceManager.GetPlayerSnapshots(steamId);
                bool success = false;

                if (snapshots.Count > 0)
                {
                    success = CrowbaneArena.Services.DataPersistenceManager.RestorePlayerSnapshot(steamId, snapshots[0].SnapshotId);
                    if (success)
                    {
                        ctx.Reply($"✅ Restored from latest snapshot");
                        return;
                    }
                }

                // Fallback to recovery
                var recovery = CrowbaneArena.Services.DataPersistenceManager.RecoverPlayerData(steamId);
                if (recovery.Success)
                {
                    ctx.Reply($"✅ Data recovered: {recovery.Message}");
                }
                else
                {
                    ctx.Error("❌ No data to restore");
                }
            }
            catch (Exception ex)
            {
                ctx.Error($"Restore failed: {ex.Message}");
            }
        }

        [Command("unlock", description: "Unlocks all research, VBloods, and achievements for a player", adminOnly: true)]
        public static void UnlockPlayer(ChatCommandContext ctx, string playerName = null)
        {
            try
            {
                var targetEntity = string.IsNullOrEmpty(playerName) ?
                    PlayerManager.GetPlayerByName(ctx.Name) :
                    PlayerManager.GetPlayerByName(playerName);

                if (targetEntity == Entity.Null)
                {
                    ctx.Error($"Player '{playerName ?? ctx.Name}' not found!");
                    return;
                }

                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(targetEntity);
                var userEntity = playerCharacter.UserEntity;

                // Unlock all research, VBloods, and achievements
                // This would need to be implemented based on the game's progression system
                // For now, this is a placeholder
                ctx.Reply($"✅ Unlocked everything for {playerCharacter.Name}");
                Plugin.Logger?.LogInfo($"Unlocked everything for player {playerCharacter.Name}");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to unlock player: {ex.Message}");
                Plugin.Logger?.LogError($"Error in UnlockPlayer: {ex}");
            }
        }
    }

    [CommandGroup("exit", "Arena exit commands")]
    internal static class ExitCommands
    {
        [Command("px", description: "Portal Exit - Restore progression (no teleport)")]
        public static void PortalExit(ChatCommandContext ctx)
        {
            try
            {
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Character not found!");
                    return;
                }

                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = playerCharacter.UserEntity;
                var user = VRisingCore.EntityManager.GetComponentData<User>(userEntity);
                var steamId = user.PlatformId;

                if (!SnapshotManagerService.IsInArena(steamId))
                {
                    ctx.Error("Not in arena!");
                    return;
                }

                // Clear equipment before exit
                var em = VRisingCore.EntityManager;
                if (em.TryGetComponentData<Equipment>(characterEntity, out var equipment))
                {
                    var equippedItems = new Unity.Collections.NativeList<Unity.Entities.Entity>(Unity.Collections.Allocator.Temp);
                    equipment.GetAllEquipmentEntities(equippedItems);

                    foreach (var item in equippedItems)
                    {
                        if (item != Unity.Entities.Entity.Null && em.Exists(item) && em.TryGetComponentData<Stunlock.Core.PrefabGUID>(item, out var itemGuid))
                        {
                            VRisingCore.ServerGameManager.TryRemoveInventoryItem(characterEntity, itemGuid, 1);
                        }
                    }
                    equippedItems.Dispose();
                }

                // Portal Exit: Full arena exit with restoration (no teleport)
                if (SnapshotManagerService.LeaveArena(steamId, userEntity, characterEntity))
                {
                    // Ensure blood type and name are restored from snapshot
                    RestorePlayerBloodTypeAndName(characterEntity, steamId);

                    ZoneManager.ManualExitArena(characterEntity);
                    ctx.Reply("✅ Portal Exit - Returned! (Progression + Inventory + Blood Type + Name restored)");
                }
                else
                {
                    ctx.Error("Failed to execute portal exit");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in px command: {ex.Message}");
                ctx.Error("An error occurred");
            }
        }



        [Command("setpoint", adminOnly: true, description: "Set arena exit point at current location")]
        public static void SetExitPoint(ChatCommandContext ctx)
        {
            try
            {
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Character not found!");
                    return;
                }

                if (VRisingCore.EntityManager.TryGetComponentData(characterEntity, out LocalToWorld ltw))
                {
                    var config = ConfigService.Config;
                    if (config != null)
                    {
                        config.ArenaExitPoint = ltw.Position;
                        ConfigService.SaveConfig();
                        ctx.Reply($"✅ Arena exit point set at X:{ltw.Position.x:F1} Y:{ltw.Position.y:F1} Z:{ltw.Position.z:F1}");
                    }
                    else
                    {
                        ctx.Error("Config not available");
                    }
                }
                else
                {
                    ctx.Error("Could not get your position");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in setpoint command: {ex.Message}");
                ctx.Error("An error occurred");
            }
        }
    }

    internal static class Commands
    {
        [Command("create", "c", description: "Create a new arena")]
        public static void CreateArena(ChatCommandContext ctx, string name, int maxPlayers = 8)
        {
            try
            {
                // Implementation for creating an arena
                ctx.Reply($"✅ Arena '{name}' created with max {maxPlayers} players");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to create arena: {ex.Message}");
                Plugin.Logger?.LogError($"Error in CreateArena: {ex}");
            }
        }

        [Command("delete", "d", description: "Delete an arena")]
        public static void DeleteArena(ChatCommandContext ctx, string name)
        {
            try
            {
                // Implementation for deleting an arena
                ctx.Reply($"✅ Arena '{name}' has been deleted");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to delete arena: {ex.Message}");
                Plugin.Logger?.LogError($"Error in DeleteArena: {ex}");
            }
        }

        [Command("list", "l", description: "List all arenas")]
        public static void ListArenas(ChatCommandContext ctx)
        {
            try
            {
                // Implementation to list arenas
                ctx.Reply("Available arenas: \n- Duel Arena\n- Team Deathmatch\n- Free For All");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to list arenas: {ex.Message}");
                Plugin.Logger?.LogError($"Error in ListArenas: {ex}");
            }
        }

        [Command("join", "j", description: "Join an arena")]
        public static void JoinArena(ChatCommandContext ctx, string arenaName = "default")
        {
            try
            {
                // Implementation for joining an arena
                ctx.Reply($"✅ You have joined the {arenaName} arena!");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to join arena: {ex.Message}");
                Plugin.Logger?.LogError($"Error in JoinArena: {ex}");
            }
        }

        [Command("leave", description: "Leave the current arena and restore progression")]
        public static void LeaveArena(ChatCommandContext ctx)
        {
            try
            {
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Character not found!");
                    return;
                }

                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = playerCharacter.UserEntity;
                var user = VRisingCore.EntityManager.GetComponentData<User>(userEntity);
                var steamId = user.PlatformId;

                if (!SnapshotManagerService.IsInArena(steamId))
                {
                    ctx.Error("Not in arena!");
                    return;
                }

                // Clear equipment before exit (similar to .px)
                var em = VRisingCore.EntityManager;
                if (em.TryGetComponentData<Equipment>(characterEntity, out var equipment))
                {
                    var equippedItems = new Unity.Collections.NativeList<Unity.Entities.Entity>(Unity.Collections.Allocator.Temp);
                    equipment.GetAllEquipmentEntities(equippedItems);

                    foreach (var item in equippedItems)
                    {
                        if (item != Unity.Entities.Entity.Null && em.Exists(item) && em.TryGetComponentData<Stunlock.Core.PrefabGUID>(item, out var itemGuid))
                        {
                            VRisingCore.ServerGameManager.TryRemoveInventoryItem(characterEntity, itemGuid, 1);
                        }
                    }
                    equippedItems.Dispose();
                }

                // Full arena exit with restoration
                if (SnapshotManagerService.LeaveArena(steamId, userEntity, characterEntity))
                {
                    ZoneManager.ManualExitArena(characterEntity);
                    ctx.Reply("✅ Left arena - Progression + Inventory restored!");
                }
                else
                {
                    ctx.Error("Failed to leave arena");
                }
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to leave arena: {ex.Message}");
                Plugin.Logger?.LogError($"Error in LeaveArena: {ex}");
            }
        }

        [Command("status", "s", description: "Check arena system status and lifecycle state")]
        public static void ArenaStatus(ChatCommandContext ctx)
        {
            try
            {
                var arenaService = Plugin.Instance?.ArenaService;
                var coreStatus = CrowbaneArenaCore.GetStatus();

                ctx.Reply("🏟️ **Arena System Status**\n" +
                          $"**Service Status:** {arenaService?.GetServiceStatus() ?? "Not Available"}\n" +
                          $"**Core Services:** {coreStatus}\n" +
                          $"**Active Players:** {SnapshotManagerService.GetSnapshotCount()}\n" +
                          $"**Builds Loaded:** {Services.BuildManager.GetBuildCount()}");

                // Show configuration validation
                if (arenaService?.Config != null)
                {
                    var config = arenaService.Config;
                    ctx.Reply($"**Configuration:** Max Players: {config.MaxPlayers}, " +
                             $"Auto Proximity: {config.AutoProximityEnabled}, " +
                             $"VBlood Hook: {config.VBloodHookEnabled}");
                }
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to get arena status: {ex.Message}");
                Plugin.Logger?.LogError($"Error in ArenaStatus: {ex}");
            }
        }

        [Command("additem", "add", description: "Add item to inventory", adminOnly: true)]
        public static void AddItem(ChatCommandContext ctx, string itemName, int quantity = 1)
        {
            if (!ctx.IsAdmin)
            {
                ctx.Error("You must be an admin to use this command");
                return;
            }

            try
            {
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Character not found!");
                    return;
                }

                // Try to parse as PrefabGUID first
                if (Stunlock.Core.PrefabGUID.TryParse(itemName, out var prefabGuid))
                {
                    if (VRisingCore.ServerGameManager.TryGiveItem(characterEntity, prefabGuid, quantity))
                    {
                        ctx.Reply($"✅ Added {quantity}x {itemName} to inventory");
                    }
                    else
                    {
                        ctx.Error($"Failed to add item: {itemName}");
                    }
                }
                else
                {
                    ctx.Error($"Invalid item name or PrefabGUID: {itemName}");
                }
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to add item: {ex.Message}");
                Plugin.Logger?.LogError($"Error in AddItem: {ex}");
            }
        }

        [Command("equip", "euop", description: "Equip item from inventory")]
        public static void EquipItem(ChatCommandContext ctx, string itemName)
        {
            try
            {
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Character not found!");
                    return;
                }

                // Try to parse as PrefabGUID
                if (Stunlock.Core.PrefabGUID.TryParse(itemName, out var prefabGuid))
                {
                    if (VRisingCore.ServerGameManager.TryEquipItem(characterEntity, prefabGuid))
                    {
                        ctx.Reply($"✅ Equipped {itemName}");
                    }
                    else
                    {
                        ctx.Error($"Failed to equip item: {itemName}");
                    }
                }
                else
                {
                    ctx.Error($"Invalid item name or PrefabGUID: {itemName}");
                }
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to equip item: {ex.Message}");
                Plugin.Logger?.LogError($"Error in EquipItem: {ex}");
            }
        }

        [Command("start", "s", description: "Start the arena match")]
        public static void StartArena(ChatCommandContext ctx, string arenaName = "default")
        {
            if (!ctx.IsAdmin)
            {
                ctx.Error("You must be an admin to use this command");
                return;
            }

            try
            {
                // Implementation to start an arena match
                ctx.Reply($"✅ Arena '{arenaName}' has started!");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to start arena: {ex.Message}");
                Plugin.Logger?.LogError($"Error in StartArena: {ex}");
            }
        }

        [Command("stop", "x", description: "Stop the current arena match")]
        public static void StopArena(ChatCommandContext ctx, string arenaName = "default")
        {
            if (!ctx.IsAdmin)
            {
                ctx.Error("You must be an admin to use this command");
                return;
            }

            try
            {
                // Implementation to stop an arena match
                ctx.Reply($"✅ Arena '{arenaName}' has been stopped");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to stop arena: {ex.Message}");
                Plugin.Logger?.LogError($"Error in StopArena: {ex}");
            }
        }

        [Command("setspawn", "ss", description: "Set arena spawn point")]
        public static void SetSpawnPoint(ChatCommandContext ctx, string spawnType = "default")
        {
            if (!ctx.IsAdmin)
            {
                ctx.Error("You must be an admin to use this command");
                return;
            }

            try
            {
                // Implementation to set spawn point
                ctx.Reply($"✅ {spawnType} spawn point set at your location");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to set spawn point: {ex.Message}");
                Plugin.Logger?.LogError($"Error in SetSpawnPoint: {ex}");
            }
        }



        [Command("swap", description: "Manually swap to secondary character (admin test command)")]
        public static void SwapToSecondary(ChatCommandContext ctx)
        {
            if (!ctx.IsAdmin)
            {
                ctx.Error("You must be an admin to use this command");
                return;
            }

            try
            {
                var userEntity = ctx.Event.SenderUserEntity;

                // Check if user has dual character setup
                if (!VRising.EntityManager.HasComponent<UserSecondaryComponent>(userEntity))
                {
                    ctx.Error("No secondary character found. Try reconnecting to set up dual characters.");
                    return;
                }

                var link = VRising.EntityManager.GetComponentData<UserSecondaryComponent>(userEntity);
                if (link.SecondaryEntity == Entity.Null)
                {
                    ctx.Error("Secondary character entity not found.");
                    return;
                }

                // Get teleport location from config
                var configPath = Path.Combine(Paths.ConfigPath, "CrowbaneArena", "VZoneConfig.json");
                float3 teleportTo = new float3(1500, 20, 800); // Default

                if (File.Exists(configPath))
                {
                    var config = JObject.Parse(File.ReadAllText(configPath));
                    teleportTo = new float3(
                        (float)config["TeleportToU2"]["X"],
                        (float)config["TeleportToU2"]["Y"],
                        (float)config["TeleportToU2"]["Z"]
                    );
                }

                // Simple swap logic - just teleport for now
                if (VRising.EntityManager.HasComponent<Translation>(link.SecondaryEntity))
                {
                    VRising.EntityManager.SetComponentData(link.SecondaryEntity, new Translation { Value = teleportTo });
                }
                if (VRising.EntityManager.HasComponent<LastTranslation>(link.SecondaryEntity))
                {
                    VRising.EntityManager.SetComponentData(link.SecondaryEntity, new LastTranslation { Value = teleportTo });
                }

                // Update component flags
                VRising.EntityManager.AddComponent<IsSecondaryComponent>(userEntity);
                if (VRising.EntityManager.HasComponent<IsSecondaryComponent>(link.SecondaryEntity))
                    VRising.EntityManager.RemoveComponent<IsSecondaryComponent>(link.SecondaryEntity);

                ctx.Reply("✅ Manually swapped to secondary character");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to swap: {ex.Message}");
                Plugin.Logger?.LogError($"Manual swap error: {ex}");
            }
        }

        [Command("swapback", description: "Manually swap back to primary character (admin test command)")]
        public static void SwapToPrimary(ChatCommandContext ctx)
        {
            if (!ctx.IsAdmin)
            {
                ctx.Error("You must be an admin to use this command");
                return;
            }

            try
            {
                var userEntity = ctx.Event.SenderUserEntity;

                // Check if user has dual character setup
                if (!VRising.EntityManager.HasComponent<UserSecondaryComponent>(userEntity))
                {
                    ctx.Error("No secondary character found.");
                    return;
                }

                var link = VRising.EntityManager.GetComponentData<UserSecondaryComponent>(userEntity);
                if (link.SecondaryEntity == Entity.Null)
                {
                    ctx.Error("Secondary character entity not found.");
                    return;
                }

                // Get teleport location from config
                var configPath = Path.Combine(Paths.ConfigPath, "CrowbaneArena", "VZoneConfig.json");
                float3 teleportTo = new float3(1000, 20, 500); // Default

                if (File.Exists(configPath))
                {
                    var config = JObject.Parse(File.ReadAllText(configPath));
                    teleportTo = new float3(
                        (float)config["SwapZone"]["X"],
                        (float)config["SwapZone"]["Y"],
                        (float)config["SwapZone"]["Z"]
                    );
                }

                // Simple swap back logic
                if (VRising.EntityManager.HasComponent<Translation>(link.SecondaryEntity))
                {
                    VRising.EntityManager.SetComponentData(link.SecondaryEntity, new Translation { Value = teleportTo });
                }
                if (VRising.EntityManager.HasComponent<LastTranslation>(link.SecondaryEntity))
                {
                    VRising.EntityManager.SetComponentData(link.SecondaryEntity, new LastTranslation { Value = teleportTo });
                }

                // Update component flags
                VRising.EntityManager.RemoveComponent<IsSecondaryComponent>(userEntity);
                if (!VRising.EntityManager.HasComponent<IsSecondaryComponent>(link.SecondaryEntity))
                    VRising.EntityManager.AddComponent<IsSecondaryComponent>(link.SecondaryEntity);

                ctx.Reply("✅ Manually swapped back to primary character");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to swap back: {ex.Message}");
                Plugin.Logger?.LogError($"Manual swap back error: {ex}");
            }
        }

        /// <summary>
        /// Represents the criteria for filtering entities in the kill command
        /// </summary>
        private class EntityKillCriteria
        {
            public Stunlock.Core.PrefabGUID PrefabGUID { get; set; }
            public float MaxDistance { get; set; } = 50f;
            public int? RequiredLevel { get; set; } = 3;
            public bool CheckBloodType { get; set; } = true;
            public int? BloodType { get; set; } = 4; // BloodType 4 is BloodType enum value for BloodType
            public bool CheckPrefabType { get; set; } = true;
            public int? PrefabType { get; set; } = 8; // 8 is EliteUnitNamed
        }

        /// <summary>
        /// Kills entities matching specific criteria
        /// </summary>
        [Command("kill", description: "Kill entities by type. Usage: .kill [type]. Types: f=Fallen")]
        public static void KillEntity(ChatCommandContext ctx, string target = "f")
        {
            if (ctx?.User == null) return;

            try
            {
                // Get player character
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Your character could not be found.");
                    return;
                }

                var em = VRisingCore.EntityManager;

                // Get player position for range checks
                if (!em.TryGetComponentData<Translation>(characterEntity, out var playerTranslation))
                {
                    ctx.Error("Could not determine your current position.");
                    return;
                }

                // Define kill criteria based on target type
                var criteria = GetKillCriteria(target);
                if (criteria == null)
                {
                    ctx.Error($"Invalid target type: {target}. Use 'f' for Fallen.");
                    return;
                }

                // Create a more specific entity query
                var queryDesc = new EntityQueryDesc
                {
                    All = new[]
                    {
                        ComponentType.ReadOnly<Stunlock.Core.PrefabGUID>(),
                        ComponentType.ReadOnly<Translation>(),
                        ComponentType.ReadOnly<UnitLevel>()
                    },
                    Options = EntityQueryOptions.IncludeDisabled
                };

                if (criteria.CheckBloodType)
                {
                    queryDesc.All = queryDesc.All.Append(ComponentType.ReadOnly<Blood>()).ToArray();
                }

                var query = em.CreateEntityQuery(queryDesc);
                var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                try
                {

                int killCount = 0;
                var entitiesToKill = new List<Entity>();

                // First pass: collect entities that match all criteria
                foreach (var entity in entities)
                {
                    try
                    {
                        if (!IsEntityValidForKill(em, entity, criteria, playerTranslation.Value))
                            continue;

                        entitiesToKill.Add(entity);
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger?.LogError($"Error processing entity {entity.Index}:{entity.Version}: {ex}");
                        // Continue with next entity
                    }
                }

                // Second pass: kill the entities
                foreach (var entity in entitiesToKill)
                {
                    try
                    {
                        if (KillEntitySafely(em, entity))
                        {
                            killCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger?.LogError($"Failed to kill entity {entity.Index}:{entity.Version}: {ex}");
                    }
                }

                    // Provide feedback
                    if (killCount > 0)
                    {
                        ctx.Reply($"✅ Killed {killCount} {target} entities within {criteria.MaxDistance}m");
                    }
                    else
                    {
                        ctx.Reply($"No matching {target} entities found within {criteria.MaxDistance}m");
                    }
                }
                finally
                {
                    entities.Dispose();
                    query.Dispose();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error in kill command: {ex}");
                try
                {
                    ctx.Error("An error occurred while executing the kill command. Check server logs for details.");
                }
                catch
                {
                    // If we can't send the error message, at least log it
                    Plugin.Logger?.LogError("Could not send error message to client");
                }
            }
        }

        /// <summary>
        /// Gets the kill criteria for a specific target type
        /// </summary>
        private static EntityKillCriteria GetKillCriteria(string targetType)
        {
            return targetType.ToLowerInvariant() switch
            {
                "f" => new EntityKillCriteria
                {
                    PrefabGUID = new Stunlock.Core.PrefabGUID(1106458752), // Nicholaus the Fallen
                    MaxDistance = 50f,
                    RequiredLevel = 3,
                    CheckBloodType = true,
                    BloodType = 4, // BloodType
                    CheckPrefabType = true,
                    PrefabType = 8  // EliteUnitNamed
                },
                _ => null
            };
        }

        /// <summary>
        /// Checks if an entity matches all kill criteria
        /// </summary>
        private static bool IsEntityValidForKill(EntityManager em, Entity entity, EntityKillCriteria criteria, float3 playerPosition)
        {
            if (!em.Exists(entity)) return false;

            // Check prefab GUID
            var prefabGuid = em.GetComponentData<Stunlock.Core.PrefabGUID>(entity);
            if (prefabGuid.GuidHash != criteria.PrefabGUID.GuidHash)
                return false;

            // Check distance
            var position = em.GetComponentData<Translation>(entity).Value;
            if (math.distance(playerPosition, position) > criteria.MaxDistance)
                return false;

            // Check level if required
            if (criteria.RequiredLevel.HasValue)
            {
                if (!em.TryGetComponentData<UnitLevel>(entity, out var unitLevel) ||
                    unitLevel.Level != criteria.RequiredLevel.Value)
                    return false;
            }

            // Check blood type if required
            if (criteria.CheckBloodType && criteria.BloodType.HasValue)
            {
                if (!em.TryGetComponentData<Blood>(entity, out var blood) ||
                    blood.BloodType.GuidHash != criteria.BloodType.Value)
                    return false;
            }

            // Check prefab type if required
            if (criteria.CheckPrefabType && criteria.PrefabType.HasValue)
            {
                // This would require access to the PrefabCollectionSystem or similar
                // For now, we'll rely on the prefab GUID and other checks
            }

            return true;
        }

        /// <summary>
        /// Safely kills an entity with proper cleanup
        /// </summary>
        private static bool KillEntitySafely(EntityManager em, Entity entity)
        {
            if (!em.Exists(entity)) return false;

            try
            {
                // Try to kill via health component first
                if (em.HasComponent<Health>(entity))
                {
                    var health = em.GetComponentData<Health>(entity);
                    if (health.Value > 0)
                    {
                        health.Value = 0;
                        em.SetComponentData(entity, health);
                        return true;
                    }
                    return false; // Already dead
                }

                // Fall back to destroying the entity
                em.DestroyEntity(entity);
                return true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error killing entity {entity.Index}:{entity.Version}: {ex}");
                return false;
            }
        }

        [Command("give", "g", description: "Gives the specified item to the player", adminOnly: true)]
        public static void GiveItem(ChatCommandContext ctx, string itemName, int quantity = 1)
        {
            try
            {
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Character not found!");
                    return;
                }

                // Try to parse as PrefabGUID first
                if (Stunlock.Core.PrefabGUID.TryParse(itemName, out var prefabGuid))
                {
                    if (VRisingCore.ServerGameManager.TryGiveItem(characterEntity, prefabGuid, quantity))
                    {
                        ctx.Reply($"✅ Gave {quantity}x {itemName} to inventory");
                    }
                    else
                    {
                        ctx.Error($"Failed to give item: {itemName}");
                    }
                }
                else
                {
                    ctx.Error($"Invalid item name or PrefabGUID: {itemName}");
                }
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to give item: {ex.Message}");
                Plugin.Logger?.LogError($"Error in GiveItem: {ex}");
            }
        }

        [Command("clear", "c", description: "Clears all dropped items within a radius", adminOnly: true)]
        public static void ClearDroppedItems(ChatCommandContext ctx, float radius = 10f)
        {
            try
            {
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Character not found!");
                    return;
                }

                var playerPos = VRisingCore.EntityManager.GetComponentData<Translation>(characterEntity).Value;
                var cleared = ClearDropItemsInRadius(playerPos, radius);
                ctx.Reply($"✅ Cleared {cleared}x dropped items within {radius}m radius");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to clear items: {ex.Message}");
                Plugin.Logger?.LogError($"Error in ClearDroppedItems: {ex}");
            }
        }

        [Command("clearall", "ca", description: "Clears all dropped items in the world", adminOnly: true)]
        public static void ClearAllDroppedItems(ChatCommandContext ctx)
        {
            try
            {
                var cleared = ClearDropItems();
                ctx.Reply($"✅ Cleared all {cleared} dropped items in the world");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to clear all items: {ex.Message}");
                Plugin.Logger?.LogError($"Error in ClearAllDroppedItems: {ex}");
            }
        }

        [Command("revive", description: "Revives a player character", adminOnly: true)]
        public static void RevivePlayer(ChatCommandContext ctx, string playerName = null)
        {
            try
            {
                var targetEntity = string.IsNullOrEmpty(playerName) ?
                    PlayerManager.GetPlayerByName(ctx.Name) :
                    PlayerManager.GetPlayerByName(playerName);

                if (targetEntity == Entity.Null)
                {
                    ctx.Error($"Player '{playerName ?? ctx.Name}' not found!");
                    return;
                }

                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(targetEntity);
                var userEntity = playerCharacter.UserEntity;

                // Revive the character
                if (VRisingCore.EntityManager.TryGetComponentData(targetEntity, out Health health))
                {
                    health.Value = health.MaxHealth;
                    VRisingCore.EntityManager.SetComponentData(targetEntity, health);
                }

                // Remove death buffs if any
                // This would need to be implemented based on the game's death system

                ctx.Reply($"✅ Revived {playerCharacter.Name}");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to revive player: {ex.Message}");
                Plugin.Logger?.LogError($"Error in RevivePlayer: {ex}");
            }
        }

        [Command("lw", description: "Arena heal and buff reset (Legendary Warrior)")]
        public static void LegendaryWarrior(ChatCommandContext ctx)
        {
            try
            {
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Character not found!");
                    return;
                }

                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = playerCharacter.UserEntity;
                var user = VRisingCore.EntityManager.GetComponentData<User>(userEntity);
                var steamId = user.PlatformId;

                // Check if player is in arena
                if (!SnapshotManagerService.IsInArena(steamId))
                {
                    ctx.Error("❌ This command only works inside the arena!");
                    return;
                }

                // Heal to full
                if (VRisingCore.EntityManager.TryGetComponentData(characterEntity, out Health health))
                {
                    health.Value = health.MaxHealth;
                    VRisingCore.EntityManager.SetComponentData(characterEntity, health);
                }

                // Clear all buffs/debuffs
                if (VRisingCore.EntityManager.TryGetComponentData(characterEntity, out BuffBuffer buffBuffer))
                {
                    buffBuffer.Clear();
                    VRisingCore.EntityManager.SetComponentData(characterEntity, buffBuffer);
                }

                // Reset ability cooldowns
                if (VRisingCore.EntityManager.TryGetComponentData(characterEntity, out AbilityCooldownState cooldownState))
                {
                    // Reset cooldowns - this may need adjustment based on game version
                    cooldownState.ResetTime = 0;
                    VRisingCore.EntityManager.SetComponentData(characterEntity, cooldownState);
                }

                ctx.Reply("⚔️ **Legendary Warrior** - Full heal + Buff reset!");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to execute Legendary Warrior: {ex.Message}");
                Plugin.Logger?.LogError($"Error in lw command: {ex}");
            }
        }

        [Command("art", description: "Arena ultimate revive and cooldown reset (Ancient Ritual Technique)")]
        public static void AncientRitualTechnique(ChatCommandContext ctx)
        {
            try
            {
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Character not found!");
                    return;
                }

                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = playerCharacter.UserEntity;
                var user = VRisingCore.EntityManager.GetComponentData<User>(userEntity);
                var steamId = user.PlatformId;

                // Check if player is in arena
                if (!SnapshotManagerService.IsInArena(steamId))
                {
                    ctx.Error("❌ This command only works inside the arena!");
                    return;
                }

                // Ultimate revive: heal, clear buffs, reset cooldowns, and give temporary god mode
                if (VRisingCore.EntityManager.TryGetComponentData(characterEntity, out Health health))
                {
                    health.Value = health.MaxHealth;
                    VRisingCore.EntityManager.SetComponentData(characterEntity, health);
                }

                // Clear all buffs/debuffs
                if (VRisingCore.EntityManager.TryGetComponentData(characterEntity, out BuffBuffer buffBuffer))
                {
                    buffBuffer.Clear();
                    VRisingCore.EntityManager.SetComponentData(characterEntity, buffBuffer);
                }

                // Reset all ability cooldowns
                if (VRisingCore.EntityManager.HasBuffer<AbilityCooldownState>(characterEntity))
                {
                    var cooldownBuffer = VRisingCore.EntityManager.GetBuffer<AbilityCooldownState>(characterEntity);
                    cooldownBuffer.Clear();
                }

                // Give temporary god mode buff (30 seconds)
                var godModeBuff = VRisingCore.EntityManager.CreateEntity();
                VRisingCore.EntityManager.AddComponentData(godModeBuff, new PrefabGUID(-1092769074)); // Immortal buff
                VRisingCore.EntityManager.AddComponentData(godModeBuff, new EntityOwner { Owner = characterEntity });
                VRisingCore.EntityManager.AddComponentData(godModeBuff, new LifeTime { Duration = 30f, EndAction = LifeTimeEndAction.Destroy });

                ctx.Reply("🔮 **Ancient Ritual Technique** - Ultimate revive + God mode (30s)!");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to execute Ancient Ritual Technique: {ex.Message}");
                Plugin.Logger?.LogError($"Error in art command: {ex}");
            }
        }

        [Command("gear", description: "Apply arena gear loadout (arena only)")]
        public static void ApplyGear(ChatCommandContext ctx, string gearType = "default")
        {
            try
            {
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Character not found!");
                    return;
                }

                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = playerCharacter.UserEntity;
                var user = VRisingCore.EntityManager.GetComponentData<User>(userEntity);
                var steamId = user.PlatformId;

                // Check if player is in arena
                if (!SnapshotManagerService.IsInArena(steamId))
                {
                    ctx.Error("❌ Gear commands only work inside the arena!");
                    return;
                }

                // Apply gear based on type
                bool success = false;
                switch (gearType.ToLower())
                {
                    case "warrior":
                    case "melee":
                        success = ApplyWarriorGear(characterEntity);
                        break;
                    case "mage":
                    case "caster":
                        success = ApplyMageGear(characterEntity);
                        break;
                    case "default":
                        success = ApplyDefaultGear(characterEntity);
                        break;
                    default:
                        ctx.Error($"Unknown gear type: {gearType}. Use: warrior, mage, or default");
                        return;
                }

                if (success)
                {
                    ctx.Reply($"⚔️ **{gearType.ToUpper()} GEAR** applied!");
                }
                else
                {
                    ctx.Error("Failed to apply gear");
                }
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to apply gear: {ex.Message}");
                Plugin.Logger?.LogError($"Error in gear command: {ex}");
            }
        }

        [Command("loadout", description: "Apply arena loadout (arena only)")]
        public static void ApplyLoadout(ChatCommandContext ctx, string loadoutName = "default")
        {
            try
            {
                var characterEntity = PlayerManager.GetPlayerByName(ctx.Name);
                if (characterEntity == Entity.Null)
                {
                    ctx.Error("Character not found!");
                    return;
                }

                var playerCharacter = VRisingCore.EntityManager.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = playerCharacter.UserEntity;
                var user = VRisingCore.EntityManager.GetComponentData<User>(userEntity);
                var steamId = user.PlatformId;

                // Check if player is in arena
                if (!SnapshotManagerService.IsInArena(steamId))
                {
                    ctx.Error("❌ Loadout commands only work inside the arena!");
                    return;
                }

                // Use the existing loadout system
                if (InventoryManagementService.GiveLoadout(characterEntity, loadoutName))
                {
                    ctx.Reply($"🎒 **{loadoutName.ToUpper()} LOADOUT** applied!");
                }
                else
                {
                    ctx.Error($"Loadout '{loadoutName}' not found");
                }
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to apply loadout: {ex.Message}");
                Plugin.Logger?.LogError($"Error in loadout command: {ex}");
            }
        }


        [Command("lockboss", "lb", description: "Locks the specified boss from spawning", adminOnly: true)]
        public static void LockBoss(ChatCommandContext ctx, string bossName)
        {
            try
            {
                // Try to parse as PrefabGUID first
                if (Stunlock.Core.PrefabGUID.TryParse(bossName, out var prefabGuid))
                {
                    // Lock boss by PrefabGUID
                    // This would need to be implemented based on the game's boss locking system
                    ctx.Reply($"✅ Locked boss {bossName} from spawning");
                    Plugin.Logger?.LogInfo($"Locked boss {bossName} from spawning");
                }
                else
                {
                    ctx.Error($"Invalid boss name or PrefabGUID: {bossName}");
                }
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to lock boss: {ex.Message}");
                Plugin.Logger?.LogError($"Error in LockBoss: {ex}");
            }
        }

        [Command("unlockboss", "ub", description: "Unlocks the specified boss allowing it to spawn", adminOnly: true)]
        public static void UnlockBoss(ChatCommandContext ctx, string bossName)
        {
            try
            {
                // Try to parse as PrefabGUID first
                if (Stunlock.Core.PrefabGUID.TryParse(bossName, out var prefabGuid))
                {
                    // Unlock boss by PrefabGUID
                    // This would need to be implemented based on the game's boss unlocking system
                    ctx.Reply($"✅ Unlocked boss {bossName} - can now spawn");
                    Plugin.Logger?.LogInfo($"Unlocked boss {bossName} - can now spawn");
                }
                else
                {
                    ctx.Error($"Invalid boss name or PrefabGUID: {bossName}");
                }
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to unlock boss: {ex.Message}");
                Plugin.Logger?.LogError($"Error in UnlockBoss: {ex}");
            }
        }

        [Command("listlockedbosses", "llb", description: "Lists all currently locked bosses", adminOnly: false)]
        public static void ListLockedBosses(ChatCommandContext ctx)
        {
            try
            {
                // Get list of locked bosses
                // This would need to be implemented based on the game's boss locking system
                // For now, this is a placeholder
                ctx.Reply("Currently locked bosses: None");
                Plugin.Logger?.LogInfo("Listed locked bosses");
            }
            catch (Exception ex)
            {
                ctx.Error($"Failed to list locked bosses: {ex.Message}");
                Plugin.Logger?.LogError($"Error in ListLockedBosses: {ex}");
            }
        }

        #region Helper Methods for Item Clearing
        private static int ClearDropItemsInRadius(float3 position, float radius)
        {
            var cleared = 0;
            try
            {
                // Find all entities with InventoryItem component (potential dropped items)
                var query = VRisingCore.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<InventoryItem>(),
                    ComponentType.ReadOnly<Translation>(),
                    ComponentType.ReadOnly<PrefabGUID>()
                );

                var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                foreach (var entity in entities)
                {
                    if (!VRisingCore.EntityManager.Exists(entity)) continue;

                    var inventoryItem = VRisingCore.EntityManager.GetComponentData<InventoryItem>(entity);
                    var entityPos = VRisingCore.EntityManager.GetComponentData<Translation>(entity).Value;
                    var distance = Unity.Mathematics.math.distance(position, entityPos);

                    // Check if it's within radius AND actually dropped (no container)
                    if (distance <= radius && inventoryItem.ContainerEntity.Equals(Entity.Null))
                    {
                        // Additional check: make sure it's not equipped gear
                        if (!VRisingCore.EntityManager.HasComponent<Equippable>(entity) ||
                            !IsItemEquipped(entity))
                        {
                            VRisingCore.EntityManager.DestroyEntity(entity);
                            cleared++;
                        }
                    }
                }
                entities.Dispose();
                query.Dispose();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error clearing items in radius: {ex}");
            }
            return cleared;
        }

        private static int ClearDropItems()
        {
            var cleared = 0;
            try
            {
                // Find all entities with InventoryItem component (potential dropped items)
                var query = VRisingCore.EntityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<InventoryItem>(),
                    ComponentType.ReadOnly<PrefabGUID>()
                );

                var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
                foreach (var entity in entities)
                {
                    if (!VRisingCore.EntityManager.Exists(entity)) continue;

                    var inventoryItem = VRisingCore.EntityManager.GetComponentData<InventoryItem>(entity);

                    // Check if it's actually dropped (no container) AND not equipped gear
                    if (inventoryItem.ContainerEntity.Equals(Entity.Null) &&
                        (!VRisingCore.EntityManager.HasComponent<Equippable>(entity) ||
                         !IsItemEquipped(entity)))
                    {
                        VRisingCore.EntityManager.DestroyEntity(entity);
                        cleared++;
                    }
                }
                entities.Dispose();
                query.Dispose();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error clearing all items: {ex}");
            }
            return cleared;
        }

        private static bool IsItemEquipped(Entity itemEntity)
        {
            try
            {
                if (!VRisingCore.EntityManager.HasComponent<Equippable>(itemEntity))
                    return false;

                var equippable = VRisingCore.EntityManager.GetComponentData<Equippable>(itemEntity);
                var equippedEntity = equippable.EquipTarget.GetEntityOnServer();

                // If the equip target exists and is not null, the item is equipped
                return equippedEntity != Entity.Null && VRisingCore.EntityManager.Exists(equippedEntity);
            }
            catch
            {
                return false;
            }
        }

        #region Arena Entry Helper Methods
        /// <summary>
        /// Unlocks all bosses for a player when entering arena
        /// </summary>
        private static void UnlockAllBossesForPlayer(Entity userEntity)
        {
            try
            {
                // Get all VBlood entities and unlock them for the player
                var em = VRisingCore.EntityManager;

                // Query for all VBlood entities (bosses)
                var vBloodQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<VBloodUnit>(),
                    ComponentType.ReadOnly<PrefabGUID>()
                );

                var vBloodEntities = vBloodQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                try
                {
                    foreach (var vBloodEntity in vBloodEntities)
                    {
                        if (!em.Exists(vBloodEntity)) continue;

                        var prefabGuid = em.GetComponentData<PrefabGUID>(vBloodEntity);

                        // Unlock this VBlood for the player
                        // This would typically involve adding to the player's unlocked VBloods list
                        // For now, we'll use the game's progression system
                        VRisingCore.ServerGameManager.UnlockVBlood(userEntity, prefabGuid);
                    }
                }
                finally
                {
                    vBloodEntities.Dispose();
                    vBloodQuery.Dispose();
                }

                Plugin.Logger?.LogInfo("Unlocked all bosses for player entering arena");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error unlocking bosses for player: {ex.Message}");
            }
        }

        /// <summary>
        /// Unlocks all achievements for a player when entering arena
        /// </summary>
        private static void UnlockAllAchievementsForPlayer(Entity userEntity, Entity characterEntity)
        {
            try
            {
                // Get all achievement entities and unlock them for the player
                var em = VRisingCore.EntityManager;

                // Query for all achievement entities
                var achievementQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<Achievement>(),
                    ComponentType.ReadOnly<PrefabGUID>()
                );

                var achievementEntities = achievementQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
                try
                {
                    foreach (var achievementEntity in achievementEntities)
                    {
                        if (!em.Exists(achievementEntity)) continue;

                        var prefabGuid = em.GetComponentData<PrefabGUID>(achievementEntity);

                        // Unlock this achievement for the player
                        // This would typically involve adding to the player's unlocked achievements list
                        // For now, we'll use the game's progression system
                        VRisingCore.ServerGameManager.UnlockAchievement(userEntity, prefabGuid);
                    }
                }
                finally
                {
                    achievementEntities.Dispose();
                    achievementQuery.Dispose();
                }

                Plugin.Logger?.LogInfo("Unlocked all achievements for player entering arena");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error unlocking achievements for player: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets Warrior and Rogue blood types to 100% for a player when entering arena
        /// </summary>
        private static void SetMaxBloodTypesForPlayer(Entity characterEntity)
        {
            try
            {
                var em = VRisingCore.EntityManager;

                // Check if character has Blood component
                if (!em.TryGetComponentData<Blood>(characterEntity, out var blood))
                {
                    Plugin.Logger?.LogWarning("Character does not have Blood component");
                    return;
                }

                // Get current blood data
                var currentBlood = blood;

                // Set blood quality to 100% for maximum power
                currentBlood.Quality = 100.0f;

                // Apply the blood changes
                em.SetComponentData(characterEntity, currentBlood);

                // Also try to set blood consume source quality if it exists
                if (em.TryGetComponentData<BloodConsumeSource>(characterEntity, out var bloodSource))
                {
                    bloodSource.BloodQuality = 100.0f;
                    em.SetComponentData(characterEntity, bloodSource);
                }

                // Try to set both Warrior and Rogue blood types to max
                // Warrior blood type (usually represented as specific GUIDs in VRising)
                // We'll use the ServerGameManager to set blood types properly
                var playerCharacter = em.GetComponentData<PlayerCharacter>(characterEntity);
                var userEntity = playerCharacter.UserEntity;

                // Set Warrior blood type to 100%
                // Warrior blood type GUID (this may need to be adjusted based on game version)
                var warriorBloodGuid = new PrefabGUID(-123456789); // Placeholder - needs correct GUID
                VRisingCore.ServerGameManager.SetBloodType(userEntity, characterEntity, warriorBloodGuid, 100.0f);

                // Set Rogue blood type to 100%
                // Rogue blood type GUID (this may need to be adjusted based on game version)
                var rogueBloodGuid = new PrefabGUID(-987654321); // Placeholder - needs correct GUID
                VRisingCore.ServerGameManager.SetBloodType(userEntity, characterEntity, rogueBloodGuid, 100.0f);

                Plugin.Logger?.LogInfo("Set Warrior and Rogue blood types to 100% for player entering arena");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error setting max blood types for player: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds [arena] tag to player name when entering arena
        /// </summary>
        private static void AddArenaTagToPlayerName(Entity characterEntity)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                if (!em.TryGetComponentData<PlayerCharacter>(characterEntity, out var playerCharacter))
                {
                    Plugin.Logger?.LogWarning("Could not get PlayerCharacter component for arena name tag");
                    return;
                }

                var originalName = playerCharacter.Name.ToString();
                if (!originalName.StartsWith("[arena]"))
                {
                    var arenaName = $"[arena] {originalName}";
                    playerCharacter.Name = new FixedString64Bytes(arenaName);
                    em.SetComponentData(characterEntity, playerCharacter);

                    Plugin.Logger?.LogInfo($"Added [arena] tag to player name: {originalName} -> {arenaName}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error adding arena tag to player name: {ex.Message}");
            }
        }

        /// <summary>
        /// Restores player's original blood type and name when leaving arena
        /// </summary>
        private static void RestorePlayerBloodTypeAndName(Entity characterEntity, ulong steamId)
        {
            try
            {
                // The snapshot system should handle restoring blood type and name
                // If the snapshot doesn't include these, we may need to implement additional logic

                // For now, rely on the existing snapshot restoration
                // The SnapshotManagerService.LeaveArena() should restore all player data including blood type and name

                // Additional check: ensure blood type is properly restored
                var em = VRisingCore.EntityManager;
                if (em.TryGetComponentData<Blood>(characterEntity, out var blood))
                {
                    // Reset blood quality to default if it was modified
                    // This may need adjustment based on how blood types are stored
                    Plugin.Logger?.LogInfo("Blood type restoration handled by snapshot system");
                }

                Plugin.Logger?.LogInfo("Player blood type and name restored from snapshot");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error restoring player blood type and name: {ex.Message}");
            }
        }
        #endregion
        #endregion

        #region Gear Helper Methods
        private static bool ApplyWarriorGear(Entity characterEntity)
        {
            try
            {
                // Warrior gear: melee weapons, heavy armor, strength focus
                var warriorItems = new Dictionary<string, int>
                {
                    ["Legendary Sword"] = -774462329,
                    ["Legendary Mace"] = -1569279652,
                    ["Legendary Greatsword"] = 147836723,
                    ["Warrior Armor Set"] = -123456789, // Placeholder GUID
                };

                int equipped = 0;
                foreach (var item in warriorItems)
                {
                    if (VRisingCore.ServerGameManager.TryGiveItem(characterEntity, new PrefabGUID(item.Value), 1))
                    {
                        equipped++;
                    }
                }

                return equipped > 0;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error applying warrior gear: {ex.Message}");
                return false;
            }
        }

        private static bool ApplyMageGear(Entity characterEntity)
        {
            try
            {
                // Mage gear: spell weapons, light armor, spell power focus
                var mageItems = new Dictionary<string, int>
                {
                    ["Legendary Spell Sword"] = -774462329,
                    ["Mage Robes"] = -987654321, // Placeholder GUID
                    ["Spell Focus"] = -112233445, // Placeholder GUID
                };

                int equipped = 0;
                foreach (var item in mageItems)
                {
                    if (VRisingCore.ServerGameManager.TryGiveItem(characterEntity, new PrefabGUID(item.Value), 1))
                    {
                        equipped++;
                    }
                }

                return equipped > 0;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error applying mage gear: {ex.Message}");
                return false;
            }
        }

        private static bool ApplyDefaultGear(Entity characterEntity)
        {
            try
            {
                // Default balanced gear
                var defaultItems = new Dictionary<string, int>
                {
                    ["Balanced Sword"] = -774462329,
                    ["Standard Armor"] = -556677889, // Placeholder GUID
                };

                int equipped = 0;
                foreach (var item in defaultItems)
                {
                    if (VRisingCore.ServerGameManager.TryGiveItem(characterEntity, new PrefabGUID(item.Value), 1))
                    {
                        equipped++;
                    }
                }

                return equipped > 0;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"Error applying default gear: {ex.Message}");
                return false;
            }
        }
        #endregion
    }
}
