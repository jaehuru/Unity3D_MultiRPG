using UnityEngine;

namespace Jae.Manager
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }
        
        private FloatingTextPool _floatingTextPool;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _floatingTextPool = FloatingTextPool.Instance;
        }

        /// <summary>
        /// Shows a floating damage text at a specific world position.
        /// </summary>
        /// <param name="position">The world position to spawn the text.</param>
        /// <param name="damage">The damage amount to display.</param>
        public void ShowFloatingText(Vector3 position, int damage)
        {
            if (_floatingTextPool == null)
            {
                _floatingTextPool = FloatingTextPool.Instance;
                if (_floatingTextPool == null)
                {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Debug.LogError("[VFXManager] FloatingTextPool instance not found!");
#endif
                    return;
                }
            }

            FloatingText text = _floatingTextPool.Get();
            text.Show(damage, position);
        }
    }
}