using Unity.Entities;

namespace CrowbaneArena
{
    /// <summary>
    /// Interface for system work definitions used with VSystemBase
    /// </summary>
    public interface ISystemWork
    {
        /// <summary>
        /// Whether to require the query for update
        /// </summary>
        bool RequireForUpdate { get; }

        /// <summary>
        /// Build the entity query for this work
        /// </summary>
        void Build(ref EntityQueryBuilder builder);

        /// <summary>
        /// Called when the system is created
        /// </summary>
        void OnCreate(SystemContext context);

        /// <summary>
        /// Called when the system starts running
        /// </summary>
        void OnStartRunning(SystemContext context);

        /// <summary>
        /// Called when the system stops running
        /// </summary>
        void OnStopRunning(SystemContext context);

        /// <summary>
        /// Called when the system is destroyed
        /// </summary>
        void OnDestroy(SystemContext context);

        /// <summary>
        /// Called every update
        /// </summary>
        void OnUpdate(SystemContext context);
    }
}
