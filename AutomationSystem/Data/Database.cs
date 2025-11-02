using System;
using System.Collections.Generic;
using BepInEx;

namespace CrowbaneArena.Data
{
    public static class Database
    {
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            // Initialize any database connections or data structures here
            Plugin.Logger?.LogInfo("Database initialized");
            _initialized = true;
        }

        public static void Shutdown()
        {
            if (!_initialized) return;

            // Cleanup database connections
            Plugin.Logger?.LogInfo("Database shutdown");
            _initialized = false;
        }
    }
}
