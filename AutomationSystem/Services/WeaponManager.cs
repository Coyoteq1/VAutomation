using System;
using System.Collections.Generic;
using Unity.Entities;
using ProjectM;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Manages weapon statistics and data for players
    /// </summary>
    public static class WeaponManager
    {
        /// <summary>
        /// Contains weapon statistics management functionality
        /// </summary>
        public static class WeaponStats
        {
            /// <summary>
            /// Types of weapon statistics that can be tracked
            /// </summary>
            public enum WeaponStatType
            {
                Damage,
                AttackSpeed,
                SpellPower,
                MovementSpeed,
                PhysicalPower,
                SpellCooldown,
                Durability,
                Level,
                Experience
            }
        }

        /// <summary>
        /// Attempts to get weapon statistics for a player
        /// </summary>
        /// <param name="steamId">Player's Steam ID</param>
        /// <param name="weaponStats">Output dictionary containing weapon stats by type</param>
        /// <returns>True if stats were found, false otherwise</returns>
        public static bool TryGetPlayerWeaponStats(ulong steamId, out Dictionary<WeaponType, List<WeaponStats.WeaponStatType>> weaponStats)
        {
            weaponStats = new Dictionary<WeaponType, List<WeaponStats.WeaponStatType>>();

            try
            {
                var userEntity = PlayerService.FindUserBySteamId(steamId);
                if (userEntity == Entity.Null)
                {
                    VRisingCore.Log?.LogWarning($"Player with SteamID {steamId} not found for weapon stats retrieval.");
                    return false;
                }

                var characterEntity = PlayerService.GetPlayerCharacter(userEntity);
                if (characterEntity == Entity.Null)
                {
                    VRisingCore.Log?.LogWarning($"Character entity not found for SteamID {steamId}.");
                    return false;
                }

                // Get player's equipment to analyze weapons
                if (VRisingCore.EntityManager.TryGetComponentData<Equipment>(characterEntity, out var equipment))
                {
                    var equippedWeapons = new List<Entity>();
                    equipment.GetAllEquipmentEntities(equippedWeapons);

                    foreach (var weaponEntity in equippedWeapons)
                    {
                        if (weaponEntity != Entity.Null && VRisingCore.EntityManager.Exists(weaponEntity))
                        {
                            // Try to determine weapon type and extract stats
                            if (TryGetWeaponTypeAndStats(weaponEntity, out var weaponType, out var stats))
                            {
                                weaponStats[weaponType] = stats;
                            }
                        }
                    }
                }

                return weaponStats.Count > 0;
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Error retrieving weapon stats for player {steamId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets weapon statistics for a player
        /// </summary>
        /// <param name="steamId">Player's Steam ID</param>
        /// <param name="data">Dictionary containing weapon stats to set</param>
        /// <returns>True if stats were set successfully, false otherwise</returns>
        public static bool SetPlayerWeaponStats(ulong steamId, Dictionary<WeaponType, List<WeaponStats.WeaponStatType>> data)
        {
            try
            {
                var userEntity = PlayerService.FindUserBySteamId(steamId);
                if (userEntity == Entity.Null)
                {
                    VRisingCore.Log?.LogWarning($"Player with SteamID {steamId} not found for weapon stats setting.");
                    return false;
                }

                var characterEntity = PlayerService.GetPlayerCharacter(userEntity);
                if (characterEntity == Entity.Null)
                {
                    VRisingCore.Log?.LogWarning($"Character entity not found for SteamID {steamId}.");
                    return false;
                }

                // Apply weapon stats to player's equipment
                if (VRisingCore.EntityManager.TryGetComponentData<Equipment>(characterEntity, out var equipment))
                {
                    var equippedWeapons = new List<Entity>();
                    equipment.GetAllEquipmentEntities(equippedWeapons);

                    foreach (var weaponEntity in equippedWeapons)
                    {
                        if (weaponEntity != Entity.Null && VRisingCore.EntityManager.Exists(weaponEntity))
                        {
                            if (TryGetWeaponType(weaponEntity, out var weaponType) && data.ContainsKey(weaponType))
                            {
                                ApplyWeaponStats(weaponEntity, data[weaponType]);
                            }
                        }
                    }
                }

                VRisingCore.Log?.LogInfo($"Weapon stats updated for player {steamId}");
                return true;
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Error setting weapon stats for player {steamId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to get weapon type and stats from a weapon entity
        /// </summary>
        private static bool TryGetWeaponTypeAndStats(Entity weaponEntity, out WeaponType weaponType, out List<WeaponStats.WeaponStatType> stats)
        {
            weaponType = WeaponType.Sword; // Default
            stats = new List<WeaponStats.WeaponStatType>();

            try
            {
                if (!VRisingCore.EntityManager.TryGetComponentData<Stunlock.Core.PrefabGUID>(weaponEntity, out var prefabGuid))
                    return false;

                // Determine weapon type from prefab GUID
                if (!TryGetWeaponTypeFromGuid(prefabGuid.GuidHash, out weaponType))
                    return false;

                // Extract available stats (this would need to be implemented based on actual weapon components)
                stats = GetAvailableWeaponStats(weaponEntity);
                return true;
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Error getting weapon type and stats: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to get weapon type from a weapon entity
        /// </summary>
        private static bool TryGetWeaponType(Entity weaponEntity, out WeaponType weaponType)
        {
            weaponType = WeaponType.Sword; // Default

            try
            {
                if (!VRisingCore.EntityManager.TryGetComponentData<Stunlock.Core.PrefabGUID>(weaponEntity, out var prefabGuid))
                    return false;

                return TryGetWeaponTypeFromGuid(prefabGuid.GuidHash, out weaponType);
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Error getting weapon type: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Determines weapon type from prefab GUID
        /// </summary>
        private static bool TryGetWeaponTypeFromGuid(int guidHash, out WeaponType weaponType)
        {
            // This would need to be implemented with actual GUID mappings
            // For now, return a default
            weaponType = WeaponType.Sword;
            return true;
        }

        /// <summary>
        /// Gets available weapon stats for a weapon entity
        /// </summary>
        private static List<WeaponStats.WeaponStatType> GetAvailableWeaponStats(Entity weaponEntity)
        {
            var stats = new List<WeaponStats.WeaponStatType>();

            try
            {
                // Check for various weapon stat components and add available stats
                // This is a placeholder implementation

                // Always include basic stats
                stats.Add(WeaponStats.WeaponStatType.Damage);
                stats.Add(WeaponStats.WeaponStatType.Level);
                stats.Add(WeaponStats.WeaponStatType.Durability);

                // Add conditional stats based on weapon type/components
                if (VRisingCore.EntityManager.HasComponent<SpellMod>(weaponEntity))
                {
                    stats.Add(WeaponStats.WeaponStatType.SpellPower);
                    stats.Add(WeaponStats.WeaponStatType.SpellCooldown);
                }

                if (VRisingCore.EntityManager.HasComponent<MovementMod>(weaponEntity))
                {
                    stats.Add(WeaponStats.WeaponStatType.MovementSpeed);
                }

                return stats;
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Error getting weapon stats: {ex.Message}");
                return stats;
            }
        }

        /// <summary>
        /// Applies weapon stats to a weapon entity
        /// </summary>
        private static void ApplyWeaponStats(Entity weaponEntity, List<WeaponStats.WeaponStatType> stats)
        {
            try
            {
                // Apply each stat type to the weapon entity
                // This is a placeholder implementation that would need to be
                // implemented based on actual weapon stat components

                foreach (var stat in stats)
                {
                    switch (stat)
                    {
                        case WeaponStats.WeaponStatType.Damage:
                            // Apply damage modifications
                            break;
                        case WeaponStats.WeaponStatType.SpellPower:
                            // Apply spell power modifications
                            break;
                        case WeaponStats.WeaponStatType.MovementSpeed:
                            // Apply movement speed modifications
                            break;
                        // Add cases for other stat types
                    }
                }
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Error applying weapon stats: {ex.Message}");
            }
        }
    }
}
