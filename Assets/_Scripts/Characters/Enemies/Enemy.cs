using System;
using Unity.Netcode;
using UnityEngine;
using Unity.Collections;
using System.Collections;
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
    private NavMeshAgent _agent;
    private IAttacker _myAttackerComponent;

    [Header("AI Settings")]
    [SerializeField] private float attackInterval = 2f;
    [SerializeField] private float chaseRange = 5f;
    [SerializeField] private float patrolStoppingDistance = 0.5f;
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float returnHomeDistance = 10f;
    [SerializeField] private float patrolWaitTime = 2f;

    private float _attackTimer;
    private Vector3 _homePosition;
    private Quaternion _homeRotation;
    private AiState _currentState = AiState.Idle;
    private Coroutine _patrolCoroutine;
    private bool _isTransitioning;

    private readonly NetworkVariable<FixedString32Bytes> networkEnemyName = new(writePerm: NetworkVariableWritePermission.Server);

    private enum AiState { Idle, Patrol, Chase, ReturnHome }

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 5f;

    public GameObject WorldSpaceUIPrefab => EnemyWorldSpaceUIPrefab;
    public Transform UIFollowTransform => transform;
    public ICharacterStats CharacterStats => enemyStats;
    public NetworkVariable<FixedString32Bytes> CharacterName => networkEnemyName;

    private void Awake()
    {
        _myAttackerComponent = GetComponent<IAttacker>();
        _damageHandler = GetComponent<DamageHandler>();
        _agent = GetComponent<NavMeshAgent>();
        _attackTimer = attackInterval;
        _agent.stoppingDistance = patrolStoppingDistance;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            networkEnemyName.Value = "Goblin ";
            _agent.enabled = true;
            _homePosition = transform.position;
            _homeRotation = transform.rotation;
            SetState(AiState.Idle);
        }
        else
        {
            _agent.enabled = false;
        }

        _damageHandler.OnDied += HandleEnemyDied;
        _damageHandler.OnRespawned += HandleEnemyRespawned;

        if (_damageHandler.IsDead())
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
                Debug.LogError($"WorldSpaceUIPrefab이 Enemy '{gameObject.name}'에 할당되지 않았습니다.");
                return;
            }
            WorldSpaceUIManager.Instance.RegisterUIProvider(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
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

        if (_currentState == AiState.Patrol && _patrolCoroutine != null)
        {
            StopCoroutine(_patrolCoroutine);
            _patrolCoroutine = null;
        }

        _currentState = newState;
        
        if (_currentState != AiState.Idle)
        {
            _isTransitioning = false;
        }
        
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
            if (_patrolCoroutine != null) StopCoroutine(_patrolCoroutine);

            GameNetworkManager.Instance.RespawnEnemy(NetworkObject, respawnDelay);
            NetworkObject.gameObject.SetActive(false);
        }
    }

    private void HandleEnemyRespawned()
    {
        if (IsServer)
        {
            NetworkObject.gameObject.SetActive(true);
            _agent.enabled = true;
            _attackTimer = attackInterval;
            SetState(AiState.Idle);
        }
    }

    public void Respawn()
    {
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
                if (!_isTransitioning)
                {
                    StartCoroutine(TransitionToPatrol());
                }
                break;
                
            case AiState.Patrol:
                if (nearestPlayer != null && Vector3.Distance(transform.position, nearestPlayer.transform.position) <= chaseRange)
                {
                    SetState(AiState.Chase);
                }
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
                float attackRange = _myAttackerComponent?.AttackRange ?? patrolStoppingDistance;

                if (distanceToPlayer <= attackRange)
                {
                    _agent.isStopped = true;
                    if (_attackTimer <= 0f)
                    {
                        if (nearestPlayer.TryGetComponent<NetworkObject>(out var targetNetworkObject))
                        {
                            _myAttackerComponent.PerformAttack(new NetworkObjectReference(targetNetworkObject));
                        }
                        _attackTimer = attackInterval;
                    }
                }
                else
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(nearestPlayer.transform.position);
                }
                break;

            case AiState.ReturnHome:
                _agent.SetDestination(_homePosition);
                if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
                {
                    _agent.isStopped = true;
                    transform.rotation = _homeRotation;
                    SetState(AiState.Idle);
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
            if (hitCollider.gameObject.CompareTag("Player") && hitCollider.gameObject.TryGetComponent<Player>(out _))
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
    
    private IEnumerator TransitionToPatrol()
    {
        _isTransitioning = true;
        yield return null;
        SetState(AiState.Patrol);
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
            _agent.isStopped = false;
            Vector3 randomPoint = _homePosition + UnityEngine.Random.insideUnitSphere * patrolRadius;
            if (NavMesh.SamplePosition(randomPoint, out var hit, patrolRadius, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);

                yield return new WaitUntil(() => !_agent.pathPending);

                if (_agent.remainingDistance > _agent.stoppingDistance)
                {
                    float timer = 0f;
                    while (_agent.remainingDistance > _agent.stoppingDistance && !_agent.pathPending)
                    {
                        timer += Time.deltaTime;
                        if (timer > 10f)
                        {
                            Debug.LogWarning($"{gameObject.name} patrol path timed out.");
                            break;
                        }
                        yield return null;
                    }
                }
            }
            
            _agent.isStopped = true;
            yield return new WaitForSeconds(patrolWaitTime + UnityEngine.Random.Range(0f, 1f));
        }
        _patrolCoroutine = null;
    }
}
