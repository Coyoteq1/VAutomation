using System;
using System.Collections.Generic;

namespace AutomationSystem
{
    /// <summary>
    /// Interface for automation tracking functionality
    /// </summary>
    public interface IAutomationTracker
    {
        /// <summary>
        /// Starts tracking an automation entity
        /// </summary>
        /// <param name="entityId">The ID of the entity to track</param>
        /// <param name="metadata">Optional metadata for the entity</param>
        void StartTracking(string entityId, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Stops tracking an automation entity
        /// </summary>
        /// <param name="entityId">The ID of the entity to stop tracking</param>
        void StopTracking(string entityId);

        /// <summary>
        /// Gets tracking information for an entity
        /// </summary>
        /// <param name="entityId">The ID of the entity</param>
        /// <returns>The tracking information, or null if not tracked</returns>
        AutomationTrackingInfo? GetTrackingInfo(string entityId);

        /// <summary>
        /// Updates tracking metadata for an entity
        /// </summary>
        /// <param name="entityId">The ID of the entity</param>
        /// <param name="key">The metadata key</param>
        /// <param name="value">The metadata value</param>
        void UpdateMetadata(string entityId, string key, object value);

        /// <summary>
        /// Gets all tracked entities
        /// </summary>
        /// <returns>Dictionary of entity IDs and their tracking information</returns>
        Dictionary<string, AutomationTrackingInfo> GetAllTrackedEntities();

        /// <summary>
        /// Checks if an entity is being tracked
        /// </summary>
        /// <param name="entityId">The ID of the entity</param>
        /// <returns>True if the entity is being tracked</returns>
        bool IsTracked(string entityId);

        /// <summary>
        /// Gets tracking statistics
        /// </summary>
        /// <returns>Tracking statistics</returns>
        AutomationTrackingStats GetTrackingStats();
    }

    /// <summary>
    /// Represents tracking information for an automation entity
    /// </summary>
    public class AutomationTrackingInfo
    {
        /// <summary>
        /// Gets or sets the entity ID
        /// </summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when tracking started
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the tracking metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Gets or sets the last activity time
        /// </summary>
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the tracking status
        /// </summary>
        public TrackingStatus Status { get; set; } = TrackingStatus.Active;
    }

    /// <summary>
    /// Represents tracking statistics
    /// </summary>
    public class AutomationTrackingStats
    {
        /// <summary>
        /// Gets or sets the total number of entities being tracked
        /// </summary>
        public int TotalTracked { get; set; }

        /// <summary>
        /// Gets or sets the number of active entities
        /// </summary>
        public int ActiveTracked { get; set; }

        /// <summary>
        /// Gets or sets the number of inactive entities
        /// </summary>
        public int InactiveTracked { get; set; }

        /// <summary>
        /// Gets or sets the average tracking duration
        /// </summary>
        public TimeSpan AverageTrackingDuration { get; set; }

        /// <summary>
        /// Gets or sets when statistics were last updated
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Represents the status of tracking
    /// </summary>
    public enum TrackingStatus
    {
        /// <summary>
        /// The entity is actively being tracked
        /// </summary>
        Active,

        /// <summary>
        /// The entity is tracked but currently inactive
        /// </summary>
        Inactive,

        /// <summary>
        /// Tracking has been paused for this entity
        /// </summary>
        Paused,

        /// <summary>
        /// Tracking has been completed for this entity
        /// </summary>
        Completed
    }
}