using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
#if UNITY_ANDROID
using UnityEngine.Android;
using NativeShareNamespace;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CSVLoader : MonoBehaviour
{
    private const string AdminEndpoint = "https://flavour-wheel-server.onrender.com/api/adminserver";
    private const string FlavourWheelEndpoint = "https://flavour-wheel-server.onrender.com/api/flavourwheel";
    private WebGLHttpClient httpClient;

    private void Start()
    {
        httpClient = gameObject.AddComponent<WebGLHttpClient>();
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
#endif
    }

    public async void DownloadData()
    {
        try
        {
            string flavourWheelJsonData = await GetAsyncWrapper(FlavourWheelEndpoint);
            string adminJsonData = await GetAsyncWrapper(AdminEndpoint);

            var flavourWheelDataList = JsonUtility.FromJson<FlavourWheelDataList>("{\"items\":" + flavourWheelJsonData + "}").items;
            var adminData = JsonUtility.FromJson<AdminDataList>("{\"items\":" + adminJsonData + "}").items[0];

            string csvData = ConvertToCSV(flavourWheelDataList, adminData);
            SaveAndHandleFile(csvData, "Master-Distiller_Data.csv");
        }
        catch (Exception e)
        {
#if UNITY_ANDROID
            ShowToast("Error downloading data");
#endif
        }
    }

    private string ConvertToCSV(List<FlavourWheelData> flavourWheelDataList, AdminData adminData)
    {
        StringBuilder csv = new StringBuilder();
        csv.AppendLine("Player ID,Username,Email,Spirit1Name,Spirit2Name,Spirit3Name,Spirit4Name,Spirit5Name," +
                      "Spirit1Flavours,Spirit2Flavours,Spirit3Flavours,Spirit4Flavours,Spirit5Flavours," +
                      "Spirit1Ratings,Spirit2Ratings,Spirit3Ratings,Spirit4Ratings,Spirit5Ratings," +
                      "Feedback,OverallRating");

        foreach (var data in flavourWheelDataList)
        {
            csv.AppendLine($"{data.id},{data.username},{data.email},{data.spirit1Name},{data.spirit2Name}," +
                          $"{data.spirit3Name},{data.spirit4Name},{data.spirit5Name},{data.spirit1Flavours}," +
                          $"{data.spirit2Flavours},{data.spirit3Flavours},{data.spirit4Flavours},{data.spirit5Flavours}," +
                          $"{data.spirit1Ratings},{data.spirit2Ratings},{data.spirit3Ratings},{data.spirit4Ratings}," +
                          $"{data.spirit5Ratings},\"{data.feedback.Replace("\"", "\"\"")}\",{data.overallRating}");
        }

        csv.AppendLine()
           .AppendLine("Admin ID,Drink Category,Spirit1,Spirit2,Spirit3,Spirit4,Spirit5,Passcode Key")
           .AppendLine($"{adminData.id},{adminData.drinkCategory},{adminData.spirit1},{adminData.spirit2}," +
                      $"{adminData.spirit3},{adminData.spirit4},{adminData.spirit5},{adminData.passcodeKey}");

        return csv.ToString();
    }

    private void SaveAndHandleFile(string csvData, string fileName)
    {
#if UNITY_EDITOR
        string filePath = Path.Combine(Application.dataPath, "..", "Downloads", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, csvData);
        EditorUtility.RevealInFinder(filePath);
#elif UNITY_ANDROID
        try
        {
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllText(filePath, csvData);
            new NativeShare()
                .AddFile(filePath)
                .SetSubject("Master Distiller Data")
                .SetText("Here's your Master Distiller data in CSV format.")
                .Share();
        }
        catch { ShowToast("Error saving file"); }
#elif UNITY_WEBGL
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(csvData);
        DownloadFile(fileName, Convert.ToBase64String(bytes));
#else
        string filePath = Path.Combine(Application.persistentDataPath, "Downloads", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        File.WriteAllText(filePath, csvData);
        Application.OpenURL("file://" + filePath);
#endif
    }

#if UNITY_ANDROID
    private void ShowToast(string message)
    {
        try
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
            toastClass.CallStatic<AndroidJavaObject>("makeText", currentActivity, message, 0).Call("show");
        }
        catch { }
    }
#endif

#if UNITY_WEBGL
    [DllImport("__Internal")]
    private static extern void DownloadFile(string fileName, string data);
#endif

    private Task<string> GetAsyncWrapper(string url)
    {
        var tcs = new TaskCompletionSource<string>();
        StartCoroutine(httpClient.GetAsync(url, result => tcs.SetResult(result)));
        return tcs.Task;
    }

    [Serializable]
    private class AdminData
    {
        public int id;
        public string drinkCategory;
        public string spirit1, spirit2, spirit3, spirit4, spirit5;
        public string passcodeKey;
    }

    [Serializable]
    private class AdminDataList { public List<AdminData> items; }

    [Serializable]
    private class FlavourWheelData
    {
        public int id;
        public string username, email;
        public string spirit1Name, spirit2Name, spirit3Name, spirit4Name, spirit5Name;
        public int spirit1Flavours, spirit2Flavours, spirit3Flavours, spirit4Flavours, spirit5Flavours;
        public int spirit1Ratings, spirit2Ratings, spirit3Ratings, spirit4Ratings, spirit5Ratings;
        public string feedback;
        public int overallRating;
    }

    [Serializable]
    private class FlavourWheelDataList { public List<FlavourWheelData> items; }
}