using System.Collections;
// Unity
using UnityEngine;
using Unity.Netcode;
using UnityEngine.AI;
// Project
using Jae.Common;
using Jae.Manager;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(ICombatant))]
public class EnemyAIController : NetworkBehaviour, IAIController
{
    public enum EnemyAIState { Idle, Patrol, Chase, ReturnHome, Dead }

    [Header("AI Settings")]
    [SerializeField] private float attackInterval = 2f;
    [SerializeField] private float chaseRange = 10f;
    [SerializeField] private float patrolStoppingDistance = 0.5f;
    [SerializeField] private float patrolRadius = 7f;
    [SerializeField] private float returnHomeDistance = 15f;
    [SerializeField] private float patrolWaitTime = 3f;

    private NavMeshAgent _agent;
    private ICombatant _combatant;
    private Transform _transform;

    // AI State
    private EnemyAIState _currentState;
    private float _attackTimer;
    private Vector3 _homePosition;
    private Coroutine _patrolCoroutine;
    private bool _isTransitioning;
    
    // --- 성능 최적화를 위한 필드 ---
    private Collider[] _overlapResults;
    [SerializeField] private int _maxOverlapResults = 10;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer)
        {
            return;
        }

        _agent = GetComponent<NavMeshAgent>();
        _combatant = GetComponent<ICombatant>();
        _transform = transform;
        _homePosition = _transform.position;
        
        if (AIManager.Instance != null)
        {
            AIManager.Instance.Register(this);
        }
        
        SetAIState(EnemyAIState.Idle);
        
        _overlapResults = new Collider[_maxOverlapResults];
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsServer && AIManager.Instance != null)
        {
            AIManager.Instance.Unregister(this);
        }
    }

    public void TickAI(float deltaTime)
    {
        if (_currentState == EnemyAIState.Dead || !_agent.enabled || !_agent.isOnNavMesh) return;

        _attackTimer -= deltaTime;

        GameObject nearestPlayer = FindNearestPlayer();
        float distanceToHome = Vector3.Distance(_transform.position, _homePosition);

        switch (_currentState)
        {
            case EnemyAIState.Idle:
                if (nearestPlayer != null) SetAIState(EnemyAIState.Chase);
                else if (!_isTransitioning)
                {
                    StartTransitionToPatrol();
                }
                break;
            case EnemyAIState.Patrol:
                if (nearestPlayer != null) SetAIState(EnemyAIState.Chase);
                break;
            case EnemyAIState.Chase:
                if (nearestPlayer == null || distanceToHome > returnHomeDistance)
                {
                    SetAIState(EnemyAIState.ReturnHome);
                    break;
                }
                float distanceToPlayer = Vector3.Distance(_transform.position, nearestPlayer.transform.position);
                float attackRange = _combatant.GetStats()?.GetStat(StatType.AttackRange) ?? 1f;

                if (distanceToPlayer <= attackRange)
                {
                    if(_agent.isStopped == false) _agent.isStopped = true;

                    // 플레이어를 바라보되, Y축은 고정하여 머리가 기울지 않도록 합니다.
                    Vector3 lookPos = nearestPlayer.transform.position;
                    lookPos.y = _transform.position.y;
                    _transform.LookAt(lookPos);

                    if (CanNormalAttack())
                    {
                        _attackTimer = attackInterval;
                        if(nearestPlayer.TryGetComponent<ICombatant>(out var targetCombatant))
                        {
                            if (CombatManager.Instance != null)
                            {
                                CombatManager.Instance.ProcessAIAttack(_combatant, targetCombatant);
                            }
                        }
                    }
                }
                else
                {
                    if(_agent.isStopped == true) _agent.isStopped = false;
                    _agent.SetDestination(nearestPlayer.transform.position);
                }
                break;
            case EnemyAIState.ReturnHome:
                _agent.SetDestination(_homePosition);
                if (!_agent.pathPending && _agent.remainingDistance <= patrolStoppingDistance)
                {
                    SetAIState(EnemyAIState.Idle);
                }
                break;
        }
    }

    public void SetState(AIState s)
    {
        // TODO: IAIController 인터페이스의 일부이지만 완전히 구현되지 않음
        // 외부 시스템에서 상태 변화를 강제로 발생시키는 데 사용 가능 (e.g. 시네마틱)
    }

    private void SetAIState(EnemyAIState newState)
    {
        if (_currentState == newState && _patrolCoroutine != null) return;

        if (_currentState == EnemyAIState.Patrol && _patrolCoroutine != null)
        {
            StopCoroutine(_patrolCoroutine);
            _patrolCoroutine = null;
        }

        _currentState = newState;
        _isTransitioning = false;

        switch (newState)
        {
            case EnemyAIState.Idle:
                if(_agent.isOnNavMesh) _agent.isStopped = true;
                break;
            case EnemyAIState.Patrol:
                if(_agent.isOnNavMesh) _agent.isStopped = false;
                StartPatrol();
                break;
            case EnemyAIState.Chase:
            case EnemyAIState.ReturnHome:
                if(_agent.isOnNavMesh) _agent.isStopped = false;
                break;
            case EnemyAIState.Dead:
                if(_agent.isOnNavMesh) _agent.isStopped = true;
                break;
        }
    }

    private void StartTransitionToPatrol()
    {
        _isTransitioning = true;
        _patrolCoroutine = StartCoroutine(TransitionToPatrolCoroutine());
    }

    private IEnumerator TransitionToPatrolCoroutine()
    {
        yield return new WaitForSeconds(patrolWaitTime);
        if (_currentState == EnemyAIState.Idle)
        {
            SetAIState(EnemyAIState.Patrol);
        }
        _isTransitioning = false;
        _patrolCoroutine = null;
    }

    private void StartPatrol()
    {
        if (_patrolCoroutine != null) StopCoroutine(_patrolCoroutine);
        _patrolCoroutine = StartCoroutine(PatrolWanderCoroutine());
    }

    private IEnumerator PatrolWanderCoroutine()
    {
        while (_currentState == EnemyAIState.Patrol)
        {
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh)
            {
                yield return null;
                continue;
            }

            Vector3 randomPoint = _homePosition + Random.insideUnitSphere * patrolRadius;
            if (NavMesh.SamplePosition(randomPoint, out var hit, patrolRadius, NavMesh.AllAreas))
            {
                if(_agent.isOnNavMesh) _agent.SetDestination(hit.position);
                yield return new WaitUntil(() => _agent != null && _agent.enabled && _agent.isOnNavMesh && !_agent.pathPending && _agent.remainingDistance <= patrolStoppingDistance);
            }
            yield return new WaitForSeconds(patrolWaitTime + Random.Range(-1f, 1f));
        }
        _patrolCoroutine = null;
    }

    private GameObject FindNearestPlayer()
    {
        int numColliders = Physics.OverlapSphereNonAlloc(_transform.position, chaseRange, _overlapResults, LayerMask.GetMask("Player"));
        
        GameObject nearestPlayer = null;
        float minDistance = float.MaxValue;

        for (int i = 0; i < numColliders; i++)
        {
            var hitCollider = _overlapResults[i];
            if (hitCollider.TryGetComponent<ICombatant>(out var p) && p.GetHealth().Current > 0)
            {
                if (p == _combatant) continue;

                float distance = Vector3.Distance(_transform.position, hitCollider.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPlayer = (p as Component)?.gameObject;
                }
            }
        }
        return nearestPlayer;
    }

    private bool CanNormalAttack()
    {
        return _attackTimer <= 0f;
    }
}
