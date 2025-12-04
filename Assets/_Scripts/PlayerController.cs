using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NavMeshAgent))]
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
        { 
            var cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.GetComponent<FollowCamera>();
                if (follow != null) follow.SetTarget(transform);
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

        // 서버에서 이동 처리
        if (IsServer && agent is not null)
        {
            HandleServerMovement();
        }
    } 

    private void HandleServerMovement() 
    { 
        if (agent is null) return;
        
        if (Vector3.Distance(agent.destination, _networkDestination.Value) > 0.1f)
        {
            agent.SetDestination(_networkDestination.Value); 
        }
    } 

    [ServerRpc] void SubmitDestinationServerRpc(Vector3 destination) 
    { 
        _networkDestination.Value = destination; 
    } 
}