using Jae.Commom;
using Unity.Netcode;
using Jae.Common;
using Jae.Manager;

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
    

    [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
    public void ServerMove_ServerRpc(MovementSnapshot snap, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        var clientId = rpcParams.Receive.SenderClientId;
        if (PlayerSessionManager.Instance.TryGetPlayerNetworkObject(clientId, out var playerNetworkObject))
        {
            if (playerNetworkObject.TryGetComponent<IMoveAuthoritative>(out var mover))
            {
                // TODO: 여기에 서버 측 유효성 검사를 추가 (e.g., 속도 최적화, 거리 확인)
                mover.ServerApplyMovement(snap);
            }
        }
    }
}
