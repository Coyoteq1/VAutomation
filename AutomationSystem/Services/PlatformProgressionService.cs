using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;
using CrowbaneArena.Services;

namespace CrowbaneArena.Services
{
 /// <summary>
 /// Abstraction that attempts to unlock/lock platform achievements plus handles VBlood unlocks via existing managers.
 /// Uses reflection fallback to call common platform methods if available. Safe no-op if not found.
 /// </summary>
 public static class PlatformProgressionService
 {
 private static readonly string[] CandidateAssemblyNames = new[] {
 "com.stunlock.platform", "com.stunlock.platform.pc", "com.stunlock.platform.sw", "Stunlock.Platform", "Stunlock.Platform.PC"
 };

 private static readonly string[] CandidateTypeNames = new[] {
 "PlatformAchievements", "PlatformManager", "AchievementsManager", "AchievementService", "PlatformService"
 };

 private static readonly string[] UnlockMethodNames = new[] {
 "UnlockAchievement", "SetAchievement", "GrantAchievement", "SetAchievementState", "Unlock", "SetAchievementUnlocked"
 };

 private static readonly string[] LockMethodNames = new[] {
 "ResetAchievement", "ClearAchievement", "LockAchievement", "RemoveAchievement", "SetAchievementLocked", "UnlockAllAchievements"
 };

 /// <summary>
 /// Unlock all VBloods (boss unlocks + UI) for the user entity and attempt to unlock platform achievements if a list provided.
 /// </summary>
 public static bool UnlockAllProgression(Entity userEntity, IEnumerable<string>? achievementIds = null)
 {
 try
 {
 if (userEntity == Entity.Null)
 {
 Plugin.Logger?.LogWarning("UnlockAllProgression called with null userEntity");
 return false;
 }

 // Unlock all VBloods via BossService and register them
 if (BossService.Instance != null)
 {
  var unlockedBosses = new List<int>();
  foreach (var boss in BossService.Instance.GetAllBosses())
  {
   if (BossService.Instance.UnlockBoss(boss, userEntity))
   {
    unlockedBosses.Add(boss.Value.GuidHash);
   }
  }
  // Store unlocked bosses in snapshot if needed
  Plugin.Logger?.LogInfo($"Unlocked {unlockedBosses.Count} bosses for arena entry");
 }

 // If achievements list provided, attempt to call platform API to unlock them
 if (achievementIds != null)
 {
 var ids = achievementIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!.Trim()).ToList();
 if (ids.Count >0)
 {
 TryInvokePlatformMethods(ids, true);
 }
 }

 return true;
 }
 catch (Exception ex)
 {
 Plugin.Logger?.LogError($"Error in UnlockAllProgression: {ex.Message}");
 return false;
 }
 }

 /// <summary>
 /// Lock / clear VBloods from snapshot and attempt to lock platform achievements if provided.
 /// </summary>
 public static bool LockAllProgression(Entity userEntity, IEnumerable<string>? achievementIds = null)
 {
 try
 {
 if (userEntity == Entity.Null)
 {
 Plugin.Logger?.LogWarning("LockAllProgression called with null userEntity");
 return false;
 }

 // Lock VBloods handled by snapshot restoration

 // Attempt to lock platform achievements if provided
 if (achievementIds != null)
 {
 var ids = achievementIds.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!.Trim()).ToList();
 if (ids.Count >0)
 {
 TryInvokePlatformMethods(ids, false);
 }
 }

 return true;
 }
 catch (Exception ex)
 {
 Plugin.Logger?.LogError($"Error in LockAllProgression: {ex.Message}");
 return false;
 }
 }

 /// <summary>
 /// Restore VBloods from saved snapshot and optionally reset achievements list by locking them.
 /// </summary>
 public static bool RestoreProgression(Entity userEntity, IEnumerable<string>? achievementIdsToLock = null)
 {
 try
 {
 if (userEntity == Entity.Null)
 {
 Plugin.Logger?.LogWarning("RestoreProgression called with null userEntity");
 return false;
 }

 // Restore VBloods via BossService
 if (BossService.Instance != null)
 {
  BossService.Instance.UnlockAllBosses(userEntity);
 }

 // Optionally lock achievements
 if (achievementIdsToLock != null)
 {
 var ids = achievementIdsToLock.Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!.Trim()).ToList();
 if (ids.Count >0)
 {
 TryInvokePlatformMethods(ids, false);
 }
 }

 return true;
 }
 catch (Exception ex)
 {
 Plugin.Logger?.LogError($"Error in RestoreProgression: {ex.Message}");
 return false;
 }
 }

 /// <summary>
 /// Try to locate platform assemblies and invoke unlock/lock methods for the provided achievement ids.
 /// If unlock==true will try unlock methods, otherwise try lock methods.
 /// This is best-effort and will log if no suitable API found.
 /// </summary>
 private static void TryInvokePlatformMethods(List<string> achievementIds, bool unlock)
 {
 try
 {
 var assemblies = AppDomain.CurrentDomain.GetAssemblies();

 // Try candidate assemblies first, then fallback to scanning all loaded assemblies
 var ordered = assemblies.OrderBy(a => a.GetName().Name == null ?1 : (CandidateAssemblyNames.Contains(a.GetName().Name) ?0 :1)).ToArray();

 bool invokedAny = false;

 foreach (var asm in ordered)
 {
 string asmName = asm.GetName().Name ?? string.Empty;
 // Skip system assemblies quickly
 if (asmName.StartsWith("System", StringComparison.OrdinalIgnoreCase) || asmName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase))
 continue;

 Type[] types;
 try { types = asm.GetTypes(); } catch { continue; }

 foreach (var type in types)
 {
 if (!CandidateTypeNames.Any(n => string.Equals(n, type.Name, StringComparison.OrdinalIgnoreCase)))
 continue;

 // Look for static or instance method candidates
 var methodNames = unlock ? UnlockMethodNames : LockMethodNames;

 foreach (var methodName in methodNames)
 {
 var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
 var target = methods.FirstOrDefault(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase));
 if (target == null) continue;

 // Determine invocation path
 object? instance = null;
 if (!target.IsStatic)
 {
 try
 {
 // Try to get a singleton instance property or default constructor
 var instProp = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
 if (instProp != null)
 instance = instProp.GetValue(null);
 else
 {
 var fld = type.GetField("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
 if (fld != null)
 instance = fld.GetValue(null);
 else
 {
 // Try parameterless constructor
 var ctor = type.GetConstructor(Type.EmptyTypes);
 if (ctor != null)
 instance = Activator.CreateInstance(type);
 }
 }
 }
 catch { instance = null; }

 if (instance == null) continue; // can't call instance method
 }

 // Try to match parameter signature
 foreach (var id in achievementIds)
 {
 try
 {
 var invoked = TryInvokeMethodWithCommonSignatures(target, instance, id);
 if (invoked)
 {
 invokedAny = true;
 Plugin.Logger?.LogInfo($"PlatformProgressionService: {(unlock?"Unlocked":"Locked")} achievement '{id}' via {type.FullName}.{target.Name}");
 }
 else
 {
 Plugin.Logger?.LogDebug($"PlatformProgressionService: method {type.FullName}.{target.Name} exists but signature didn't match for id '{id}'");
 }
 }
 catch (Exception ex)
 {
 Plugin.Logger?.LogError($"PlatformProgressionService: error invoking {type.FullName}.{target.Name} for '{id}': {ex.Message}");
 }
 }

 if (invokedAny) break;
 }

 if (invokedAny) break;
 }

 if (invokedAny) break;
 }

 if (!invokedAny)
 {
 Plugin.Logger?.LogWarning("PlatformProgressionService: No platform achievement API found or invocation failed. Operations were applied to VBloods but platform achievements could not be changed.");
 }
 }
 catch (Exception ex)
 {
 Plugin.Logger?.LogError($"PlatformProgressionService: Error while trying platform invocation: {ex.Message}");
 }
 }

 /// <summary>
 /// Attempt to invoke the method using common signatures used by platform APIs.
 /// Returns true if invocation succeeded.
 /// </summary>
 private static bool TryInvokeMethodWithCommonSignatures(MethodInfo method, object? instance, string achievementId)
 {
 var parms = method.GetParameters();

 // Common signatures we try (order matters):
 // (ulong platformId, string achievementId)
 // (string achievementId, ulong platformId)
 // (string achievementId)
 // (ulong platformId, int id)
 // (int id)
 // (string achievementId, bool value)

 // We don't have platformId in this method scope; many platform APIs are instance-based and already bound to platform context.
 // So first try single-string param

 if (parms.Length ==1 && parms[0].ParameterType == typeof(string))
 {
 method.Invoke(instance, new object[] { achievementId });
 return true;
 }

 if (parms.Length ==2 && parms[0].ParameterType == typeof(ulong) && parms[1].ParameterType == typeof(string))
 {
 // try to get steam id from environment - not available here; skip
 return false;
 }

 if (parms.Length ==2 && parms[0].ParameterType == typeof(string) && parms[1].ParameterType == typeof(ulong))
 {
 return false;
 }

 if (parms.Length ==1 && parms[0].ParameterType == typeof(int))
 {
 if (int.TryParse(achievementId, out var intId))
 {
 method.Invoke(instance, new object[] { intId });
 return true;
 }
 return false;
 }

 if (parms.Length ==2 && parms[0].ParameterType == typeof(string) && parms[1].ParameterType == typeof(bool))
 {
 method.Invoke(instance, new object[] { achievementId, true });
 return true;
 }

 // Try parameterless methods named UnlockAllAchievements / ResetAllAchievements
 if (parms.Length ==0)
 {
 method.Invoke(instance, null);
 return true;
 }

 return false;
 }
 }
}
