using System;
using System.Collections.Generic;
// Unity
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Unity.Collections;
using Unity.Netcode.Components;
// Project
using Jae.Common;
using Jae.Manager;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
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
    IStateActivable
{
    private NetworkTransform _networkTransform;
    private readonly NetworkVariable<FixedString32Bytes> networkPlayerName = new(writePerm: NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<bool> IsActive = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // --- Animation ---
    private Animator _animator;
    private int _animIDSpeedX;
    private int _animIDSpeedY;
    private int _animIDGrounded;
    private int _animIDJumpTrigger;
    private int _animIDFreeFall;
    
    // --- Networked Animation State ---
    public readonly NetworkVariable<float> NetworkAnimationX = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<float> NetworkAnimationY = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<bool> NetworkAnimationGrounded = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<bool> NetworkAnimationFreeFall = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    // --- Stats ---
    private readonly NetworkVariable<float> _currentHealth = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> _maxHealth = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 2.0f;
    [SerializeField] private float runSpeed = 5.0f;
    [SerializeField] private float sprintSpeed = 7.5f;

    [Header("Combat Settings")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 2f;

    [Header("World Space UI")]
    [SerializeField] private GameObject playerWorldSpaceUIPrefab;

    // --- Interfaces ---
    private IHealth _playerHealth;
    



    private void OnActiveStateChanged(bool previousValue, bool newValue)
    {
        if (TryGetComponent<PlayerController>(out var controller)) controller.enabled = newValue;
        if (TryGetComponent<Collider>(out var col)) col.enabled = newValue;
        var mainRenderer = GetComponentInChildren<Renderer>();
        if (mainRenderer != null) mainRenderer.enabled = newValue;
    }

    public void Activate()
    {
        if(IsServer) IsActive.Value = true;
    }

    public void Deactivate()
    {
        if(IsServer) IsActive.Value = false;
    }

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
            StatType.WalkSpeed => walkSpeed,
            StatType.RunSpeed => runSpeed,
            StatType.SprintSpeed => sprintSpeed,
            StatType.AttackDamage => attackDamage,
            StatType.AttackRange => attackRange,
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
        if (IsServer)
        {
            if (PlayerSessionManager.Instance != null && PlayerSessionManager.Instance.TryGetClientInfo(OwnerClientId, out var clientInfo))
            {
                networkPlayerName.Value = clientInfo.Uid;
            }
        }
        
        if (ctx != null && ctx.Point != null && IsServer)
        {
             transform.position = ctx.Point.GetPosition();
             transform.rotation = ctx.Point.GetRotation();
        }
    }
    public IRespawnPolicy GetRespawnPolicy() => new PlayerRespawnPolicy(this);

    // IAttackHandler
    public bool CanNormalAttack() => true; // TODO: This logic should be based on state
    public void NormalAttack()
    {
        //  TODO: 공격 애니메이션 재생과 같은 표현 로직만 남겨야 함
        //  모든 실제 데미지 계산 및 적용 로직은 CombatManager로 이전됨
    }
    public AttackType GetAttackType() => AttackType.Melee;
    public DamageType GetDefaultDamageType() => DamageType.Physical;

    // IHUDUpdatable
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
    public void Move(Vector3 direction, float deltaTime)
    {
        // MovementManager.ServerMove will now directly manipulate transform.position
    }
    public void Teleport(Vector3 pos)
    {
        _networkTransform.Teleport(pos, transform.rotation, transform.localScale);
    }

    // IAnimPlayable
    public void Play(string state)
    {
        if (_animator != null) _animator.Play(state);
    }
    public void CrossFade(string state, float floatDuration)
    {
        if (_animator != null) _animator.CrossFade(state, floatDuration);
    }

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

#region Server RPCs
    [ServerRpc]
    public void RequestMove_ServerRpc(MovementSnapshot snap)
    {
        if (MovementManager.Instance != null)
        {
            MovementManager.Instance.ServerMove(OwnerClientId, snap);
        }
    }
#endregion

#region Client RPCs
    [ClientRpc]
    public void TriggerJumpAnimation_ClientRpc()
    {
        _animator.SetTrigger(_animIDJumpTrigger);
    }
#endregion

#region Nested Classes (Health, Respawn Policy)
    private class PlayerHealth : IHealth
    {
        private readonly PlayerCharacter _owner;
        public float Current => _owner.GetStat(StatType.Health);
        public float Max => _owner.GetStat(StatType.MaxHealth);

        public event Action<DamageEvent> OnDamaged;
        public event Action OnDied;
        public event Action<float, float> OnHealthUpdated;

        public PlayerHealth(PlayerCharacter owner)
        {
            _owner = owner;
            _owner._currentHealth.OnValueChanged += (prev, curr) =>
            {
                OnHealthUpdated?.Invoke(curr, Max);

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
        _playerHealth = new PlayerHealth(this);
        _networkTransform = GetComponent<NetworkTransform>();
        _animator = GetComponent<Animator>();
        
        AssignAnimationIDs();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        IsActive.OnValueChanged += OnActiveStateChanged;
        OnActiveStateChanged(false, IsActive.Value);

        // 모든 클라이언트에서 애니메이션 변수 구독
        NetworkAnimationX.OnValueChanged += (prev, curr) => _animator.SetFloat(_animIDSpeedX, curr);
        NetworkAnimationY.OnValueChanged += (prev, curr) => _animator.SetFloat(_animIDSpeedY, curr);
        NetworkAnimationGrounded.OnValueChanged += (prev, curr) => _animator.SetBool(_animIDGrounded, curr);
        NetworkAnimationFreeFall.OnValueChanged += (prev, curr) => _animator.SetBool(_animIDFreeFall, curr);
        
        if (!IsLocalPlayer)
        {
            if (TryGetComponent<PlayerInput>(out var playerInput))
            {
                playerInput.enabled = false;
            }
        }
        
        if (IsLocalPlayer)
        {
            if (TryGetComponent<PlayerController>(out var playerController))
            {
                Transform cameraTarget = transform.Find("CinemachineCameraTarget");
                if (cameraTarget)
                {
                    playerController.CinemachineCameraTarget = cameraTarget.gameObject;
                }
                else
                {
                    Debug.LogError("[PlayerCharacter] 'CinemachineCameraTarget' 자식 오브젝트를 찾을 수 없습니다. 프리팹을 확인해주세요.");
                }
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.RegisterLocalPlayer(this);
            }
            else
            {
                Debug.LogError("[PlayerCharacter] UIManager.Instance is null. Make sure a UIManager exists in the scene.");
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        IsActive.OnValueChanged -= OnActiveStateChanged;
    }
    
    private void AssignAnimationIDs()
    {
        _animIDSpeedX = Animator.StringToHash("SpeedX");
        _animIDSpeedY = Animator.StringToHash("SpeedY");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJumpTrigger = Animator.StringToHash("JumpTrigger");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
    }
}
