using Unity.Netcode;
using System.Collections.Generic;
using Jae.Common;

namespace Jae.Manager
{
    public class WorldSpaceUIManager : NetworkBehaviour
    {
        public static WorldSpaceUIManager Instance { get; private set; }

        private Dictionary<ulong, IWorldSpaceUIProvider> _registeredProviders = new Dictionary<ulong, IWorldSpaceUIProvider>();

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
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public void RegisterUIProvider(ulong networkObjectId, IWorldSpaceUIProvider provider)
        {
            if (IsServer)
            {
                if (!_registeredProviders.ContainsKey(networkObjectId))
                {
                    _registeredProviders.Add(networkObjectId, provider);
                    // TODO: world space UI 인스턴스화하고 관리
                }
            }
        }

        public void UnregisterUIProvider(ulong networkObjectId)
        {
            if (IsServer)
            {
                if (_registeredProviders.ContainsKey(networkObjectId))
                {
                    _registeredProviders.Remove(networkObjectId);
                    // TODO: world space UI 파괴
                }
            }
        }
        
        private void Update()
        {
            if (!IsServer) return;

            foreach (var entry in _registeredProviders)
            {
                IWorldSpaceUIProvider provider = entry.Value;
                // TODO: provider.GetDisplayName() 및 provider.GetHealthRatio()를 기반으로 UI 요소를 업데이트
            }
        }
    }
}
