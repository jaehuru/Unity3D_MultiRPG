using Jae.Common;
using UnityEngine;
using Jae.Manager;
using System.Collections;

public class WorldSpaceUIComponent : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Text displayNameText;
    [SerializeField] private UnityEngine.UI.Slider healthBarSlider;
    
    private IWorldSpaceUIProvider _provider;
    private Camera _mainCamera;
    
    public void Initialize(IWorldSpaceUIProvider provider)
    {
        _provider = provider;
        
        if (_provider != null && transform.parent != _provider.GetTransform())
        {
            transform.SetParent(_provider.GetTransform(), false);
            transform.localPosition = new Vector3(0, 2f, 0);
            transform.localRotation = Quaternion.identity;
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            if (CameraManager.Instance != null && CameraManager.Instance.MainCamera != null)
            {
                _mainCamera = CameraManager.Instance.MainCamera;
                canvas.worldCamera = _mainCamera;
            }
            else
            {
                // 카메라를 즉시 찾지 못하면, 사용 가능해질 때까지 기다리는 코루틴 시작
                StartCoroutine(AssignCameraWhenAvailable(canvas));
            }
        }

        UpdateUI(provider); // 초기 업데이트
    }
    
    private IEnumerator AssignCameraWhenAvailable(Canvas canvas)
    {
        yield return new WaitUntil(() => CameraManager.Instance != null && CameraManager.Instance.MainCamera != null);
        
        _mainCamera = CameraManager.Instance.MainCamera;
        canvas.worldCamera = _mainCamera;
    }
    
    private void LateUpdate()
    {
        if (_mainCamera != null)
        {
            transform.rotation = _mainCamera.transform.rotation;
        }
    }
    
    public void UpdateUI(IWorldSpaceUIProvider provider)
    {
        if (provider == null) return;

        if (displayNameText != null)
        {
            displayNameText.text = provider.GetDisplayName();
        }
        if (healthBarSlider != null)
        {
            healthBarSlider.value = provider.GetHealthRatio();
        }
    }
}
