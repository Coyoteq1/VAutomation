using System;
using System.Linq;
using CrowbaneArena.Data;
using CrowbaneArena.Services;

namespace CrowbaneArena.Commands.Converters
{
    internal static class FoundVBloodConverter
    {
        private static readonly (int GuidHash, string Name)[] BossMappings = new[]
        {
            (-1905691330, "Matka"),
            (-1905691331, "Terah"),
            (-1905691332, "Jade"),
            (-1905691333, "Beatrice"),
            (-1905691334, "Nicholaus"),
            (-1905691335, "Quincey"),
            (-1905691336, "Ungora"),
            (-1905691337, "Terrorclaw"),
            (-1905691338, "Lidia"),
            (-1905691339, "Goreswine"),
            (-1905691340, "Octavian"),
            (-1905691357, "Solarus"),
            (-1905691385, "Dracula")
        };

        public static bool Parse(string name, out FoundVBlood foundVBlood)
        {
            var mapping = BossMappings.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (mapping != default)
            {
                foundVBlood = new FoundVBlood(new Stunlock.Core.PrefabGUID(mapping.GuidHash), mapping.Name);
                return true;
            }

            foundVBlood = default;
            return false;
        }
    }
}
