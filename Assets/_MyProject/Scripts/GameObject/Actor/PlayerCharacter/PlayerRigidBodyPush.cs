using UnityEngine;

public class PlayerRigidBodyPush : MonoBehaviour
{
    [Tooltip("밀어낼 RigidBody가 속한 레이어 마스크")]
    public LayerMask pushLayers;
    [Tooltip("Rigidbody를 밀어낼 수 있는지 여부")]
    public bool canPush = true;
    [Tooltip("밀어내는 힘의 세기")]
    [Range(0.5f, 5f)] public float strength = 1.1f;
    
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (canPush) PushRigidBodies(hit);
    }

    private void PushRigidBodies(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) return;
        
        var bodyLayerMask = 1 << body.gameObject.layer;
        if ((bodyLayerMask & pushLayers.value) == 0) return;
        
        if (hit.moveDirection.y < -0.3f) return;
        
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);
        
        body.AddForce(pushDir * strength, ForceMode.Impulse);
    }
}
