using System.Collections.Concurrent;

namespace VAutomation.Services
{
    public static class ArenaState
    {
        private static readonly ConcurrentDictionary<string, bool> InArena = new();

        public static void MarkInArena(string userId, bool value) => InArena[userId] = value;
        public static bool IsInArena(string userId) => InArena.TryGetValue(userId, out var v) && v;
        public static void Clear(string userId) => InArena.TryRemove(userId, out _);
        public static void ClearAll() => InArena.Clear();
    }
}
