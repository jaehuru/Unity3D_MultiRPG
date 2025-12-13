using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Unity.Collections;
using System;
using System.Collections.Generic;
using Jae.Commom;
using Jae.Common;
using Jae.Manager;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerController))]
public class PlayerCharacter : NetworkBehaviour,
    IActor,
    IInteractor,
    ICombatant,
    ISpawnable,
    IHUDUpdatable,
    IInventoryHandler,
    IMovable,
    IAnimPlayable,
    ISaveable,
    IWorldSpaceUIProvider,
    IStatProvider,
    IAttackHandler,
    IMoveAuthoritative
{
    private NavMeshAgent agent;
    private readonly NetworkVariable<FixedString32Bytes> networkPlayerName = new(writePerm: NetworkVariableWritePermission.Server);

    // --- Stats ---
    private readonly NetworkVariable<float> _currentHealth = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> _maxHealth = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    [Header("World Space UI")]
    [SerializeField] private GameObject playerWorldSpaceUIPrefab;

    // --- Interfaces ---
    private IHealth _playerHealth;
    
    #region Interface Implementations
    // IActor Implementation
    public string GetId() => OwnerClientId.ToString();
    public Transform GetTransform() => transform;


    // IInteractor
    public void TryInteract(IInteractable target) { throw new NotImplementedException(); }
    public IInteractable GetCurrentTarget() { throw new NotImplementedException(); }

    // ICombatant
    public IHealth GetHealth() => _playerHealth;
    public IStatProvider GetStats() => this;
    public IStatusController GetStatusController() { throw new NotImplementedException(); }
    public IAttackHandler GetAttackHandler() => this;
    public ISkillCaster GetSkillCaster() { throw new NotImplementedException(); }
    public IHitDetector GetHitDetector() { throw new NotImplementedException(); }
    public ICombatCooldown GetCooldown() { throw new NotImplementedException(); }
    public ICombatAnimationBinder GetAnimationBinder() { throw new NotImplementedException(); }
    public void OnSpawned(ISpawnContext ctx) { /* Logic now in OnNetworkSpawn */ }

    // IStatProvider
    public float GetStat(StatType stat)
    {
        return stat switch
        {
            StatType.Health => _currentHealth.Value,
            StatType.MaxHealth => _maxHealth.Value,
            _ => 0f,
        };
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
    public void RemoveModifier(System.Guid id) { throw new NotImplementedException(); }

    // ISpawnable
    public void OnSpawn(ISpawnContext ctx)
    {
        agent.enabled = IsServer;
        
        if (IsServer)
        {
            if (PlayerSessionManager.Instance.TryGetClientInfo(OwnerClientId, out var clientInfo))
            {
                networkPlayerName.Value = clientInfo.Uid;
            }
        }
        
        if (IsOwner)
        {
            if (HUDUIController.Instance != null)
            {
                HUDUIController.Instance.RegisterLocalPlayerHealth(this);
            }
        }
        
        if (ctx != null && ctx.Point != null && agent != null && agent.enabled && IsServer)
        {
             agent.Warp(ctx.Point.GetPosition());
        }
    }
    public IRespawnPolicy GetRespawnPolicy() => new PlayerRespawnPolicy(this);

    // IAttackHandler
    public bool CanNormalAttack() => true; // TODO: This logic should be based on state
    public void NormalAttack(AttackContext ctx)
    {
        if (!IsServer || !CanNormalAttack()) return;
        
        if (!ctx.TargetNetworkObjectRef.TryGet(out var targetNetworkObject)) return;
        
        var target = targetNetworkObject.gameObject;


        if (target.TryGetComponent<ICombatant>(out var combatant))
        {
            CombatManager.Instance.ProcessAttack(this, combatant);
        }
    }
    public AttackType GetAttackType() => AttackType.Melee;
    public DamageType GetDefaultDamageType() => DamageType.Physical;

    // IHUDUpdatable
    public event Action<float, float> OnHealthChanged;
    public event Action<int> OnResourceChanged;

    // IInventoryHandler
    public bool CanAddItem(IInventoryItem item) { throw new NotImplementedException(); }
    public void AddItem(IInventoryItem item) { throw new NotImplementedException(); }
    public bool RemoveItem(string itemId, int count) { throw new NotImplementedException(); }
    public IInventoryItem GetItem(string itemId) { throw new NotImplementedException(); }
    public IEnumerable<IInventoryItem> GetAllItems() { throw new NotImplementedException(); }
    public event Action<IInventoryItem> OnItemAdded;
    public event Action<IInventoryItem, int> OnItemRemoved;

    // IMovable
    public void Move(Vector3 direction, float deltaTime) { if(agent.enabled) agent.Move(transform.TransformDirection(direction) * agent.speed * deltaTime); }
    public void Teleport(Vector3 pos)
    {
        if (agent != null && agent.enabled)
        {
            agent.Warp(pos);
        }
        else
        {
            transform.position = pos;
        }
    }
    
    // IMoveAuthoritative
    public void ServerApplyMovement(MovementSnapshot snap)
    {
        if (!IsServer) return;
        
        transform.rotation = snap.LookRotation;
        Vector3 moveDir = new Vector3(snap.MoveInput.x, 0, snap.MoveInput.y);
        Move(moveDir, snap.DeltaTime);
    }

    // IAnimPlayable
    public void Play(string state) { /* TODO: Animator logic */ }
    public void CrossFade(string state, float duration) { /* TODO: Animator logic */ }

    // ISaveable
    public SaveData SerializeForSave() { throw new NotImplementedException(); }
    public void DeserializeFromSave(SaveData data) { throw new NotImplementedException(); }
    
    // IWorldSpaceUIProvider
    public GameObject WorldSpaceUIPrefab => playerWorldSpaceUIPrefab;
    public Transform UIFollowTransform => transform;
    public string GetDisplayName() => networkPlayerName.Value.ToString();
    public float GetHealthRatio() => _maxHealth.Value > 0 ? _currentHealth.Value / _maxHealth.Value : 0;
    public NetworkVariable<FixedString32Bytes> CharacterName => networkPlayerName;
    #endregion

    #region Nested Classes (Health, Respawn Policy)
    private class PlayerHealth : IHealth
    {
        private readonly PlayerCharacter _owner;
        public float Current => _owner.GetStat(StatType.Health);
        public float Max => _owner.GetStat(StatType.MaxHealth);

        public event Action<DamageEvent> OnDamaged;
        public event Action OnDied;

        public PlayerHealth(PlayerCharacter owner)
        {
            _owner = owner;
            _owner._currentHealth.OnValueChanged += (prev, curr) =>
            {
                _owner.OnHealthChanged?.Invoke(curr, _owner._maxHealth.Value);
                if (curr < prev)
                {
                    OnDamaged?.Invoke(new DamageEvent { Amount = prev - curr });
                }
                if (curr <= 0 && prev > 0)
                {
                    OnDied?.Invoke();
                }
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

    private class PlayerRespawnPolicy : IRespawnPolicy
    {
        public PlayerRespawnPolicy(PlayerCharacter owner) { }
        public bool ShouldRespawn(ISpawnable target) => true;
        public TimeSpan GetRespawnDelay(ISpawnable target) => TimeSpan.FromSeconds(5);
        public ISpawnPoint SelectRespawnPoint(ISpawnable target)
        {
            return null;
        }
    }
    
#endregion

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        _playerHealth = new PlayerHealth(this);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        if (IsClient && WorldSpaceUIManager.Instance != null)
        {
            WorldSpaceUIManager.Instance.UnregisterUIProvider(NetworkObjectId);
        }
    }
}