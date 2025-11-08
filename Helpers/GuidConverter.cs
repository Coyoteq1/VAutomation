using Stunlock.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CrowbaneArena.Helpers
{
    /// <summary>
    /// Centralized utility for converting between different GUID formats and handling PrefabGUID operations
    /// </summary>
    public static class GuidConverter
    {
        /// <summary>
        /// Convert long to PrefabGUID
        /// </summary>
        public static PrefabGUID ToPrefabGUID(long guid)
        {
            return new PrefabGUID((int)guid);
        }

        /// <summary>
        /// Convert int to PrefabGUID
        /// </summary>
        public static PrefabGUID ToPrefabGUID(int guid)
        {
            return new PrefabGUID(guid);
        }

        /// <summary>
        /// Convert string to PrefabGUID (parses as int)
        /// </summary>
        public static PrefabGUID ToPrefabGUID(string guid)
        {
            if (int.TryParse(guid, out var intGuid))
            {
                return new PrefabGUID(intGuid);
            }
            return new PrefabGUID(0);
        }

        /// <summary>
        /// Get GuidHash from PrefabGUID (replaces .guid usage)
        /// </summary>
        public static int GetGuidHash(PrefabGUID prefabGuid)
        {
            return prefabGuid.GuidHash;
        }

        /// <summary>
        /// Convert collection of longs to PrefabGUIDs
        /// </summary>
        public static List<PrefabGUID> ToPrefabGUIDs(IEnumerable<long> guids)
        {
            return guids?.Select(ToPrefabGUID).ToList() ?? new List<PrefabGUID>();
        }

        /// <summary>
        /// Convert collection of ints to PrefabGUIDs
        /// </summary>
        public static List<PrefabGUID> ToPrefabGUIDs(IEnumerable<int> guids)
        {
            return guids?.Select(ToPrefabGUID).ToList() ?? new List<PrefabGUID>();
        }

        /// <summary>
        /// Convert collection of strings to PrefabGUIDs
        /// </summary>
        public static List<PrefabGUID> ToPrefabGUIDs(IEnumerable<string> guids)
        {
            return guids?.Select(ToPrefabGUID).Where(g => g.GuidHash != 0).ToList() ?? new List<PrefabGUID>();
        }

        /// <summary>
        /// Convert PrefabGUIDs to GuidHash collection
        /// </summary>
        public static List<int> ToGuidHashes(IEnumerable<PrefabGUID> prefabGuids)
        {
            return prefabGuids?.Select(GetGuidHash).ToList() ?? new List<int>();
        }

        /// <summary>
        /// Check if PrefabGUID is valid (non-zero)
        /// </summary>
        public static bool IsValid(PrefabGUID prefabGuid)
        {
            return prefabGuid.GuidHash != 0;
        }

        /// <summary>
        /// Filter out invalid (zero) PrefabGUIDs
        /// </summary>
        public static List<PrefabGUID> FilterValid(IEnumerable<PrefabGUID> prefabGuids)
        {
            return prefabGuids?.Where(IsValid).ToList() ?? new List<PrefabGUID>();
        }

        /// <summary>
        /// Find PrefabGUID in collection by GuidHash
        /// </summary>
        public static PrefabGUID FindByGuidHash(IEnumerable<PrefabGUID> prefabGuids, int guidHash)
        {
            return prefabGuids?.FirstOrDefault(g => g.GuidHash == guidHash) ?? new PrefabGUID(0);
        }

        /// <summary>
        /// Check if collection contains PrefabGUID with specific GuidHash
        /// </summary>
        public static bool ContainsGuidHash(IEnumerable<PrefabGUID> prefabGuids, int guidHash)
        {
            return prefabGuids?.Any(g => g.GuidHash == guidHash) ?? false;
        }

        /// <summary>
        /// Convert PrefabGUID to string representation
        /// </summary>
        public static string ToString(PrefabGUID prefabGuid)
        {
            return prefabGuid.GuidHash.ToString();
        }

        /// <summary>
        /// Convert collection of PrefabGUIDs to string collection
        /// </summary>
        public static List<string> ToStrings(IEnumerable<PrefabGUID> prefabGuids)
        {
            return prefabGuids?.Select(ToString).ToList() ?? new List<string>();
        }
    }
}
