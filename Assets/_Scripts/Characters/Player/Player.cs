using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Unity.Collections;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(DamageHandler))]
[RequireComponent(typeof(AttackHandler))] 
public class Player : NetworkBehaviour, IWorldSpaceUIProvider
{ 
    private NavMeshAgent agent; 
    private ICharacterStats characterStats; 
    private IAttacker _myAttackerComponent;
    private readonly NetworkVariable<Vector3> _networkDestination = new(writePerm: NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString32Bytes> networkPlayerName = new(writePerm: NetworkVariableWritePermission.Server);
    
    [Header("World Space UI")]
    [SerializeField] private GameObject playerWorldSpaceUIPrefab;
    
    public GameObject WorldSpaceUIPrefab => playerWorldSpaceUIPrefab;
    public Transform UIFollowTransform => transform;
    public ICharacterStats CharacterStats => characterStats;
    public NetworkVariable<FixedString32Bytes> CharacterName => networkPlayerName;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        characterStats = GetComponent<ICharacterStats>();
        _myAttackerComponent = GetComponent<IAttacker>();
    } 

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsServer)
        {
            agent.enabled = true;
            if (GameNetworkManager.Instance != null)
            {
                GameNetworkManager.Instance.AddPlayerToList(OwnerClientId, NetworkObject);
                if (GameNetworkManager.Instance.TryGetClientInfo(OwnerClientId, out var clientInfo))
                {
                    networkPlayerName.Value = clientInfo.Uid;
                }
            }
        }

        if (IsClient)
        {
            if (playerWorldSpaceUIPrefab == null)
            {
                Debug.LogError($"Player World Space UI Prefab is not assigned on Player '{gameObject.name}'.");
            }
            else
            {
                WorldSpaceUIManager.Instance.RegisterUIProvider(this);
            }
        }

        if (IsOwner)
        {
            if (characterStats != null && HUDUIManager.Instance != null)
            {
                HUDUIManager.Instance.RegisterLocalPlayerHealth(characterStats);
            }
        }
        
        _networkDestination.OnValueChanged += OnDestinationChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _networkDestination.OnValueChanged -= OnDestinationChanged;
        
        if (IsClient)
        {
            WorldSpaceUIManager.Instance.UnregisterUIProvider(this);
        }
    }
    
    void Update() 
    { 
        if (IsOwner)
        { 
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                var cam = Camera.main;
                if (cam is not null)
                {
                    Vector2 mousePosition = Mouse.current.position.ReadValue();
                    Ray ray = cam.ScreenPointToRay(mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        SubmitDestinationServerRpc(hit.point);
                    }
                }
            }
            
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                var cam = Camera.main;
                if (cam is not null)
                {
                    Vector2 mousePosition = Mouse.current.position.ReadValue();
                    Ray ray = cam.ScreenPointToRay(mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        if (_myAttackerComponent != null)
                        {
                            if (hit.collider.TryGetComponent<NetworkObject>(out NetworkObject targetNetworkObject))
                            {
                                RequestAttackServerRpc(targetNetworkObject);
                            }
                            else
                            {
                                Debug.LogWarning("Target does not have a NetworkObject component.");
                            }
                        }
                    }
                }
            }
        }
    } 

    private void OnDestinationChanged(Vector3 previousValue, Vector3 newValue)
    {
        if (!IsServer || agent == null || !agent.enabled) return;
        
        agent.SetDestination(newValue);
    }

    [ServerRpc] 
    void SubmitDestinationServerRpc(Vector3 destination) 
    {
        _networkDestination.Value = destination; 
    }
    
    [ServerRpc]
    private void RequestAttackServerRpc(NetworkObjectReference targetNetworkObjectRef)
    {
        if (!IsServer) return;

        if (_myAttackerComponent != null)
        {
            _myAttackerComponent.PerformAttack(targetNetworkObjectRef);
        }
        else
        {
            Debug.LogError($"Player '{gameObject.name}' does not have an IAttacker component (AttackHandler).");
        }
    }

    public NetworkVariable<FixedString32Bytes> GetNetworkPlayerName()
    {
        return networkPlayerName;
    }
}