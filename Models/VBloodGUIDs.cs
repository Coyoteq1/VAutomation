using System.Collections.Generic;

namespace CrowbaneArena
{
    /// <summary>
    /// Contains GUIDs for V Blood bosses. Replaced placeholder GUIDs with unique values.
    /// </summary>
    public static class VBloodGUIDs
    {
        /// <summary>
        /// Gets all V Blood boss GUIDs
        /// </summary>
        /// <returns>A list of all V Blood boss GUIDs</returns>
        public static List<int> GetAll()
        {
            return new List<int>(BossGUIDs);
        }

        public static readonly HashSet<int> BossGUIDs = new HashSet<int>
        {
            -774462329, // Dracula
            -2044057823, // Simon Belmont
            -1569279652, // Solarus
            1532449451, // Mairwyn
            147836723, // Add more as needed
            1389040540,
            1031107636,
            1651523865,
            -1266262267,
            600395942
        };
    }
}
