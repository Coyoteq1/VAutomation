using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CrowbaneArena.Services
{
    /// <summary>
    /// Service to protect against HarmonyX type loading exceptions during assembly scanning
    /// </summary>
    public static class HarmonyProtectionService
    {
        private static readonly HashSet<string> _problematicAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "__Generated",                    // Generated code assemblies often have issues
            "UnityEngine.PhysicsModule",      // Known problematic Unity module
            "UnityEngine.ParticleSystemModule", // Known problematic Unity module  
            "UnityEngine.UIElementsModule",   // Known problematic Unity module
            "UnityEngine.VirtualTexturingModule", // Known problematic Unity module
        };

        private static readonly HashSet<string> _problematicTypePatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "MotionBlob",                     // Rukhanka animation system issues
            "ChildMotionBlob",                // Rukhanka animation system issues
            "BlendTreeBlob",                  // Rukhanka animation system issues
            "Flags",                          // Unity physics module issues
            "LightProbesQueryDisposeJob",     // Unity core module issues
            "PageStatistics",                 // Unity UI elements issues
            "TextureStackBase`1",             // Unity virtual texturing issues
            "HybridEquipmentStreamingHandler+Singleton", // ProjectM streaming issues
            "HybridModelStreamingHandler+Singleton",     // ProjectM streaming issues
            "UIAssetSubSceneLoader_ClientWorld+Singleton", // ProjectM UI loading issues
            "UIAssetSubSceneLoader_DefaultWorld+Singleton"  // ProjectM UI loading issues
        };

        /// <summary>
        /// Install protected Harmony instance with custom patch all logic
        /// </summary>
        public static void InstallProtectedPatches(Harmony harmony, Assembly assembly)
        {
            try
            {
                Plugin.Logger?.LogInfo("[HarmonyProtection] Installing protected Harmony patches...");
                
                // Get all patch methods from the assembly
                var patchMethods = GetPatchMethodsFromAssembly(assembly);
                
                int successfulPatches = 0;
                int failedPatches = 0;
                
                foreach (var patchMethod in patchMethods)
                {
                    try
                    {
                        // Create harmony method with error handling
                        var harmonyMethod = new HarmonyMethod(patchMethod);
                        harmonyMethod.method = patchMethod;
                        
                        // Try to patch - Harmony will handle method discovery internally
                        harmony.PatchAll(assembly);
                        successfulPatches++;
                        
                        Plugin.Logger?.LogDebug($"[HarmonyProtection] ✅ Successfully processed patch method: {patchMethod.DeclaringType?.Name}.{patchMethod.Name}");
                    }
                    catch (Exception ex)
                    {
                        failedPatches++;
                        Plugin.Logger?.LogWarning($"[HarmonyProtection] ❌ Failed to process patch method {patchMethod.DeclaringType?.Name}.{patchMethod.Name}: {ex.Message}");
                        
                        // Log more details for debugging
                        if (ex is TargetInvocationException targetEx && targetEx.InnerException != null)
                        {
                            Plugin.Logger?.LogWarning($"[HarmonyProtection] Inner exception: {targetEx.InnerException.Message}");
                        }
                    }
                }
                
                Plugin.Logger?.LogInfo($"[HarmonyProtection] Patch installation complete: {successfulPatches} successful, {failedPatches} failed");
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[HarmonyProtection] Critical error during protected patch installation: {ex.Message}");
            }
        }

        /// <summary>
        /// Safe assembly type loader that handles problematic assemblies
        /// </summary>
        public static Type[] GetSafeTypesFromAssembly(Assembly assembly)
        {
            try
            {
                // Skip obviously problematic assemblies
                var assemblyName = assembly.GetName().Name;
                if (_problematicAssemblyNames.Contains(assemblyName))
                {
                    Plugin.Logger?.LogDebug($"[HarmonyProtection] Skipping problematic assembly: {assemblyName}");
                    return Array.Empty<Type>();
                }
                
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Plugin.Logger?.LogWarning($"[HarmonyProtection] ReflectionTypeLoadException for {assembly.GetName().Name}: {ex.Message}");
                
                // Filter out problematic types
                var safeTypes = ex.Types
                    .Where(t => t != null && !IsProblematicType(t))
                    .ToArray();
                    
                Plugin.Logger?.LogInfo($"[HarmonyProtection] Loaded {safeTypes.Length} safe types from {assembly.GetName().Name} (filtered from {ex.Types.Length})");
                return safeTypes;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[HarmonyProtection] Error loading types from {assembly.GetName().Name}: {ex.Message}");
                return Array.Empty<Type>();
            }
        }

        /// <summary>
        /// Check if a type is known to cause problems
        /// </summary>
        private static bool IsProblematicType(Type type)
        {
            if (type == null) return true;
            
            var typeName = type.Name;
            var fullTypeName = type.FullName;
            
            // Check for problematic type patterns
            foreach (var pattern in _problematicTypePatterns)
            {
                if (typeName.Contains(pattern) || (fullTypeName != null && fullTypeName.Contains(pattern)))
                {
                    return true;
                }
            }
            
            // Check for generic constraint violations
            if (type.IsGenericType && fullTypeName != null)
            {
                if (fullTypeName.Contains("TCompareComponent") && fullTypeName.Contains("violates the constraint"))
                {
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Get all patch methods from assembly
        /// </summary>
        private static IEnumerable<MethodInfo> GetPatchMethodsFromAssembly(Assembly assembly)
        {
            var methods = new List<MethodInfo>();
            
            try
            {
                var types = GetSafeTypesFromAssembly(assembly);
                
                foreach (var type in types)
                {
                    try
                    {
                        // Look for Harmony patch attributes
                        var patchAttributes = type.GetCustomAttributes(inherit: true)
                            .Where(a => a.GetType().Name.Contains("Harmony") || a.GetType().Name.Contains("Patch"))
                            .ToList();
                            
                        if (patchAttributes.Any())
                        {
                            var typeMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                                .Where(m => m.GetCustomAttributes(inherit: true).Any(a => a.GetType().Name.Contains("Harmony")))
                                .ToList();
                                
                            methods.AddRange(typeMethods);
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger?.LogDebug($"[HarmonyProtection] Error processing type {type.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[HarmonyProtection] Error getting patch methods from assembly: {ex.Message}");
            }
            
            return methods;
        }

        /// <summary>
        /// Log assembly information for debugging
        /// </summary>
        public static void LogAssemblyInfo()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.GetName().Name.Contains("ProjectM") || 
                               a.GetName().Name.Contains("UnityEngine") ||
                               a.GetName().Name.Contains("Rukhanka"))
                    .ToList();
                    
                Plugin.Logger?.LogInfo($"[HarmonyProtection] Found {assemblies.Count} relevant assemblies:");
                
                foreach (var assembly in assemblies.OrderBy(a => a.GetName().Name))
                {
                    var isProblematic = _problematicAssemblyNames.Contains(assembly.GetName().Name);
                    var status = isProblematic ? "⚠️ PROBLEMATIC" : "✅ OK";
                    
                    try
                    {
                        var typeCount = assembly.GetTypes().Length;
                        Plugin.Logger?.LogInfo($"[HarmonyProtection] {status} {assembly.GetName().Name} v{assembly.GetName().Version} ({typeCount} types)");
                    }
                    catch (Exception ex)
                    {
                        Plugin.Logger?.LogInfo($"[HarmonyProtection] {status} {assembly.GetName().Name} v{assembly.GetName().Version} (error loading types: {ex.Message})");
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[HarmonyProtection] Error logging assembly info: {ex.Message}");
            }
        }
    }
}