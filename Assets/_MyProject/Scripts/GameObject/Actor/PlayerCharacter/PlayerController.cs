// Unity
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
// Project
using Jae.Common;
using Jae.Manager;

[RequireComponent(typeof(PlayerCharacter))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("서버로 전송될 캐릭터 회전 속도 (카메라 회전은 PlayerCameraController에서 처리)")]
    [SerializeField] private float rotationSpeed = 80f;

    [Header("Auto-Save Settings")]
    [Tooltip("자동 저장 간격(초)")]
    [SerializeField] private float autoSaveInterval = 10f;
    
    private PlayerCharacter _playerCharacter;
    
    // --- Public-Facing Input Properties (for other local components like camera) ---
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    
    // --- Internal Input State ---
    private bool _jumpInput;
    private bool _sprintInput;
    private bool _walkInput;

    // --- Server-Side State ---
    private float _saveTimer = 0f;
    
    // --- Cinemachine ---
    [HideInInspector]
    public GameObject CinemachineCameraTarget;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (!IsLocalPlayer && !IsServer)
        {
            enabled = false;
        }

        if (!IsLocalPlayer)
        {
            if (TryGetComponent<PlayerInput>(out var playerInput))
            {
                playerInput.enabled = false;
            }
        }

        _playerCharacter = GetComponent<PlayerCharacter>();
    }

    private void Update()
    {
        if (IsServer)
        {
            HandleAutoSave();
        }
        
        if (IsLocalPlayer)
        {
            HandleMovementInput();
        }
    }

    private void HandleMovementInput()
    {
        var snapshot = new MovementSnapshot
        {
            MoveInput = this.MoveInput,
            LookDelta = this.LookInput,
            IsJumping = _jumpInput,
            IsSprinting = _sprintInput,
            IsWalking = _walkInput,
            RotationSpeed = rotationSpeed,
            DeltaTime = Time.deltaTime
        };
        
        if (_playerCharacter != null)
        {
            _playerCharacter.RequestMove_ServerRpc(snapshot);
        } 
        
        _jumpInput = false;
    }

    private void HandleAutoSave()
    {
        _saveTimer += Time.deltaTime;
        if (_saveTimer >= autoSaveInterval)
        {
            _saveTimer = 0f;
            if (OwnerClientId != NetworkManager.ServerClientId)
            {
                RequestSave();
            }
        }
    }

    private void RequestSave()
    {
        PlayerSessionManager.Instance?.RequestSavePosition(OwnerClientId, transform.position);
    }
    
#region Input System Events (Called by PlayerInput component)
    public void OnMove(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayer) return;
        MoveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayer) return;
        LookInput = context.ReadValue<Vector2>();
    }
    
    public void OnJump(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayer || !context.performed) return;
        _jumpInput = true;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayer) return;
        _sprintInput = context.ReadValueAsButton();
    }
    
    public void OnWalk(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayer) return;
        _walkInput = context.ReadValueAsButton();
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayer || !context.performed) return;
        RequestAttackServerRpc();
    }
    
    public void OnToggleQuitMenu(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayer || !context.performed) return;
        UIManager.Instance?.ToggleQuitMenu();
    }
#endregion

    [ServerRpc]
    private void RequestAttackServerRpc(ServerRpcParams rpcParams = default)
    {
        if (CombatManager.Instance == null) return;
        CombatManager.Instance.PlayerAttackRequestServerRpc(rpcParams.Receive.SenderClientId);
    }
}