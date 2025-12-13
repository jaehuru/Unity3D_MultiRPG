using UnityEngine;
using Unity.Netcode;
using Jae.Common;
using System.Collections.Generic;
using System.Collections;
using System;

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
        
        if (networkObject.TryGetComponent<ISpawnable>(out var spawnable))
        {
            // 플레이어 임시 프리팹 생성
            ISpawnPoint playerSpawnPoint = new SimpleSpawnPoint(spawnPosition, Quaternion.identity, SpawnFilter.None);

            // PlayerRespawnPolicy는 현재 플레이어 캐릭터 자체에서 가져옴
            // ISpawnPolicy는 현재 ISpawnable.OnSpawn에서 직접 사용되지 않음
            ISpawnContext context = new DefaultSpawnContext(playerSpawnPoint, null, spawnable.GetRespawnPolicy());
            spawnable.OnSpawn(context);
        }

        SubscribeToDeathEvent(playerInstance);
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

            if (networkObject.TryGetComponent<ISpawnable>(out var spawnable))
            {
                ISpawnPoint enemySpawnPoint = new SimpleSpawnPoint(enemyInfo.spawnPoint.position, enemyInfo.spawnPoint.rotation, SpawnFilter.None);
                ISpawnContext context = new DefaultSpawnContext(enemySpawnPoint, null, spawnable.GetRespawnPolicy());
                spawnable.OnSpawn(context);
            }

            SubscribeToDeathEvent(enemyInstance);
        }
        _initialEnemiesSpawned = true;
    }

    private void SubscribeToDeathEvent(GameObject instance)
    {
        if(instance.TryGetComponent<ICombatant>(out var combatant))
        {
            combatant.GetHealth().OnDied += () => StartCoroutine(RespawnCoroutine(combatant));
        }
    }

    private IEnumerator RespawnCoroutine(ICombatant combatant)
    {
        var spawnableCombatant = combatant as ISpawnable;
        var respawnPolicy = spawnableCombatant?.GetRespawnPolicy();

        if (respawnPolicy == null || !respawnPolicy.ShouldRespawn(spawnableCombatant))
        {
            if (combatant is Component component && component.TryGetComponent<NetworkObject>(out var netObj))
            {
                if (netObj.IsSpawned) netObj.Despawn(true);
            }
            yield break;
        }

        TimeSpan delay = respawnPolicy.GetRespawnDelay(spawnableCombatant);
        yield return new WaitForSeconds((float)delay.TotalSeconds);

        if (spawnableCombatant != null)
        {
            ISpawnPoint point = respawnPolicy.SelectRespawnPoint(spawnableCombatant);
            if (point != null)
            {
                if (combatant is Component component && component.TryGetComponent<IMovable>(out var movable))
                {
                    movable.Teleport(point.GetPosition());
                }
                
                var health = combatant.GetHealth();
                health.Heal(health.Max);

                Debug.Log($"{((Component)combatant).name} has respawned at {point.GetPosition()}.");
            }
        }
    }
}