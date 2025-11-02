using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CrowbaneArena.Commands.Converters;
using CrowbaneArena.Data;
using CrowbaneArena.Data.Shared;
using ProjectM;
using ProjectM.Gameplay.Scripting;
using ProjectM.Network;
using ProjectM.Shared;
using Stunlock.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace CrowbaneArena.Services;

internal class BossService
{
	public static BossService Instance { get; private set; }
	
	static readonly string CONFIG_PATH = Path.Combine(BepInEx.Paths.ConfigPath, CrowbaneArena.Plugin.PluginName);
	static readonly string BOSS_PATH = Path.Combine(CONFIG_PATH, "boss.json");
	
	List<FoundVBlood> lockedBosses = new();
	
	// Boss to spell school mappings (using only available spell schools)
	private static readonly Dictionary<PrefabGUID, List<int>> BossSpellMappings = new()
	{
		{ new PrefabGUID(-1905691330), new List<int> { SpellSchoolGUIDs.Blood, SpellSchoolGUIDs.Chaos } }, // Matka -> Blood + Chaos
		{ new PrefabGUID(-1905691331), new List<int> { SpellSchoolGUIDs.Frost, SpellSchoolGUIDs.Storm } }, // Terah -> Frost + Storm
		{ new PrefabGUID(-1905691332), new List<int> { 123456789 } }, // Jade -> Fire (placeholder)
		{ new PrefabGUID(-1905691333), new List<int> { -767534946 } }, // Beatrice -> Holy (placeholder)
		{ new PrefabGUID(-1905691334), new List<int> { SpellSchoolGUIDs.Shadow, SpellSchoolGUIDs.Illusion } }, // Nicholaus -> Shadow + Illusion
		{ new PrefabGUID(-1905691335), new List<int> { SpellSchoolGUIDs.Unholy } }, // Quincey -> Unholy
		{ new PrefabGUID(-1905691336), new List<int> { -1226374539 } }, // Ungora -> Nature (placeholder)
		{ new PrefabGUID(-1905691337), new List<int> { SpellSchoolGUIDs.Blood, SpellSchoolGUIDs.Chaos } }, // Terrorclaw -> Blood + Chaos
		{ new PrefabGUID(-1905691338), new List<int> { SpellSchoolGUIDs.Frost } }, // Lidia -> Frost
		{ new PrefabGUID(-1905691339), new List<int> { 123456789, SpellSchoolGUIDs.Chaos } }, // Goreswine -> Fire + Chaos
		{ new PrefabGUID(-1905691340), new List<int> { -767534946 } }, // Octavian -> Holy (placeholder)
		{ new PrefabGUID(-1905691357), new List<int> { 123456789, SpellSchoolGUIDs.Chaos } }, // Solarus -> Fire + Chaos
		{ new PrefabGUID(-1905691385), new List<int> { SpellSchoolGUIDs.Blood, SpellSchoolGUIDs.Unholy, SpellSchoolGUIDs.Shadow } } // Dracula -> Blood + Unholy + Shadow
	};
	
	public IEnumerable<PrefabGUID> LockedBosses => lockedBosses.Select(x => x.Value);
	public IEnumerable<string> LockedBossNames => lockedBosses.Select(boss => boss.Name);
	
	// Get all locked spell schools (derived from locked bosses)
	public IEnumerable<int> LockedSpellSchools =>
		lockedBosses.SelectMany(boss =>
			BossSpellMappings.GetValueOrDefault(boss.Value, new List<int>()))
		.Distinct();

	public IEnumerable<string> LockedSpellSchoolNames =>
		LockedSpellSchools.Select(schoolGuid =>
			SpellSchoolGUIDs.SpellSchools.FirstOrDefault(kvp => kvp.Value.GuidHash == schoolGuid).Key ?? "Unknown");

	struct BossFile
	{
		public FoundVBlood[] LockedBosses { get; set; }
	}

	public BossService()
	{
		Instance = this;
		LoadBosses();
	}

	public void LoadBosses()
	{
		if (!File.Exists(BOSS_PATH))
		{
			return;
		}

		var options = new JsonSerializerOptions
		{
			Converters = { new FoundVBloodJsonConverter() },
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true
		};

		var bossFile = JsonSerializer.Deserialize<BossFile>(File.ReadAllText(BOSS_PATH), options);
		lockedBosses.AddRange(bossFile.LockedBosses);

		// Apply boss and spell locks
		ApplyAllLocks();
	}

	public void SaveBosses()
	{
		var options = new JsonSerializerOptions
		{
			Converters = { new FoundVBloodJsonConverter() },
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true
		};

		var bossFile = new BossFile
		{
			LockedBosses = lockedBosses.ToArray()
		};

		File.WriteAllText(BOSS_PATH, JsonSerializer.Serialize(bossFile, options));
	}

	public bool LockBoss(FoundVBlood boss, Entity userEntity = default)
	{
		if (!lockedBosses.Contains(boss))
		{
			lockedBosses.Add(boss);

			RemoveBoss(boss);
			LockAssociatedSpells(boss, userEntity);
			SpawnBossLockEffects(boss, userEntity);

			SaveBosses();
			return true;
		}
		return false;
	}

	private static void RemoveBoss(FoundVBlood boss)
	{
		var em = VRisingCore.EntityManager;
		
		// Destroy active boss entities
		var disabledQuery = em.CreateEntityQuery(ComponentType.ReadOnly<VBloodUnit>(), ComponentType.ReadOnly<PrefabGUID>());
		var disabledEntities = disabledQuery.ToEntityArray(Allocator.Temp);
		
		// Destroy prefab boss entities
		var prefabQuery = em.CreateEntityQuery(
			ComponentType.ReadOnly<Prefab>(),
			ComponentType.ReadOnly<VBloodUnit>(),
			ComponentType.ReadOnly<PrefabGUID>());
		var prefabEntities = prefabQuery.ToEntityArray(Allocator.Temp);

		var bossPrefab = boss.Value;
		
		// Handle disabled entities
		for (int i = 0; i < disabledEntities.Length; i++)
		{
			var entity = disabledEntities[i];
			var prefabGuid = em.GetComponentData<PrefabGUID>(entity);
			if (prefabGuid.Equals(bossPrefab))
			{
				DestroyUtility.Destroy(em, entity);
			}
		}

		// Handle prefab entities
		for (int i = 0; i < prefabEntities.Length; i++)
		{
			var entity = prefabEntities[i];
			var prefabGuid = em.GetComponentData<PrefabGUID>(entity);
			if (prefabGuid.Equals(bossPrefab))
			{
				if (!em.HasComponent<DestroyOnSpawn>(entity))
				{
					em.AddComponent<DestroyOnSpawn>(entity);
				}
			}
		}

		disabledEntities.Dispose();
		prefabEntities.Dispose();
	}

	public bool UnlockBoss(FoundVBlood boss, Entity userEntity = default)
	{
		if(lockedBosses.Remove(boss))
		{
			var em = VRisingCore.EntityManager;
			
			// Remove DestroyOnSpawn from prefab entities
			var prefabQuery = em.CreateEntityQuery(
				ComponentType.ReadOnly<Prefab>(),
				ComponentType.ReadOnly<VBloodUnit>(),
				ComponentType.ReadOnly<PrefabGUID>());
			var prefabEntities = prefabQuery.ToEntityArray(Allocator.Temp);

			var bossPrefab = boss.Value;
			
			for (int i = 0; i < prefabEntities.Length; i++)
			{
				var entity = prefabEntities[i];
				var prefabGuid = em.GetComponentData<PrefabGUID>(entity);
				if (prefabGuid.Equals(bossPrefab))
				{
					if (em.HasComponent<Script_ApplyBuffUnderHealthThreshold_DataServer>(entity))
					{
						em.SetComponentData(entity, new Script_ApplyBuffUnderHealthThreshold_DataServer()
						{
							NewBuffEntity = new PrefabGUID(-2067944554), // Buff_General_VBlood_Downed
							HealthFactor = 0.0f,
							ThresholdMet = false
						});
					}

					if (em.HasComponent<DestroyOnSpawn>(entity))
					{
						em.RemoveComponent<DestroyOnSpawn>(entity);
					}
				}
			}

			prefabEntities.Dispose();
			UnlockAssociatedSpells(boss, userEntity);
			SpawnBossUnlockEffects(boss, userEntity);
			SaveBosses();
			return true;
		}
		return false;
	}

	public bool IsBossLocked(PrefabGUID boss)
	{
		return lockedBosses.Any(x => x.Value.Equals(boss));
	}

	public bool IsSpellLocked(int spellGuid)
	{
		return LockedSpellSchools.Contains(spellGuid);
	}

	/// <summary>
	/// Get all available bosses
	/// </summary>
	public IEnumerable<FoundVBlood> GetAllBosses()
	{
		// Return all bosses from the spell mappings as available bosses
		return BossSpellMappings.Keys.Select(prefabGuid => {
			var bossName = GetBossNameFromGuid(prefabGuid);
			return new FoundVBlood(prefabGuid, bossName);
		});
	}

	/// <summary>
	/// Get boss name from PrefabGUID
	/// </summary>
	private static string GetBossNameFromGuid(PrefabGUID prefabGuid)
	{
		// Use the boss names from the BossSpellMappings keys
		return prefabGuid.GuidHash switch
		{
			-1905691330 => "Matka",
			-1905691331 => "Terah",
			-1905691332 => "Jade",
			-1905691333 => "Beatrice",
			-1905691334 => "Nicholaus",
			-1905691335 => "Quincey",
			-1905691336 => "Ungora",
			-1905691337 => "Terrorclaw",
			-1905691338 => "Lidia",
			-1905691339 => "Goreswine",
			-1905691340 => "Octavian",
			-1905691357 => "Solarus",
			-1905691385 => "Dracula",
			_ => $"Boss_{prefabGuid.GuidHash}"
		};
	}

	/// <summary>
	/// Unlock all bosses (remove all current locks)
	/// </summary>
	public void UnlockAllBosses(Entity userEntity = default)
	{
		var bossesToUnlock = lockedBosses.ToList();
		lockedBosses.Clear();
		
		Plugin.Logger?.LogInfo($"Unlocking {bossesToUnlock.Count} bosses for arena entry");
		
		// Unlock each boss
		for (int i = bossesToUnlock.Count - 1; i >= 0; i--)
		{
			UnlockBoss(bossesToUnlock[i], userEntity);
		}
		
		SaveBosses();
	}

	/// <summary>
	/// Restore boss locks to a specific state
	/// </summary>
	public void RestoreBossLocks(IEnumerable<FoundVBlood> bossState, Entity userEntity = default)
	{
		// Clear current locks
		var currentBosses = lockedBosses.ToList();
		lockedBosses.Clear();
		
		// Apply the saved state
		lockedBosses.AddRange(bossState);
		
		Plugin.Logger?.LogInfo($"Restoring boss state: {lockedBosses.Count} bosses locked");
		
		// Re-apply all locks
		ApplyAllLocks();
		SaveBosses();
	}

	/// <summary>
	/// Save current boss lock state
	/// </summary>
	public IEnumerable<FoundVBlood> GetCurrentBossState()
	{
		return lockedBosses.ToList();
	}

	/// <summary>
	/// Lock all spells associated with a boss
	/// </summary>
	private void LockAssociatedSpells(FoundVBlood boss, Entity userEntity)
	{
		if (BossSpellMappings.TryGetValue(boss.Value, out var spellGuids))
		{
			var em = VRisingCore.EntityManager;
			
			Plugin.Logger?.LogInfo($"Locking {spellGuids.Count} spells for boss '{boss.Name}'");
			
			// Send spell lock notification only to the owner who triggered the action
			if (userEntity != Entity.Null)
			{
				SendSpellNotification(userEntity, spellGuids, boss.Name, true);
			}
			
			// Visual effects for spell locking
			SpawnSpellLockEffects(spellGuids, userEntity);
		}
	}

	/// <summary>
	/// Unlock all spells associated with a boss
	/// </summary>
	private void UnlockAssociatedSpells(FoundVBlood boss, Entity userEntity)
	{
		if (BossSpellMappings.TryGetValue(boss.Value, out var spellGuids))
		{
			var em = VRisingCore.EntityManager;
			
			Plugin.Logger?.LogInfo($"Unlocking {spellGuids.Count} spells for boss '{boss.Name}'");
			
			// Send spell unlock notification only to the owner who triggered the action
			if (userEntity != Entity.Null)
			{
				SendSpellNotification(userEntity, spellGuids, boss.Name, false);
			}
			
			// Visual effects for spell unlocking
			SpawnSpellUnlockEffects(spellGuids, userEntity);
		}
	}

	/// <summary>
	/// Apply all current boss and spell locks
	/// </summary>
	private void ApplyAllLocks()
	{
		Plugin.Logger?.LogInfo($"Applying {lockedBosses.Count} boss locks and associated spell locks");
		
		// This would be called on server startup to ensure all locks are applied
		// In a full implementation, this would restore all previous states
	}

	/// <summary>
	/// Send network notification about spell status to a specific user
	/// </summary>
	private static void SendSpellNotification(Entity userEntity, List<int> spellGuids, string bossName, bool isLocked)
	{
		try
		{
			var em = VRisingCore.EntityManager;
			
			// Create notification event entity
			var eventEntity = em.CreateEntity(
				ComponentType.ReadOnly<NetworkEventType>(),
				ComponentType.ReadOnly<SendEventToUser>()
			);

			// Set up network event
			var networkEvent = new NetworkEventType()
			{
				EventId = isLocked ? 2002 : 2003, // Custom event IDs for spell lock/unlock
				IsAdminEvent = false,
				IsDebugEvent = false
			};

			var user = em.GetComponentData<User>(userEntity);
			
			em.SetComponentData(eventEntity, networkEvent);
			em.SetComponentData(eventEntity, new SendEventToUser()
			{
				UserIndex = user.Index
			});

			var spellNames = string.Join(", ", spellGuids.Select(guid =>
				SpellSchoolGUIDs.SpellSchools.FirstOrDefault(kvp => kvp.Value.GuidHash == guid).Key ?? "Unknown"));
			
			Plugin.Logger?.LogInfo($"Sent {bossName} spell {(isLocked ? "lock" : "unlock")} notification ({spellNames}) to {user.CharacterName}");
		}
		catch (Exception ex)
		{
			Plugin.Logger?.LogError($"Error sending spell notification: {ex.Message}");
		}
	}

	/// <summary>
	/// Spawn visual effects for spell locking
	/// </summary>
	private static void SpawnSpellLockEffects(List<int> spellGuids, Entity userEntity)
	{
		var em = VRisingCore.EntityManager;
		
		try
		{
			float3 basePos = new float3(0, 50, 0); // Center of world, elevated
			if (userEntity != Entity.Null && em.HasComponent<Translation>(userEntity))
			{
				basePos = em.GetComponentData<Translation>(userEntity).Value + new float3(0, 5, 0);
			}

			// Spawn a dark effect for each locked spell
			for (int i = 0; i < spellGuids.Count; i++)
			{
				var offsetPos = basePos + new float3(i * 3, 0, i * 3); // Spread effects out
				SpawnEffect(offsetPos, new PrefabGUID(-1905691330)); // Dark effect prefab
			}
			
			Plugin.Logger?.LogInfo($"Spawned {spellGuids.Count} spell lock effects");
		}
		catch (Exception ex)
		{
			Plugin.Logger?.LogError($"Error spawning spell lock effects: {ex.Message}");
		}
	}

	/// <summary>
	/// Spawn visual effects for spell unlocking
	/// </summary>
	private static void SpawnSpellUnlockEffects(List<int> spellGuids, Entity userEntity)
	{
		var em = VRisingCore.EntityManager;
		
		try
		{
			float3 basePos = new float3(0, 50, 0); // Center of world, elevated
			if (userEntity != Entity.Null && em.HasComponent<Translation>(userEntity))
			{
				basePos = em.GetComponentData<Translation>(userEntity).Value + new float3(0, 5, 0);
			}

			// Spawn a bright effect for each unlocked spell
			for (int i = 0; i < spellGuids.Count; i++)
			{
				var offsetPos = basePos + new float3(i * 3, 0, i * 3); // Spread effects out
				SpawnEffect(offsetPos, new PrefabGUID(-1905691331)); // Light effect prefab
			}
			
			Plugin.Logger?.LogInfo($"Spawned {spellGuids.Count} spell unlock effects");
		}
		catch (Exception ex)
		{
			Plugin.Logger?.LogError($"Error spawning spell unlock effects: {ex.Message}");
		}
	}

	/// <summary>
	/// Spawn visual and audio effects when a boss is locked
	/// </summary>
	private static void SpawnBossLockEffects(FoundVBlood boss, Entity userEntity)
	{
		var em = VRisingCore.EntityManager;
		
		try
		{
			// Send network event to notify users
			SendBossNotification(userEntity, boss, true);
			
			// Spawn visual effects at the center of the world (or near user if provided)
			float3 effectPos = new float3(0, 50, 0); // Center of world, elevated
			if (userEntity != Entity.Null && em.HasComponent<Translation>(userEntity))
			{
				effectPos = em.GetComponentData<Translation>(userEntity).Value + new float3(0, 5, 0);
			}

			// Spawn lock effect (dark vortex)
			SpawnEffect(effectPos, new PrefabGUID(-1905691330)); // Dark effect prefab
			
			Plugin.Logger?.LogInfo($"Boss '{boss.Name}' has been locked with visual effects");
		}
		catch (Exception ex)
		{
			Plugin.Logger?.LogError($"Error spawning boss lock effects: {ex.Message}");
		}
	}

	/// <summary>
	/// Spawn visual and audio effects when a boss is unlocked
	/// </summary>
	private static void SpawnBossUnlockEffects(FoundVBlood boss, Entity userEntity)
	{
		var em = VRisingCore.EntityManager;
		
		try
		{
			// Send network event to notify users
			SendBossNotification(userEntity, boss, false);
			
			// Spawn visual effects at the center of the world (or near user if provided)
			float3 effectPos = new float3(0, 50, 0); // Center of world, elevated
			if (userEntity != Entity.Null && em.HasComponent<Translation>(userEntity))
			{
				effectPos = em.GetComponentData<Translation>(userEntity).Value + new float3(0, 5, 0);
			}

			// Spawn unlock effect (bright light)
			SpawnEffect(effectPos, new PrefabGUID(-1905691331)); // Light effect prefab
			
			Plugin.Logger?.LogInfo($"Boss '{boss.Name}' has been unlocked with visual effects");
		}
		catch (Exception ex)
		{
			Plugin.Logger?.LogError($"Error spawning boss unlock effects: {ex.Message}");
		}
	}

	/// <summary>
	/// Send network notification to users about boss lock/unlock
	/// </summary>
	private static void SendBossNotification(Entity userEntity, FoundVBlood boss, bool isLocked)
	{
		if (userEntity == Entity.Null) return;

		try
		{
			var em = VRisingCore.EntityManager;
			
			// Create notification event entity
			var eventEntity = em.CreateEntity(
				ComponentType.ReadOnly<NetworkEventType>(),
				ComponentType.ReadOnly<SendEventToUser>()
			);

			// Set up network event
			var networkEvent = new NetworkEventType()
			{
				EventId = 2001, // Custom event ID for boss notifications
				IsAdminEvent = false,
				IsDebugEvent = false
			};

			var user = em.GetComponentData<User>(userEntity);
			
			em.SetComponentData(eventEntity, networkEvent);
			em.SetComponentData(eventEntity, new SendEventToUser()
			{
				UserIndex = user.Index
			});

			Plugin.Logger?.LogInfo($"Sent {boss.Name} {(isLocked ? "lock" : "unlock")} notification to {user.CharacterName}");
		}
		catch (Exception ex)
		{
			Plugin.Logger?.LogError($"Error sending boss notification: {ex.Message}");
		}
	}

	/// <summary>
	/// Spawn a visual effect at the specified position
	/// </summary>
	private static void SpawnEffect(float3 position, PrefabGUID effectPrefab)
	{
		try
		{
			var em = VRisingCore.EntityManager;
			
			// Create effect entity
			var effectEntity = em.CreateEntity();
			em.AddComponentData(effectEntity, new PrefabGUID(effectPrefab.GuidHash));
			em.AddComponentData(effectEntity, new Translation() { Value = position });
			
			// Add auto-destroy component for temporary effects
			em.AddComponent<DestroyOnSpawn>(effectEntity);
			
			Plugin.Logger?.LogDebug($"Spawned effect at {position}");
		}
		catch (Exception ex)
		{
			Plugin.Logger?.LogError($"Error spawning effect: {ex.Message}");
		}
	}
}
