using System;
using System.Collections.Generic;
using System.Threading.Tasks;
// Unity
using UnityEngine;
using UnityEngine.Networking;

namespace Jae.UnityAdapter
{
    public struct WebRequestResult
    {
        public bool Success;
        public string ResponseText;
        public long ResponseCode;
        public string Error;
    }
    
    public class WebRequestAdapter : MonoBehaviour
    {
        public static WebRequestAdapter Instance { get; private set; }

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

        public async Task<WebRequestResult> PostAsync(string url, string jsonPayload, Dictionary<string, string> headers)
        {
            byte[] bodyRaw = null;
            if (!string.IsNullOrEmpty(jsonPayload))
            {
                bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            }

            using (var request = new UnityWebRequest(url, "POST"))
            {
                if (bodyRaw != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                }
                request.downloadHandler = new DownloadHandlerBuffer();

                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.SetRequestHeader(header.Key, header.Value);
                    }
                }
                
                try
                {
                    await request.SendWebRequest();
                }
                catch (Exception e)
                {
                    return new WebRequestResult
                    {
                        Error = e.Message,
                        Success = false
                    };
                }

                return new WebRequestResult
                {
                    ResponseCode = request.responseCode,
                    ResponseText = request.downloadHandler.text,
                    Error = request.error,
                    Success = request.result == UnityWebRequest.Result.Success
                };
            }
        }

        public async Task<WebRequestResult> GetAsync(string url, Dictionary<string, string> headers)
        {
            using (var request = UnityWebRequest.Get(url))
            {
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        request.SetRequestHeader(header.Key, header.Value);
                    }
                }
                
                try
                {
                    await request.SendWebRequest();
                }
                catch (Exception e)
                {
                    return new WebRequestResult
                    {
                        Error = e.Message,
                        Success = false
                    };
                }
                
                return new WebRequestResult
                {
                    ResponseCode = request.responseCode,
                    ResponseText = request.downloadHandler.text,
                    Error = request.error,
                    Success = request.result == UnityWebRequest.Result.Success
                };
            }
        }
    }
}
