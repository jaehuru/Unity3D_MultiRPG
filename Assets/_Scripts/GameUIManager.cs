using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Collections;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject quitPanel;

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
        else
        {
            Debug.LogError("[GameUIManager] Quit Panel is not assigned in the inspector!");
        }
    }
    
    public void OnToggleQuitMenu(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (quitPanel != null)
            {
                quitPanel.SetActive(!quitPanel.activeSelf);
            }
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
