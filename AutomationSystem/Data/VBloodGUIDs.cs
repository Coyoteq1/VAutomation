namespace CrowbaneArena.Data
{
    /// <summary>
    /// Contains V Blood boss GUIDs for the arena system
    /// </summary>
    public static class VBloodGUIDs
    {
        private static readonly List<int> _allVBloods = new()
        {
            -1905691330, // Alpha Wolf
            -1342764880, // Keely the Frost Archer
            1699865363, // Rufus the Foreman
            -2025101517, // Errol the Stonebreaker
            1362041468, // Lidia the Chaos Archer
            -1065970933, // Jade the Vampire Hunter
            435934037, // Putrid Rat
            -1208888966, // Goreswine the Ravager
            1124739990, // Clive the Firestarter
            2054432370, // Polora the Feywalker
            -1449631170, // Ferocious Bear
            1106458752, // Nicholaus the Fallen
            -1347412392, // Quincey the Bandit King
            1896428751, // Vincent the Frostbringer
            -484556888, // Christina the Sun Priestess
            2089106511, // Tristan the Vampire Hunter
            -2137261854, // The Winged Horror
            1233988687, // Ungora the Spider Queen
            -1391546313, // Terrorclaw the Ogre
            -680831417, // Willfred the Werewolf Chief
            114912615, // Octavian the Militia Captain
            -1659822956 // Solarus the Immaculate
        };

        /// <summary>
        /// Gets all V Blood GUIDs
        /// </summary>
        public static List<int> GetAll()
        {
            return new List<int>(_allVBloods);
        }
    }
}