using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Collections;
using Unity.Netcode;

/// <summary>
/// 모든 월드 공간 캐릭터 UI (예: HP 바, 이름표)의 기본 클래스
/// 공통적인 UI 요소 연결, 데이터 구독, 위치 및 회전 로직을 처리
/// </summary>
public abstract class WorldSpaceCharacterUI : MonoBehaviour
{
    [Header("Common UI Elements")]
    [SerializeField] protected Slider healthSlider;
    [SerializeField] protected Image healthFillImage;
    [SerializeField] protected TMP_Text nameText;  

    protected IWorldSpaceUIProvider _provider;
    protected ICharacterStats _characterStats;
    protected NetworkVariable<FixedString32Bytes> _characterName;
    protected Camera _mainCamera;

    protected virtual void Awake()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError($"{gameObject.name}: 메인 카메라를 찾을 수 없습니다! UI가 올바르게 작동하지 않을 수 있습니다.");
        }
    }

    protected virtual void LateUpdate()
    {
        if (_mainCamera != null && _provider != null && _provider.UIFollowTransform != null)
        {
            transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                             _mainCamera.transform.rotation * Vector3.up);
        }
        
        if (_provider != null && _provider.UIFollowTransform != null)
        {
            Vector3 offset = GetUIOffset(); 
            transform.position = _provider.UIFollowTransform.position + offset;
        }
    }

    /// <summary>
    /// UI의 초기화를 담당하는 메서드
    /// 하위 클래스에서 재정의하여 추가적인 초기화 로직을 구현
    /// </summary>
    public virtual void Init(IWorldSpaceUIProvider provider)
    {
        if (provider == null || provider.UIFollowTransform == null)
        {
            Debug.LogError($"{gameObject.name}: 유효하지 않은 UI Provider입니다. UI를 초기화할 수 없습니다.");
            return;
        }

        _provider = provider;
        _characterStats = provider.CharacterStats;
        _characterName = provider.CharacterName;
        
        if (_characterStats != null)
        {
            _characterStats.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(_characterStats.CurrentHealth.Value, _characterStats.MaxHealth.Value);
        }
        else
        {
            if (healthSlider != null) healthSlider.gameObject.SetActive(false);
            Debug.LogWarning($"{_provider.UIFollowTransform.name}에 ICharacterStats가 없습니다. HP UI를 표시하지 않습니다.");
        }
        
        if (_characterName != null)
        {
            _characterName.OnValueChanged += UpdateNameUI;
            UpdateNameUI(default, _characterName.Value);
        }
        else
        {
            if (nameText != null) nameText.gameObject.SetActive(false);
            Debug.LogWarning($"{_provider.UIFollowTransform.name}에 이름 스탯이 없습니다. 이름 UI를 표시하지 않습니다.");
        }
    }

    /// <summary>
    /// HP 바를 업데이트
    /// </summary>
    protected virtual void UpdateHealthUI(int currentHealth, int maxHealth)
    {
        if (healthSlider == null) return;

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;

        SetHealthFillColor(currentHealth, maxHealth);
    }

    /// <summary>
    /// HP 바 채우기 색상을 설정
    /// 하위 클래스에서 재정의하여 사용자 정의 색상을 제공
    /// </summary>
    protected virtual void SetHealthFillColor(int currentHealth, int maxHealth)
    {
        // 하위 클래스에서 구현
    }

    /// <summary>
    /// 이름 텍스트를 업데이트
    /// </summary>
    protected virtual void UpdateNameUI(FixedString32Bytes previousValue, FixedString32Bytes newValue)
    {
        if (nameText != null)
        {
            nameText.text = newValue.ToString();
        }
    }

    /// <summary>
    /// UI가 파괴될 때 이벤트 구독을 해지
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (_characterStats != null)
        {
            _characterStats.OnHealthChanged -= UpdateHealthUI;
        }
        if (_characterName != null)
        {
            _characterName.OnValueChanged -= UpdateNameUI;
        }
    }

    /// <summary>
    /// UI의 위치 오프셋을 제공
    /// 하위 클래스에서 재정의하여 사용자 정의 오프셋을 제공
    /// </summary>
    protected virtual Vector3 GetUIOffset()
    {
        return new Vector3(0, 2f, 0);
    }
}
