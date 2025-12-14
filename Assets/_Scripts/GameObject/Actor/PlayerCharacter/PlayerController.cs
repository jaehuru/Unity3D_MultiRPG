using Jae.Commom;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Jae.Manager;

[RequireComponent(typeof(PlayerCharacter))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 120f;

    [Header("Auto-Save Settings")]
    [Tooltip("자동 저장 간격(초)")]
    [SerializeField] private float autoSaveInterval = 10f;
    
    private PlayerCharacter _playerCharacter;

    // --- Input Handling (Client-side) ---
    private Vector2 _moveInput;
    private Vector2 _lookInput;

    // --- Auto-Save (Server-side) ---
    private float _saveTimer = 0f;

    public Vector2 GetLookInput() => _lookInput;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner && !IsServer)
        {
            enabled = false;
        }
        
        if (!IsOwner && TryGetComponent<PlayerInput>(out var playerInput))
        {
            playerInput.enabled = false;
        }

        _playerCharacter = GetComponent<PlayerCharacter>();
    }

    private void Update()
    {
        if (IsServer)
        {
            HandleAutoSave();
        }

        if (IsOwner)
        {
            HandleOwnerMovement();
        }
    }

    private void HandleOwnerMovement()
    {
        // 클라이언트에서 직접 Transform을 조작하지 않고, 서버로 LookDelta를 전달합니다.
        // Transform 회전은 서버에서 처리합니다.
        
        var snapshot = new MovementSnapshot
        {
            MoveInput = _moveInput,
            LookDelta = _lookInput, // LookDelta를 스냅샷에 포함
            DeltaTime = Time.deltaTime
        };
        
        if (_playerCharacter != null)
        {
            _playerCharacter.RequestMove_ServerRpc(snapshot);
        }
    }

    private void HandleAutoSave()
    {
        _saveTimer += Time.deltaTime;
        if (_saveTimer >= autoSaveInterval)
        {
            _saveTimer = 0f;
            RequestSave();
        }
    }

    private void RequestSave()
    {
        PlayerSessionManager.Instance?.RequestSavePosition(OwnerClientId, transform.position);
    }


    #region Input System Events (Called by PlayerInput component)
    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;
        _moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (!IsOwner) return;
        _lookInput = value.Get<Vector2>();
    }

    public void OnAttack(InputValue value)
    {
        if (!IsOwner || !value.isPressed) return;
        RequestAttackServerRpc();
    }
    #endregion

    [ServerRpc]
    private void RequestAttackServerRpc()
    {
        if (CombatManager.Instance == null) return;
        CombatManager.Instance.PlayerAttackRequestServerRpc(OwnerClientId);
    }
}