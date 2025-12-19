// Unity
using UnityEngine;
// Project
using Jae.Manager;

namespace Jae.UI
{
    /// <summary>
    /// 종료 메뉴 패널의 활성화/비활성화를 담당하는 간단한 컨트롤러입니다.
    /// </summary>
    public class QuitMenuController : MonoBehaviour
    {
        private void Awake()
        {
            // UIManager의 이벤트에 구독
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OnQuitMenuToggled += HandleQuitMenuToggle;
            }
            else
            {
                Debug.LogError("UIManager.Instance is null. Cannot subscribe to QuitMenu events.", this);
            }

            // 시작 시에는 비활성화
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            // 메모리 누수 방지를 위해 구독 해제
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
            // UIManager를 통해 메뉴를 닫도록 요청
            UIManager.Instance?.ToggleQuitMenu();
        }
    }
}
