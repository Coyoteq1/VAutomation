using Il2CppInterop.Runtime;
using ProjectM;
using ProjectM.CastleBuilding;
using ProjectM.Gameplay.Systems;
using ProjectM.Network;
using ProjectM.Scripting;
using ProjectM.Shared.Systems;
using ProjectM.Tiles;
using Unity.Entities;

namespace CrowbaneArena.Services;

public class SystemService
{
    readonly World _world;

    private AbilitySpawnSystem _abilitySpawnSystem;

    private ActivateVBloodAbilitySystem _activateVBloodAbilitySystem;

    private AttachParentIdSystem _attachParentIdSystem;

    // private BehaviourTreeBindingSystem_Spawn _behaviourTreeBindingSystem; // Not available

    private BuffSystem_Spawn_Server _buffSystem_Spawn_Server;

    private ClaimAchievementSystem _claimAchievementSystem;

    // private CombatMusicSystem_Server _combatMusicSystem_Server; // Not available

    private CurrentCastsSystem.Singleton _currentCastsSystem_Singleton;

    private Entity _dayNightCycleSystem;

    DebugEventsSystem _debugEventsSystem;

    private EndSimulationEntityCommandBufferSystem _endSimulationEntityCommandBufferSystem;

    private EntityCommandBufferSystem _entityCommandBufferSystem;

    private EquipItemSystem _equipItemSystem;

    private GameDataSystem _gameDataSystem;

    // private GenerateCastleSystem _generateCastleSystem; // Not available in VRising.GameData 0.3.3

    private InstantiateMapIconsSystem_Spawn _instantiateMapIconsSystem_Spawn;

    private JewelSpawnSystem _jewelSpawnSystem;

    private MapZoneCollectionSystem _mapZoneCollectionSystem;

    private ModificationSystem _modificationSystem;

    private NameableInteractableSystem _nameableInteractableSystem;

    private NetworkIdSystem.Singleton _networkIdSystem_Singleton;

    PrefabCollectionSystem _prefabCollectionSystem;

    private RemoveCharmSourceFromVBloods_Hotfix_0_6 _removeCharmSourceVBloodSystem;

    private ReplaceAbilityOnSlotSystem _replaceAbilityOnSlotSystem;

    private ScriptSpawnServer _scriptSpawnServer;

    private ServantPowerSystem _servantPowerSystem;

    private ServerBootstrapSystem _serverBootstrapSystem;

    ServerGameSettingsSystem _serverGameSettingsSystem;

    ServerScriptMapper _serverScriptMapper;

    // private SetTeamOnSpawnSystem _setTeamOnSpawnSystem; // Not available in VRising.GameData 0.3.3

    private SpawnAbilityGroupSlotsSystem _spawnAbilityGroupSlotSystem;

    // private SpawnTeamSystem _spawnTeamSystem; // Not available in VRising.GameData 0.3.3

    private SpellModSyncSystem_Server _spellModSyncSystem_Server;

    SpellSchoolMappingSystem _spellSchoolMappingSystem;

    private StatChangeSystem _statChangeSystem;

    private Entity _tileModelSpatialLookupSystem;

    private TraderPurchaseSystem _traderPurchaseSystem;

    private UnEquipItemSystem _unEquipItemSystem;

    private UpdateBuffsBuffer_Destroy _updateBuffsBuffer_Destroy;

    private Update_ReplaceAbilityOnSlotSystem _updateReplaceAbilityOnSlotSystem;

    private UserActivityGridSystem _userActivityGridSystem;

    public SystemService(World world)
    {
        _world = world ?? throw new ArgumentNullException(nameof(world));
    }

    public DebugEventsSystem DebugEventsSystem => _debugEventsSystem ??= GetSystem<DebugEventsSystem>();

    public PrefabCollectionSystem PrefabCollectionSystem =>
        _prefabCollectionSystem ??= GetSystem<PrefabCollectionSystem>();

    public ServerGameSettingsSystem ServerGameSettingsSystem =>
        _serverGameSettingsSystem ??= GetSystem<ServerGameSettingsSystem>();

    public ServerScriptMapper ServerScriptMapper => _serverScriptMapper ??= GetSystem<ServerScriptMapper>();

    public SpellSchoolMappingSystem SpellSchoolMappingSystem =>
        _spellSchoolMappingSystem ??= GetSystem<SpellSchoolMappingSystem>();

    public EntityCommandBufferSystem EntityCommandBufferSystem =>
        _entityCommandBufferSystem ??= GetSystem<EntityCommandBufferSystem>();

    public ClaimAchievementSystem ClaimAchievementSystem =>
        _claimAchievementSystem ??= GetSystem<ClaimAchievementSystem>();

    public GameDataSystem GameDataSystem => _gameDataSystem ??= GetSystem<GameDataSystem>();
    public ScriptSpawnServer ScriptSpawnServer => _scriptSpawnServer ??= GetSystem<ScriptSpawnServer>();

    // CombatMusicSystem_Server not available
    // public CombatMusicSystem_Server CombatMusicSystem_Server =>
    //     _combatMusicSystem_Server ??= GetSystem<CombatMusicSystem_Server>();

    public NameableInteractableSystem NameableInteractableSystem =>
        _nameableInteractableSystem ??= GetSystem<NameableInteractableSystem>();

    public ActivateVBloodAbilitySystem ActivateVBloodAbilitySystem =>
        _activateVBloodAbilitySystem ??= GetSystem<ActivateVBloodAbilitySystem>();

    public EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem =>
        _endSimulationEntityCommandBufferSystem ??= GetSystem<EndSimulationEntityCommandBufferSystem>();

    public ReplaceAbilityOnSlotSystem ReplaceAbilityOnSlotSystem =>
        _replaceAbilityOnSlotSystem ??= GetSystem<ReplaceAbilityOnSlotSystem>();

    public UnEquipItemSystem UnEquipItemSystem => _unEquipItemSystem ??= GetSystem<UnEquipItemSystem>();
    public EquipItemSystem EquipItemSystem => _equipItemSystem ??= GetSystem<EquipItemSystem>();

    public Update_ReplaceAbilityOnSlotSystem Update_ReplaceAbilityOnSlotSystem =>
        _updateReplaceAbilityOnSlotSystem ??= GetSystem<Update_ReplaceAbilityOnSlotSystem>();

    public StatChangeSystem StatChangeSystem => _statChangeSystem ??= GetSystem<StatChangeSystem>();

    // GenerateCastleSystem not available in VRising.GameData 0.3.3
    // public GenerateCastleSystem GenerateCastleSystem =>
    //     _generateCastleSystem ??= GetOrCreateSystem<GenerateCastleSystem>();

    public ServerBootstrapSystem ServerBootstrapSystem => _serverBootstrapSystem ??= GetSystem<ServerBootstrapSystem>();

    // BehaviourTreeBindingSystem_Spawn not available
    // public BehaviourTreeBindingSystem_Spawn BehaviourTreeBindingSystem_Spawn =>
    //     _behaviourTreeBindingSystem ??= GetSystem<BehaviourTreeBindingSystem_Spawn>();

    public SpawnAbilityGroupSlotsSystem SpawnAbilityGroupSlotSystem =>
        _spawnAbilityGroupSlotSystem ??= GetSystem<SpawnAbilityGroupSlotsSystem>();

    public AttachParentIdSystem AttachParentIdSystem => _attachParentIdSystem ??= GetSystem<AttachParentIdSystem>();
    public AbilitySpawnSystem AbilitySpawnSystem => _abilitySpawnSystem ??= GetSystem<AbilitySpawnSystem>();
    // SpawnTeamSystem not available in VRising.GameData 0.3.3
    // public SpawnTeamSystem SpawnTeamSystem => _spawnTeamSystem ??= GetSystem<SpawnTeamSystem>();
    public ModificationSystem ModificationSystem => _modificationSystem ??= GetSystem<ModificationSystem>();
    // SetTeamOnSpawnSystem not available in VRising.GameData 0.3.3
    // public SetTeamOnSpawnSystem SetTeamOnSpawnSystem => _setTeamOnSpawnSystem ??= GetSystem<SetTeamOnSpawnSystem>();

    public SpellModSyncSystem_Server SpellModSyncSystem_Server =>
        _spellModSyncSystem_Server ??= GetSystem<SpellModSyncSystem_Server>();

    public JewelSpawnSystem JewelSpawnSystem => _jewelSpawnSystem ??= GetSystem<JewelSpawnSystem>();
    public TraderPurchaseSystem TraderPurchaseSystem => _traderPurchaseSystem ??= GetSystem<TraderPurchaseSystem>();

    public UpdateBuffsBuffer_Destroy UpdateBuffsBuffer_Destroy =>
        _updateBuffsBuffer_Destroy ??= GetSystem<UpdateBuffsBuffer_Destroy>();

    public BuffSystem_Spawn_Server BuffSystem_Spawn_Server =>
        _buffSystem_Spawn_Server ??= GetSystem<BuffSystem_Spawn_Server>();

    public InstantiateMapIconsSystem_Spawn InstantiateMapIconsSystem_Spawn =>
        _instantiateMapIconsSystem_Spawn ??= GetSystem<InstantiateMapIconsSystem_Spawn>();

    public MapZoneCollectionSystem MapZoneCollectionSystem =>
        _mapZoneCollectionSystem ??= GetSystem<MapZoneCollectionSystem>();

    public UserActivityGridSystem UserActivityGridSystem =>
        _userActivityGridSystem ??= GetSystem<UserActivityGridSystem>();

    public ServantPowerSystem ServantPowerSystem => _servantPowerSystem ??= GetSystem<ServantPowerSystem>();

    public RemoveCharmSourceFromVBloods_Hotfix_0_6 RemoveCharmSourceVBloodSystem => _removeCharmSourceVBloodSystem ??=
        GetSystem<RemoveCharmSourceFromVBloods_Hotfix_0_6>();

    public NetworkIdSystem.Singleton NetworkIdSystem
    {
        get
        {
            if (_networkIdSystem_Singleton.Equals(default(NetworkIdSystem.Singleton)))
            {
                _networkIdSystem_Singleton = GetSingleton<NetworkIdSystem.Singleton>();
            }

            return _networkIdSystem_Singleton;
        }
    }

    public CurrentCastsSystem.Singleton CurrentCastsSystem
    {
        get
        {
            if (_currentCastsSystem_Singleton.Equals(default(CurrentCastsSystem.Singleton)))
            {
                _currentCastsSystem_Singleton = GetSingleton<CurrentCastsSystem.Singleton>();
            }

            return _currentCastsSystem_Singleton;
        }
    }

    public Entity TileModelSpatialLookupSystem
    {
        get
        {
            if (!_tileModelSpatialLookupSystem.Equals(Entity.Null))
            {
                _tileModelSpatialLookupSystem = GetSingletonEntity<TileModelSpatialLookupSystem.Singleton>();
            }

            return _tileModelSpatialLookupSystem;
        }
    }

    public Entity DayNightCycleSystem
    {
        get
        {
            if (!_dayNightCycleSystem.Equals(Entity.Null))
            {
                _dayNightCycleSystem = GetSingletonEntityFromAccessor<DayNightCycle>();
            }

            return _dayNightCycleSystem;
        }
    }

    T GetSystem<T>() where T : ComponentSystemBase
    {
        return _world.GetExistingSystemManaged<T>() ??
               throw new InvalidOperationException($"[{_world.Name}] - failed to get ({Il2CppType.Of<T>().FullName})");
    }

    T GetOrCreateSystem<T>() where T : ComponentSystemBase
    {
        return _world.GetOrCreateSystemManaged<T>() ??
               throw new InvalidOperationException($"[{_world.Name}] - failed to get ({Il2CppType.Of<T>().FullName})");
    }

    T GetSingleton<T>()
    {
        return ServerScriptMapper.GetSingleton<T>();
    }

    Entity GetSingletonEntity<T>()
    {
        return ServerScriptMapper.GetSingletonEntity<T>();
    }

    private Entity GetSingletonEntityFromAccessor<T>() where T : unmanaged
    {
        var query = _world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());
        var entity = query.GetSingletonEntity();
        query.Dispose();
        return entity;
    }
}