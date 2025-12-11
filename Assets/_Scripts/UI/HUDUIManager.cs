using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class HUDUIManager : MonoBehaviour
{
    public static HUDUIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject quitPanel;

    [Header("HUD Elements")]
    [SerializeField] private Slider hudHealthSlider;
    [SerializeField] private TextMeshProUGUI hudHealthText;

    private ICharacterStats localPlayerHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
        if (localPlayerHealth != null)
        {
            localPlayerHealth.OnHealthChanged -= UpdateHUDHealth;
        }
    }
    
    // ============================================
    //  HEALTH UI
    // ============================================
    public void RegisterLocalPlayerHealth(ICharacterStats playerHealth)
    {
        localPlayerHealth = playerHealth;
        if (localPlayerHealth != null)
        {
            localPlayerHealth.OnHealthChanged += UpdateHUDHealth;
            UpdateHUDHealth(localPlayerHealth.CurrentHealth.Value, localPlayerHealth.MaxHealth.Value);
        }
    }

    private void UpdateHUDHealth(int currentHealth, int maxHealth)
    {
        if (hudHealthSlider != null)
        {
            if (maxHealth > 0)
            {
                hudHealthSlider.value = (float)currentHealth / maxHealth;
            }
        }
        if (hudHealthText != null)
        {
            hudHealthText.text = $"HP : {currentHealth} / {maxHealth}";
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
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        if (AuthService.Instance != null)
        {
            AuthService.Instance.ClearStoredToken();
        }
        
        StartCoroutine(LoadMainSceneAfterFrame());
    }

    private IEnumerator LoadMainSceneAfterFrame()
    {
        yield return null;
        SceneManager.LoadScene("MainScene");
    }

    public void OnQuitGameButtonClicked()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OnCancelButtonClicked()
    {
        if (quitPanel != null)
        {
            quitPanel.SetActive(false);
        }
    }
}
