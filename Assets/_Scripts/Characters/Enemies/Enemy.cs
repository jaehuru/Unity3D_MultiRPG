using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

public class Enemy : NetworkBehaviour, IWorldSpaceUIProvider
{
    [Header("UI Settings")]
    [SerializeField] private GameObject EnemyWorldSpaceUIPrefab;

    [Header("References")]
    [SerializeField] private EnemyStats enemyStats;

    private readonly NetworkVariable<FixedString32Bytes> networkEnemyName = new(writePerm: NetworkVariableWritePermission.Server);
    
    public GameObject WorldSpaceUIPrefab => EnemyWorldSpaceUIPrefab;
    public Transform UIFollowTransform => transform;
    public ICharacterStats CharacterStats => enemyStats;
    public NetworkVariable<FixedString32Bytes> CharacterName => networkEnemyName;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // TODO: 실제 적 이름 설정 로직 (예: 스폰 시 데이터에서 가져오기)
            networkEnemyName.Value = "Goblin " + OwnerClientId; 
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

        if (IsClient)
        {
            WorldSpaceUIManager.Instance.UnregisterUIProvider(this);
        }
    }
}

