using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 월드 공간 UI 요소를 중앙에서 관리하는 싱글턴 매니저
/// IWorldSpaceUIProvider 인터페이스를 구현하는 객체들의 UI를 인스턴스화, 위치 지정, 업데이트 및 파괴
/// </summary>
public class WorldSpaceUIManager : MonoBehaviour
{
    public static WorldSpaceUIManager Instance { get; private set; }
    
    private Dictionary<IWorldSpaceUIProvider, GameObject> _activeWorldSpaceUIs = new Dictionary<IWorldSpaceUIProvider, GameObject>();
    
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// 월드 공간 UI를 관리할 Provider를 등록
    /// </summary>
    public void RegisterUIProvider(IWorldSpaceUIProvider provider)
    {
        if (provider == null || _activeWorldSpaceUIs.ContainsKey(provider))
        {
            Debug.LogWarning("WorldSpaceUIManager: 이미 등록되었거나 유효하지 않은 UI Provider입니다.");
            return;
        }

        if (provider.WorldSpaceUIPrefab == null)
        {
            Debug.LogError($"WorldSpaceUIManager: {provider.UIFollowTransform.name}의 WorldSpaceUIPrefab이 할당되지 않았습니다. UI를 생성할 수 없습니다.");
            return;
        }
        
        GameObject uiInstance = Instantiate(provider.WorldSpaceUIPrefab);
        _activeWorldSpaceUIs.Add(provider, uiInstance);
        
        WorldSpaceCharacterUI characterUI = uiInstance.GetComponent<WorldSpaceCharacterUI>();
        if (characterUI != null)
        {
            characterUI.Init(provider);
        }
        else
        {
            Debug.LogError($"WorldSpaceUIManager: {provider.WorldSpaceUIPrefab.name}에 WorldSpaceCharacterUI 컴포넌트가 없습니다. UI 초기화에 실패했습니다.");
        }
    }

    /// <summary>
    /// 등록된 월드 공간 UI Provider를 해제하고 해당 UI를 파괴
    /// </summary>
    public void UnregisterUIProvider(IWorldSpaceUIProvider provider)
    {
        if (provider == null || !_activeWorldSpaceUIs.ContainsKey(provider))
        {
            Debug.LogWarning("WorldSpaceUIManager: 등록되지 않은 UI Provider이거나 유효하지 않습니다.");
            return;
        }

        GameObject uiInstance = _activeWorldSpaceUIs[provider];
        _activeWorldSpaceUIs.Remove(provider);

        if (uiInstance != null)
        {
            Destroy(uiInstance);
        }
    }
}
