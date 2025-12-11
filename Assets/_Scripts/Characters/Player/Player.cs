using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Unity.Collections;
using System.Collections; // Coroutine 사용을 위해 추가
using System; // Action 사용을 위해 추가

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(DamageHandler))]
[RequireComponent(typeof(AttackHandler))] 
public class Player : NetworkBehaviour, IWorldSpaceUIProvider
{ 
    private NavMeshAgent agent; 
    private ICharacterStats characterStats; 
    private IAttacker _myAttackerComponent;
    private DamageHandler _damageHandler; 
    private readonly NetworkVariable<Vector3> _networkDestination = new(writePerm: NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<FixedString32Bytes> networkPlayerName = new(writePerm: NetworkVariableWritePermission.Server);
    
    [Header("World Space UI")]
    [SerializeField] private GameObject playerWorldSpaceUIPrefab;

    [Header("Player Death Settings")]
    [SerializeField] private float respawnTime = 3f;
    [SerializeField] private Transform[] spawnPoints; // 플레이어 스폰 포인트 배열
    
    public GameObject WorldSpaceUIPrefab => playerWorldSpaceUIPrefab;
    public Transform UIFollowTransform => transform;
    public ICharacterStats CharacterStats => characterStats;
    public NetworkVariable<FixedString32Bytes> CharacterName => networkPlayerName;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        characterStats = GetComponent<ICharacterStats>();
        _myAttackerComponent = GetComponent<IAttacker>();
        _damageHandler = GetComponent<DamageHandler>();
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
        else // 클라이언트일 경우 NavMeshAgent는 비활성화
        {
            agent.enabled = false;
        }

        // 사망 이벤트 구독
        _damageHandler.OnDied += HandlePlayerDied;
        _damageHandler.OnRespawned += HandlePlayerRespawned;

        // 초기 상태 설정
        if (_damageHandler.IsDead())
        {
            HandlePlayerDied();
        }
        else
        {
            HandlePlayerRespawned();
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
        
        // 사망 이벤트 구독 해제
        _damageHandler.OnDied -= HandlePlayerDied;
        _damageHandler.OnRespawned -= HandlePlayerRespawned;

        if (IsClient)
        {
            WorldSpaceUIManager.Instance.UnregisterUIProvider(this);
        }
    }
    
    private void HandlePlayerDied()
    {
        if (IsServer) // 서버 로직
        {
            agent.enabled = false;
            if (TryGetComponent<Collider>(out var collider)) collider.enabled = false;
            if (TryGetComponent<Renderer>(out var renderer)) renderer.enabled = false;
            // TODO: 플레이어 데스 애니메이션 재생

            StartCoroutine(RespawnCoroutine());
            Debug.Log($"{gameObject.name}이(가) 사망했습니다. {respawnTime}초 후 리스폰됩니다.");
        }
        else if (IsOwner) // 클라이언트 (소유자) 로직
        {
            // 입력 비활성화
            // 예: GetComponent<PlayerInput>().enabled = false;
            // 로컬 플레이어 시각적 처리
        }
        else // 클라이언트 (비소유자) 로직
        {
            // 다른 플레이어의 시각적 처리
        }
    }

    private void HandlePlayerRespawned()
    {
        if (IsServer) // 서버 로직
        {
            agent.enabled = true;
            if (TryGetComponent<Collider>(out var collider)) collider.enabled = true;
            if (TryGetComponent<Renderer>(out var renderer)) renderer.enabled = true;
            // TODO: 플레이어 리스폰 애니메이션 또는 효과 재생
            Debug.Log($"{gameObject.name}이(가) 리스폰되었습니다.");
        }
        else if (IsOwner) // 클라이언트 (소유자) 로직
        {
            // 입력 활성화
            // 예: GetComponent<PlayerInput>().enabled = true;
            // 로컬 플레이어 시각적 처리
        }
        else // 클라이언트 (비소유자) 로직
        {
            // 다른 플레이어의 시각적 처리
        }
    }

    private System.Collections.IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnTime);

        if (IsServer)
        {
            _damageHandler.ResetDeathState(); // 사망 상태 초기화 (OnRespawned 이벤트 발생)
            characterStats.CurrentHealth.Value = characterStats.MaxHealth.Value; // 체력 회복

            // 스폰 포인트로 텔레포트
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, spawnPoints.Length);
                transform.position = spawnPoints[randomIndex].position;
            }
            else
            {
                Debug.LogWarning("플레이어 스폰 포인트가 설정되지 않았습니다. 기본 위치로 리스폰됩니다.");
                transform.position = Vector3.zero; // 기본 위치
            }
            // 활성화 로직은 HandlePlayerRespawned에서 처리됩니다.
        }
    }

    void Update() 
    { 
        if (!IsOwner || _damageHandler.IsDead()) return; // 사망 상태에서는 입력 및 행동 불가
        
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
                            RequestAttackServerRpc(new NetworkObjectReference(targetNetworkObject));
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