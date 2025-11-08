using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;
using Stunlock.Core;
using Unity.Entities;
using Unity.Transforms;
using ProjectM;
using ProjectM.Network;
using ProjectM.Terrain;

namespace CrowbaneArena
{
    public class PlayerSnapshot
    {
        public string SnapshotId { get; set; } = Guid.NewGuid().ToString();

        // Schema metadata for versioning
        public int SchemaVersion { get; set; } = 1;
        public string CreatedAtUtc { get; set; } = System.DateTime.UtcNow.ToString("o");

        public float OriginalX { get; set; }
        public float OriginalY { get; set; }
        public float OriginalZ { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public float3 OriginalLocation
        {
            get => new float3(OriginalX, OriginalY, OriginalZ);
            set { OriginalX = value.x; OriginalY = value.y; OriginalZ = value.z; }
        }

        public string OriginalName { get; set; } = "";
        public float Health { get; set; }
        public int BloodTypeGuid { get; set; }
        public float BloodQuality { get; set; }
        public Dictionary<int, ItemData> InventoryItems { get; set; } = new();
        public Dictionary<int, EquipmentData> EquipmentItems { get; set; } = new();
        public List<EquippedItemData> EquippedItems { get; set; } = new();
        public List<int> AbilityGuids { get; set; } = new();
        public int Experience { get; set; }
        public int Level { get; set; }
        public bool IsInArena { get; set; }
        public string LoadoutName { get; set; } = "";

        // Enhanced progression tracking
        public List<int> UnlockedVBloods { get; set; } = new();
        public List<int> UnlockedRecipes { get; set; } = new();
        public List<int> UnlockedResearch { get; set; } = new();
        public List<int> CompletedQuests { get; set; } = new();
        public List<int> ActiveQuests { get; set; } = new();
    }

    public class ItemData
    {
        public int ItemGuidHash { get; set; }
        public int Amount { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public PrefabGUID ItemGUID
        {
            get => new PrefabGUID(ItemGuidHash);
            set => ItemGuidHash = value.GuidHash;
        }
    }

    public class EquipmentData
    {
        public int ItemGuidHash { get; set; }
        public int SlotIndex { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public PrefabGUID ItemGUID
        {
            get => new PrefabGUID(ItemGuidHash);
            set => ItemGuidHash = value.GuidHash;
        }
    }

    public class EquippedItemData : ItemData
    {
        // Slot index or enum value identifying the equipment slot
        public int SlotId { get; set; }
        
        // Additional metadata for better restoration
        public int Quality { get; set; } = 0;
        public string SlotName { get; set; } = "";
    }

    public class Player
    {
        private Entity _user;
        private Entity _character;
        private ulong _steamID;

        public Entity User
        {
            get
            {
                return _user;
            }
            set { SetUser(value); }
        }

        public Entity Character
        {
            get
            {
                return _character;
            }
            set { SetCharacter(value); }
        }

        public ulong SteamID
        {
            get => _steamID == default && _user != default && CrowbaneArenaCore.EntityManager.Exists(_user) ? CrowbaneArenaCore.EntityManager.GetComponentData<User>(_user).PlatformId : _steamID;
            set => _steamID = value;
        }

        public Entity Clan => GetClan();

        public string Name => GetName();
        public string FullName => GetFullName();
        public int Level => GetLevel();
        public int RecordLevel
        {
            get
            {
                // Placeholder for RecordLevelSystem - needs implementation
                var level = RecordLevelSystem.GetRecord(SteamID);
                if (level == 0 && Level != 0)
                {
                    RecordLevelSystem.SetRecord(SteamID);
                    return Level;
                }
                return level;
            }
        }
        public int Height => GetHeight();
        public bool IsAdmin => GetIsAdmin();
        public bool IsAdminCapable => GetIsAdminCapable();
        public bool IsOnline => GetIsOnline();
        public bool IsAlive => GetIsAlive();
        public Entity Inventory => GetInventory();
        public Equipment Equipment => GetEquipment();
        public List<Entity> EquipmentEntities => GetEquipmentEntities();
        public Entity ControlledEntity => GetControlledEntity();

        public float3 Position => GetPosition();
        public float3 AimPosition => GetAimPosition();
        public int2 TilePosition => GetTilePosition();
        public Team Team => GetTeam();

        public ProjectM.Terrain.WorldRegionType WorldZone => GetWorldZone();
        public string WorldZoneString => GetWorldZoneString();

        private void SetUser(Entity user)
        {
            if (_user != user)
            {
                if (!CrowbaneArenaCore.EntityManager.Exists(user))
                {
                    throw new Exception("Invalid User");
                }

                _user = user;

                _steamID = CrowbaneArenaCore.EntityManager.GetComponentData<User>(_user).PlatformId;
                if (!CrowbaneArenaCore.EntityManager.Exists(_character))
                {
                    _character = CrowbaneArenaCore.EntityManager.GetComponentData<User>(_user).LocalCharacter._Entity;
                }
            }
        }

        private void SetCharacter(Entity character)
        {
            if (CrowbaneArenaCore.EntityManager.Exists(character))
            {
                _character = character;
                if (!CrowbaneArenaCore.EntityManager.Exists(_user))
                {
                    var userEntity = CrowbaneArenaCore.EntityManager.GetComponentData<PlayerCharacter>(_character).UserEntity;
                    if (CrowbaneArenaCore.EntityManager.Exists(userEntity))
                    {
                        _user = userEntity;
                        _steamID = CrowbaneArenaCore.EntityManager.GetComponentData<User>(_user).PlatformId;
                    }
                    else
                    {
                        throw new Exception("Tried to load a player without a valid user");
                    }
                }
            }
        }

        private string GetName()
        {
            var userData = CrowbaneArenaCore.EntityManager.GetComponentData<User>(User);
            var name = userData.CharacterName.ToString();
            if (name == "")
            {
                name = $"[No Character - {userData.PlatformId}]";
            }
            return name;
        }

        public string GetFullName()
        {
            var playerCharacter = CrowbaneArenaCore.EntityManager.GetComponentData<PlayerCharacter>(Character);
            if (!playerCharacter.SmartClanName.IsEmpty)
            {
                return $"{playerCharacter.SmartClanName} {Name}";
            }
            else
            {
                return Name;
            }
        }

        private int GetHeight()
        {
            return VRisingCore.EntityManager.GetComponentData<TilePosition>(Character).HeightLevel;
        }
        private int GetLevel()
        {
            return (int)VRisingCore.EntityManager.GetComponentData<Equipment>(Character).GetFullLevel();
        }

        private Entity GetClan()
        {
            return VRisingCore.EntityManager.GetComponentData<User>(User).ClanEntity._Entity;
        }

        private Team GetTeam()
        {
            // TODO: Implement proper team retrieval
            return default;
        }

        private bool GetIsAdmin()
        {
            return VRisingCore.EntityManager.GetComponentData<User>(User).IsAdmin;
        }

        private bool GetIsAdminCapable()
        {
            // Placeholder - needs AdminAuthSystem access
            return false;
        }

        private bool GetIsOnline()
        {
            if (VRisingCore.EntityManager.Exists(User))
            {
                return VRisingCore.EntityManager.GetComponentData<User>(User).IsConnected;
            }
            else
            {
                return false;
            }
        }
        private float3 GetAimPosition()
        {
            return VRisingCore.EntityManager.GetComponentData<EntityInput>(User).AimPosition;
        }

        private float3 GetPosition()
        {
            return VRisingCore.EntityManager.GetComponentData<Translation>(Character).Value;
        }

        private int2 GetTilePosition()
        {
            if (VRisingCore.EntityManager.HasComponent<TilePosition>(Character))
            {
                return VRisingCore.EntityManager.GetComponentData<TilePosition>(Character).Tile;
            }
            else
            {
                return new int2(0, 0);
            }
        }


        private bool GetIsAlive()
        {
            // Placeholder - needs BuffUtil and Prefabs
            return !VRisingCore.EntityManager.GetComponentData<Health>(Character).IsDead;
        }

        private Entity GetInventory()
        {
            var inventoryBuffer = VRisingCore.EntityManager.GetBuffer<InventoryInstanceElement>(Character);
            return inventoryBuffer[0].ExternalInventoryEntity._Entity;
        }

        private Equipment GetEquipment()
        {
            return VRisingCore.EntityManager.GetComponentData<Equipment>(Character);
        }

        private List<Entity> GetEquipmentEntities()
        {
            var equipmentEntities = new NativeList<Entity>(Allocator.Temp);
            var equipment = Equipment;
            // equipment.GetAllEquipmentEntities(equipmentEntities, true); // Placeholder
            var results = new List<Entity>();

            // TODO: Implement proper equipment entity retrieval
            // Fill with available equipment slots - placeholder implementation
            // if (equipment.AmuletSlot.SlotEntity._Entity != Entity.Null) results.Add(equipment.AmuletSlot.SlotEntity._Entity);
            // etc.

            equipmentEntities.Dispose();
            return results;
        }

        public ProjectM.Terrain.WorldRegionType GetWorldZone()
        {
            return VRisingCore.EntityManager.GetComponentData<CurrentWorldRegion>(User).CurrentRegion;
        }

        public string GetWorldZoneString(bool shortName = false)
        {
            var region = VRisingCore.EntityManager.GetComponentData<CurrentWorldRegion>(User).CurrentRegion;
            if (WorldRegions.WorldRegionToString.TryGetValue(region, out var zoneName))
            {
                return shortName ? zoneName.Short : zoneName.Long;
            }
            return "";
        }

        public Entity GetControlledEntity()
        {
            return VRisingCore.EntityManager.GetComponentData<Controller>(User).Controlled._Entity;
        }

        public FromCharacter ToFromCharacter()
        {
            return new FromCharacter
            {
                Character = this.Character,
                User = this.User
            };
        }

        public bool IsAlliedWith(Player player)
        {
            // TODO: Implement proper alliance checking
            return false;
        }

        public bool HasControlledEntity()
        {
            if (ControlledEntity == Character)
            {
                return true;
            }
            else
            {
                bool isDead;
                if (VRisingCore.EntityManager.HasComponent<Health>(ControlledEntity))
                {
                    isDead = VRisingCore.EntityManager.GetComponentData<Health>(ControlledEntity).IsDead;
                }
                else
                {
                    isDead = true;
                }

                if (isDead)
                {
                    return false;
                }

                return VRisingCore.EntityManager.Exists(ControlledEntity) && VRisingCore.EntityManager.HasComponent<PrefabGUID>(ControlledEntity);
            }
        }

        public override int GetHashCode()
        {
            return SteamID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Player other = (Player)obj;
            return SteamID == other.SteamID;
        }

        public override string ToString()
        {
            return Name;
        }

        // Placeholder systems - need implementation
        private static class RecordLevelSystem
        {
            public static int GetRecord(ulong steamId) => 0;
            public static void SetRecord(ulong steamId) { }
        }
    }

    /// <summary>
    /// Represents the alignment or allegiance of a territory.
    /// </summary>
    public enum TerritoryAlignment
    {
        /// <summary>
        /// Territory is aligned with the player or their allies.
        /// </summary>
        Friendly,

        /// <summary>
        /// Territory is controlled by hostile forces.
        /// </summary>
        Enemy,

        /// <summary>
        /// Territory has no specific alignment to any faction.
        /// </summary>
        Neutral,

        /// <summary>
        /// Territory has not been assigned an alignment.
        /// </summary>
        None
    }

    /// <summary>
    /// Player information structure for performance optimization service.
    /// </summary>
    public class PlayerInfo
    {
        public Entity UserEntity { get; set; }
        public Entity CharacterEntity { get; set; }
        public ulong SteamId { get; set; }
        public string PlayerName { get; set; } = "";
        public bool IsConnected { get; set; }
        public bool IsAdmin { get; set; }
    }

    /// <summary>
    /// Entity validation result structure.
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = "";
        public Entity Entity { get; set; }
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Contains mapping of world region types to their display strings.
    /// </summary>
    public static class WorldRegions
    {
        /// <summary>
        /// Dictionary mapping WorldRegionType enum values to their human-readable string representations.
        /// </summary>
        public static Dictionary<ProjectM.Terrain.WorldRegionType, (string Long, string Short)> WorldRegionToString = new()
        {
            { ProjectM.Terrain.WorldRegionType.CursedForest, ("Cursed Forest", "Forest") },
            { ProjectM.Terrain.WorldRegionType.SilverlightHills, ("Silverlight Hills", "Silverlight") },
            { ProjectM.Terrain.WorldRegionType.DunleyFarmlands, ("Dunley Farmlands", "Dunley") },
            { ProjectM.Terrain.WorldRegionType.HallowedMountains, ("Hallowed Mountains", "Hallowed") },
            { ProjectM.Terrain.WorldRegionType.FarbaneWoods, ("Farbane Woods", "Farbane") },
            { ProjectM.Terrain.WorldRegionType.Gloomrot_North, ("Gloomrot North", "N. Gloomrot") },
            { ProjectM.Terrain.WorldRegionType.Gloomrot_South, ("Gloomrot South", "S. Gloomrot") },
            { ProjectM.Terrain.WorldRegionType.RuinsOfMortium, ("Ruins of Mortium", "Mortium") },
            { ProjectM.Terrain.WorldRegionType.StartCave, ("Start Cave", "Start Cave") },
            { ProjectM.Terrain.WorldRegionType.Strongblade, ("Oakveil Woodlands", "Oakveil") },
            { ProjectM.Terrain.WorldRegionType.Other, ("Unknown Location", "Unknown Location") },
            { ProjectM.Terrain.WorldRegionType.None, ("Unknown Location", "Unknown Location") },
        };

        /// <summary>
        /// Gets the long display string for the given world region type.
        /// </summary>
        /// <param name="region">The world region type to get the string for.</param>
        /// <returns>The long display string for the region.</returns>
        public static string ToString(ProjectM.Terrain.WorldRegionType region)
        {
            return WorldRegionToString[region].Long;
        }

        /// <summary>
        /// Gets the short display string for the given world region type.
        /// </summary>
        /// <param name="region">The world region type to get the string for.</param>
        /// <returns>The short display string for the region.</returns>
        public static string ToShortString(ProjectM.Terrain.WorldRegionType region)
        {
            return WorldRegionToString[region].Short;
        }

        /// <summary>
        /// Extension method that gets the short display string for the world region type.
        /// </summary>
        /// <param name="region">The world region type to get the string for.</param>
        /// <returns>The short display string for the region.</returns>
        public static string ToStringShort(this ProjectM.Terrain.WorldRegionType region)
        {
            return WorldRegionToString[region].Short;
        }

        /// <summary>
        /// Extension method that gets the long display string for the world region type.
        /// </summary>
        /// <param name="region">The world region type to get the string for.</param>
        /// <returns>The long display string for the region.</returns>
        public static string ToStringLong(this ProjectM.Terrain.WorldRegionType region)
        {
            return WorldRegionToString[region].Long;
        }
    }
}
