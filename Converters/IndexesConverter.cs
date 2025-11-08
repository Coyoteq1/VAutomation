using System.Collections.Generic;
using System.Linq;
using CrowbaneArena.Extensions;
using VampireCommandFramework;

namespace CrowbaneArena.Commands.Converters;

internal static class IndexesConverter
{
    public static List<int> ToIndexesList(ChatCommandContext ctx, string input, int length, bool ignoreZero = true)
    {
        var fixedInput = input;
        if (ignoreZero)
        {
            fixedInput = input.Replace("0", "");
        }

        // add '0' at the end to pad
        fixedInput = fixedInput.PadRight(length, '0');

        // index unique check
        var listWithoutZero = fixedInput.Select(c => c.ToString()).Where(i => i != "0").ToList();
        if (listWithoutZero.Distinct().Count() != listWithoutZero.Count)
        {
            throw ctx.Error($"Indexes must be different (<color=white>{input}</color>).");
        }

        // fix list size
        if (fixedInput.Length > length)
        {
            ctx.Reply($"Only {length} indexes are required. Keeping <color=white>{fixedInput[..length]}</color>.");
            fixedInput = fixedInput.Substring(0, length);
        }

        return fixedInput.Select(c => c.ToString().FromBase36()).ToList();
    }

    public static List<int> ParseBase36Indexes(string input, int maxLength = 10)
    {
        if (string.IsNullOrEmpty(input))
            return new List<int>();

        var result = new List<int>();
        foreach (var c in input)
        {
            if (result.Count >= maxLength) break;
            try
            {
                var index = c.ToString().FromBase36();
                result.Add(index);
            }
            catch
            {
                // Skip invalid characters
                continue;
            }
        }

        return result;
    }

    public static string FormatIndexesList(List<int> indexes)
    {
        return string.Join("", indexes.Select(i => i.ToBase36()));
    }
}
