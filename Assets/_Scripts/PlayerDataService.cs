using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class PlayerDataService : MonoBehaviour
{
    public static PlayerDataService Instance { get; private set; }
    private string saveDir;
    private Dictionary<string, PlayerSaveData> cache = new Dictionary<string, PlayerSaveData>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        saveDir = Path.Combine(Application.persistentDataPath, "PlayerSaveData");
        if (!Directory.Exists(saveDir)) Directory.CreateDirectory(saveDir);
    }

    private string GetPath(string playerId) => Path.Combine(saveDir, playerId + ".json");

    public PlayerSaveData Load(string playerId)
    {
        if (string.IsNullOrEmpty(playerId)) return null;

        if (cache.TryGetValue(playerId, out var cached)) return cached;

        string path = GetPath(playerId);
        if (!File.Exists(path))
        {
            Debug.Log($"[PlayerDataService] No save for {playerId}");
            return null;
        }

        string json = File.ReadAllText(path);
        PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
        cache[playerId] = data;
        Debug.Log($"[PlayerDataService] Loaded {playerId} from {path}");
        return data;
    }

    public void Save(PlayerSaveData data)
    {
        if (data == null || string.IsNullOrEmpty(data.PlayerID)) return;

        cache[data.PlayerID] = data;
        string path = GetPath(data.PlayerID);
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(path, json);
        Debug.Log($"[PlayerDataService] Saved {data.PlayerID} to {path}");
    }

    public void UpdateCachePosition(string playerId, Vector3 pos)
    {
        if (string.IsNullOrEmpty(playerId)) return;
        if (cache.TryGetValue(playerId, out var d))
        {
            d.PositionX = pos.x; d.PositionY = pos.y; d.PositionZ = pos.z;
        }
        else
        {
            cache[playerId] = new PlayerSaveData(playerId, pos);
        }
    }

    // 강제 플러시(예: 서버 종료 시)
    public void FlushAllToDisk()
    {
        foreach (var kv in cache)
        {
            Save(kv.Value);
        }
        Debug.Log("[PlayerDataService] FlushAllToDisk finished");
    }
}
