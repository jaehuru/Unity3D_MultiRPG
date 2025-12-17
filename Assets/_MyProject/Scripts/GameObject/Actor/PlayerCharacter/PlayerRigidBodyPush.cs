using UnityEngine;

// 이 스크립트는 CharacterController가 Rigidbody를 밀어낼 수 있도록 합니다.
// BasicRigidBodyPush.cs (StarterAssets)를 참고하여 구현되었습니다.
public class PlayerRigidBodyPush : MonoBehaviour
{
    [Tooltip("밀어낼 RigidBody가 속한 레이어 마스크")]
    public LayerMask pushLayers;
    [Tooltip("Rigidbody를 밀어낼 수 있는지 여부")]
    public bool canPush = true;
    [Tooltip("밀어내는 힘의 세기")]
    [Range(0.5f, 5f)] public float strength = 1.1f;

    // CharacterController가 다른 콜라이더와 충돌했을 때 호출됩니다.
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (canPush) PushRigidBodies(hit);
    }

    private void PushRigidBodies(ControllerColliderHit hit)
    {
        // 키네마틱이 아닌 Rigidbody인지 확인합니다.
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) return;

        // 지정된 레이어에 속한 Rigidbody만 밀어냅니다.
        var bodyLayerMask = 1 << body.gameObject.layer;
        if ((bodyLayerMask & pushLayers.value) == 0) return;

        // 아래에 있는 오브젝트는 밀어내지 않습니다.
        if (hit.moveDirection.y < -0.3f) return;

        // 이동 방향에서 수평 방향으로만 밀어내는 힘의 방향을 계산합니다.
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);

        // 힘을 적용합니다.
        body.AddForce(pushDir * strength, ForceMode.Impulse);
    }
}
