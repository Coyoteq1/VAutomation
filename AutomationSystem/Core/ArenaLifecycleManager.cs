using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using CrowbaneArena.Systems;
using CrowbaneArena.Stubs;
using RisingV.Shared.Managers;
using RisingV.Shared.Plugins;
using Unity.Entities;
using UnityEngine;

namespace CrowbaneArena.Core
{
  public class ArenaLifecycleManager : IManageableLifecycle
    {
private static ManualLogSource? _logger;
    private bool _isInitialized;
  private bool _isLoaded;
  private bool _isReady;
  private bool _isTerminated;

        private DualCharacterSetup _dualCharacterSetup;
        private AutoSwapSystem _autoSwapSystem;
        private CharacterAutoSelector _characterAutoSelector;
  private StubPlugin _arenaUpdateBehaviour;

  public string LifecycleStatus => $"Initialized: {_isInitialized}, Loaded: {_isLoaded}, Ready: {_isReady}, Terminated: {_isTerminated}";

        public ArenaLifecycleManager()
  {
  _logger = Plugin.Logger;
    }

      #region IManageableLifecycle Implementation
public void Initialize(IManager manager)
     {
   try
        {
       _logger?.LogInfo("ArenaLifecycleManager: Initializing...");

           CrowbaneArenaCore.Initialize();
       VRisingCore.Initialize();
       DotsHookManager.Initialize();

          _isInitialized = true;
     _logger?.LogInfo("ArenaLifecycleManager: Initialization completed successfully");
       }
      catch (Exception ex)
   {
     _logger?.LogError($"ArenaLifecycleManager initialization failed: {ex.Message}");
     _isInitialized = false;
     throw;
       }
        }

 public void Initialize(IManager manager, List<IPlugin> plugins)
        {
     var arenaPlugins = plugins.Where(p => p.Name.Contains("Arena") || p.Name.Contains("Crowbane")).ToList();
   _logger?.LogInfo($"ArenaLifecycleManager: Initializing with {arenaPlugins.Count} relevant plugins");
     Initialize(manager);
   foreach (var plugin in arenaPlugins)
  {
         _logger?.LogInfo($"ArenaLifecycleManager: Initialized with plugin: {plugin.Name}");
    }
      }

     public void Load(IManager manager)
        {
    if (!_isInitialized)
     throw new InvalidOperationException("Cannot load before initialization");

       try
     {
    _logger?.LogInfo("ArenaLifecycleManager: Loading components...");

        try
  {
      World.DefaultGameObjectInjectionWorld?.CreateSystem<ArenaPlayerTrackerSystem>();
    _logger?.LogInfo("ArenaLifecycleManager: ECS tracking systems initialized");
      }
      catch (Exception ecsEx)
        {
   _logger?.LogError($"ArenaLifecycleManager: ECS system initialization failed: {ecsEx}");
     throw;
        }

           try
  {
      VampireCommandFramework.CommandRegistry.RegisterAll(System.Reflection.Assembly.GetExecutingAssembly());
          _logger?.LogInfo("ArenaLifecycleManager: VCF commands registered successfully");
      }
                catch (Exception cmdEx)
       {
  _logger?.LogError($"ArenaLifecycleManager: VCF command registration failed: {cmdEx.Message}");
      throw;
       }

   ArenaConfigLoader.Initialize();
   CrowbaneArena.Data.Database.Initialize();
       Services.BuildManager.LoadData();

       _dualCharacterSetup = new DualCharacterSetup(CrowbaneArenaCore.EntityManager);
   _autoSwapSystem = new AutoSwapSystem(CrowbaneArenaCore.EntityManager);
 _characterAutoSelector = new CharacterAutoSelector(CrowbaneArenaCore.EntityManager, _dualCharacterSetup);

    _logger?.LogInfo("ArenaLifecycleManager: Dual character systems initialized");

       ArenaTerritory.InitializeArenaGrid();
    PlayerTracker.Initialize();

           _isLoaded = true;
      _logger?.LogInfo("ArenaLifecycleManager: Loading completed successfully");
     }
  catch (Exception ex)
   {
     _logger?.LogError($"ArenaLifecycleManager loading failed: {ex.Message}");
   _isLoaded = false;
         throw;
          }
  }

        public void Load(IManager manager, List<IPlugin> plugins)
   {
        Load(manager);
    }

        public void Ready(IManager manager)
      {
 if (!_isLoaded)
  throw new InvalidOperationException("Cannot ready before loading");

     try
  {
   _logger?.LogInfo("ArenaLifecycleManager: Preparing for active use...");
  _arenaUpdateBehaviour = Plugin.Instance.AddComponent<StubPlugin>();
     _isReady = true;
             _logger?.LogInfo("ArenaLifecycleManager: Ready for active use");
            }
    catch (Exception ex)
    {
   _logger?.LogError($"ArenaLifecycleManager ready phase failed: {ex.Message}");
     _isReady = false;
       throw;
 }
        }

      public void Ready(IManager manager, List<IPlugin> plugins)
        {
       Ready(manager);
  }

        public void Terminate(IManager manager)
   {
 if (!_isReady)
       {
       _logger?.LogWarning("ArenaLifecycleManager: Terminate called but system not ready");
          return;
        }

         try
    {
         _logger?.LogInfo("ArenaLifecycleManager: Terminating active components...");

  if (_arenaUpdateBehaviour != null)
{
      UnityEngine.Object.Destroy(_arenaUpdateBehaviour);
         _arenaUpdateBehaviour = null;
     }

     SnapshotManagerService.ClearAllSnapshots();

   _characterAutoSelector = null;
    _autoSwapSystem = null;
      _dualCharacterSetup = null;

       _isTerminated = true;
        _logger?.LogInfo("ArenaLifecycleManager: Termination completed");
    }
catch (Exception ex)
     {
     _logger?.LogError($"ArenaLifecycleManager termination failed: {ex.Message}");
           throw;
    }
        }

        public void Terminate(IManager manager, List<IPlugin> plugins)
    {
        Terminate(manager);
    }

 public void Unload(IManager manager)
        {
        try
   {
   _logger?.LogInfo("ArenaLifecycleManager: Unloading components...");
  _isReady = false;
       _isLoaded = false;
    _isInitialized = false;
     CrowbaneArenaCore.Reinitialize();
 _isTerminated = false;
     _logger?.LogInfo("ArenaLifecycleManager: Unloading completed");
        }
    catch (Exception ex)
  {
  _logger?.LogError($"ArenaLifecycleManager unloading failed: {ex.Message}");
throw;
            }
    }

        public void Unload(IManager manager, List<IPlugin> plugins)
  {
Unload(manager);
        }
 #endregion

        #region Helper Methods
  private void HandleReload(IManager manager, ReloadReason reason)
        {
     _logger?.LogInfo($"ArenaLifecycleManager reloading due to: {reason}");

    try
   {
      if (_isLoaded)
    {
  Terminate(manager);
    Unload(manager);
Initialize(manager);
 Load(manager);
         Ready(manager);
   }
    else if (_isInitialized)
     {
 Unload(manager);
   Initialize(manager);
      }

_logger?.LogInfo("ArenaLifecycleManager reload completed");
            }
  catch (Exception ex)
     {
   _logger?.LogError($"ArenaLifecycleManager reload failed: {ex.Message}");
       throw;
  }
     }

  private void HandleReload(IManager manager, List<IPlugin> plugins, ReloadReason reason)
        {
  _logger?.LogInfo($"ArenaLifecycleManager reloading with {plugins.Count} plugins due to: {reason}");

 try
    {
         var arenaPlugins = plugins.Where(p => p.Name.Contains("Arena") || p.Name.Contains("Crowbane")).ToList();
          HandleReload(manager, reason);

 foreach (var plugin in arenaPlugins)
{
      _logger?.LogInfo($"Reloaded with plugin dependency: {plugin.Name}");
          }
 }
       catch (Exception ex)
   {
 _logger?.LogError($"ArenaLifecycleManager reload with plugins failed: {ex.Message}");
       throw;
          }
        }

        public string GetStatus()
    {
  return $"ArenaLifecycleManager - {LifecycleStatus}";
    }

        public bool ValidateState()
        {
    if (_isReady && !_isLoaded) return false;
         if (_isLoaded && !_isInitialized) return false;
  if (_isTerminated && _isReady) return false;
        return true;
      }
     #endregion
  }
}
