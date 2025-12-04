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
    
    private void SavePositionNow()
    {
        ulong owner = OwnerClientId;
        
        var gnm = FindFirstObjectByType<GameNetworkManager>();
        if (gnm is null) return;

        if (gnm.TryGetAccountId(owner, out string uid))
        {
            Vector3 pos = transform.position;
            PlayerDataService.Instance.UpdateCachePosition(uid, pos);
            PlayerDataService.Instance.Save(new PlayerSaveData(uid, pos));
            Debug.Log($"[PlayerServerSave] Autosaved uid:{uid} pos:{pos}");
        }
        else
        {
            Debug.LogWarning("[PlayerServerSave] UID not found for autosave");
        }
    }
}

