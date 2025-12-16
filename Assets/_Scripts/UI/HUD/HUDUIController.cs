// Unity
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
// Project
using Jae.Common;
using Jae.Manager;

public class HUDUIController : MonoBehaviour
{
    public static HUDUIController Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject quitPanel;

    [Header("HUD Elements")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;
    
    private IHealth healthComponent;

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

    void Start()
    {
        if (quitPanel != null)
        {
            quitPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (healthComponent != null)
        {
            healthComponent.OnHealthUpdated -= UpdateHealthUI;
        }
    }
    
    // ============================================
    //  HEALTH UI
    // ============================================
    public void RegisterLocalPlayerHealth(ICombatant playerCombatant)   
    {
        if (this.healthComponent != null)
        {
            this.healthComponent.OnHealthUpdated -= UpdateHealthUI;
        }
        
        this.healthComponent = playerCombatant.GetHealth();
        if (this.healthComponent != null)
        {
            this.healthComponent.OnHealthUpdated += UpdateHealthUI;
            
            UpdateHealthUI(this.healthComponent.Current, this.healthComponent.Max);
        }
        else
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.LogError("RegisterLocalPlayerHealth: IHealth 컴포넌트를 찾을 수 없습니다! 전달된 ICombatant에 IHealth가 구현되어 있지 않습니다.", this);  
#endif
        }          
    }

    private void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        
        if (healthText != null)
        {
            healthText.text = $"HP : {currentHealth:F0} / {maxHealth:F0}";
        }
    }

    // ============================================
    //  QUIT MENU
    // ============================================
    public void OnToggleQuitMenu(InputAction.CallbackContext context)
    {
        if (context.performed && quitPanel != null)
        {
            quitPanel.SetActive(!quitPanel.activeSelf);
        }
    }

    public void OnLogoutButtonClicked()
    {
        GameManager.Instance?.RequestLogout();
    }

    public void OnQuitGameButtonClicked()
    {
        GameManager.Instance?.QuitApplication();
    }

    public void OnCancelButtonClicked()
    {
        if (quitPanel != null)
        {
            quitPanel.SetActive(false);
        }
    }
}