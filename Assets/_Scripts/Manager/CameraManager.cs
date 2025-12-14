using UnityEngine;

namespace Jae.Manager
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        public Camera MainCamera { get; private set; }

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
        
        public void RegisterMainCamera(Camera cam)
        {
            MainCamera = cam;
            // Debug.Log("[CameraManager] Main Camera registered.");
        }
        
        public void UnregisterMainCamera()
        {
            MainCamera = null;
            // Debug.Log("[CameraManager] Main Camera unregistered.");
        }
    }
}
