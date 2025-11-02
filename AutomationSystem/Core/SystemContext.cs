using System;
using Unity.Collections;
using Unity.Entities;

namespace CrowbaneArena
{
    /// <summary>
    /// Context passed to system work implementations
    /// </summary>
    public readonly struct SystemContext
    {
        public SystemBase System { get; }
        public EntityManager EntityManager { get; }
        public EntityQuery Query { get; }
        public EntityTypeHandle EntityTypeHandle { get; }
        public EntityStorageInfoLookup EntityStorageInfoLookup { get; }
        public IRegistrar Registrar { get; }
        
        private readonly Action<EntityQuery, Action<NativeArray<Entity>>> _withTempEntities;
        private readonly Action<EntityQuery, Action<Entity>> _forEachEntity;
        private readonly Action<EntityQuery, Action<NativeArray<ArchetypeChunk>>> _withTempChunks;
        private readonly Action<EntityQuery, Action<ArchetypeChunk>> _forEachChunk;
        private readonly Func<Entity, bool> _exists;

        public SystemContext(
            SystemBase system,
            EntityManager entityManager,
            EntityQuery query,
            EntityTypeHandle entityTypeHandle,
            EntityStorageInfoLookup entityStorageInfoLookup,
            IRegistrar registrar,
            Action<EntityQuery, Action<NativeArray<Entity>>> withTempEntities,
            Action<EntityQuery, Action<Entity>> forEachEntity,
            Action<EntityQuery, Action<NativeArray<ArchetypeChunk>>> withTempChunks,
            Action<EntityQuery, Action<ArchetypeChunk>> forEachChunk,
            Func<Entity, bool> exists)
        {
            System = system;
            EntityManager = entityManager;
            Query = query;
            EntityTypeHandle = entityTypeHandle;
            EntityStorageInfoLookup = entityStorageInfoLookup;
            Registrar = registrar;
            _withTempEntities = withTempEntities;
            _forEachEntity = forEachEntity;
            _withTempChunks = withTempChunks;
            _forEachChunk = forEachChunk;
            _exists = exists;
        }

        /// <summary>
        /// Executes the supplied callback with a temporary entity array for the provided query
        /// </summary>
        public void WithTempEntities(EntityQuery query, Action<NativeArray<Entity>> action)
            => _withTempEntities(query, action);

        /// <summary>
        /// Iterates every entity in the query using a temporary array allocation
        /// </summary>
        public void ForEachEntity(EntityQuery query, Action<Entity> action)
            => _forEachEntity(query, action);

        /// <summary>
        /// Executes the supplied callback with a temporary archetype chunk array for the provided query
        /// </summary>
        public void WithTempChunks(EntityQuery query, Action<NativeArray<ArchetypeChunk>> action)
            => _withTempChunks(query, action);

        /// <summary>
        /// Iterates every chunk in the query using a temporary array allocation
        /// </summary>
        public void ForEachChunk(EntityQuery query, Action<ArchetypeChunk> action)
            => _forEachChunk(query, action);

        /// <summary>
        /// Determines whether the entity still exists according to the latest storage lookup
        /// </summary>
        public bool Exists(Entity entity) => _exists(entity);
    }

    /// <summary>
    /// Interface for registering refresh actions
    /// </summary>
    public interface IRegistrar
    {
        void Register(Action<SystemBase> refreshAction);
    }
}
