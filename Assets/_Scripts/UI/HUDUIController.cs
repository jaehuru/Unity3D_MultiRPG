using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using Jae.Common;
using Jae.Manager;

// This is a Controller. It manages the HUD view and sends signals to managers.
public class HUDUIController : MonoBehaviour
{
    public static HUDUIController Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject quitPanel;

    [Header("HUD Elements")]
    [SerializeField] private Slider hudHealthSlider;
    [SerializeField] private TextMeshProUGUI hudHealthText;

    private IHUDUpdatable localPlayerHUD;

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
        if (localPlayerHUD != null)
        {
            localPlayerHUD.OnHealthChanged -= UpdateHUDHealth;
        }
    }
    
    // ============================================
    //  HEALTH UI
    // ============================================
    public void RegisterLocalPlayerHealth(IHUDUpdatable playerHUD)
    {
        localPlayerHUD = playerHUD;
        if (localPlayerHUD != null)
        {
            localPlayerHUD.OnHealthChanged += UpdateHUDHealth;
        }
    }

    private void UpdateHUDHealth(float currentHealth, float maxHealth)
    {
        if (hudHealthSlider != null)
        {
            if (maxHealth > 0)
            {
                hudHealthSlider.value = currentHealth / maxHealth;
            }
        }
        if (hudHealthText != null)
        {
            hudHealthText.text = $"HP : {currentHealth:F0} / {maxHealth:F0}";
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
        GameManager.Instance.RequestLogout();
    }

    public void OnQuitGameButtonClicked()
    {
        GameManager.Instance.QuitApplication();
    }

    public void OnCancelButtonClicked()
    {
        if (quitPanel != null)
        {
            quitPanel.SetActive(false);
        }
    }
}
