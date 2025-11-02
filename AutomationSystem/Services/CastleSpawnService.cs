using System.Collections.Generic;
using Unity.Mathematics;

namespace CrowbaneArena.Services
{
    public static class CastleSpawnService
    {
        private static Dictionary<int, float3> _castleSpawns = new();

        public static void SetCastle(int index, float3 position)
        {
            _castleSpawns[index] = position;
            Plugin.Logger?.LogInfo($"Castle {index} set to ({position.x}, {position.y}, {position.z})");
        }

        public static bool TryGetCastle(int index, out float3 position)
        {
            return _castleSpawns.TryGetValue(index, out position);
        }

        public static Dictionary<int, float3> GetAllCastles() => new(_castleSpawns);

        public static void RemoveCastle(int index)
        {
            _castleSpawns.Remove(index);
        }

        public static void ClearAll()
        {
            _castleSpawns.Clear();
        }
    }
}
