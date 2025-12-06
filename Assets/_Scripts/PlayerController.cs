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
            _networkDestination.Value = transform.position;
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
        }
        
        if (IsServer && agent != null && agent.enabled)
        {
            HandleServerMovement();
        }
    } 

    private void HandleServerMovement() 
    { 
        if (agent == null) return;
        
        float distance = Vector3.Distance(agent.destination, _networkDestination.Value);
        if (distance > 0.1f)
        {
            Debug.Log($"[Server] Moving Agent. Current Dest: {agent.destination}, New Dest: {_networkDestination.Value}, Distance: {distance}");
            agent.SetDestination(_networkDestination.Value); 
        }
    } 

    [ServerRpc] void SubmitDestinationServerRpc(Vector3 destination) 
    {
        Debug.Log($"[ServerRpc] Received new destination on server: {destination}");
        _networkDestination.Value = destination; 
    } 
}