using Jae.Commom;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Jae.Common;
using Jae.Manager;

[RequireComponent(typeof(PlayerCharacter))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float attackRange = 2f;

    [Header("Auto-Save Settings")]
    [Tooltip("자동 저장 간격(초)")]
    [SerializeField] private float autoSaveInterval = 10f;
    
    private PlayerCharacter _playerCharacter;
    private IAttackHandler _attackHandler;

    // --- Input Handling (Client-side) ---
    private Vector2 _moveInput;
    private Vector2 _lookInput;

    // --- Auto-Save (Server-side) ---
    private float _saveTimer = 0f;

    public Vector2 GetLookInput() => _lookInput;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // This component runs on the owner client and the server, but not on non-owner clients.
        if (!IsOwner && !IsServer)
        {
            enabled = false;
        }

        // Deactivate PlayerInput component for non-owners
        if (!IsOwner && TryGetComponent<PlayerInput>(out var playerInput))
        {
            playerInput.enabled = false;
        }

        _playerCharacter = GetComponent<PlayerCharacter>();
        _attackHandler = GetComponent<IAttackHandler>();
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
        // Rotation is client-authoritative for responsiveness
        transform.Rotate(0, _lookInput.x * Time.deltaTime * rotationSpeed, 0);

        // Movement is requested from the server
        var snapshot = new MovementSnapshot
        {
            MoveInput = _moveInput,
            LookRotation = transform.rotation,
            DeltaTime = Time.deltaTime
        };
        
        if (MovementManager.Instance != null)
        {
            MovementManager.Instance.ServerMove_ServerRpc(snapshot);
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
        // This is called on the server, so OwnerClientId is the correct client ID to save for.
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
        NetworkObjectReference targetNetworkObjectRef = default; 
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.forward, out hit, attackRange + 1f))
        {
            if (hit.collider.TryGetComponent<NetworkObject>(out var targetNetworkObject))
            {
                targetNetworkObjectRef = new NetworkObjectReference(targetNetworkObject);
            }
        }
        
        _attackHandler?.NormalAttack(new AttackContext { TargetNetworkObjectRef = targetNetworkObjectRef });
    }
}