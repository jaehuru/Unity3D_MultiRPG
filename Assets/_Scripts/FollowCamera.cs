using Unity.Netcode;
using UnityEngine;

public class FollowCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 12.1f, -8);
    public float smoothSpeed = 0.2f;
    void LateUpdate()
    {
        if (target == null)
        {
            foreach (var player in GameObject.FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
            {
                if (player.IsOwner)
                {
                    target = player.transform;
                    break;
                }
            }
            if (target == null) return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.LookAt(target.position);
    }
}
