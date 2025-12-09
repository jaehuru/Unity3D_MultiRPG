using UnityEngine;
using UnityEngine.Networking;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

[Serializable]
public class PlayerPosition
{
    public float x;
    public float y;
    public float z;

    public PlayerPosition(Vector3 pos)
    {
        x = pos.x;
        y = pos.y;
        z = pos.z;
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[Serializable]
public class PlayerData
{
    // - Player data variables to be expanded
    public PlayerPosition position;
    public int health;
    public List<string> inventory;
    
    public PlayerData() 
    {
        position = new PlayerPosition(Vector3.zero);
        health = 100;
        inventory = new List<string>();
    }

    public PlayerData(Vector3 pos)
    {
        position = new PlayerPosition(pos);
        health = 100;
        inventory = new List<string>();
    }
}

[Serializable]
public class LoadPlayerResponse
{
    public string message;
    public PlayerData playerData;
}

[Serializable]
public class SavePlayerRequest
{
    public PlayerData playerData;
}

public class PlayerServerDataService : MonoBehaviour
{
    public static PlayerServerDataService Instance { get; private set; }

    [Header("Player Data Server Settings")]
    public string loadDataUrl = "http://localhost:3000/api/playerdata/load";
    public string saveDataUrl = "http://localhost:3000/api/playerdata/save";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public async Task<PlayerData> LoadPlayerDataAsync(string jwtToken)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(loadDataUrl))
        {
            request.SetRequestHeader("Authorization", "Bearer " + jwtToken);
            request.downloadHandler = new DownloadHandlerBuffer();

    
            var operation = request.SendWebRequest();

            while (!operation.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[PlayerServerDataService] Load player data failed: {request.error} - {request.downloadHandler.text}");
                return null;
            }
            else
            {
                string responseText = request.downloadHandler.text;
    
                try
                {
                    LoadPlayerResponse response = JsonUtility.FromJson<LoadPlayerResponse>(responseText);
                    return response.playerData;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PlayerServerDataService] Error parsing load player data response: {e.Message} - {responseText}");
                    return null;
                }
            }
        }
    }

    public async Task<bool> SavePlayerDataAsync(string jwtToken, PlayerData dataToSave)
    {
        if (dataToSave == null)
        {
            Debug.LogError("[PlayerServerDataService] Data to save is null.");
            return false;
        }

        SavePlayerRequest requestBody = new SavePlayerRequest { playerData = dataToSave };
        string jsonPayload = JsonUtility.ToJson(requestBody);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(saveDataUrl, "POST"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + jwtToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

    
            var operation = request.SendWebRequest();

            while (!operation.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[PlayerServerDataService] Save player data failed: {request.error} - {request.downloadHandler.text}");
                return false;
            }
            else
            {
    
                return true;
            }
        }
    }
}