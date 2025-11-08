using HarmonyLib;
using ProjectM;
using ProjectM.Network;
using Unity.Collections;
using Unity.Entities;
using CrowbaneArena.Services;
using System;
using System.Collections.Generic;
using Stunlock.Core;
using System.Reflection;
using System.Linq;

namespace CrowbaneArena.Patches
{
    /// <summary>
    /// Runtime installer for unlock-all progression hooks using reflection.
    /// Tries multiple candidate types/methods and installs Harmony prefixes that
    /// return true while the player is in arena (UI-only unlocks).
    /// </summary>
    public static class VBloodHookPatch
    {
        private static EntityManager EM => VRisingCore.EntityManager;

        private static readonly string[] CandidateTypes = new[]
        {
            "ProjectM.ProgressionEventsSystem",
            "ProjectM.ProgressionSystem",
            "ProjectM.SpellUnlockSystem",
            "ProjectM.AbilityUnlockSystem"
        };

        private static readonly string[] CandidateMethods = new[]
        {
            "HasConsumedVBlood",
            "HasSlainVBlood",
            "IsUnlocked",
            "HasUnlocked"
        };

        public static void Install(Harmony harmony)
        {
            int installed = 0;
            Plugin.Logger?.LogInfo("[VBloodHook] Starting VBlood hook installation...");
            
            // Get a curated set of ProjectM assemblies to minimize TypeLoadExceptions
            var allowedAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ProjectM",                      // Core gameplay
                "ProjectM.Shared",               // Shared utilities
                "ProjectM.Shared.Systems",       // Shared systems
                "ProjectM.Gameplay.Systems",     // Gameplay systems
                "ProjectM.ScriptableSystems",    // Data/scriptable systems
                "ProjectM.CodeGeneration"        // Generated helpers
            };

            var projectMAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => allowedAssemblyNames.Contains(a.GetName().Name))
                .ToList();
                
            Plugin.Logger?.LogInfo($"[VBloodHook] Found {projectMAssemblies.Count} ProjectM assemblies to search");
            
            foreach (var assembly in projectMAssemblies)
            {
                Plugin.Logger?.LogInfo($"[VBloodHook] Searching assembly: {assembly.GetName().Name}");
                
                try
                {
                    // Get all types in the assembly safely
                    var allTypes = assembly.GetTypes();
                    
                    foreach (var typeName in CandidateTypes)
                    {
                        var fullTypeName = typeName;
                        
                        // Try to find type by exact name first
                        var t = allTypes.FirstOrDefault(x => x.FullName == fullTypeName);
                        
                        // If not found, try by simple name
                        if (t == null)
                        {
                            var simpleName = typeName.Split('.').Last();
                            t = allTypes.FirstOrDefault(x => x.Name == simpleName);
                        }
                        
                        if (t == null)
                        {
                            Plugin.Logger?.LogDebug($"[VBloodHook] Type not found: {typeName} in {assembly.GetName().Name}");
                            continue;
                        }
                        
                        Plugin.Logger?.LogInfo($"[VBloodHook] Found type: {t.FullName} in {assembly.GetName().Name}");
                        
                        foreach (var mName in CandidateMethods)
                        {
                            try
                            {
                                // Get all methods with matching names
                                var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                    .Where(mi => string.Equals(mi.Name, mName, StringComparison.Ordinal))
                                    .ToList();
                                    
                                foreach (var mi in methods)
                                {
                                    var pars = mi.GetParameters();
                                    if (pars.Length < 1) continue;

                                    try
                                    {
                                        var prefix = typeof(VBloodHookPatch).GetMethod(nameof(GenericUnlockPrefix), BindingFlags.Static | BindingFlags.NonPublic);
                                        harmony.Patch(mi, prefix: new HarmonyMethod(prefix));
                                        installed++;
                                        Plugin.Logger?.LogInfo($"[VBloodHook] ✅ Patched {t.Name}.{mi.Name} ({pars.Length} params) from {assembly.GetName().Name}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Plugin.Logger?.LogWarning($"[VBloodHook] ❌ Failed to patch {t.Name}.{mi.Name}: {ex.Message}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Plugin.Logger?.LogWarning($"[VBloodHook] Error processing methods for {t.Name}.{mName}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // Summarize and continue with what we have; avoid noisy per-type warnings
                    var loadedTypes = ex.Types.Where(t => t != null).ToArray();
                    Plugin.Logger?.LogWarning($"[VBloodHook] Partial load for {assembly.GetName().Name}: using {loadedTypes.Length} types (reason: {ex.Message})");
                    
                    // Process the loaded types only
                    foreach (var loadedType in loadedTypes)
                    {
                        if (loadedType == null) continue;
                        
                        foreach (var typeName in CandidateTypes)
                        {
                            if (!loadedType.FullName.Contains(typeName.Split('.').Last())) continue;
                            
                            foreach (var mName in CandidateMethods)
                            {
                                try
                                {
                                    var methods = loadedType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                        .Where(mi => string.Equals(mi.Name, mName, StringComparison.Ordinal))
                                        .ToList();
                                        
                                    foreach (var mi in methods)
                                    {
                                        var pars = mi.GetParameters();
                                        if (pars.Length < 1) continue;

                                        try
                                        {
                                            var prefix = typeof(VBloodHookPatch).GetMethod(nameof(GenericUnlockPrefix), BindingFlags.Static | BindingFlags.NonPublic);
                                            harmony.Patch(mi, prefix: new HarmonyMethod(prefix));
                                            installed++;
                                            Plugin.Logger?.LogInfo($"[VBloodHook] ✅ Patched {loadedType.Name}.{mi.Name} ({pars.Length} params) from {assembly.GetName().Name}");
                                        }
                                        catch (Exception innerEx)
                                        {
                                            Plugin.Logger?.LogWarning($"[VBloodHook] ❌ Failed to patch {loadedType.Name}.{mi.Name}: {innerEx.Message}");
                                        }
                                    }
                                }
                                catch (Exception methodEx)
                                {
                                    Plugin.Logger?.LogWarning($"[VBloodHook] Error processing methods for {loadedType.Name}.{mName}: {methodEx.Message}");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogWarning($"[VBloodHook] General error processing assembly {assembly.GetName().Name}: {ex.Message}");
                }
            }

            if (installed == 0)
            {
                Plugin.Logger?.LogInfo("[VBloodHook] No unlock methods patched. UI unlock hook inactive for this game version (non-fatal).");
            }
            else
            {
                Plugin.Logger?.LogInfo($"[VBloodHook] ✅ Successfully installed {installed} VBlood unlock hooks");
            }
        }

        // Generic prefix used for all candidate unlock checks
        private static bool GenericUnlockPrefix(ref bool __result, object __instance, params object[] __args)
        {
            try
            {
                // Try to extract platformId from first param if it's a User/UserHandle
                ulong pid = 0;

                if (__args != null && __args.Length > 0)
                {
                    var a0 = __args[0];
                    if (a0 is User u)
                    {
                        pid = u.PlatformId;
                    }
                    // UserHandle not available in current assemblies
                    // else if (a0 is UserHandle uh && uh.IsValid)
                    // {
                    //     try
                    //     {
                    //         var ue = uh.GetEntityOnServer();
                    //         if (ue != Entity.Null && EM.Exists(ue) && EM.HasComponent<User>(ue))
                    //         {
                    //             pid = EM.GetComponentData<User>(ue).PlatformId;
                    //         }
                    //     }
                    //     catch (Exception ex)
                    //     {
                    //         Plugin.Logger?.LogWarning($"Error getting entity from UserHandle: {ex.Message}");
                    //     }
                    // }
                }

                if (pid != 0 && GameSystems.IsPlayerInArena(pid))
                {
                    __result = true;
                    return false; // skip original
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogDebug($"[VBloodHook] Error in GenericUnlockPrefix: {ex.Message}");
            }

            return true; // run original
        }
    }
}
