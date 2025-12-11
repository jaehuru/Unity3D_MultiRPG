using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections; // Coroutine 사용을 위해 추가
using UnityEngine.AI; 


[RequireComponent(typeof(DamageHandler))]
[RequireComponent(typeof(AttackHandler))]
[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : NetworkBehaviour, IWorldSpaceUIProvider
{
    [Header("UI Settings")]
    [SerializeField] private GameObject EnemyWorldSpaceUIPrefab;

    [Header("References")]
    [SerializeField] private EnemyStats enemyStats;
    private DamageHandler _damageHandler;
    private UnityEngine.AI.NavMeshAgent _agent;

    [Header("AI Settings")]
    [SerializeField] private float attackInterval = 2f;
    [SerializeField] private float chaseRange = 5f; // 플레이어 추적 시작 및 중지 범위
    [SerializeField] private float stoppingDistance = 1.5f; // 공격을 위해 멈추는 거리 (NavMeshAgent stoppingDistance와 일치)
    [SerializeField] private float patrolRadius = 3f; // 홈 포지션 주변 순찰 반경
    [SerializeField] private float returnHomeDistance = 5f; // 이 거리 이상 멀어지면 홈으로 복귀
    [SerializeField] private float patrolWaitTime = 2f; // 순찰 지점 도달 후 대기 시간

    private float _attackTimer;
    private Vector3 _homePosition;
    private Quaternion _homeRotation;
    private AiState _currentState = AiState.Patrol; // 초기 상태는 순찰 또는 Idle
    private Coroutine _patrolCoroutine;

    private readonly NetworkVariable<FixedString32Bytes> networkEnemyName = new(writePerm: NetworkVariableWritePermission.Server);
    private IAttacker _myAttackerComponent;
    
    // AI 상태 정의
    private enum AiState { Idle, Patrol, Chase, ReturnHome }

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 5f;
    
    // IWorldSpaceUIProvider 구현
    public GameObject WorldSpaceUIPrefab => EnemyWorldSpaceUIPrefab;
    public Transform UIFollowTransform => transform;
    public ICharacterStats CharacterStats => enemyStats;
    public NetworkVariable<FixedString32Bytes> CharacterName => networkEnemyName;

    private void Awake()
    {
        _myAttackerComponent = GetComponent<IAttacker>();
        _damageHandler = GetComponent<DamageHandler>();
        _agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        _attackTimer = attackInterval;
        _agent.stoppingDistance = stoppingDistance; // NavMeshAgent의 정지 거리를 공격 정지 거리와 동기화
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            networkEnemyName.Value = "Goblin "; 
            _agent.enabled = true; // 서버에서만 에이전트 활성화
            _homePosition = transform.position; // 현재 위치를 홈으로 설정
            _homeRotation = transform.rotation; // 현재 회전을 홈으로 설정
            SetState(AiState.Patrol); // 초기 상태 설정
        }
        else
        {
            _agent.enabled = false; // 클라이언트에서는 에이전트 비활성화
        }

        // 사망 이벤트 구독
        _damageHandler.OnDied += HandleEnemyDied;
        _damageHandler.OnRespawned += HandleEnemyRespawned;

        // 초기 상태 설정
        if (_damageHandler.IsDead()) // DamageHandler에 IsDead() getter가 필요함
        {
            HandleEnemyDied();
        }
        else
        {
            HandleEnemyRespawned();
        }

        if (IsClient)
        {
            if (EnemyWorldSpaceUIPrefab == null)
            {
                Debug.LogError($"WorldSpaceUIPrefab이 Enemy '{gameObject.name}'에 할당되지 않았습니다. UI를 표시할 수 없습니다.");
                return;
            }
            
            WorldSpaceUIManager.Instance.RegisterUIProvider(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        // 사망 이벤트 구독 해제
        _damageHandler.OnDied -= HandleEnemyDied;
        _damageHandler.OnRespawned -= HandleEnemyRespawned;

        if (IsClient)
        {
            WorldSpaceUIManager.Instance.UnregisterUIProvider(this);
        }
    }

    private void SetState(AiState newState)
    {
        if (_currentState == newState) return;

        // 이전 상태 정리 (필요시)
        if (_currentState == AiState.Patrol && _patrolCoroutine != null)
        {
            StopCoroutine(_patrolCoroutine);
            _patrolCoroutine = null;
        }

        _currentState = newState;
        // Debug.Log($"{gameObject.name} 상태 변경: {_currentState}");

        // 새 상태 초기화 (필요시)
        if (_currentState == AiState.Patrol)
        {
            StartPatrol();
        }
    }

    private void HandleEnemyDied()
    {
        if (IsServer)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
            NetworkObject.gameObject.SetActive(false); // 서버에서 GameObject 비활성화 (모든 클라이언트에 동기화)
            if (_patrolCoroutine != null) StopCoroutine(_patrolCoroutine); // 순찰 코루틴 중지
            StartCoroutine(RespawnCoroutine());
            Debug.Log($"{gameObject.name}이(가) 사망했습니다. {respawnDelay}초 후 리스폰됩니다.");
        }
        else // 클라이언트 로직 (시각적 처리)
        {
            // 클라이언트에서는 모델 렌더러/콜라이더 비활성화 등 시각적 처리
            // GameObject 활성 상태는 서버에서 제어하므로 건드릴 필요 없음
            // 예: GetComponent<Renderer>().enabled = false;
            // 예: GetComponent<Collider>().enabled = false;
        }
    }

    private void HandleEnemyRespawned()
    {
        if (IsServer)
        {
            NetworkObject.gameObject.SetActive(true);
            _agent.enabled = true;
            _attackTimer = attackInterval;
            SetState(AiState.Patrol); // 리스폰 후 순찰 상태로
            Debug.Log($"{gameObject.name}이(가) 리스폰되었습니다.");
        }
        else // 클라이언트 로직 (시각적 처리)
        {
            // 클라이언트에서는 모델 렌더러/콜라이더 활성화 등 시각적 처리
            // GameObject 활성 상태는 서버에서 제어하므로 건드릴 필요 없음
            // 예: GetComponent<Renderer>().enabled = true;
            // 예: GetComponent<Collider>().enabled = true;
        }
    }

    private System.Collections.IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (IsServer)
        {
            _damageHandler.ResetDeathState();
            enemyStats.CurrentHealth.Value = enemyStats.MaxHealth.Value;
        }
    }

    private void Update()
    {
        if (!IsServer || _damageHandler.IsDead()) return;

        _attackTimer -= Time.deltaTime;
        
        GameObject nearestPlayer = FindNearestPlayer();
        float distanceToHome = Vector3.Distance(transform.position, _homePosition);

        switch (_currentState)
        {
            case AiState.Idle:
            case AiState.Patrol:
                if (nearestPlayer != null && Vector3.Distance(transform.position, nearestPlayer.transform.position) <= chaseRange)
                {
                    SetState(AiState.Chase);
                    break;
                }
                
                // 순찰 로직은 PatrolWander 코루틴에서 처리
                break;

            case AiState.Chase:
                if (nearestPlayer == null || distanceToHome > returnHomeDistance)
                {
                    SetState(AiState.ReturnHome);
                    _agent.SetDestination(_homePosition);
                    _agent.isStopped = false;
                    break;
                }

                float distanceToPlayer = Vector3.Distance(transform.position, nearestPlayer.transform.position);
                if (distanceToPlayer <= _agent.stoppingDistance) // 공격 범위 내
                {
                    _agent.isStopped = true;
                    if (_attackTimer <= 0f)
                    {
                        if (nearestPlayer.TryGetComponent<NetworkObject>(out NetworkObject targetNetworkObject))
                        {
                            if (_myAttackerComponent != null)
                            {
                                _myAttackerComponent.PerformAttack(new NetworkObjectReference(targetNetworkObject));
                            }
                            else
                            {
                                Debug.LogError($"Enemy '{gameObject.name}' does not have an IAttacker component (AttackHandler).");
                            }
                        }
                        _attackTimer = attackInterval;
                    }
                }
                else // 공격 범위 밖, 추적 계속
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(nearestPlayer.transform.position);
                }
                break;

            case AiState.ReturnHome:
                _agent.SetDestination(_homePosition); // 홈으로 돌아가기
                if (!_agent.pathPending && _agent.remainingDistance < 0.5f) // 홈 근처에 도달
                {
                    _agent.isStopped = true;
                    transform.rotation = _homeRotation; // 원래 방향으로 회전
                    SetState(AiState.Patrol); // 순찰 상태로 전환
                }
                break;
        }
    }
    
    private GameObject FindNearestPlayer()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, chaseRange); 
        GameObject nearestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.CompareTag("Player") && hitCollider.gameObject.TryGetComponent<Player>(out Player player))
            {
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPlayer = hitCollider.gameObject;
                }
            }
        }
        return nearestPlayer;
    }

    private void StartPatrol()
    {
        if (_patrolCoroutine != null) StopCoroutine(_patrolCoroutine);
        _patrolCoroutine = StartCoroutine(PatrolWander());
    }

    private IEnumerator PatrolWander()
    {
        while (_currentState == AiState.Patrol)
        {
            Debug.Log($"{gameObject.name}: 순찰 시작 - 현재 상태: {_currentState}");
            Vector3 randomPoint = _homePosition + UnityEngine.Random.insideUnitSphere * patrolRadius;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, patrolRadius, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                Debug.Log($"{gameObject.name}: 순찰 목적지 설정: {hit.position}, 남은 거리: {_agent.remainingDistance:F2}");
                yield return new WaitUntil(() => !_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance + 0.1f); // 목적지 도착 대기
                Debug.Log($"{gameObject.name}: 순찰 목적지 도착! 남은 거리: {_agent.remainingDistance:F2}");
            }
            else
            {
                Debug.LogWarning($"{gameObject.name}: NavMesh.SamplePosition 실패. 유효한 순찰 지점을 찾을 수 없습니다.");
            }
            yield return new WaitForSeconds(patrolWaitTime + UnityEngine.Random.Range(0f, 1f)); // 순찰 지점 사이의 대기 시간
        }
        Debug.Log($"{gameObject.name}: PatrolWander 코루틴 종료.");
        _patrolCoroutine = null;
    }
}
