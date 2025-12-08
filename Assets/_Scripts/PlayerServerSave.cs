using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class PlayerServerSave : NetworkBehaviour
{
    [Tooltip("자동 저장 간격(초)")]
    public float autoSaveInterval = 10f;

    private float timer = 0f;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) return;
        timer = 0f;
    }

    void Update()
    {
        if (!IsServer) return;

        timer += Time.deltaTime;
        if (timer >= autoSaveInterval)
        {
            timer = 0f;
            SavePositionNow();
        }
    }
    
    private async void SavePositionNow()
    {
        ulong owner = OwnerClientId;
        
        if (GameNetworkManager.Instance == null)
        {
            Debug.LogWarning("[PlayerServerSave] GameNetworkManager.Instance is null. Cannot autosave.");
            return;
        }

        if (GameNetworkManager.Instance.TryGetClientInfo(owner, out var clientInfo))
        {
            Vector3 pos = transform.position;
            string uid = clientInfo.Uid;
            string jwtToken = clientInfo.JwtToken;

            if (PlayerServerDataService.Instance == null)
            {
                Debug.LogError("[PlayerServerSave] PlayerServerDataService.Instance is null. Cannot autosave.");
                return;
            }

            PlayerData dataToSave = new PlayerData(pos);
            
            bool saveSuccess = await PlayerServerDataService.Instance.SavePlayerDataAsync(jwtToken, dataToSave);
            if (saveSuccess)
            {
        
            }
            else
            {
                Debug.LogWarning($"[PlayerServerSave] Failed to autosave uid:{uid}.");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerServerSave] ClientInfo not found for autosave.");
        }
    }
}