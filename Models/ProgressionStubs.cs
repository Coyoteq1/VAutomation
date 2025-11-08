using System;
using Unity.Entities;
using ProjectM;
using Stunlock.Core;

namespace CrowbaneArena.Models
{
    // Lightweight local stubs for progression-related buffer/component types.
    // These are minimal implementations to allow compilation and basic snapshotting.

    public struct VBloodUnitStatus
    {
        public PrefabGUID UnitType;
        public bool Defeated;
    }

    public struct ResearchUnlockBuffer
    {
        public PrefabGUID ResearchId;
        public bool Unlocked;
    }

    public struct SpellSchoolProgress
    {
        public PrefabGUID SpellSchoolId;
        public float Level;
    }

    public struct Experience
    {
        public float Value;
    }

    public struct Level
    {
        public int Value;
    }

    public struct AbilityBuffer
    {
        public PrefabGUID AbilityId;
        public bool Learned;
    }

    public struct AchievementBuffer
    {
        public PrefabGUID AchievementId;
        public bool Completed;
        public float Progress;
    }

    public struct WaypointBuffer
    {
        public int WaypointId;
        public bool Discovered;
    }

    public struct MapRevealBuffer
    {
        public int X;
        public int Y;
    }
}
