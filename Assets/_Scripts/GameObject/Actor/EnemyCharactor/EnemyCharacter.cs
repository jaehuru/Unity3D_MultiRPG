using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections;
using UnityEngine.AI;
using Jae.Common;
using Jae.DataTypes;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyCharacter : NetworkBehaviour,
    IActor,
    ICombatant,
    ISpawnable,
    IMovable,
    IAnimPlayable,
    ISaveable,
    IWorldSpaceUIProvider,
    IStatProvider,
    IAttackHandler
{
    public string GetId() => NetworkObjectId.ToString();
    public Transform GetTransform() => transform;

    private NavMeshAgent _agent;
    private IStatProvider _statProvider;
    private IAttackHandler _attackHandler;
    private EnemyHealth _enemyHealth;

    // --- Stats ---
    private readonly NetworkVariable<float> _currentHealth = new(50f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> _maxHealth = new(50f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString32Bytes> _networkEnemyName = new("Goblin", writePerm: NetworkVariableWritePermission.Server);

    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private int attackDamage = 5;

    [Header("UI Settings")]
    [SerializeField] private GameObject EnemyWorldSpaceUIPrefab;

    [Header("AI Settings")]
    [SerializeField] private float attackInterval = 2f;
    [SerializeField] private float chaseRange = 10f;
    [SerializeField] private float patrolStoppingDistance = 0.5f;
    [SerializeField] private float patrolRadius = 7f;
    [SerializeField] private float returnHomeDistance = 15f;
    [SerializeField] private float patrolWaitTime = 3f;

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 5f;

    // IWorldSpaceUIProvider Implementation
    public GameObject WorldSpaceUIPrefab => EnemyWorldSpaceUIPrefab;
    public Transform UIFollowTransform => transform;
    public string GetDisplayName() => _networkEnemyName.Value.ToString();
    public float GetHealthRatio() => _maxHealth.Value > 0 ? _currentHealth.Value / _maxHealth.Value : 0f;
    public NetworkVariable<FixedString32Bytes> CharacterName => _networkEnemyName;

#region Interface Implementations

    // ICombatant
    public IHealth GetHealth() => _enemyHealth;
    public IStatProvider GetStats() => this;
    public IStatusController GetStatusController() { throw new NotImplementedException(); }
    public IAttackHandler GetAttackHandler() => this;
    public ISkillCaster GetSkillCaster() { throw new NotImplementedException(); }
    public IHitDetector GetHitDetector() { throw new NotImplementedException(); }
    public ICombatCooldown GetCooldown() { throw new NotImplementedException(); }
    public ICombatAnimationBinder GetAnimationBinder() { throw new NotImplementedException(); }
    public void OnSpawned(ISpawnContext ctx) { /* TODO: Implement if needed */ }

    // IStatProvider
    public float GetStat(StatType stat)
    {
        switch (stat)
        {
            case StatType.Health: return _currentHealth.Value;
            case StatType.MaxHealth: return _maxHealth.Value;
            case StatType.AttackDamage: return attackDamage;
            default: return 0f;
        }
    }
    public void SetStat(StatType stat, float value)
    {
        if (!IsServer) return;
        switch (stat)
        {
            case StatType.Health: _currentHealth.Value = Mathf.Clamp(value, 0, _maxHealth.Value); break;
            case StatType.MaxHealth: _maxHealth.Value = value; break;
        }
    }
    public void AddModifier(StatModifier m) { throw new NotImplementedException(); }
    public void RemoveModifier(Guid id) { throw new NotImplementedException(); }

    // IAttackHandler
    public bool CanNormalAttack() => true;
    public void NormalAttack(AttackContext ctx)
    {
        if (!IsServer) return;

        if (!ctx.TargetNetworkObjectRef.TryGet(out var targetNetworkObject)) return;
        
        var target = targetNetworkObject.gameObject;
        
        if (target.TryGetComponent<ICombatant>(out var combatant))
        {
            CombatManager.Instance.ProcessAttack(this, combatant);
        }
    }
    public AttackType GetAttackType() => AttackType.Melee;
    public DamageType GetDefaultDamageType() => DamageType.Physical;

    // ISpawnable
    public void OnSpawn(ISpawnContext ctx)
    {
        _agent.enabled = IsServer;

        _currentHealth.OnValueChanged += OnHealthUpdated;
        OnHealthUpdated(0, _currentHealth.Value);
        
        if (ctx != null && ctx.Point != null)
        {
            transform.position = ctx.Point.GetPosition();
            transform.rotation = ctx.Point.GetRotation();
        }

        _agent.stoppingDistance = patrolStoppingDistance;
        
        if (IsServer)
        {
            AIManager.Instance.RegisterAI(
                this, 
                _agent, 
                transform, 
                transform.position,
                transform.rotation,
                attackInterval,
                chaseRange,
                patrolStoppingDistance,
                patrolRadius,
                returnHomeDistance,
                patrolWaitTime
            );
        }
    }
    
    public IRespawnPolicy GetRespawnPolicy() => new EnemyRespawnPolicy(this);


    // IMovable
    public void Move(Vector3 direction, float deltaTime) { if(_agent.enabled) _agent.Move(direction * deltaTime); }
    public void Teleport(Vector3 pos)
    {
        if (_agent.enabled) _agent.Warp(pos);
        else transform.position = pos;
    }

    // IAnimPlayable
    public void Play(string state) { /* TODO: Connect to Animator */ }
    public void CrossFade(string state, float duration) { /* TODO: Connect to Animator */ }

    // ISaveable
    public SaveData SerializeForSave() { throw new NotImplementedException(); }
    public void DeserializeFromSave(SaveData data) { throw new NotImplementedException(); }
    #endregion

    #region Nested Classes (Health, Respawn Policy)
    private class EnemyHealth : IHealth
    {
        private readonly EnemyCharacter _owner;
        public float Current => _owner.GetStat(StatType.Health);
        public float Max => _owner.GetStat(StatType.MaxHealth);

        public event Action<DamageEvent> OnDamaged;
        public event Action OnDied;

        public EnemyHealth(EnemyCharacter owner)
        {
            _owner = owner;
            _owner._currentHealth.OnValueChanged += (prev, curr) =>
            {
                if (curr < prev)
                    OnDamaged?.Invoke(new DamageEvent { Amount = prev - curr });
                if (curr <= 0 && prev > 0)
                    OnDied?.Invoke();
            };
        }

        public void ApplyDamage(DamageEvent evt)
        {
            if (!_owner.IsServer) return;
            var newHealth = Current - evt.Amount;
            _owner.SetStat(StatType.Health, newHealth);
        }

        public void Heal(float amount)
        {
            if (!_owner.IsServer) return;
            var newHealth = Current + amount;
            _owner.SetStat(StatType.Health, newHealth);
        }
    }

    private class EnemyRespawnPolicy : IRespawnPolicy
    {
        private readonly EnemyCharacter _owner;
        public EnemyRespawnPolicy(EnemyCharacter owner) { _owner = owner; }
        public bool ShouldRespawn(ISpawnable target) => true;
        public TimeSpan GetRespawnDelay(ISpawnable target) => TimeSpan.FromSeconds(_owner.respawnDelay);
        public ISpawnPoint SelectRespawnPoint(ISpawnable target)
        {
            return null;
        }
    }

#endregion

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _enemyHealth = new EnemyHealth(this);
        
        _statProvider = this;
        _attackHandler = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _currentHealth.OnValueChanged -= OnHealthUpdated;

        if (IsServer)
        {
            AIManager.Instance.UnregisterAI(this);
        }
    }

    private void OnHealthUpdated(float previousValue, float newValue)
    {
        bool isDead = newValue <= 0;
        
        if (TryGetComponent<Collider>(out var col)) col.enabled = !isDead;
        
        if(IsServer && isDead)
        {
            AIManager.Instance.SetEnemyDead(this);
        }
    }
}