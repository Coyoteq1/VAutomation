// DEPRECATED: Use PlayerSnapshotService instead
// This file is kept for reference only

/*
using ProjectM;
using ProjectM.Scripting;
using Stunlock.Core;
using Unity.Entities;

// Placeholder types for missing V Rising API types
public struct AbilityBar
{
    public AbilitySlot[] Slots;
}

public struct AbilitySlot
{
    public PrefabGUID Group;
    public int Slot;
}

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service for capturing and restoring player progression (V Blood, Research, Quests)
    /// Based on KindredCommands patterns
    /// </summary>
    public static class ProgressionCaptureService
    {
        private struct AbilityGroupSlotBuffer
        {
            public PrefabGUID Group;
            public int Slot;
        }
        private static EntityManager EM => VRisingCore.EntityManager;
        private static ServerGameManager SGM => VRisingCore.ServerGameManager;

        /// <summary>
        /// Capture all V Blood unlocks from user entity
        /// </summary>
        public static List<int> CaptureVBloodUnlocks(Entity userEntity)
        {
            var unlocks = new List<int>();

            // Return all VBlood GUIDs as unlocked for arena players
            var allVBloods = Data.VBloodGUIDs.GetAll();
            unlocks.AddRange(allVBloods);

            VRisingCore.Log?.LogInfo($"Captured {unlocks.Count} VBlood GUIDs for arena player");
            return unlocks;
        }

        /// <summary>
        /// Restore V Blood unlocks to user entity
        /// </summary>
        public static bool RestoreVBloodUnlocks(Entity userEntity, List<int> unlocks)
        {
            if (unlocks == null || unlocks.Count == 0)
            {
                VRisingCore.Log?.LogInfo("No V Blood unlocks to restore");
                return true;
            }

            VRisingCore.Log?.LogInfo($"Restored {unlocks.Count} VBlood GUIDs (simulated)");
            return true;
        }

        /// <summary>
        /// Capture research/recipe unlocks (placeholder for future implementation)
        /// </summary>
        public static List<int> CaptureResearchUnlocks(Entity userEntity)
        {
            var unlocks = new List<int>();
            VRisingCore.Log?.LogInfo("Research capture not implemented yet");
            return unlocks;
        }

        /// <summary>
        /// Restore research/recipe unlocks (placeholder for future implementation)
        /// </summary>
        public static bool RestoreResearchUnlocks(Entity userEntity, List<int> unlocks)
        {
            VRisingCore.Log?.LogInfo("Research restore not implemented yet");
            return true;
        }

        /// <summary>
        /// Capture ability progression from character entity
        /// </summary>
        public static List<int> CaptureAbilities(Entity characterEntity)
        {
            var abilities = new List<int>();

            try
            {
                VRisingCore.Log?.LogInfo("Capturing player abilities...");

                // Try to get ability bar component
                if (EM.TryGetComponentData<AbilityBar>(characterEntity, out var abilityBar))
                {
                    foreach (var slot in abilityBar.Slots)
                    {
                        if (slot.Group.GuidHash != 0)
                        {
                            abilities.Add(slot.Group.GuidHash);
                        }
                    }

                    VRisingCore.Log?.LogInfo($"✓ Captured {abilities.Count} abilities from AbilityBar");
                }
                else
                {
                    VRisingCore.Log?.LogWarning("AbilityBar component not found on character entity");
                }

                // Also try to get abilities from ability group slots buffer
                if (EM.TryGetBuffer<AbilityGroupSlotBuffer>(characterEntity, out var abilityBuffer))
                {
                    foreach (var slot in abilityBuffer)
                    {
                        if (slot.Group.GuidHash != 0 && !abilities.Contains(slot.Group.GuidHash))
                        {
                            abilities.Add(slot.Group.GuidHash);
                        }
                    }

                    VRisingCore.Log?.LogInfo($"✓ Added {abilityBuffer.Length} abilities from AbilityGroupSlotBuffer");
                }
                else
                {
                    VRisingCore.Log?.LogWarning("AbilityGroupSlotBuffer not found on character entity");
                }
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Error capturing abilities: {ex.Message}");
            }

            return abilities;
        }

        /// <summary>
        /// Restore ability progression to character entity
        /// </summary>
        public static bool RestoreAbilities(Entity characterEntity, List<int> abilities)
        {
            try
            {
                if (abilities == null || abilities.Count == 0)
                {
                    VRisingCore.Log?.LogInfo("No abilities to restore");
                    return true;
                }

                VRisingCore.Log?.LogInfo($"Restoring {abilities.Count} abilities...");

                // Clear existing abilities first
                if (EM.TryGetComponentData<AbilityBar>(characterEntity, out var abilityBar))
                {
                    // Clear all slots
                    for (int i = 0; i < abilityBar.Slots.Length; i++)
                    {
                        abilityBar.Slots[i] = new AbilitySlot();
                    }

                    EM.SetComponentData(characterEntity, abilityBar);
                }

                // Restore abilities to slots
                if (EM.TryGetComponentData<AbilityBar>(characterEntity, out abilityBar))
                {
                    int slotIndex = 0;
                    foreach (var abilityGuid in abilities)
                    {
                        if (slotIndex >= abilityBar.Slots.Length)
                            break;

                        abilityBar.Slots[slotIndex] = new AbilitySlot
                        {
                            Group = new PrefabGUID(abilityGuid),
                            Slot = slotIndex
                        };
                        slotIndex++;
                    }

                    EM.SetComponentData(characterEntity, abilityBar);
                }

                VRisingCore.Log?.LogInfo(
                    $"✓ Restored {Math.Min(abilities.Count, abilityBar.Slots.Length)} abilities to ability bar");
                return true;
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Error restoring abilities: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get user entity from character entity
        /// </summary>
        public static Entity GetUserEntity(Entity characterEntity)
        {
            try
            {
                if (EM.TryGetComponentData<PlayerCharacter>(characterEntity, out var playerChar))
                {
                    return playerChar.UserEntity;
                }
            }
            catch (Exception ex)
            {
                VRisingCore.Log?.LogError($"Error getting user entity: {ex.Message}");
            }

            return Entity.Null;
        }
    }
}
*/