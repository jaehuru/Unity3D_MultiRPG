using Unity.Netcode;

namespace Jae.Manager
{
    public class GameManager : NetworkBehaviour
    {
        public static GameManager Instance { get; private set; }

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

            // 서버 시작 시 초기 설정
            // SpawnManager는 GameNetworkManager에서 직접 호출되어 초기 적 및 플레이어 스폰을 처리
            // AIManager는 필요한 경우 GameNetworkManager에서 관리하거나, OnNetworkSpawn을 통해 자체적으로 관리 가능
        }
        
        public override void OnNetworkDespawn()
        {
            // 서버 측 구독을 모두 정리하세요
        }
    }
}