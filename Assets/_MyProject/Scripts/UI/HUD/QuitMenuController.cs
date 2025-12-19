// Unity
using UnityEngine;
// Project
using Jae.Manager;

namespace Jae.UI
{
    public class QuitMenuController : MonoBehaviour
    {
        private void Awake()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OnQuitMenuToggled += HandleQuitMenuToggle;
            }
            else
            {
                Debug.LogError("UIManager.Instance is null. Cannot subscribe to QuitMenu events.", this);
            }
            
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OnQuitMenuToggled -= HandleQuitMenuToggle;
            }
        }

        private void HandleQuitMenuToggle(bool isOpen)
        {
            if (gameObject.activeSelf != isOpen)
            {
                gameObject.SetActive(isOpen);
            }
        }

        // ============================================
        //  BUTTON ONCLICK HANDLERS
        // ============================================

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
            UIManager.Instance?.ToggleQuitMenu();
        }
    }
}
