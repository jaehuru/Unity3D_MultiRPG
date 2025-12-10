using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FollowCamera : MonoBehaviour
{
    [Tooltip("추적 대상 Transform")] public Transform target;
    [Tooltip("카메라 오프셋")] public Vector3 offset = new Vector3(0, 12.1f, -8);
    [Tooltip("스무딩 계수 (0..1, 작을수록 느리게)")] [Range(0.01f, 1f)]
    public float smoothSpeed = 0.2f;
    
    void LateUpdate()
    {
        if (target == null)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClient != null)
            {
                var localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
                if (localPlayer != null)
                {
                    SetTarget(localPlayer.transform);
                }
            }
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.LookAt(target.position);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
