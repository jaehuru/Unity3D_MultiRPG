using UnityEngine;
using Unity.Netcode;
using System.Text;

public class ClientAuthManager : MonoBehaviour
{
    public void StartClientWithUid(string uid)
    {
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("[ClientAuth] UID is empty");
            return;
        }

        byte[] payload = Encoding.UTF8.GetBytes(uid);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log($"[ClientAuth] StartClient with UID: {uid}");
        }
    }
    
    // for Host(Test)
    public void StartHostWithUid(string uid)
    {
        if (string.IsNullOrEmpty(uid))
        {
            Debug.LogWarning("[ClientAuth] UID is empty");
            return;
        }

        byte[] payload = Encoding.UTF8.GetBytes(uid);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = payload;

        if (!NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.StartHost();
            Debug.Log($"[ClientAuth] StartHost with UID: {uid}");
        }
    }
}