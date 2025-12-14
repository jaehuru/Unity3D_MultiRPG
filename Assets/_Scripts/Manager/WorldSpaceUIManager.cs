using Unity.Netcode;
using System.Collections.Generic;
using Jae.Common;
using UnityEngine;


namespace Jae.Manager
{
    public class WorldSpaceUIManager : NetworkBehaviour
    {
        public static WorldSpaceUIManager Instance { get; private set; }

        private Dictionary<ulong, IWorldSpaceUIProvider> _registeredProviders = new Dictionary<ulong, IWorldSpaceUIProvider>();
        private Dictionary<ulong, GameObject> _activeWorldSpaceUIs = new Dictionary<ulong, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        public override void OnNetworkSpawn()
        {
            // Instance 설정은 Awake에서 이미 처리됨
        }

        public override void OnNetworkDespawn()
        {
            foreach (var uiObject in _activeWorldSpaceUIs.Values)
            {
                if (uiObject != null)
                    Destroy(uiObject);
            }
            _activeWorldSpaceUIs.Clear();
            _registeredProviders.Clear();
        }

        public void RegisterUIProvider(ulong networkObjectId, IWorldSpaceUIProvider provider)
        {
            // 로컬 플레이어의 캐릭터인 경우에만 UI 생성을 건너뜁니다.
            // (Host 모드에서 Enemy와 같은 서버 소유 오브젝트의 UI가 숨겨지는 문제 해결)
            if (provider is Component providerComponent)
            {
                var localPlayerObject = NetworkManager.Singleton?.LocalClient?.PlayerObject;
                if (localPlayerObject != null && providerComponent.gameObject == localPlayerObject.gameObject)
                {
                    return; // 로컬 플레이어의 캐릭터이므로 UI를 생성하지 않습니다.
                }
            }

            if (!_registeredProviders.ContainsKey(networkObjectId))
            {
                _registeredProviders.Add(networkObjectId, provider);

                GameObject prefabToInstantiate = provider.WorldSpaceUIPrefab;

                if (prefabToInstantiate != null)
                {
                    GameObject uiInstance = Instantiate(prefabToInstantiate, provider.GetTransform());
                    _activeWorldSpaceUIs.Add(networkObjectId, uiInstance);

                    if (uiInstance.TryGetComponent<WorldSpaceUIComponent>(out var uiComponent))
                    {
                        uiComponent.Initialize(provider);
                    }
                    else
                    {
                        Debug.LogWarning($"Instantiated UI for {provider.GetDisplayName()} but it's missing the WorldSpaceUIComponent script!");
                    }
                }
                else
                {
                    Debug.LogWarning($"Provider '{provider.GetDisplayName()}' ({networkObjectId}) has a null WorldSpaceUIPrefab. UI cannot be instantiated.");
                }
            }
            else
            {
                Debug.LogWarning($"Attempted to register UI for {provider.GetDisplayName()} ({networkObjectId}) but it is already registered.");
            }
        }

        public void UnregisterUIProvider(ulong networkObjectId)
        {
            if (_registeredProviders.ContainsKey(networkObjectId))
            {
                _registeredProviders.Remove(networkObjectId);

                if (_activeWorldSpaceUIs.TryGetValue(networkObjectId, out var uiInstance))
                {
                    if (uiInstance != null)
                        Destroy(uiInstance);
                    _activeWorldSpaceUIs.Remove(networkObjectId);
                }
            }
        }

        private void Update()
        {
            foreach (var entry in _registeredProviders)
            {
                IWorldSpaceUIProvider provider = entry.Value;
                if (_activeWorldSpaceUIs.TryGetValue(entry.Key, out var uiInstance))
                {
                    if (uiInstance == null)
                    {
                        _activeWorldSpaceUIs.Remove(entry.Key);
                        continue;
                    }

                    if (uiInstance.TryGetComponent<WorldSpaceUIComponent>(out var uiComponent))
                    {
                        uiComponent.UpdateUI(provider);
                    }
                }
            }
        }
    }
}