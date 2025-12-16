// Unity
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;
// Project
using Jae.Common;

public abstract class WorldSpaceUIController : NetworkBehaviour
{
    [SerializeField] protected GameObject worldSpaceCanvasPrefab;
    protected GameObject worldSpaceCanvas;

    private VisibilityController _visibilityController;
    private bool _isFirstCameraCheck = true;

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
    
    private void LateUpdate()
    {
        if (!IsClient || worldSpaceCanvas == null || !worldSpaceCanvas.activeInHierarchy) return;
        
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            worldSpaceCanvas.transform.rotation = mainCamera.transform.rotation;
            _isFirstCameraCheck = true;
        }
        else
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (_isFirstCameraCheck)
            {
                Debug.LogWarning($"[{gameObject.name}] 빌보드: Camera.main을 찾을 수 없습니다. 씬에 'MainCamera' 태그가 지정된 카메라가 있는지 확인하세요.", this);
                _isFirstCameraCheck = false;
            }
#endif
        }
    }
}