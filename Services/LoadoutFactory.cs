using System;
using System.Collections.Generic;
using System.Linq;
using CrowbaneArena.Core;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Helper for assembling ArenaLoadout objects on-the-fly from CSV strings or name lists.
    /// Consumes the dictionaries exposed by <see cref="Plugin"/> (WeaponsDB, ArmorSetsDB, ConsumablesDB).
    /// </summary>
    public static class LoadoutFactory
    {
        /// <summary>
        /// Build a loadout from comma-separated weapon / armor-set / consumable lists.
        /// Unknown components are skipped; warnings are logged.
        /// </summary>
        /// <param name="loadoutName">Display / key name.</param>
        /// <param name="weaponsCsv">e.g. "sword,axe"</param>
        /// <param name="armorSetsCsv">e.g. "warrior,rogue"</param>
        /// <param name="consumablesCsv">e.g. "blood_rose,physical_brew"</param>
        /// <param name="enabled">Whether loadout is active.</param>
        /// <returns>A fully assembled ArenaLoadout.</returns>
        public static ArenaLoadout BuildFromCsv(string loadoutName, string weaponsCsv, string armorSetsCsv, string consumablesCsv, bool enabled = true)
        {
            var loadout = new ArenaLoadout { Name = loadoutName, Enabled = enabled };

            // Weapons
            foreach (var token in SplitCsv(weaponsCsv))
            {
                if (Plugin.WeaponsDB.TryGetValue(token, out var guid))
                {
                    loadout.Weapons.Add(guid);
                }
                else
                {
                    Plugin.Logger?.LogWarning($"[LoadoutFactory] Missing weapon '{token}' while building '{loadoutName}'.");
                }
            }

            // Armor sets
            foreach (var token in SplitCsv(armorSetsCsv))
            {
                if (Plugin.ArmorSetsDB.TryGetValue(token, out var armorSet))
                {
                    loadout.Armor.AddRange(armorSet.Guids);
                }
                else
                {
                    Plugin.Logger?.LogWarning($"[LoadoutFactory] Missing armor set '{token}' while building '{loadoutName}'.");
                }
            }

            // Consumables (use default amounts defined in DB)
            foreach (var token in SplitCsv(consumablesCsv))
            {
                if (Plugin.ConsumablesDB.TryGetValue(token, out var cons))
                {
                    loadout.Consumables.Add(new ArenaItem { Guid = cons.Guid, Amount = cons.DefaultAmount });
                }
                else
                {
                    Plugin.Logger?.LogWarning($"[LoadoutFactory] Missing consumable '{token}' while building '{loadoutName}'.");
                }
            }

            return loadout;
        }

        /// <summary>
        /// Try to build a loadout; returns false if no valid items were added (empty loadout).
        /// </summary>
        public static bool TryBuildFromCsv(string loadoutName, string weaponsCsv, string armorCsv, string consumablesCsv, out ArenaLoadout loadout)
        {
            loadout = BuildFromCsv(loadoutName, weaponsCsv, armorCsv, consumablesCsv);
            return loadout.Weapons.Count + loadout.Armor.Count + loadout.Consumables.Count > 0;
        }

        private static IEnumerable<string> SplitCsv(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) yield break;
            foreach (var part in csv.Split(','))
            {
                var token = part.Trim().ToLowerInvariant();
                if (!string.IsNullOrEmpty(token)) yield return token;
            }
        }
    }
}
