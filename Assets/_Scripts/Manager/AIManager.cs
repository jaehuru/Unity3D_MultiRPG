using Unity.Netcode;
using System.Collections.Generic;


public class AIManager : NetworkBehaviour
{
    public static AIManager Instance { get; private set; }

    private readonly List<EnemyAIController> _aiControllers = new List<EnemyAIController>();

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
        // This manager is server-only.
        this.enabled = IsServer;
    }

    public void Register(EnemyAIController controller)
    {
        if (!IsServer || controller == null) return;
        if (!_aiControllers.Contains(controller))
        {
            _aiControllers.Add(controller);
        }
    }

    public void Unregister(EnemyAIController controller)
    {
        if (!IsServer || controller == null) return;
        if (_aiControllers.Contains(controller))
        {
            _aiControllers.Remove(controller);
        }
    }

    // AIManager는 이제 개별 AI의 상태를 직접 제어하지 않습니다.
    // 리스폰/디스폰 시 AI 활성화/비활성화는 SpawnManager가
    // EnemyAIController 컴포넌트를 직접 활성화/비활성화하는 방식으로 처리됩니다.
}