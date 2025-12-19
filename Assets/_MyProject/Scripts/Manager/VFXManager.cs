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
            }
            else
            {
                Instance = this;
            }

            _floatingTextPool = FloatingTextPool.Instance;
        }
        
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
