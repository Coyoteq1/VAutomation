using System.Collections.Generic;

namespace CrowbaneArena
{
    /// <summary>
    /// Reference list of all V Blood bosses in V Rising with character names and types.
    /// </summary>
    public static class BossReference
    {
        public static readonly List<Boss> AllBosses = new List<Boss>
        {
            new Boss("Dracula", VBloodType.Vampire, true),
            new Boss("Simon Belmont", VBloodType.Human, false),
            new Boss("Solarus", VBloodType.Vampire, true),
            new Boss("Mairwyn", VBloodType.Vampire, false),
            // Add more bosses as per the complete list
            // This is a partial list; expand with all bosses
        };
    }

    public enum VBloodType
    {
        Vampire,
        Human,
        Beast
    }

    public class Boss
    {
        public string Name { get; }
        public VBloodType Type { get; }
        public bool HasPrimalVariant { get; }

        public Boss(string name, VBloodType type, bool hasPrimal)
        {
            Name = name;
            Type = type;
            HasPrimalVariant = hasPrimal;
        }
    }
}
