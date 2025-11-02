using System;
using System.Collections.Generic;
using System.Linq;
using ProjectM;
using Unity.Entities;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service for managing weapon variants and spawning items based on arena config.
    /// </summary>
    public static class WeaponVariantService
    {
        private static readonly ISpawner Spawner = new DefaultSpawner();
        /// <summary>
        /// Spawn a weapon variant for a player based on weapon name and mod combo
        /// </summary>
        /// <param name="player">Player entity</param>
        /// <param name="weaponName">Base weapon name (e.g., "sword", "axe")</param>
        /// <param name="modCombo">Mod combo string (e.g., "s123" for Storm + Movement/Attack/Spell)</param>
        /// <param name="quantity">Number of weapons to spawn (default: 1)</param>
        /// <returns>True if weapon was spawned successfully</returns>
        public static bool SpawnWeaponVariant(Entity player, string weaponName, string modCombo, int quantity = 1)
        {
            var weapon = ArenaConfigLoader.GetWeapons().FirstOrDefault(w => w.Name.Equals(weaponName, StringComparison.OrdinalIgnoreCase));
            if (weapon == null)
            {
                VRisingCore.Log?.LogWarning($"Weapon '{weaponName}' not found in config.");
                return false;
            }

            var variant = weapon.Variants.FirstOrDefault(v => v.ModCombo.Equals(modCombo, StringComparison.OrdinalIgnoreCase));
            if (variant == null)
            {
                VRisingCore.Log?.LogWarning($"Mod combo '{modCombo}' not found for weapon '{weaponName}'.");
                return false;
            }

            VRisingCore.Log?.LogInfo($"Spawning weapon variant: {variant.FriendlyName} (GUID: {variant.VariantGuid}) for player {player}, quantity: {quantity}");
            return Spawner.SpawnItem(player, (int)variant.VariantGuid, quantity);
        }

        /// <summary>
        /// Spawn a weapon variant with spell school only (no stat mods)
        /// </summary>
        public static bool SpawnWeaponWithSpellSchool(Entity player, string weaponName, string spellSchool, int quantity = 1)
        {
            return SpawnWeaponVariant(player, weaponName, spellSchool, quantity);
        }

        /// <summary>
        /// Spawn a base weapon without any mods
        /// </summary>
        public static bool SpawnBaseWeapon(Entity player, string weaponName, int quantity = 1)
        {
            var weapon = ArenaConfigLoader.GetWeapons().FirstOrDefault(w => w.Name.Equals(weaponName, StringComparison.OrdinalIgnoreCase));
            if (weapon == null)
            {
                VRisingCore.Log?.LogWarning($"Weapon '{weaponName}' not found in config.");
                return false;
            }

            VRisingCore.Log?.LogInfo($"Spawning base weapon: {weapon.Description} (GUID: {weapon.Guid}) for player {player}, quantity: {quantity}");
            return Spawner.SpawnItem(player, (int)weapon.Guid, quantity);
        }

        /// <summary>
        /// Get a friendly description of a weapon variant
        /// </summary>
        public static string GetWeaponVariantDescription(string weaponName, string modCombo)
        {
            var weapon = ArenaConfigLoader.GetWeapons().FirstOrDefault(w => w.Name.Equals(weaponName, StringComparison.OrdinalIgnoreCase));
            if (weapon == null) return $"Unknown weapon: {weaponName}";

            var variant = weapon.Variants.FirstOrDefault(v => v.ModCombo.Equals(modCombo, StringComparison.OrdinalIgnoreCase));
            if (variant != null)
            {
                return $"{variant.FriendlyName} ({weapon.Description})";
            }
            return $"{weapon.Description} (Base)";
        }

        /// <summary>
        /// Validate if a weapon variant can be spawned
        /// </summary>
        public static bool ValidateWeaponVariant(string weaponName, string modCombo, out string errorMessage)
        {
            var weapon = ArenaConfigLoader.GetWeapons().FirstOrDefault(w => w.Name.Equals(weaponName, StringComparison.OrdinalIgnoreCase));
            if (weapon == null)
            {
                errorMessage = $"Weapon '{weaponName}' not found.";
                return false;
            }

            var variant = weapon.Variants.FirstOrDefault(v => v.ModCombo.Equals(modCombo, StringComparison.OrdinalIgnoreCase));
            if (variant == null)
            {
                errorMessage = $"Mod combo '{modCombo}' not found for weapon '{weaponName}'.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Get all available variants for a weapon
        /// </summary>
        public static List<string> GetAvailableVariants(string weaponName)
        {
            var weapon = ArenaConfigLoader.GetWeapons().FirstOrDefault(w => w.Name.Equals(weaponName, StringComparison.OrdinalIgnoreCase));
            if (weapon == null) return new List<string>();

            var variants = weapon.Variants.Select(v => v.ModCombo).ToList();
            variants.Add("base"); // Base weapon
            return variants;
        }

        /// <summary>
        /// Spawn multiple weapon variants at once (for loadouts)
        /// </summary>
        public static int SpawnWeaponVariants(Entity player, List<(string weaponName, string modCombo, int quantity)> weapons)
        {
            int successCount = 0;

            foreach (var (weaponName, modCombo, quantity) in weapons)
            {
                if (SpawnWeaponVariant(player, weaponName, modCombo, quantity))
                {
                    successCount++;
                }
            }

            return successCount;
        }

        /// <summary>
        /// Spawn an armor set for a player
        /// </summary>
        public static bool SpawnArmorSet(Entity player, string armorSetName)
        {
            var armorSet = ArenaConfigLoader.GetArmorSets().FirstOrDefault(a => a.Name.Equals(armorSetName, StringComparison.OrdinalIgnoreCase));
            if (armorSet == null)
            {
                VRisingCore.Log?.LogWarning($"Armor set '{armorSetName}' not found in config.");
                return false;
            }

            VRisingCore.Log?.LogInfo($"Spawning armor set: {armorSet.Description} for player {player}");
            bool ok = true;
            if (armorSet.ChestGuid != 0) ok &= Spawner.EquipArmor(player, (int)armorSet.ChestGuid);
            if (armorSet.LegsGuid != 0) ok &= Spawner.EquipArmor(player, (int)armorSet.LegsGuid);
            if (armorSet.BootsGuid != 0) ok &= Spawner.EquipArmor(player, (int)armorSet.BootsGuid);
            if (armorSet.GlovesGuid != 0) ok &= Spawner.EquipArmor(player, (int)armorSet.GlovesGuid);
            return ok;
        }

        /// <summary>
        /// Spawn consumables for a player
        /// </summary>
        public static bool SpawnConsumable(Entity player, string consumableName)
        {
            var consumable = ArenaConfigLoader.ArenaSettings.Consumables.FirstOrDefault(c => c.Name.Equals(consumableName, StringComparison.OrdinalIgnoreCase));
            if (consumable == null)
            {
                VRisingCore.Log?.LogWarning($"Consumable '{consumableName}' not found in config.");
                return false;
            }

            var qty = consumable.DefaultAmount > 0 ? consumable.DefaultAmount : 1;
            VRisingCore.Log?.LogInfo($"Spawning consumable: {consumable.Name} (GUID: {consumable.Guid}) for player {player}, qty={qty}");
            return Spawner.SpawnItem(player, (int)consumable.Guid, qty);
        }

        /// <summary>
        /// Apply a loadout to a player
        /// </summary>
        public static bool ApplyLoadout(Entity player, string loadoutName)
        {
            var loadout = ArenaConfigLoader.GetLoadouts().FirstOrDefault(l => l.Name.Equals(loadoutName, StringComparison.OrdinalIgnoreCase));
            if (loadout == null)
            {
                VRisingCore.Log?.LogWarning($"Loadout '{loadoutName}' not found in config.");
                return false;
            }

            VRisingCore.Log?.LogInfo($"Applying loadout: {loadout.Description} for player {player}");

            // Spawn weapons
            foreach (var weapon in loadout.Weapons)
            {
                SpawnBaseWeapon(player, weapon);
            }

            // Spawn armor sets
            foreach (var armor in loadout.ArmorSets)
            {
                SpawnArmorSet(player, armor);
            }

            // Spawn consumables
            foreach (var consumable in loadout.Consumables)
            {
                SpawnConsumable(player, consumable);
            }

            return true;
        }
    }
}