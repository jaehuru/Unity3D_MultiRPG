using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using Jae.Common;
using System.Collections;
using UnityEngine.AI;

public class AIManager : NetworkBehaviour
{
    public static AIManager Instance { get; private set; }
    
    public enum EnemyAIState { Idle, Patrol, Chase, ReturnHome, Dead }
    
    private class EnemyAIData
    {
        public ICombatant Combatant;
        public NavMeshAgent Agent;
        public Transform Transform; 

        // AI State
        public EnemyAIState CurrentState;
        public float AttackTimer;
        public Vector3 HomePosition;
        public Quaternion HomeRotation;
        public Coroutine PatrolCoroutine;
        public bool IsTransitioning;
        
        // AI Settings
        public float AttackInterval;
        public float ChaseRange;
        public float PatrolStoppingDistance;
        public float PatrolRadius;
        public float ReturnHomeDistance;
        public float PatrolWaitTime;

        public EnemyAIData(ICombatant combatant, NavMeshAgent agent, Transform transform, Vector3 homePos, Quaternion homeRot, float attackInt, float chaseR, float patrolStopD, float patrolR, float returnHD, float patrolWT)
        {
            Combatant = combatant;
            Agent = agent;
            Transform = transform;
            HomePosition = homePos;
            HomeRotation = homeRot;
            AttackInterval = attackInt;
            ChaseRange = chaseR;
            PatrolStoppingDistance = patrolStopD;
            PatrolRadius = patrolR;
            ReturnHomeDistance = returnHD;
            PatrolWaitTime = patrolWT;
            CurrentState = EnemyAIState.Idle;
            AttackTimer = 0f;
            IsTransitioning = false;
        }
    }

    private readonly Dictionary<ICombatant, EnemyAIData> _enemyAIDataMap = new Dictionary<ICombatant, EnemyAIData>(); 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        this.enabled = IsServer;
    }

    void Update()
    {
        if (!IsServer) return;

        float deltaTime = Time.deltaTime;
        
        foreach (var kvp in _enemyAIDataMap)
        {
            EnemyAIData aiData = kvp.Value;
            if (aiData.Combatant.GetHealth().Current <= 0)
            {
                if (aiData.CurrentState != EnemyAIState.Dead)
                {
                    SetAIState(aiData, EnemyAIState.Dead);
                }
                continue;
            }

            aiData.AttackTimer -= deltaTime;
            
            GameObject nearestPlayer = FindNearestPlayer(aiData);
            float distanceToHome = Vector3.Distance(aiData.Transform.position, aiData.HomePosition);

            switch (aiData.CurrentState)
            {
                case EnemyAIState.Idle:
                    if (nearestPlayer != null) SetAIState(aiData, EnemyAIState.Chase);
                    else if (!aiData.IsTransitioning)
                    {
                        StartTransitionToPatrol(aiData);
                    }
                    break;
                case EnemyAIState.Patrol:
                    if (nearestPlayer != null) SetAIState(aiData, EnemyAIState.Chase);
                    break;
                case EnemyAIState.Chase:
                    if (nearestPlayer == null || distanceToHome > aiData.ReturnHomeDistance)
                    {
                        SetAIState(aiData, EnemyAIState.ReturnHome);
                        break;
                    }
                    float distanceToPlayer = Vector3.Distance(aiData.Transform.position, nearestPlayer.transform.position);

                    if (distanceToPlayer <= aiData.AttackInterval) // TODO: 'AttackInterval'을 실제 공격 범위에 맞게 조정
                    {
                        aiData.Agent.isStopped = true;
                        aiData.Transform.LookAt(nearestPlayer.transform.position);
                        if (CanNormalAttack(aiData))
                        {
                            if (nearestPlayer.TryGetComponent<NetworkObject>(out var targetNetworkObject))
                            {
                                if (aiData.Combatant is Component attackerComponent)
                                {
                                    if (targetNetworkObject.TryGetComponent<ICombatant>(out var targetCombatant))
                                    {
                                        CombatManager.Instance.ProcessAttack(aiData.Combatant, targetCombatant);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        aiData.Agent.isStopped = false;
                        aiData.Agent.SetDestination(nearestPlayer.transform.position);
                    }
                    break;
                case EnemyAIState.ReturnHome:
                    aiData.Agent.SetDestination(aiData.HomePosition);
                    if (!aiData.Agent.pathPending && aiData.Agent.remainingDistance <= aiData.PatrolStoppingDistance) // Fixed
                    {
                        SetAIState(aiData, EnemyAIState.Idle);
                    }
                    break;
                case EnemyAIState.Dead:
                    break;
            }
        }
    }

    private void SetAIState(EnemyAIData aiData, EnemyAIState newState)
    {
        if (!IsServer || aiData.CurrentState == newState) return;
        
        if (aiData.CurrentState == EnemyAIState.Patrol && aiData.PatrolCoroutine != null)
        {
            StopCoroutine(aiData.PatrolCoroutine);
            aiData.PatrolCoroutine = null;
        }
        
        aiData.CurrentState = newState;
        aiData.IsTransitioning = false;
        
        switch (newState)
        {
            case EnemyAIState.Idle:
                aiData.Agent.isStopped = true;
                break;
            case EnemyAIState.Patrol:
                aiData.Agent.isStopped = false;
                StartPatrol(aiData);
                break;
            case EnemyAIState.Chase:
            case EnemyAIState.ReturnHome:
                aiData.Agent.isStopped = false;
                break;
            case EnemyAIState.Dead:
                aiData.Agent.isStopped = true;
                if ((aiData.Combatant as Component).TryGetComponent<Collider>(out var col)) col.enabled = false;
                break;
        }
    }
    
    private void StartTransitionToPatrol(EnemyAIData aiData)
    {
        aiData.IsTransitioning = true;
        aiData.PatrolCoroutine = StartCoroutine(TransitionToPatrolCoroutine(aiData));
    }
    
    private IEnumerator TransitionToPatrolCoroutine(EnemyAIData aiData)
    {
        yield return new WaitForSeconds(aiData.PatrolWaitTime);
        if (aiData.CurrentState == EnemyAIState.Idle)
        {
           SetAIState(aiData, EnemyAIState.Patrol);
        }
        aiData.IsTransitioning = false;
        aiData.PatrolCoroutine = null;
    }
    
    private void StartPatrol(EnemyAIData aiData)
    {
        if (aiData.PatrolCoroutine != null) StopCoroutine(aiData.PatrolCoroutine);
        aiData.PatrolCoroutine = StartCoroutine(PatrolWanderCoroutine(aiData));
    }
    
    private IEnumerator PatrolWanderCoroutine(EnemyAIData aiData)
    {
        while (aiData.CurrentState == EnemyAIState.Patrol)
        {
            Vector3 randomPoint = aiData.HomePosition + UnityEngine.Random.insideUnitSphere * aiData.PatrolRadius;
            if (NavMesh.SamplePosition(randomPoint, out var hit, aiData.PatrolRadius, NavMesh.AllAreas))
            {
                aiData.Agent.SetDestination(hit.position);
                yield return new WaitUntil(() =>
                    !aiData.Agent.pathPending && aiData.Agent.remainingDistance <= aiData.PatrolStoppingDistance);
            }

            yield return new WaitForSeconds(aiData.PatrolWaitTime + UnityEngine.Random.Range(-1f, 1f));
        }
        aiData.PatrolCoroutine = null;
    }
    
    private GameObject FindNearestPlayer(EnemyAIData aiData)
    {
        Collider[] hitColliders = Physics.OverlapSphere(aiData.Transform.position, aiData.ChaseRange, LayerMask.GetMask("Player"));
        GameObject nearestPlayer = null;
        float minDistance = float.MaxValue;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.TryGetComponent<ICombatant>(out var combatant) && combatant.GetHealth().Current > 0)
            {
                // Ensure it's not the AI itself
                if (combatant == aiData.Combatant) continue;

                float distance = Vector3.Distance(aiData.Transform.position, hitCollider.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPlayer = (combatant as Component)?.gameObject;
                }
            }
        }
        return nearestPlayer;
    }
    
    private bool CanNormalAttack(EnemyAIData aiData)
    {
        return aiData.AttackTimer <= 0f;
    }
    
    public void RegisterAI(ICombatant enemyCombatant, NavMeshAgent agent, Transform transform, Vector3 homePos, Quaternion homeRot, float attackInt, float chaseR, float patrolStopD, float patrolR, float returnHD, float patrolWT)
    {
        if (!IsServer) return;
        if (!_enemyAIDataMap.ContainsKey(enemyCombatant))
        {
            EnemyAIData newAIData = new EnemyAIData(enemyCombatant, agent, transform, homePos, homeRot, attackInt, chaseR, patrolStopD, patrolR, returnHD, patrolWT);
            _enemyAIDataMap.Add(enemyCombatant, newAIData);
        }
    }

    public void UnregisterAI(ICombatant enemyCombatant)
    {
        if (!IsServer) return;
        if (_enemyAIDataMap.ContainsKey(enemyCombatant))
        {
            if (_enemyAIDataMap[enemyCombatant].PatrolCoroutine != null)
            {
                StopCoroutine(_enemyAIDataMap[enemyCombatant].PatrolCoroutine);
            }
            _enemyAIDataMap.Remove(enemyCombatant);
        }
    }

    public void SetEnemyDead(ICombatant enemyCombatant)
    {
        if (!IsServer) return;

        if (_enemyAIDataMap.TryGetValue(enemyCombatant, out EnemyAIData aiData))
        {
            if (aiData.Combatant.GetHealth().Current <= 0 && aiData.CurrentState != EnemyAIState.Dead)
            {
                SetAIState(aiData, EnemyAIState.Dead);
            }
        }
    }
}