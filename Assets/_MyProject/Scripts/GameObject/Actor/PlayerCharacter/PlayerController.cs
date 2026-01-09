using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Jae.Common;
using Jae.Manager;

[RequireComponent(typeof(PlayerCharacter))]
public class PlayerController : NetworkBehaviour
{
    // --- Public-Facing Input Properties (for other local components like camera) ---
    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    
    // --- Internal Input State ---
    private bool _jumpInput;
    private bool _sprintInput;
    private bool _walkInput;

    [HideInInspector]
    public GameObject CinemachineCameraTarget;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsLocalPlayer)
        {
            // PlayerInput 컴포넌트는 Local Player만 사용해야 함
            if (TryGetComponent<PlayerInput>(out var playerInput))
            {
                playerInput.enabled = false;
            }
            // PlayerController 자체도 비활성화
            enabled = false;
        }
    }

    public MovementInput GetInput(int currentTick)
    {
        var input = new MovementInput
        {
            Tick = currentTick,
            Move = this.MoveInput,
            LookDelta = this.LookInput,
            Jump = _jumpInput,
            Sprint = _sprintInput,
            Walk = _walkInput
        };

        // 점프 입력은 한 번만 처리되도록 리셋
        _jumpInput = false;
        // 룩 입력도 매 프레임 누적되지 않도록 리셋
        LookInput = Vector2.zero;
        
        return input;
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
        // TODO: 공격 로직도 틱 기반으로 재설계 필요
        // RequestAttackServerRpc();
    }
    
    public void OnToggleQuitMenu(InputAction.CallbackContext context)
    {
        if (!IsLocalPlayer || !context.performed) return;
        UIManager.Instance?.ToggleQuitMenu();
    }
#endregion
    
    // TODO: 공격 RPC도 새로운 시스템에 맞게 수정 필요
    // [ServerRpc]
    // private void RequestAttackServerRpc(ServerRpcParams rpcParams = default)
    // {
    //     if (CombatManager.Instance == null) return;
    //     CombatManager.Instance.PlayerAttackRequestServerRpc(rpcParams.Receive.SenderClientId);
    // }
}