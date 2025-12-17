// Unity
using Unity.Netcode;
using UnityEngine;

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
        // 로컬 플레이어가 아니면 이 컴포넌트를 비활성화합니다.
        if (!IsLocalPlayer)
        {
            enabled = false;
            return;
        }

        _playerController = GetComponent<PlayerController>();
        if (_playerController == null)
        {
            Debug.LogError("[PlayerCameraController] PlayerController를 찾을 수 없습니다!");
            enabled = false;
            return;
        }
        
        // PlayerCharacter에서 설정해준 CameraTarget을 가져옵니다.
        // 이 컴포넌트가 PlayerController보다 늦게 초기화될 수 있으므로, 여기서도 타겟을 받아옵니다.
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

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void CameraRotation()
    {
        if (_playerController == null) return;
        
        // PlayerController는 입력만 받고, 실제 카메라 회전은 여기서 처리합니다.
        Vector2 lookInput = _playerController.LookInput;
        
        if (lookInput.sqrMagnitude >= 0.01f)
        {
            float deltaTimeMultiplier = Time.deltaTime;

            _cinemachineTargetYaw += lookInput.x * deltaTimeMultiplier;
            _cinemachineTargetPitch += lookInput.y * deltaTimeMultiplier;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Cinemachine will follow this target
        CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }
}

