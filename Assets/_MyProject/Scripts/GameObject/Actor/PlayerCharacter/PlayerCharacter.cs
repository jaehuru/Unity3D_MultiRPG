using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine.InputSystem;
using Jae.Common;
using Jae.Manager;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
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
    // --- Helper Properties for Network Roles ---
    public bool IsPureClient => IsClient && !IsServer;
    // --- Components ---
    private CharacterController _controller;
    private PlayerController _playerController;
    private Animator _animator;
    private IHealth _playerHealth;

    // --- Networked State ---
    private readonly NetworkVariable<FixedString32Bytes> networkPlayerName = new(writePerm: NetworkVariableWritePermission.Server);
    public readonly NetworkVariable<bool> IsActive = new(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> _currentHealth = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<float> _maxHealth = new(100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    [Header("Stat Settings")]
    [SerializeField] private float walkSpeed = 2.0f;
    [SerializeField] private float runSpeed = 5.0f;
    [SerializeField] private float sprintSpeed = 7.5f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 2f;

    [Header("World Space UI")]
    [SerializeField] private GameObject playerWorldSpaceUIPrefab;

    // --- Tick-Based Movement State ---
    private const int BUFFER_SIZE = 1024;
    private MovementInput[] _clientInputBuffer; // 클라이언트 예측용
    private MovementInput[] _serverInputBuffer; // 서버 권위 처리용
    private MovementState[] _stateBuffer;
    private MovementState _serverState;
    private MovementState _predictedState;
    private MovementInput _lastValidInput; // 마지막 유효 입력 캐시
    
    // 서버 틱 관리
    private int _serverTickToProcess = 0;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _playerController = GetComponent<PlayerController>();
        _animator = GetComponent<Animator>();
        _playerHealth = new PlayerHealth(this);

        _clientInputBuffer = new MovementInput[BUFFER_SIZE];
        _serverInputBuffer = new MovementInput[BUFFER_SIZE];
        _stateBuffer = new MovementState[BUFFER_SIZE];
    }

    public override void OnNetworkSpawn()
    {
        IsActive.OnValueChanged += OnActiveStateChanged;
        OnActiveStateChanged(false, IsActive.Value);

        if (IsLocalPlayer)
        {
            _predictedState = new MovementState { Position = transform.position, Rotation = transform.rotation };
            if (UIManager.Instance != null) UIManager.Instance.RegisterLocalPlayer(this);
        }
        else
        {
            if (TryGetComponent<PlayerInput>(out var pi)) pi.enabled = false;
            if (_playerController) _playerController.enabled = false;
        }

        if (IsServer)
        {
            _serverState = new MovementState { Position = transform.position, Rotation = transform.rotation };
            
            _lastValidInput = new MovementInput
            {
                Tick = 0,
                Move = Vector2.zero,
                LookDelta = Vector2.zero
            };

            _serverTickToProcess = 0;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        IsActive.OnValueChanged -= OnActiveStateChanged;
    }

    // --- Tick-Based Movement ---
    public void ProcessLocalInput(int currentTick)
    {
        if (!IsLocalPlayer) return;
        
        var input = _playerController.GetInput(currentTick);
        int bufferIndex = currentTick % BUFFER_SIZE;
        _clientInputBuffer[bufferIndex] = input;

        ProcessInputServerRpc(input);
        
        // Velocity 계산
        MovementManager.Instance.SimulateMovement(ref _predictedState, input, this);
        _stateBuffer[bufferIndex] = _predictedState;
        
        // 클라이언트 예측 결과를 직접 적용
        Vector3 motion = _predictedState.Velocity * MovementManager.TickRate;
        _controller.Move(motion);
        transform.rotation = _predictedState.Rotation;
        _predictedState.Position = transform.position; // CC.Move 이후 실제 위치를 다시 반영
    }

    public void ProcessServerMovement()
    {
        if (!IsServer) return;
        
        int bufferIndex = _serverTickToProcess % BUFFER_SIZE;
        MovementInput inputToProcess = _serverInputBuffer[bufferIndex];

        // 해당 틱에 수신된 입력이 없으면(Tick이 0이면) 마지막 유효 입력 사용
        if (inputToProcess.Tick == 0)
        {
            inputToProcess = _lastValidInput;
        }
        else
        {
            _lastValidInput = inputToProcess;
        }

        // 시뮬레이션 및 상태 적용
        MovementManager.Instance.SimulateMovement(ref _serverState, inputToProcess, this);
        ApplyState(ref _serverState);

        // 클라이언트로 상태 전송
        if (_serverTickToProcess % 5 == 0)
        {
            UpdateClientStateClientRpc(_serverState.Position, _serverState.Velocity, _serverState.Rotation, _serverTickToProcess);
        }

        _serverTickToProcess++;
    }

    // ApplyState는 서버에서만 CharacterController를 움직이고, 그 결과를 state에 다시 반영
    private void ApplyState(ref MovementState state)
    {
        if (!IsServer) return;

        Vector3 motion = state.Velocity * MovementManager.TickRate;
        _controller.Move(motion);
        transform.rotation = state.Rotation;
        
        // 서버의 실제 위치를 상태에 다시 반영
        state.Position = transform.position;
    }

    private void Reconcile(int serverTick, Vector3 serverPos, Vector3 serverVel, Quaternion serverRot)
    {
        // 서버가 알려준 상태로 즉시 스냅 (CC를 잠시 꺼서 충돌 방지)
        _controller.enabled = false;
        transform.position = serverPos;
        transform.rotation = serverRot;
        _controller.enabled = true;
        
        _predictedState.Position = serverPos;
        _predictedState.Velocity = serverVel;
        _predictedState.Rotation = serverRot;
        
        int bufferIndex = serverTick % BUFFER_SIZE;
        _stateBuffer[bufferIndex] = _predictedState;

        // 서버가 알려준 Tick 이후의 클라이언트 입력을 다시 시뮬레이션 (Replay)
        int currentTick = MovementManager.Instance.CurrentTick;
        for (int tick = serverTick + 1; tick <= currentTick; ++tick)
        {
            bufferIndex = tick % BUFFER_SIZE;
            MovementInput input = _clientInputBuffer[bufferIndex];
            MovementManager.Instance.SimulateMovement(ref _predictedState, input, this);
            
            // Replay 중에는 예측 결과를 바로 적용 (화면에 즉시 반영되도록)
            Vector3 motion = _predictedState.Velocity * MovementManager.TickRate;
            _controller.Move(motion);
            _predictedState.Position = transform.position;
        }
    }

    // --- RPCs ---
    [ServerRpc]
    private void ProcessInputServerRpc(MovementInput input)
    {
        int bufferIndex = input.Tick % BUFFER_SIZE;
        _serverInputBuffer[bufferIndex] = input;
    }

    [ClientRpc]
    private void UpdateClientStateClientRpc(Vector3 position, Vector3 velocity, Quaternion rotation, int tick)
    {
        if (!IsLocalPlayer) return;
        Reconcile(tick, position, velocity, rotation);
    }

    // --- Active State ---
    private void OnActiveStateChanged(bool previousValue, bool newValue)
    {
        if (_playerController) _playerController.enabled = newValue;
        if (TryGetComponent<Collider>(out var col)) col.enabled = newValue;
        var mainRenderer = GetComponentInChildren<Renderer>();
        if (mainRenderer != null) mainRenderer.enabled = newValue;
    }

    public void Activate() { if (IsServer) IsActive.Value = true; }
    public void Deactivate() { if (IsServer) IsActive.Value = false; }
    
#region Interface Implementations
    public string GetId() => OwnerClientId.ToString();
    public Transform GetTransform() => transform;
    public void TryInteract(IInteractable target) => throw new NotImplementedException();
    public IInteractable GetCurrentTarget() => throw new NotImplementedException();
    public IHealth GetHealth() => _playerHealth;
    public IStatProvider GetStats() => this;
    public IStatusController GetStatusController() => throw new NotImplementedException();
    public IAttackHandler GetAttackHandler() => this;
    public ISkillCaster GetSkillCaster() => throw new NotImplementedException();
    public IHitDetector GetHitDetector() => throw new NotImplementedException();
    public ICombatCooldown GetCooldown() => throw new NotImplementedException();
    public ICombatAnimationBinder GetAnimationBinder() => throw new NotImplementedException();
    public void OnSpawned(ISpawnContext ctx) { /* Logic now in OnNetworkSpawn */ }
    
    public float GetStat(StatType stat)
    {
        return stat switch
        {
            StatType.Health => _currentHealth.Value,
            StatType.MaxHealth => _maxHealth.Value,
            StatType.WalkSpeed => walkSpeed > 0 ? walkSpeed : 2.0f,
            StatType.RunSpeed => runSpeed > 0 ? runSpeed : 5.0f,
            StatType.SprintSpeed => sprintSpeed > 0 ? sprintSpeed : 7.5f,
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
    public void AddModifier(StatModifier m) => throw new NotImplementedException();
    public void RemoveModifier(Guid id) => throw new NotImplementedException();

    public void OnSpawn(ISpawnContext ctx)
    {
        if (IsServer)
        {
            if (PlayerSessionManager.Instance != null && PlayerSessionManager.Instance.TryGetClientInfo(OwnerClientId, out var clientInfo))
            {
                networkPlayerName.Value = clientInfo.Uid;
            }
            if (ctx != null && ctx.Point != null)
            {
                transform.position = ctx.Point.GetPosition();
                transform.rotation = ctx.Point.GetRotation();
            }
        }
    }
    public IRespawnPolicy GetRespawnPolicy() => new PlayerRespawnPolicy(this);
    public bool CanNormalAttack() => true;
    public void NormalAttack() { /* Visuals only */ }
    public AttackType GetAttackType() => AttackType.Melee;
    public DamageType GetDefaultDamageType() => DamageType.Physical;
    public event Action<int> OnResourceChanged;
    public bool CanAddItem(IInventoryItem item) => throw new NotImplementedException();
    public void AddItem(IInventoryItem item) => throw new NotImplementedException();
    public bool RemoveItem(string itemId, int count) => throw new NotImplementedException();
    public IInventoryItem GetItem(string itemId) => throw new NotImplementedException();
    public IEnumerable<IInventoryItem> GetAllItems() => throw new NotImplementedException();
    public event Action<IInventoryItem> OnItemAdded;
    public event Action<IInventoryItem, int> OnItemRemoved;
    public void Move(Vector3 direction, float deltaTime) { /* Deprecated by tick-based movement */ }
    public void Teleport(Vector3 pos) { if(IsServer) transform.position = pos; }
    public void Play(string state) { if (_animator != null) _animator.Play(state); }
    public void CrossFade(string state, float floatDuration) { if (_animator != null) _animator.CrossFade(state, floatDuration); }
    public SaveData SerializeForSave() => throw new NotImplementedException();
    public void DeserializeFromSave(SaveData data) => throw new NotImplementedException();
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
        public event Action<float, float> OnHealthUpdated;

        public PlayerHealth(PlayerCharacter owner)
        {
            _owner = owner;
            _owner._currentHealth.OnValueChanged += (prev, curr) =>
            {
                OnHealthUpdated?.Invoke(curr, Max);
                if (curr < prev) OnDamaged?.Invoke(new DamageEvent { Amount = prev - curr });
                if (curr <= 0 && prev > 0) OnDied?.Invoke();
            };
        }
        public void ApplyDamage(DamageEvent evt)
        {
            if (!_owner.IsServer) return;
            _owner.SetStat(StatType.Health, Current - evt.Amount);
        }
        public void Heal(float amount)
        {
            if (!_owner.IsServer) return;
            _owner.SetStat(StatType.Health, Current + amount);
        }
    }

    private class PlayerRespawnPolicy : IRespawnPolicy
    {
        public PlayerRespawnPolicy(PlayerCharacter owner) { }
        public bool ShouldRespawn(ISpawnable target) => true;
        public TimeSpan GetRespawnDelay(ISpawnable target) => TimeSpan.FromSeconds(5);
        public ISpawnPoint SelectRespawnPoint(ISpawnable target) => null;
    }
    #endregion
}
