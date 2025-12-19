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
    
    // 이 컨트롤러가 연결된 PlayerCharacter
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
    // PlayerCharacter가 OnNetworkSpawn에서 할당해줍니다.
    [HideInInspector]
    public GameObject CinemachineCameraTarget;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // 이 컴포넌트는 로컬 플레이어에서만 입력을 처리하고, 서버에서는 자동 저장을 처리합니다.
        // 다른 클라이언트에서는 비활성화합니다.
        if (!IsLocalPlayer && !IsServer)
        {
            enabled = false;
        }
        
        // PlayerInput 컴포넌트는 로컬 플레이어에게만 필요합니다.
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
        // 서버는 자동 저장 로직을 처리합니다.
        if (IsServer)
        {
            HandleAutoSave();
        }

        // 로컬 플레이어는 입력을 서버로 전송합니다.
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
        
        // 점프 입력은 한 번만 처리되도록 리셋
        _jumpInput = false;
    }

    private void HandleAutoSave()
    {
        _saveTimer += Time.deltaTime;
        if (_saveTimer >= autoSaveInterval)
        {
            _saveTimer = 0f;
            // 호스트가 아닌 경우에만 저장 요청 (데디케이티드 서버 환경)
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