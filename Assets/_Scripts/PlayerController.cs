using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine.InputSystem;
public class PlayerController : NetworkBehaviour 
{ 
    private NavMeshAgent agent; 
    private readonly NetworkVariable<Vector3> _networkDestination = new(writePerm: NetworkVariableWritePermission.Server); 

    void Start() 
    { 
        agent = GetComponent<NavMeshAgent>(); 
    } 

    public override void OnNetworkSpawn() 
    { 
        base.OnNetworkSpawn(); 
        if (IsOwner) 
        { FollowCamera followCamera = Camera.main.GetComponent<FollowCamera>(); 
            if (followCamera != null) 
            { 
                followCamera.target = transform; 
            } 
        } 
        if (IsServer) 
        { 
            _networkDestination.Value = transform.position; 
        } 
    } 

    void Update() 
    { 
        if (IsOwner) 
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) 
            { 
                Vector2 mousePosition = Mouse.current.position.ReadValue(); 
                Ray ray = Camera.main.ScreenPointToRay(mousePosition); 
                if (Physics.Raycast(ray, out RaycastHit hit)) 
                { 
                    SubmitDestinationServerRpc(hit.point); 
                } 
            } 
        } 
        if (IsServer) 
        {
            HandleServerMovement(); 
        } 
    } 

    private void HandleServerMovement() 
    { 
        if (agent.destination != _networkDestination.Value)
        {
            agent.SetDestination(_networkDestination.Value); 
        }
    } 

    [ServerRpc] void SubmitDestinationServerRpc(Vector3 destination) 
    { 
        _networkDestination.Value = destination; 
    } 
}