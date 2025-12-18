// Unity
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
// Project
using Jae.Common;
using Jae.Manager;

public abstract class WorldSpaceUIController : NetworkBehaviour
{
    [SerializeField] protected GameObject worldSpaceCanvasPrefab;
    protected GameObject worldSpaceCanvas;

    private VisibilityController _visibilityController;
    
    protected Slider healthSlider;
    protected TextMeshProUGUI nameText;

    private IWorldSpaceUIProvider uiProvider;
    private IHealth healthComponent;

    private bool _isCameraVisible = false;

    public override void OnNetworkSpawn()
    {
        if (worldSpaceCanvasPrefab != null)
        {
            worldSpaceCanvas = Instantiate(worldSpaceCanvasPrefab, transform);
            // UIManager에 UI 캔버스 등록
            if (UIManager.Instance != null)
            {
                UIManager.Instance.RegisterWorldSpaceCanvas(worldSpaceCanvas.transform);
            }
            else
            {
                Debug.LogError("UIManager.Instance가 null입니다. 씬에 UIManager가 존재하는지 확인하세요.", this);
            }
        }
        else
        {
            Debug.LogError("worldSpaceCanvasPrefab이 할당되지 않았습니다!", this);
            enabled = false;
            return;
        }

        healthSlider = worldSpaceCanvas.GetComponentInChildren<Slider>();
        if (healthSlider == null)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogWarning("WorldSpaceUI prefab에 Slider 컴포넌트가 없습니다. 체력 업데이트가 불가능합니다.", this);
#endif
        }
        nameText = worldSpaceCanvas.GetComponentInChildren<TextMeshProUGUI>();
        if (nameText == null)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogWarning("WorldSpaceUI prefab에 TextMeshProUGUI 컴포넌트가 없습니다. 이름 표시가 불가능합니다.", this);
#endif
        }
        
        uiProvider = GetComponent<IWorldSpaceUIProvider>();
        if (uiProvider == null)
        {
            Debug.LogError("WorldSpaceUIController가 붙은 GameObject에 IWorldSpaceUIProvider 인터페이스가 없습니다!", this);
            enabled = false;
            return;
        }
        
        if (TryGetComponent<ICombatant>(out var combatant))
        {
            healthComponent = combatant.GetHealth();
            if (healthComponent == null)
            {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                Debug.LogWarning("ICombatant는 있으나 IHealth 컴포넌트를 찾을 수 없습니다.", this);
#endif
            }
        }
        else
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogWarning("ICombatant 인터페이스를 찾을 수 없습니다. 체력 업데이트가 불가능할 수 있습니다.", this);
#endif
        }
        
        if (healthComponent != null)
        {
            healthComponent.OnHealthUpdated += UpdateHealthUI;
        }

        
        Renderer mainRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (mainRenderer == null)
        {
            mainRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (mainRenderer != null)
        {
            _visibilityController = mainRenderer.gameObject.GetComponent<VisibilityController>();
            if (_visibilityController == null)
            {
                _visibilityController = mainRenderer.gameObject.AddComponent<VisibilityController>();
            }
            _visibilityController.OnVisibilityChanged += HandleVisibilityChanged;
        }
        else
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogWarning("자식 오브젝트에서 SkinnedMeshRenderer 또는 MeshRenderer를 찾을 수 없습니다. UI가 카메라에 항상 보이는 것으로 간주됩니다.", this);
#endif
            _isCameraVisible = true;
        }
        
        UpdateUIVisibility();
        UpdateAllUI();
    }

    public override void OnNetworkDespawn()
    {
        // UIManager에 UI 캔버스 등록 해제
        if (UIManager.Instance != null && worldSpaceCanvas != null)
        {
            UIManager.Instance.UnregisterWorldSpaceCanvas(worldSpaceCanvas.transform);
        }

        if (_visibilityController != null)
        {
            _visibilityController.OnVisibilityChanged -= HandleVisibilityChanged;
        }
        if (healthComponent != null)
        {
            healthComponent.OnHealthUpdated -= UpdateHealthUI;
        }
        if (worldSpaceCanvas != null)
        {
            Destroy(worldSpaceCanvas);
        }
    }

    private void HandleVisibilityChanged(bool isVisible)
    {
        _isCameraVisible = isVisible;
        UpdateUIVisibility();
        if (isVisible) UpdateAllUI();
    }
    
    protected void UpdateUIVisibility()
    {
        if (worldSpaceCanvas == null) return;

        bool shouldBeActive = _isCameraVisible && ShouldLogicVisible();
        
        if (worldSpaceCanvas.activeSelf != shouldBeActive)
        {
            worldSpaceCanvas.SetActive(shouldBeActive);
            if (shouldBeActive) UpdateAllUI();
        }
    }

    protected abstract bool ShouldLogicVisible();
    
    private void UpdateAllUI()
    {
        if (uiProvider == null || worldSpaceCanvas == null) return;
        
        if (nameText != null)
        {
            nameText.text = uiProvider.GetDisplayName();
        }
        
        if (healthSlider != null && healthComponent != null)
        {
            healthSlider.maxValue = healthComponent.Max;
            healthSlider.value = healthComponent.Current;
        }
    }
    
    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }
}