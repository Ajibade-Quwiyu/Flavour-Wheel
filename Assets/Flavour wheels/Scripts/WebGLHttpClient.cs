using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class WebGLHttpClient : MonoBehaviour
{
    public IEnumerator GetAsync(string url, System.Action<string> callback)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError("GetAsync: URL is null or empty");
            callback(null);
            yield break;
        }

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"GetAsync Error: {www.error}");
                callback(null);
            }
            else
            {
                callback(www.downloadHandler.text);
            }
        }
    }

    public IEnumerator PostAsync(string url, string jsonData, System.Action<string> callback)
    {
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            string responseText = www.downloadHandler.text;

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"PostAsync Error: {www.error}, Response Code: {www.responseCode}, Response: {responseText}");
                callback($"Error: {www.error} (Code: {www.responseCode}) - {responseText}");
            }
            else
            {
                callback(responseText);
            }
        }
    }

    public IEnumerator PutAsync(string url, string jsonData, System.Action<string> callback)
    {
        using (UnityWebRequest www = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            string responseText = www.downloadHandler.text;

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"PutAsync Error: {www.error}, Response Code: {www.responseCode}, Response: {responseText}");
                callback($"Error: {www.error} (Code: {www.responseCode}) - {responseText}");
            }
            else
            {
                callback(responseText);
            }
        }
    }

    public IEnumerator DeleteAsync(string url, System.Action<string> callback)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError("DeleteAsync: URL is null or empty");
            callback("Error: URL is null or empty");
            yield break;
        }

        using (UnityWebRequest www = UnityWebRequest.Delete(url))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                string errorMessage = $"DeleteAsync Error: {www.error}, Response Code: {www.responseCode}";
                if (!string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    errorMessage += $", Response: {www.downloadHandler.text}";
                }
                Debug.LogError(errorMessage);
                callback($"Error: {errorMessage}");
            }
            else
            {
                callback(www.downloadHandler.text);
            }
        }
    }
}