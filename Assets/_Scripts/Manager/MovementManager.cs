// Unity
using Unity.Netcode;
using UnityEngine;
// Project
using Jae.Common;

namespace Jae.Manager
{
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

        public void ServerMove(ulong clientId, MovementSnapshot snap)
        {
            if (!IsServer) return;

            if (PlayerSessionManager.Instance.TryGetPlayerNetworkObject(clientId, out var playerNetworkObject))
            {
                // TODO: 여기에 서버 측 유효성 검사를 추가 (e.g., 속도 최적화, 거리 확인)

                float yaw = snap.LookDelta.x * snap.RotationSpeed * snap.DeltaTime;
                playerNetworkObject.transform.Rotate(0f, yaw, 0f);

                float speed = 5f;
                if (playerNetworkObject.TryGetComponent<IStatProvider>(out var stat))
                    speed = stat.GetStat(StatType.MovementSpeed);

                Vector3 moveDir = new Vector3(snap.MoveInput.x, 0, snap.MoveInput.y);
                Vector3 worldDir = playerNetworkObject.transform.TransformDirection(moveDir);

                playerNetworkObject.transform.position += worldDir * speed * snap.DeltaTime;
            }
        }
    }
}