using ProjectM;
using ProjectM.Network;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CrowbaneArena.Services
{
    public static class CaveAutoEnterService
    {
        private static EntityManager EM => CrowbaneArenaCore.EntityManager;
        private static float3 _enterPoint = new float3(-1000f, 0f, -500f);
        private static float3 _exitPoint = new float3(-1000f, 0f, -500f);
        private static float _enterRadius = 50f;
        private static float _exitRadius = 70f;
        private static bool _enabled = false;

        public static void SetZone(float3 enterPoint, float enterRadius)
        {
            _enterPoint = enterPoint;
            _exitPoint = enterPoint;
            _enterRadius = enterRadius;
            _exitRadius = enterRadius + 20f;
            _enabled = true;
            Plugin.Logger?.LogInfo($"Arena zone set: enter={enterPoint}, enterRadius={enterRadius}, exitRadius={_exitRadius}");
        }

        public static void SetEnterPoint(float3 point, float radius)
        {
            _enterPoint = point;
            _enterRadius = radius;
            _enabled = true;
            Plugin.Logger?.LogInfo($"Arena enter point set: {point}, radius={radius}");
        }

        public static void SetExitPoint(float3 point, float radius)
        {
            _exitPoint = point;
            _exitRadius = radius;
            Plugin.Logger?.LogInfo($"Arena exit point set: {point}, radius={radius}");
        }

        public static void Disable()
        {
            _enabled = false;
            Plugin.Logger?.LogInfo("Arena zone auto-enter disabled");
        }

        public static float3 GetEnterPoint() => _enterPoint;
        public static float3 GetExitPoint() => _exitPoint;

        public static void Update()
        {
            if (!_enabled || !CrowbaneArenaCore.HasInitialized) return;

            var userQuery = EM.CreateEntityQuery(ComponentType.ReadOnly<User>());
            var users = userQuery.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var userEntity in users)
            {
                if (!EM.TryGetComponentData(userEntity, out User user)) continue;
                if (!user.IsConnected) continue;

                var characterEntity = user.LocalCharacter._Entity;
                if (characterEntity == Entity.Null || !EM.Exists(characterEntity)) continue;
                if (!EM.TryGetComponentData(characterEntity, out LocalToWorld ltw)) continue;

                var isInArena = SnapshotManagerService.IsInArena(user.PlatformId);

                if (!isInArena)
                {
                    var distanceToEnter = math.distance(ltw.Position, _enterPoint);
                    if (distanceToEnter <= _enterRadius)
                    {
                        SnapshotManagerService.EnterArena(userEntity, characterEntity, ZoneManager.SpawnPoint, 0);
                        Plugin.Logger?.LogInfo($"Auto-entered {user.CharacterName} into arena (distance: {distanceToEnter:F1})");
                    }
                }
                else
                {
                    var distanceToExit = math.distance(ltw.Position, _exitPoint);
                    if (distanceToExit <= _exitRadius)
                    {
                        SnapshotManagerService.LeaveArena(user.PlatformId, userEntity, characterEntity);
                        Plugin.Logger?.LogInfo($"Auto-exited {user.CharacterName} from arena (distance: {distanceToExit:F1})");
                    }
                }
            }

            users.Dispose();
        }
    }
}
