using Jae.Commom;
using Unity.Netcode;
using Jae.Manager;
using UnityEngine;
using Jae.Common;

public class MovementManager : NetworkBehaviour
{
    public static MovementManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    
    public void ServerMove(ulong clientId, MovementSnapshot snap) // No longer an RPC
    {
        if (!IsServer) return; // Still only runs on server

        // The clientId is now passed directly from the PlayerCharacter's RPC
        if (PlayerSessionManager.Instance.TryGetPlayerNetworkObject(clientId, out var playerNetworkObject))
        {
            // TODO: 여기에 서버 측 유효성 검사를 추가 (e.g., 속도 최적화, 거리 확인)
            
            // 회전 (서버 권위)
            float yaw = snap.LookDelta.x * 120f * snap.DeltaTime; // Use a constant like rotationSpeed or get from StatProvider
            playerNetworkObject.transform.Rotate(0f, yaw, 0f);

            // 이동
            float speed = 5f; // Default speed
            if (playerNetworkObject.TryGetComponent<IStatProvider>(out var stat))
                speed = stat.GetStat(StatType.MovementSpeed);

            Vector3 moveDir = new Vector3(snap.MoveInput.x, 0, snap.MoveInput.y);
            Vector3 worldDir = playerNetworkObject.transform.TransformDirection(moveDir);

            playerNetworkObject.transform.position += worldDir * speed * snap.DeltaTime;
        }
    }
}