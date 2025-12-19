// Unity
using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerCameraController : NetworkBehaviour
{
    [Header("Cinemachine")]
    [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
    public GameObject CinemachineCameraTarget;
    [Tooltip("How far in degrees can you move the camera up")]
    public float TopClamp = 70.0f;
    [Tooltip("How far in degrees can you move the camera down")]
    public float BottomClamp = -30.0f;

    private PlayerController _playerController;
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        var virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
        if (virtualCamera == null)
        {
            Debug.LogError("[PlayerCameraController] 이 프리팹 내에서 CinemachineVirtualCamera를 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        if (IsLocalPlayer)
        {
            // 이 카메라가 메인 카메라가 되도록 우선순위를 높입니다.
            virtualCamera.Priority = 11;
            
            // 로컬 플레이어의 카메라에서만 소리를 듣도록 AudioListener를 활성화합니다.
            var audioListener = virtualCamera.GetComponent<AudioListener>();
            if (audioListener != null)
            {
                audioListener.enabled = true;
            }
            
            _playerController = GetComponent<PlayerController>();
            if (_playerController == null)
            {
                Debug.LogError("[PlayerCameraController] PlayerController를 찾을 수 없습니다!");
                enabled = false;
                return;
            }
        
            if (CinemachineCameraTarget == null)
            {
                CinemachineCameraTarget = _playerController.CinemachineCameraTarget;
            }

            if (CinemachineCameraTarget != null)
            {
                _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            }
            else
            {
                Debug.LogError("[PlayerCameraController] CinemachineCameraTarget이 할당되지 않았습니다!");
                enabled = false;
            }
        }
        else
        {
            // 다른 플레이어의 카메라는 우선순위를 낮추고 비활성화합니다.
            virtualCamera.Priority = 0;
            virtualCamera.enabled = false;
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void CameraRotation()
    {
        if (_playerController == null) return;
        
        Vector2 lookInput = _playerController.LookInput;
        
        if (lookInput.sqrMagnitude >= 0.01f)
        {
            float deltaTimeMultiplier = Time.deltaTime;

            _cinemachineTargetYaw += lookInput.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += lookInput.y * deltaTimeMultiplier;
        }
        
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
        
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}

