// Unity
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// Project
using Jae.Manager;

namespace Jae.UI
{
    /// <summary>
    /// 플레이어의 체력, 스태미나 등 주요 HUD 요소를 표시하는 컨트롤러입니다.
    /// UIManager로부터 데이터를 받아 시각적으로만 표현하는 '뷰'의 역할을 합니다.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("HUD Elements")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthText;

        private void Start()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OnHealthUpdated += UpdateHealthUI;
            }
            else
            {
                Debug.LogError("UIManager.Instance is null! HUD health cannot be updated.", this);
            }
        }

        private void OnDestroy()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OnHealthUpdated -= UpdateHealthUI;
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
    }
}