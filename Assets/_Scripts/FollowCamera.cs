using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Camera))]
public class FollowCamera : MonoBehaviour
{
    [Tooltip("추적 대상 Transform")] public Transform target;

    [Tooltip("카메라 오프셋")] public Vector3 offset = new Vector3(0, 12.1f, -8);

    [Tooltip("스무딩 계수 (0..1, 작을수록 느리게)")] [Range(0.01f, 1f)]
    public float smoothSpeed = 0.2f;

    [Tooltip("씬 시작 시 한 번만 Owner를 자동으로 찾아 target에 할당할지 여부 (권장: false, 퍼포먼스 안전)")]
    public bool autoFindOnce = false;

    void Start()
    {
        if (autoFindOnce && target == null)
        {
            TryFindOwnerOnce();
        }
    }

    void LateUpdate()
    {
        if (target is null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.LookAt(target.position);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void TryFindOwnerOnce()
    {
        var netObjects = GameObject.FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);
        foreach (var no in netObjects)
        {
            if (no.IsOwner)
            {
                target = no.transform;
                return;
            }
        }
    }
}
