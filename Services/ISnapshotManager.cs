using System;
using System.Threading.Tasks;
using CrowbaneArena.Models;

namespace CrowbaneArena.Services
{
    public interface ISnapshotManager
    {
        void Initialize();
        void Shutdown();
        Task CreateSnapshotAsync(ulong steamId, PlayerSnapshot snapshot);
        Task<PlayerSnapshot?> GetLatestSnapshotAsync(ulong steamId);
        Task RestoreSnapshotAsync(ulong steamId, string snapshotId);
        bool HasSnapshot(ulong platformId);
        void DeleteSnapshot(ulong platformId);
        void DeleteAllSnapshots();
        ProgressionSnapshot GetSnapshot(ulong platformId);
        Task<bool> CreateSnapshotAsync(Unity.Entities.Entity userEntity, Unity.Entities.Entity characterEntity);
        Task<bool> RestoreSnapshotAsync(Unity.Entities.Entity userEntity, Unity.Entities.Entity characterEntity);
    }
}
