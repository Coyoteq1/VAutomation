using System;
using System.Collections.Generic;
using System.Text;
using Stunlock.Core;
using VampireCommandFramework;

namespace CrowbaneArena.Commands.Converters
{
    internal class ItemParameterConverter : CommandArgumentConverter<ItemParameter>
    {
        private static readonly Dictionary<string, PrefabGUID> CustomItems = new()
        {
            {"bloodment", new PrefabGUID(-1905691330)},
            {"iron", new PrefabGUID(-1905691331)},
            {"copper", new PrefabGUID(-1905691332)},
            {"stone", new PrefabGUID(-1905691333)},
            {"wood", new PrefabGUID(-1905691334)}
        };

        public override ItemParameter Parse(ICommandContext ctx, string input)
        {
            if (int.TryParse(input, out var integral))
            {
                return new ItemParameter(new(integral));
            }

            if (CustomItems.TryGetValue(input.ToLower(), out var customItem))
            {
                return new ItemParameter(customItem);
            }

            if (TryGet(input, out var result)) return result;

            var inputIngredientAdded = "Item_Ingredient_" + input;
            if (TryGet(inputIngredientAdded, out result)) return result;

            var standardPostfix = inputIngredientAdded + "_Standard";
            if (TryGet(standardPostfix, out result)) return result;

            throw ctx.Error($"Invalid item id: {input}");
        }

        private static bool TryGet(string input, out ItemParameter item)
        {
            item = new ItemParameter(new(0));
            return false;
        }
    }
}
