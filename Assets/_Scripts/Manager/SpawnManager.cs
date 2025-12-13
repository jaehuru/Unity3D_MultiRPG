using UnityEngine;
using Unity.Netcode;
using Jae.Common;
using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine.AI;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Player Spawn Settings")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] playerSpawnPoints;

    [System.Serializable]
    public struct EnemySpawnInfo
    {
        public GameObject enemyPrefab;
        public Transform spawnPoint;
    }

    [Header("Enemy Spawn Settings")]
    [SerializeField] private List<EnemySpawnInfo> enemySpawnList;

    private bool _initialEnemiesSpawned = false;
    private Dictionary<ICombatant, Vector3> _initialPositions = new Dictionary<ICombatant, Vector3>();


    // Concrete implementation of ISpawnContext
    public class DefaultSpawnContext : ISpawnContext
    {
        public ISpawnPoint Point { get; }
        public ISpawnPolicy SpawnPolicy { get; }
        public IRespawnPolicy RespawnPolicy { get; }

        public DefaultSpawnContext(ISpawnPoint point, ISpawnPolicy spawnPolicy, IRespawnPolicy respawnPolicy)
        {
            Point = point;
            SpawnPolicy = spawnPolicy;
            RespawnPolicy = respawnPolicy;
        }
    }

    // Concrete implementation of ISpawnPoint
    private class SimpleSpawnPoint : ISpawnPoint
    {
        private readonly Vector3 _position;
        private readonly Quaternion _rotation;
        private readonly SpawnFilter _filter;
        public SimpleSpawnPoint(Vector3 position, Quaternion rotation, SpawnFilter filter) { _position = position; _rotation = rotation; _filter = filter; }
        public Vector3 GetPosition() => _position;
        public Quaternion GetRotation() => _rotation;
        public SpawnFilter GetFilter() => _filter;
    }

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
        if (!IsServer) return;
        base.OnNetworkSpawn();
    }

    public NetworkObject SpawnPlayer(ulong clientId, Vector3 spawnPosition)
    {
        if (!IsServer) return null;

        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is not set in SpawnManager.");
            return null;
        }

        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        NetworkObject networkObject = playerInstance.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError("Player prefab is missing NetworkObject component.");
            Destroy(playerInstance);
            return null;
        }
        networkObject.SpawnAsPlayerObject(clientId, true);

        if (networkObject.TryGetComponent<ICombatant>(out var combatant))
        {
            if (combatant is ISpawnable spawnable)
            {
                ISpawnPoint playerSpawnPoint = new SimpleSpawnPoint(spawnPosition, Quaternion.identity, SpawnFilter.None);
                ISpawnContext context = new DefaultSpawnContext(playerSpawnPoint, null, spawnable.GetRespawnPolicy());
                spawnable.OnSpawn(context);
            }
            
            RegisterSpawnable(combatant);
        }

        return networkObject;
    }

    public void SpawnInitialEnemies()
    {
        if (!IsServer || _initialEnemiesSpawned) return;

        foreach (var enemyInfo in enemySpawnList)
        {
            if (enemyInfo.enemyPrefab == null || enemyInfo.spawnPoint == null)
            {
                Debug.LogWarning("Incomplete enemy spawn info, skipping.");
                continue;
            }

            GameObject enemyInstance = Instantiate(enemyInfo.enemyPrefab, enemyInfo.spawnPoint.position, enemyInfo.spawnPoint.rotation);
            NetworkObject networkObject = enemyInstance.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogError("Enemy prefab is missing NetworkObject component.");
                Destroy(enemyInstance);
                continue;
            }
            networkObject.Spawn(true);

            if (networkObject.TryGetComponent<ICombatant>(out var combatant))
            {
                if (combatant is ISpawnable spawnable)
                {
                    ISpawnPoint enemySpawnPoint = new SimpleSpawnPoint(enemyInfo.spawnPoint.position, enemyInfo.spawnPoint.rotation, SpawnFilter.None);
                    ISpawnContext context = new DefaultSpawnContext(enemySpawnPoint, null, spawnable.GetRespawnPolicy());
                    spawnable.OnSpawn(context);
                }

                RegisterSpawnable(combatant);
                _initialPositions[combatant] = enemyInfo.spawnPoint.position;
            }
        }
        _initialEnemiesSpawned = true;
    }

    private void RegisterSpawnable(ICombatant combatant)
    {
        var health = combatant.GetHealth();
        if (health != null)
        {
            health.OnDied += () => HandleDeath(combatant);
        }
        else
        {
            Debug.LogError($"[SpawnManager] {((Component)combatant).name} has no IHealth component to register for death events.");
        }
    }

    public void HandleDeath(ICombatant combatant)
    {
        if (!IsServer) return;

        var combatantObj = combatant as UnityEngine.Object;
        if (combatantObj == null)
        {
            Debug.LogError("[SpawnManager] HandleDeath: Dead combatant is null or destroyed.");
            return;
        }
        Debug.Log($"[SpawnManager] {((Component)combatant).name}의 죽음을 처리합니다.");

        var spawnable = combatant as ISpawnable;
        if (spawnable == null)
        {
            Debug.LogError($"[SpawnManager] {((Component)combatant).name} is not ISpawnable. Cannot process death.");
            return;
        }

        var respawnPolicy = spawnable.GetRespawnPolicy();
        if (respawnPolicy != null && respawnPolicy.ShouldRespawn(spawnable))
        {
            StartCoroutine(RespawnCoroutine(combatant));
        }
        else
        {
            StartCoroutine(DespawnCoroutine(combatant));
        }
    }
    
    private IEnumerator DespawnCoroutine(ICombatant combatant)
    {
        Debug.Log($"[SpawnManager] {((Component)combatant).name}을(를) 디스폰합니다.");
        
        if (combatant is EnemyCharacter)
        {
            AIManager.Instance.UnregisterAI(combatant);
        }
        
        if (combatant is Component component && component.TryGetComponent<NetworkObject>(out var netObj))
        {
            if (netObj.IsSpawned)
            {
                netObj.Despawn(true);
            }
        }
        yield break;
    }

    private IEnumerator RespawnCoroutine(ICombatant combatant)
    {
        var spawnable = combatant as ISpawnable;
        var respawnPolicy = spawnable.GetRespawnPolicy();

        SetCharacterState(combatant, false);
        
        TimeSpan delay = respawnPolicy.GetRespawnDelay(spawnable);
        yield return new WaitForSeconds((float)delay.TotalSeconds);

        ISpawnPoint point = respawnPolicy.SelectRespawnPoint(spawnable);
        if (point == null)
        {
            if (_initialPositions.TryGetValue(combatant, out var initialPos))
            {
                point = new SimpleSpawnPoint(initialPos, Quaternion.identity, SpawnFilter.None);
            }
            else if (playerSpawnPoints.Length > 0)
            {
                point = new SimpleSpawnPoint(playerSpawnPoints[0].position, playerSpawnPoints[0].rotation, SpawnFilter.Player);
            } else
            {
                Debug.LogError("리스폰 지점을 찾을 수 없습니다!");
                yield break;
            }
        }

        if (combatant is Component component && component.TryGetComponent<IMovable>(out var movable))
        {
            movable.Teleport(point.GetPosition());
        }
        
        var health = combatant.GetHealth();
        health.Heal(health.Max);

        SetCharacterState(combatant, true);

        Debug.Log($"{((Component)combatant).name}이(가) {point.GetPosition()}에서 리스폰되었습니다.");
    }
    
    private void SetCharacterState(ICombatant combatant, bool active)
    {
        if (combatant as Component == null) return;
        
        var component = combatant as Component;
        if (component.TryGetComponent<Collider>(out var col)) col.enabled = active;
        if (component.TryGetComponent<Renderer>(out var rend)) rend.enabled = active;
        if (component.TryGetComponent<NavMeshAgent>(out var agent))
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = !active;
            }
            agent.enabled = active;
        }

        if (combatant is EnemyCharacter)
        {
            if (active)
                AIManager.Instance.SetEnemyAlive(combatant);
            else
                AIManager.Instance.SetEnemyDead(combatant);
        }
    }
}