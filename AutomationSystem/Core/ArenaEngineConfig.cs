using System;
using System.Collections.Generic;
using BepInEx.Logging;

// Note: This assumes RisingV.Shared and RisingV.Core are available.
// If not, you'll need to add the NuGet packages:
// <PackageReference Include="RisingV.Shared" Version="0.1.3" />
// <PackageReference Include="RisingV.Core" Version="0.1.3" />

namespace CrowbaneArena.Core
{
    /// <summary>
    /// Configuration for the Arena Service
    /// Defines all configurable aspects of the arena system
    /// </summary>
    public class ArenaServiceConfig
    {
        /// <summary>
        /// Whether the arena system is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Maximum number of players allowed in arena simultaneously
        /// </summary>
        public int MaxPlayers { get; set; } = 10;

        /// <summary>
        /// Arena center position coordinates
        /// </summary>
        public float3 ArenaCenter { get; set; } = new float3(1000, 20, 800);

        /// <summary>
        /// Distance from center to trigger arena entry
        /// </summary>
        public float EnterRadius { get; set; } = 50f;

        /// <summary>
        /// Distance from center to trigger arena exit
        /// </summary>
        public float ExitRadius { get; set; } = 75f;

        /// <summary>
        /// How often to check for proximity triggers (seconds)
        /// </summary>
        public float UpdateFrequencySeconds { get; set; } = 2f;

        /// <summary>
        /// Whether to enable automatic arena entry/exit based on proximity
        /// </summary>
        public bool AutoProximityEnabled { get; set; } = true;

        /// <summary>
        /// Blood type to set for arena combat
        /// </summary>
        public PrefabGUID ArenaBloodType { get; set; } = new PrefabGUID(1558171501); // Brute

        /// <summary>
        /// Blood quality percentage for arena (0-100)
        /// </summary>
        public float ArenaBloodQuality { get; set; } = 100f;

        /// <summary>
        /// Whether to enable VBlood hook system
        /// </summary>
        public bool VBloodHookEnabled { get; set; } = true;

        /// <summary>
        /// Whether to enable build switching in arena
        /// </summary>
        public bool BuildSwitchingEnabled { get; set; } = true;

        /// <summary>
        /// Whether to enable dual character system
        /// </summary>
        public bool DualCharacterEnabled { get; set; } = true;

        /// <summary>
        /// Path to builds configuration file
        /// </summary>
        public string BuildsConfigPath { get; set; } = "config/crowbanearena/builds.json";

        /// <summary>
        /// Path to arena configuration file
        /// </summary>
        public string ArenaConfigPath { get; set; } = "config/crowbanearena/arena_config.json";

        /// <summary>
        /// Path to snapshots storage
        /// </summary>
        public string SnapshotsPath { get; set; } = "config/CrowbaneArena_Snapshots.json";
    }
}
