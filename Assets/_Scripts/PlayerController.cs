using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerController : NetworkBehaviour 
{ 
    private NavMeshAgent agent; 
    private readonly NetworkVariable<Vector3> _networkDestination = new(writePerm: NetworkVariableWritePermission.Server);

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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
            }
        }
        _networkDestination.OnValueChanged += OnDestinationChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _networkDestination.OnValueChanged -= OnDestinationChanged;
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
        }
    } 

    private void OnDestinationChanged(Vector3 previousValue, Vector3 newValue)
    {
        if (!IsServer || agent == null || !agent.enabled) return;
        
        agent.SetDestination(newValue);
    }

    [ServerRpc] void SubmitDestinationServerRpc(Vector3 destination) 
    {
        Debug.Log($"[ServerRpc] Received new destination on server: {destination}");
        _networkDestination.Value = destination; 
    } 
}