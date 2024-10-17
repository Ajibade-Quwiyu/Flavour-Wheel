using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine.UI;

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
    }

    public async void DownloadData()
    {
        try
        {
            // Fetch data from both endpoints
            string flavourWheelJsonData = await GetAsyncWrapper(FlavourWheelEndpoint);
            string adminJsonData = await GetAsyncWrapper(AdminEndpoint);

            List<FlavourWheelData> flavourWheelDataList = JsonUtility.FromJson<FlavourWheelDataList>("{\"items\":" + flavourWheelJsonData + "}").items;
            AdminData adminData = JsonUtility.FromJson<AdminDataList>("{\"items\":" + adminJsonData + "}").items[0];

            // Convert to CSV
            string csvData = ConvertToCSV(flavourWheelDataList, adminData);

            // Download the CSV file
            DownloadCSVFile(csvData, "Master-Distiller_Data.csv");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"DownloadData Exception: {e.Message}\nStack Trace: {e.StackTrace}");
        }
    }

    private string ConvertToCSV(List<FlavourWheelData> flavourWheelDataList, AdminData adminData)
    {
        StringBuilder csv = new StringBuilder();

        // Add FlavourWheel headers
        csv.AppendLine("Player ID,Username,Email,Spirit1Name,Spirit2Name,Spirit3Name,Spirit4Name,Spirit5Name," +
                       "Spirit1Flavours,Spirit2Flavours,Spirit3Flavours,Spirit4Flavours,Spirit5Flavours," +
                       "Spirit1Ratings,Spirit2Ratings,Spirit3Ratings,Spirit4Ratings,Spirit5Ratings," +
                       "Feedback,OverallRating");

        // Add FlavourWheel data rows
        foreach (var data in flavourWheelDataList)
        {
            csv.AppendLine($"{data.id},{data.username},{data.email},{data.spirit1Name},{data.spirit2Name},{data.spirit3Name},{data.spirit4Name},{data.spirit5Name}," +
                           $"{data.spirit1Flavours},{data.spirit2Flavours},{data.spirit3Flavours},{data.spirit4Flavours},{data.spirit5Flavours}," +
                           $"{data.spirit1Ratings},{data.spirit2Ratings},{data.spirit3Ratings},{data.spirit4Ratings},{data.spirit5Ratings}," +
                           $"\"{data.feedback.Replace("\"", "\"\"")}\",{data.overallRating}");
        }

        // Add a blank line for separation
        csv.AppendLine();

        // Add Admin headers
        csv.AppendLine("Admin ID,Drink Category,Spirit1,Spirit2,Spirit3,Spirit4,Spirit5,Passcode Key");

        // Add Admin data
        csv.AppendLine($"{adminData.id},{adminData.drinkCategory},{adminData.spirit1},{adminData.spirit2},{adminData.spirit3},{adminData.spirit4},{adminData.spirit5},{adminData.passcodeKey}");

        return csv.ToString();
    }

    private void DownloadCSVFile(string csvData, string fileName)
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        // Write the CSV data to a file
        File.WriteAllText(filePath, csvData);

        // Platform-specific file handling
#if UNITY_EDITOR
            EditorUtility.RevealInFinder(filePath);
#elif UNITY_WEBGL
            DownloadFileInBrowser(csvData, fileName);
#elif UNITY_ANDROID
            StartAndroidFileDownload(filePath, fileName);
#else
        Application.OpenURL("file://" + filePath);
#endif

        Debug.Log($"CSV file saved to: {filePath}");
    }

#if UNITY_WEBGL
    private void DownloadFileInBrowser(string csvData, string fileName)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(csvData);
        string encodedData = System.Convert.ToBase64String(bytes);
        DownloadFile(fileName, encodedData);
    }

    [DllImport("__Internal")]
    private static extern void DownloadFile(string fileName, string data);
#endif

#if UNITY_ANDROID
    private void StartAndroidFileDownload(string filePath, string fileName)
    {
        AndroidJavaClass intentClass = new AndroidJavaClass("android.content.Intent");
        AndroidJavaObject intentObject = new AndroidJavaObject("android.content.Intent");

        intentObject.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_VIEW"));
        AndroidJavaClass uriClass = new AndroidJavaClass("android.net.Uri");
        AndroidJavaObject uriObject = uriClass.CallStatic<AndroidJavaObject>("parse", "file://" + filePath);
        intentObject.Call<AndroidJavaObject>("setDataAndType", uriObject, "text/csv");
        intentObject.Call<AndroidJavaObject>("addFlags", intentClass.GetStatic<int>("FLAG_GRANT_READ_URI_PERMISSION"));

        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        currentActivity.Call("startActivity", intentObject);
    }
#endif

    private Task<string> GetAsyncWrapper(string url)
    {
        var tcs = new TaskCompletionSource<string>();
        StartCoroutine(httpClient.GetAsync(url, result => tcs.SetResult(result)));
        return tcs.Task;
    }

    [System.Serializable]
    private class AdminData
    {
        public int id;
        public string drinkCategory;
        public string spirit1;
        public string spirit2;
        public string spirit3;
        public string spirit4;
        public string spirit5;
        public string passcodeKey;
    }

    [System.Serializable]
    private class AdminDataList
    {
        public List<AdminData> items;
    }

    [System.Serializable]
    private class FlavourWheelData
    {
        public int id;
        public string username;
        public string email;
        public string spirit1Name;
        public string spirit2Name;
        public string spirit3Name;
        public string spirit4Name;
        public string spirit5Name;
        public int spirit1Flavours;
        public int spirit2Flavours;
        public int spirit3Flavours;
        public int spirit4Flavours;
        public int spirit5Flavours;
        public int spirit1Ratings;
        public int spirit2Ratings;
        public int spirit3Ratings;
        public int spirit4Ratings;
        public int spirit5Ratings;
        public string feedback;
        public int overallRating;
    }

    [System.Serializable]
    private class FlavourWheelDataList
    {
        public List<FlavourWheelData> items;
    }
}