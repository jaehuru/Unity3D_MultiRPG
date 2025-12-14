using Jae.Common;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Jae.Manager;
using System.Collections;

// WorldSpaceUIManager.cs 내의 중첩 클래스에서 분리
public class WorldSpaceUIComponent : MonoBehaviour
{
    [SerializeField] private TMPro.TMP_Text displayNameText; // 표시할 이름 텍스트
    [SerializeField] private UnityEngine.UI.Slider healthBarSlider; // 체력 바 슬라이더
    
    private IWorldSpaceUIProvider _provider;
    private Camera _mainCamera; // 카메라 참조를 저장할 필드

    /// <summary>
    /// UI를 초기화하고 Provider 데이터를 연결합니다.
    /// </summary>
    /// <param name="provider">UI 데이터를 제공할 Provider.</param>
    public void Initialize(IWorldSpaceUIProvider provider)
    {
        _provider = provider;

        // UI 오브젝트의 위치를 Provider의 위치에 맞춥니다.
        if (_provider != null && transform.parent != _provider.GetTransform())
        {
            transform.SetParent(_provider.GetTransform(), false); // 부모 설정
            transform.localPosition = new Vector3(0, 2f, 0); // UI 오프셋 위치 적용
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one; // 스케일 조절
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

    /// <summary>
    /// 카메라가 준비될 때까지 기다렸다가 Canvas에 할당하는 코루틴입니다.
    /// </summary>
    private IEnumerator AssignCameraWhenAvailable(Canvas canvas)
    {
        // CameraManager와 그 안의 MainCamera가 모두 준비될 때까지 매 프레임 기다립니다.
        yield return new WaitUntil(() => CameraManager.Instance != null && CameraManager.Instance.MainCamera != null);
        
        _mainCamera = CameraManager.Instance.MainCamera;
        canvas.worldCamera = _mainCamera;
    }
    
    private void LateUpdate()
    {
        // 빌보드 효과: UI가 항상 카메라를 마주보도록 함
        if (_mainCamera != null)
        {
            // UI의 회전 값을 카메라의 회전 값과 동일하게 설정하여 항상 카메라와 평행하게 만듦
            transform.rotation = _mainCamera.transform.rotation;
        }
    }

    /// <summary>
    /// UI 요소들을 Provider 데이터 기반으로 업데이트합니다.
    /// </summary>
    /// <param name="provider">UI 데이터를 제공할 Provider.</param>
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
