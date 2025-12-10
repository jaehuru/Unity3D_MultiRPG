using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class PlayerController : NetworkBehaviour 
{ 
    private NavMeshAgent agent; 
    private Health health; 
    private readonly NetworkVariable<Vector3> _networkDestination = new(writePerm: NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString32Bytes> networkPlayerName = new(writePerm: NetworkVariableWritePermission.Server);
    
    // --- Combat ---
    [Header("Combat Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private int attackDamage = 10;
    
    // --- World Space UI ---
    [Header("World Space UI")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private Slider playerHealthSlider;
    [SerializeField] private Canvas worldSpaceUICanvas;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
    } 

    void Start()
    {
        if (worldSpaceUICanvas != null)
        {
            worldSpaceUICanvas.worldCamera = Camera.main;
        }
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

        if (IsOwner)
        {
            if (health != null && GameUIManager.Instance != null)
            {
                GameUIManager.Instance.RegisterLocalPlayerHealth(health);
            }
        }
        
        health.OnHealthChanged += UpdateWorldHealthBar;
        UpdateWorldHealthBar(health.CurrentHealth.Value, health.MaxHealth.Value);

        networkPlayerName.OnValueChanged += OnPlayerNameChanged;
        OnPlayerNameChanged(default, networkPlayerName.Value); 
        _networkDestination.OnValueChanged += OnDestinationChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        _networkDestination.OnValueChanged -= OnDestinationChanged;
        networkPlayerName.OnValueChanged -= OnPlayerNameChanged;
        
        if (health != null)
        {
            health.OnHealthChanged -= UpdateWorldHealthBar;
        }
    }
    
    // ============================================
    // World Space UI Methods
    // ============================================
    private void OnPlayerNameChanged(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        if (playerNameText != null)
        {
            playerNameText.text = newValue.ToString();
        }
    }

    private void UpdateWorldHealthBar(int currentHealth, int maxHealth)
    {
        if (playerHealthSlider != null)
        {
            if (maxHealth > 0)
            {
                playerHealthSlider.value = (float)currentHealth / maxHealth;
            }
        }
    }

    void LateUpdate()
    {
        if (worldSpaceUICanvas != null && Camera.main != null)
        {
            worldSpaceUICanvas.transform.position = transform.position + Vector3.up * 2.0f;
            worldSpaceUICanvas.transform.rotation = Camera.main.transform.rotation;
        }
    }
    // ============================================
    
    void Update() 
    { 
        if (IsOwner)
        { 
            // Left-click to move
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
            
            // Right-click to attack
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                AttackServerRpc();
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
    private void AttackServerRpc()
    {
        Debug.Log($"[Server] Attack received from client {OwnerClientId}.");
        
        // Find all colliders in attack range
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, attackRange);
        
        foreach (var hitCollider in hitColliders)
        {
            // Don't hit ourselves
            if (hitCollider.gameObject == gameObject)
            {
                continue;
            }
            
            // Check if the collider has a Health component
            if (hitCollider.TryGetComponent<Health>(out Health targetHealth))
            {
                Debug.Log($"[Server] Found target with health: {hitCollider.name}. Applying damage.");
                targetHealth.TakeDamage(attackDamage);
            }
        }
    }
}