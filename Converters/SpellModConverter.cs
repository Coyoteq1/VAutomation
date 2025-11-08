using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CrowbaneArena.Extensions;
using VampireCommandFramework;

namespace CrowbaneArena.Commands.Converters;

internal static class SpellModConverter
{
    // Simplified spell mod mappings - would be replaced with database in full implementation
    private static readonly Dictionary<string, List<string>> AbilitySpellMods = new()
    {
        ["veil_of_blood"] = new() { "SpellMod_VeilOfBlood_01", "SpellMod_VeilOfBlood_02" },
        ["sanguine_coil"] = new() { "SpellMod_SanguineCoil_01", "SpellMod_SanguineCoil_02" }
    };

    public static string GetSpellMod(ChatCommandContext ctx, string abilityName, int spellModIndex)
    {
        if (AbilitySpellMods.TryGetValue(abilityName, out var spellMods))
        {
            if (spellModIndex >= 1 && spellModIndex <= spellMods.Count)
            {
                return spellMods[spellModIndex - 1];
            }

            throw ctx.Error($"Unknown spell mod index <color=white>{spellModIndex.ToBase36()}</color>.");
        }

        throw ctx.Error($"Ability <color=white>{abilityName}</color> has no spell mods.");
    }

    public static List<string> GetSpellModNameList(ChatCommandContext ctx, string abilityName)
    {
        if (AbilitySpellMods.TryGetValue(abilityName, out var spellMods))
        {
            return spellMods
                .Select(s => s.Replace("_", "")
                    .Replace(abilityName, "", System.StringComparison.OrdinalIgnoreCase)
                    .Replace("SpellMod", "")
                    .Replace("Shared", ""))
                .Select(s => Regex.Replace(s, @"([A-Z])", " $1").Trim())
                .ToList();
        }

        throw ctx.Error($"Ability <color=white>{abilityName}</color> has no spell mods.");
    }

    public static string FormatSpellModList(string abilityName)
    {
        try
        {
            var names = GetSpellModNameList(null, abilityName);
            return string.Join(", ", names.Select((name, index) => $"{(index + 1).ToBase36()}: {name}"));
        }
        catch
        {
            return "No spell mods available";
        }
    }
}
