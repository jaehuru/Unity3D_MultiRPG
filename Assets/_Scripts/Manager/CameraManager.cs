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
            if (cam == null)
            {
                Debug.LogWarning("[CameraManager] RegisterMainCamera에 null 카메라를 등록하려고 시도했습니다.");
                return;
            }
            
            MainCamera = cam;
        }
        
        public void UnregisterMainCamera()
        {
            if (MainCamera == null) return;
            
            MainCamera = null;
        }
    }
}
