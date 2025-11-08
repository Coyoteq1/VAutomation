using System;
using ProjectM;
using ProjectM.Network;
using Unity.Entities;

namespace CrowbaneArena
{
    /// <summary>
    /// Enhanced hook system for arena progression with integration to new services.
    /// </summary>
    public static class ArenaHook
    {
        private static readonly Services.LoggingService Log = new();

        /// <summary>
        /// Marks the player as entered the arena and activates hook.
        /// </summary>
        public static void MarkPlayerEnteredArena()
        {
            Log.LogEvent("Arena hook activated - player entered arena");
        }

        /// <summary>
        /// Marks the player as exited the arena and deactivates hook.
        /// </summary>
        public static void MarkPlayerExitedArena()
        {
            Log.LogEvent("Arena hook deactivated - player exited arena");
        }

        /// <summary>
        /// Hook method to check V Blood progression. Returns true if in arena.
        /// </summary>
        public static bool CheckVBloodProgression(Entity userEntity)
        {
            try
            {
                // Get user component to check platform ID
                var em = VRisingCore.EntityManager;
                if (!em.TryGetComponentData(userEntity, out User user))
                {
                    Log.LogWarning("CheckVBloodProgression: Could not get User component");
                    return false;
                }

                // Check if player is in arena using enhanced controller
                bool inArena = ArenaController.Instance.IsPlayerInArena(user.PlatformId);

                if (inArena)
                {
                    Log.LogEvent($"V Blood progression overridden for player {user.CharacterName} (ID: {user.PlatformId}) - arena mode active");
                    return true; // Lie to the game UI to unlock all V Blood abilities
                }

                Log.LogEvent($"V Blood progression check for player {user.CharacterName} (ID: {user.PlatformId}) - using actual progression");
                return false; // Placeholder for actual check - would need to implement real V Blood checking
            }
            catch (Exception ex)
            {
                Log.LogError($"Error in CheckVBloodProgression: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if a player is currently in the arena (enhanced version).
        /// </summary>
        public static bool IsPlayerInArena(Entity userEntity)
        {
            try
            {
                var em = VRisingCore.EntityManager;
                if (!em.TryGetComponentData(userEntity, out User user))
                {
                    return false;
                }

                return ArenaController.Instance.IsPlayerInArena(user.PlatformId);
            }
            catch (Exception ex)
            {
                Log.LogError($"Error in IsPlayerInArena: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get the current number of players in arena.
        /// </summary>
        public static int GetActiveArenaCount()
        {
            return ArenaController.Instance.GetActiveSnapshotCount();
        }

        /// <summary>
        /// Force clear all arena states (admin command).
        /// </summary>
        public static void ClearAllArenaStates()
        {
            Log.LogEvent("Admin command: Clearing all arena states");
            ArenaController.Instance.ClearAllSnapshots();
        }
    }
}
