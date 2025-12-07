using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class PlayerController : NetworkBehaviour 
{ 
    [SerializeField] private TMP_Text playerNameText;
    private NavMeshAgent agent; 
    private readonly NetworkVariable<Vector3> _networkDestination = new(writePerm: NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString32Bytes> networkPlayerName = new(writePerm: NetworkVariableWritePermission.Server);

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
                if (GameNetworkManager.Instance.TryGetClientInfo(OwnerClientId, out var clientInfo))
                {
                    networkPlayerName.Value = clientInfo.Uid;
                }
            }
        }

  
        networkPlayerName.OnValueChanged += OnPlayerNameChanged;
        OnPlayerNameChanged(default, networkPlayerName.Value); 
        _networkDestination.OnValueChanged += OnDestinationChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _networkDestination.OnValueChanged -= OnDestinationChanged;
        networkPlayerName.OnValueChanged -= OnPlayerNameChanged;
    }

    private void OnPlayerNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        if (playerNameText != null)
        {
            playerNameText.text = newValue.ToString();
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
    } 

    void LateUpdate()
    {
        if (playerNameText != null)
        {
            playerNameText.transform.position = transform.position + Vector3.up * 2.0f;
            
            if (Camera.main != null)
            {
                playerNameText.transform.rotation = Camera.main.transform.rotation; 
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