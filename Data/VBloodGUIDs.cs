using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using ProjectM;

namespace CrowbaneArena.Data
{
    /// <summary>
    /// Manages V Blood boss data and operations
    /// </summary>
    public static class VBloodGUIDs
    {
        private static readonly Dictionary<string, int> _vbloods = new(StringComparer.OrdinalIgnoreCase)
        {
            ["alphawolf"] = -1905691330,
            ["keely"] = -1342764880,
            ["rufus"] = 1699865363,
            ["errol"] = -2025101517,
            ["lidia"] = 1362041468,
            ["jade"] = -1065970933,
            ["putridrat"] = 435934037,
            ["goreswine"] = -1208888966,
            ["clive"] = 1124739990,
            ["polora"] = 2054432370,
            ["bear"] = -1449631170,
            ["nicholaus"] = 1106458752,
            ["quincey"] = -1347412392,
            ["vincent"] = 1896428751,
            ["christina"] = -484556888,
            ["tristan"] = 2089106511,
            ["wingedhorror"] = -2137261854,
            ["ungora"] = 1233988687,
            ["terrorclaw"] = -1391546313,
            ["willfred"] = -680831417,
            ["octavian"] = 114912615,
            ["solarus"] = -1659822956
        };

        /// <summary>
        /// Gets all V Blood GUIDs
        /// </summary>
        public static List<int> GetAll() => _vbloods.Values.ToList();

        /// <summary>
        /// Gets all V Blood names
        /// </summary>
        public static List<string> GetNames() => _vbloods.Keys.ToList();

        /// <summary>
        /// Gets the GUID for a V Blood by name
        /// </summary>
        public static int? GetGuid(string name) =>
            _vbloods.TryGetValue(name, out var guid) ? guid : null;

        /// <summary>
        /// Gets all V Blood names and their GUIDs
        /// </summary>
        public static IReadOnlyDictionary<string, int> GetAllVBloods() => _vbloods;

        /// <summary>
        /// Checks if a V Blood exists with the given name
        /// </summary>
        public static bool Exists(string name) => _vbloods.ContainsKey(name);
    }
}
