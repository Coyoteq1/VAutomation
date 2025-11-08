using System;
using System.Linq;
using CrowbaneArena.Configs;

namespace CrowbaneArena.Services
{
    public static class ConfigValidationHelper
    {
        public static ValidationResult ValidateArenaConfig(CrowbaneArena.Configs.ArenaConfig config)
        {
            var errors = new System.Collections.Generic.List<string>();

            if (config == null)
            {
                errors.Add("Config is null");
                return new ValidationResult { IsValid = false, ErrorMessage = "Config is null" };
            }

            // Check required sections
            if (config.Offensive == null)
            {
                errors.Add("Offensive build config is missing");
            }
            else
            {
                ValidateBuildConfig(config.Offensive, "Offensive", errors);
            }

            if (config.Defensive == null)
            {
                errors.Add("Defensive build config is missing");
            }
            else
            {
                ValidateBuildConfig(config.Defensive, "Defensive", errors);
            }

            if (config.Minions == null)
            {
                errors.Add("Minions build config is missing");
            }
            else
            {
                ValidateBuildConfig(config.Minions, "Minions", errors);
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                ErrorMessage = string.Join("; ", errors)
            };
        }

        private static void ValidateBuildConfig(CrowbaneArena.Configs.BuildConfig build, string buildName, System.Collections.Generic.List<string> errors)
        {
            if (build.Blood == null)
            {
                errors.Add($"{buildName}: Blood config is missing");
            }
            else
            {
                if (build.Blood.PrimaryQuality < 0 || build.Blood.PrimaryQuality > 100)
                {
                    errors.Add($"{buildName}: Blood primary quality must be between 0-100");
                }
                if (build.Blood.SecondaryQuality < 0 || build.Blood.SecondaryQuality > 100)
                {
                    errors.Add($"{buildName}: Blood secondary quality must be between 0-100");
                }
            }

            if (build.Weapons != null && build.Weapons.Any())
            {
                foreach (var weapon in build.Weapons)
                {
                    if (string.IsNullOrEmpty(weapon.Name))
                    {
                        errors.Add($"{buildName}: Weapon name is empty");
                    }
                }
            }

            if (build.Items != null && build.Items.Any())
            {
                foreach (var item in build.Items)
                {
                    if (string.IsNullOrEmpty(item.Name))
                    {
                        errors.Add($"{buildName}: Item name is empty");
                    }
                    if (item.Amount <= 0)
                    {
                        errors.Add($"{buildName}: Item amount must be positive");
                    }
                }
            }
        }

        public static ValidationResult ValidateHarmonyProtectionConfig(HarmonyProtectionConfig config)
        {
            var errors = new System.Collections.Generic.List<string>();

            if (config == null)
            {
                return new ValidationResult { IsValid = false, ErrorMessage = "Config is null" };
            }

            if (config.HarmonyProtection == null)
            {
                errors.Add("Harmony protection settings are missing");
            }
            else
            {
                if (config.HarmonyProtection.MaxRetries < 0)
                {
                    errors.Add("Max retries must be non-negative");
                }
            }

            if (config.VBloodHookSettings == null)
            {
                errors.Add("VBlood hook settings are missing");
            }

            if (config.Debugging == null)
            {
                errors.Add("Debugging settings are missing");
            }

            return new ValidationResult
            {
                IsValid = errors.Count == 0,
                ErrorMessage = string.Join("; ", errors)
            };
        }
    }
}
