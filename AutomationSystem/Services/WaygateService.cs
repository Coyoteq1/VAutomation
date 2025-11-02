using ProjectM;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CrowbaneArena.Services
{
    class WaygateService
    {
        const float UnlockDistance = 25f;
        readonly EntityQuery connectedUserQuery;
        readonly EntityQuery waypointQuery;
        readonly EntityQuery spawnedWaypointQuery;

        readonly Dictionary<Entity, List<NetworkId>> unlockedSpawnedWaypoints = [];

        public WaygateService()
        {
            var spawnedWaypointQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
                .AddAll(ComponentType.ReadOnly<ChunkWaypoint>())
                .AddAll(ComponentType.ReadOnly<SpawnedBy>())
                .WithOptions(EntityQueryOptions.IncludeDisabled);
            spawnedWaypointQuery = CrowbaneArenaCore.EntityManager.CreateEntityQuery(ref spawnedWaypointQueryBuilder);
            spawnedWaypointQueryBuilder.Dispose();

            var connectedUserQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
                .AddAll(ComponentType.ReadOnly<IsConnected>())
                .AddAll(ComponentType.ReadOnly<User>());

            connectedUserQuery = CrowbaneArenaCore.EntityManager.CreateEntityQuery(ref connectedUserQueryBuilder);
            connectedUserQueryBuilder.Dispose();

            var waypointQueryBuilder = new EntityQueryBuilder(Allocator.Temp)
                .AddAll(ComponentType.ReadOnly<ChunkWaypoint>())
                .WithOptions(EntityQueryOptions.IncludeDisabled);
            waypointQuery = CrowbaneArenaCore.EntityManager.CreateEntityQuery(ref waypointQueryBuilder);
            waypointQueryBuilder.Dispose();
        }

        List<NetworkId> InitializeUnlockedWaypoints(Entity userEntity)
        {
            var unlockedUserSpawnedWaypoints = new List<NetworkId>();
            unlockedSpawnedWaypoints.Add(userEntity, unlockedUserSpawnedWaypoints);

            var unlockedWaypoints = CrowbaneArenaCore.EntityManager.GetBuffer<UnlockedWaypointElement>(userEntity);
            var spawnedWaypoints = spawnedWaypointQuery.ToEntityArray(Allocator.Temp);
            var spawnedWaypointsArray = spawnedWaypoints.ToArray();
            spawnedWaypoints.Dispose();

            foreach (var waypoint in unlockedWaypoints)
            {
                if (spawnedWaypointsArray.Any(x => x.Read<NetworkId>() == waypoint.Waypoint))
                {
                    unlockedUserSpawnedWaypoints.Add(waypoint.Waypoint);
                }
            }

            return unlockedUserSpawnedWaypoints;
        }

        public bool CreateWaygate(Entity character, PrefabGUID waypointPrefabGUID)
        {
            if (!CrowbaneArenaCore.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(waypointPrefabGUID, out var waypointPrefab))
            {
                Plugin.Logger?.LogError($"Failed to find {waypointPrefabGUID.LookupName()} Prefab entity");
                return false;
            }

            var pos = character.Read<Translation>().Value;
            var chunk = new ProjectM.Terrain.TerrainChunk { X = (sbyte)((pos.x + 3200) / 160), Y = (sbyte)((pos.z + 3200) / 160) };
            var waypoints = waypointQuery.ToEntityArray(Allocator.Temp);
            try
            {
                foreach (var waypoint in waypoints)
                {
                    if (waypoint.Has<CastleWorkstation>())
                        continue;
                    var waypointPos = waypoint.Read<Translation>().Value;
                    var waypointChunk = new ProjectM.Terrain.TerrainChunk { X = (sbyte)((waypointPos.x + 3200) / 160), Y = (sbyte)((waypointPos.z + 3200) / 160) };
                    if (waypointChunk.X == chunk.X && waypointChunk.Y == chunk.Y)
                        return false;
                }
            }
            finally
            {
                waypoints.Dispose();
            }

            var rot = character.Read<Rotation>().Value;

            var newWaypoint = CrowbaneArenaCore.EntityManager.Instantiate(waypointPrefab);

            newWaypoint.Write(new Translation { Value = pos });
            newWaypoint.Write(new Rotation { Value = rot });
            newWaypoint.Add<SpawnedBy>();
            newWaypoint.Write(new SpawnedBy { Value = character });

            return true;
        }

        public bool TeleportToClosestWaygate(Entity character)
        {
            var pos = character.Read<Translation>().Value;
            var spawnedWaypoints = spawnedWaypointQuery.ToEntityArray(Allocator.Temp);
            var closestWaypoint = spawnedWaypoints.ToArray().OrderBy(x => math.distance(pos, x.Read<Translation>().Value)).FirstOrDefault();
            spawnedWaypoints.Dispose();
            if (closestWaypoint == Entity.Null) return false;

            var waypointPos = closestWaypoint.Read<Translation>().Value;
            var waypointRot = closestWaypoint.Read<Rotation>().Value;

            character.Write(new Translation { Value = waypointPos });
            character.Write(new LastTranslation { Value = waypointPos });
            character.Write(new Rotation { Value = waypointRot });
            return true;
        }

        public void UnlockWaypoint(Entity userEntity, NetworkId waypointNetworkId)
        {
            if (waypointNetworkId == NetworkId.Empty)
            {
                Plugin.Logger?.LogError("Attempted to unlock an empty waypoint");
                return;
            }
            if (!unlockedSpawnedWaypoints.TryGetValue(userEntity, out var unlockedWaypoints))
            {
                unlockedWaypoints = InitializeUnlockedWaypoints(userEntity);
            }
            unlockedWaypoints.Add(waypointNetworkId);

            var unlockedWaypointElements = CrowbaneArenaCore.EntityManager.GetBuffer<UnlockedWaypointElement>(userEntity);
            foreach (var unlockedWaypoint in unlockedWaypointElements)
            {
                if (unlockedWaypoint.Waypoint == waypointNetworkId) return;
            }

            Plugin.Logger?.LogInfo($"Waypoint {waypointNetworkId} unlocked for {userEntity.Read<User>().CharacterName}");
            unlockedWaypointElements.Add(new() { Waypoint = waypointNetworkId });
        }

        public bool DestroyWaygate(Entity senderCharacterEntity)
        {
            const float DISTANCE_TO_DESTROY = 10f;
            var pos = senderCharacterEntity.Read<Translation>().Value;
            var spawnedWaygates = spawnedWaypointQuery.ToEntityArray(Allocator.Temp);
            var closestWaypoint = spawnedWaygates.ToArray()
                .OrderBy(x => math.distance(pos, x.Read<Translation>().Value))
                .FirstOrDefault(x => math.distance(pos, x.Read<Translation>().Value) < DISTANCE_TO_DESTROY);
            spawnedWaygates.Dispose();
            if (closestWaypoint == Entity.Null) return false;

            DestroyUtility.Destroy(CrowbaneArenaCore.EntityManager, closestWaypoint);
            return true;
        }
    }
}
