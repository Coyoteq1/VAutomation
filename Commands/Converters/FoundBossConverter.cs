using System.Collections.Generic;
using System.Linq;
using Stunlock.Core;
using VampireCommandFramework;

namespace CrowbaneArena.Commands.Converters
{
    public record struct FoundVBlood(PrefabGUID Value, string Name);

    public class FoundVBloodConverter : CommandArgumentConverter<FoundVBlood>
    {
        public readonly static Dictionary<string, PrefabGUID> NameToVBloodPrefab = new()
        {
            {"Matka", new PrefabGUID(-1905691330)},
            {"Terah", new PrefabGUID(-1905691331)},
            {"Jade", new PrefabGUID(-1905691332)},
            {"Beatrice", new PrefabGUID(-1905691333)},
            {"Nicholaus", new PrefabGUID(-1905691334)},
            {"Quincey", new PrefabGUID(-1905691335)},
            {"Ungora", new PrefabGUID(-1905691336)},
            {"Terrorclaw", new PrefabGUID(-1905691337)},
            {"Lidia", new PrefabGUID(-1905691338)},
            {"Goreswine", new PrefabGUID(-1905691339)},
            {"Octavian", new PrefabGUID(-1905691340)},
            {"Leandra", new PrefabGUID(-1905691341)},
            {"Rufus", new PrefabGUID(-1905691342)},
            {"Keely", new PrefabGUID(-1905691343)},
            {"Kodia", new PrefabGUID(-1905691344)},
            {"Christina", new PrefabGUID(-1905691345)},
            {"Clive", new PrefabGUID(-1905691346)},
            {"Gorecrusher", new PrefabGUID(-1905691347)},
            {"Foulrot", new PrefabGUID(-1905691348)},
            {"Polora", new PrefabGUID(-1905691349)},
            {"Styx", new PrefabGUID(-1905691350)},
            {"Mairwyn", new PrefabGUID(-1905691351)},
            {"Albert", new PrefabGUID(-1905691352)},
            {"Talzur", new PrefabGUID(-1905691353)},
            {"Vincent", new PrefabGUID(-1905691354)},
            {"Raziel", new PrefabGUID(-1905691355)},
            {"Morian", new PrefabGUID(-1905691356)},
            {"Solarus", new PrefabGUID(-1905691357)},
            {"Tristan", new PrefabGUID(-1905691358)},
            {"Errol", new PrefabGUID(-1905691359)},
            {"Azariel", new PrefabGUID(-1905691360)},
            {"Willfred", new PrefabGUID(-1905691361)},
            {"Alpha Wolf", new PrefabGUID(-1905691362)},
            {"Meredith", new PrefabGUID(-1905691363)},
            {"Nibbles", new PrefabGUID(-1905691364)},
            {"Frostmaw", new PrefabGUID(-1905691365)},
            {"Grayson", new PrefabGUID(-1905691366)},
            {"Adam", new PrefabGUID(-1905691367)},
            {"Voltatia", new PrefabGUID(-1905691368)},
            {"Ziva", new PrefabGUID(-1905691369)},
            {"Angram", new PrefabGUID(-1905691370)},
            {"Henry", new PrefabGUID(-1905691371)},
            {"Domina", new PrefabGUID(-1905691372)},
            {"Cyril", new PrefabGUID(-1905691373)},
            {"Ben", new PrefabGUID(-1905691374)},
            {"Baron", new PrefabGUID(-1905691375)},
            {"Magnus", new PrefabGUID(-1905691376)},
            {"Maja", new PrefabGUID(-1905691377)},
            {"Bane", new PrefabGUID(-1905691378)},
            {"Grethel", new PrefabGUID(-1905691379)},
            {"Kriig", new PrefabGUID(-1905691380)},
            {"Finn", new PrefabGUID(-1905691381)},
            {"Elena", new PrefabGUID(-1905691382)},
            {"Valencia", new PrefabGUID(-1905691383)},
            {"Cassius", new PrefabGUID(-1905691384)},
            {"Dracula", new PrefabGUID(-1905691385)}
        };

        public readonly static Dictionary<PrefabGUID, string> VBloodPrefabToName = 
            NameToVBloodPrefab.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        public override FoundVBlood Parse(ICommandContext ctx, string input)
        {
            var matches = NameToVBloodPrefab.Where(kvp => 
                kvp.Key.ToLower().Replace(" ", "").Contains(input.Replace(" ", "").ToLower()));

            if (matches.Count() == 1)
            {
                var theMatch = matches.First();
                return new FoundVBlood(theMatch.Value, theMatch.Key);
            }

            if (matches.Count() > 1)
            {
                throw ctx.Error($"Multiple bosses found matching {input}. Please be more specific.\n" + 
                    string.Join("\n", matches.Select(x => x.Key)));
            }

            throw ctx.Error("Could not find boss");
        }

        public static bool Parse(string input, out FoundVBlood foundVBlood)
        {
            var matches = NameToVBloodPrefab.Where(kvp => 
                kvp.Key.ToLower().Replace(" ", "").Contains(input.Replace(" ", "").ToLower()));

            if (matches.Count() == 1)
            {
                var theMatch = matches.First();
                foundVBlood = new FoundVBlood(theMatch.Value, theMatch.Key);
                return true;
            }

            foundVBlood = new FoundVBlood();
            return false;
        }
    }
}
