using UnityEngine;

namespace Jae.Manager
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// Shows a floating damage text at a specific world position.
        /// </summary>
        /// <param name="position">The world position to spawn the text.</param>
        /// <param name="damage">The damage amount to display.</param>
        public void ShowFloatingText(Vector3 position, int damage)
        {
            if (FloatingTextPool.Instance == null)
            {
                Debug.LogError("[VFXManager] FloatingTextPool instance not found!");
                return;
            }

            FloatingText text = FloatingTextPool.Instance.Get();
            text.Show(damage, position);
        }
    }
}
