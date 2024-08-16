using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine.UI;

public class FlavourWheelAPI : MonoBehaviour
{
    private const string apiUrl = "https://27fd-102-88-81-120.ngrok-free.app/api/flavourwheel";

    [SerializeField]
    private TextMeshProUGUI resultText;

    [SerializeField]
    private Button triggerButton;

    public List<FlavourWheelData> flavourWheelDataList = new List<FlavourWheelData>();

    [System.Serializable]
    public class FlavourWheelData
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

    private void Start()
    {
        if (triggerButton != null)
        {
            triggerButton.onClick.AddListener(OnButtonClick);
        }
        else
        {
            Debug.LogError("Button not assigned to FlavourWheelAPI script!");
        }
    }

    private void OnButtonClick()
    {
        StartCoroutine(PostRandomDataAndGetAll());
    }

    private IEnumerator PostRandomDataAndGetAll()
    {
        UpdateResultText("Processing...");

        // Post random data
        yield return StartCoroutine(PostRandomData());

        // Get all data and display it
        yield return StartCoroutine(GetAllData());
    }

    private IEnumerator PostRandomData()
    {
        FlavourWheelData randomData = GenerateRandomData();
        string jsonData = JsonUtility.ToJson(randomData);

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(apiUrl, jsonData))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error posting data: " + www.error);
            }
            else
            {
                Debug.Log("Successfully posted random data");
            }
        }
    }

    private IEnumerator GetAllData()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(apiUrl))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error getting data: " + www.error);
                UpdateResultText("Error getting data: " + www.error);
            }
            else
            {
                string jsonResult = www.downloadHandler.text;
                // Wrap the JSON array in an object for JsonUtility to parse
                jsonResult = "{\"items\":" + jsonResult + "}";
                FlavourWheelDataList dataList = JsonUtility.FromJson<FlavourWheelDataList>(jsonResult);

                // Clear the existing list and populate it with new data
                flavourWheelDataList.Clear();
                flavourWheelDataList.AddRange(dataList.items);

                // Process and display the retrieved data
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < flavourWheelDataList.Count; i++)
                {
                    var data = flavourWheelDataList[i];
                    sb.AppendLine($"Data {i + 1}:");
                    sb.AppendLine($"ID: {data.id}");
                    sb.AppendLine($"Username: {data.username}");
                    sb.AppendLine($"Email: {data.email}");
                    sb.AppendLine($"Spirit 1: {data.spirit1Name} (Flavours: {data.spirit1Flavours}, Rating: {data.spirit1Ratings})");
                    sb.AppendLine($"Spirit 2: {data.spirit2Name} (Flavours: {data.spirit2Flavours}, Rating: {data.spirit2Ratings})");
                    sb.AppendLine($"Spirit 3: {data.spirit3Name} (Flavours: {data.spirit3Flavours}, Rating: {data.spirit3Ratings})");
                    sb.AppendLine($"Spirit 4: {data.spirit4Name} (Flavours: {data.spirit4Flavours}, Rating: {data.spirit4Ratings})");
                    sb.AppendLine($"Spirit 5: {data.spirit5Name} (Flavours: {data.spirit5Flavours}, Rating: {data.spirit5Ratings})");
                    sb.AppendLine($"Feedback: {data.feedback}");
                    sb.AppendLine($"Overall Rating: {data.overallRating}");
                    sb.AppendLine("--------------------");
                }
                UpdateResultText(sb.ToString());
            }
        }
    }

    private FlavourWheelData GenerateRandomData()
    {
        return new FlavourWheelData
        {
            username = "user" + Random.Range(1, 1000),
            email = "user" + Random.Range(1, 1000) + "@example.com",
            spirit1Name = "spirit" + Random.Range(1, 6),
            spirit2Name = "spirit" + Random.Range(1, 6),
            spirit3Name = "spirit" + Random.Range(1, 6),
            spirit4Name = "spirit" + Random.Range(1, 6),
            spirit5Name = "spirit" + Random.Range(1, 6),
            spirit1Flavours = Random.Range(0, 5),
            spirit2Flavours = Random.Range(0, 5),
            spirit3Flavours = Random.Range(0, 5),
            spirit4Flavours = Random.Range(0, 5),
            spirit5Flavours = Random.Range(0, 5),
            spirit1Ratings = Random.Range(0, 5),
            spirit2Ratings = Random.Range(0, 5),
            spirit3Ratings = Random.Range(0, 5),
            spirit4Ratings = Random.Range(0, 5),
            spirit5Ratings = Random.Range(0, 5),
            feedback = "Random feedback " + Random.Range(1, 100),
            overallRating = Random.Range(0, 5)
        };
    }

    private void UpdateResultText(string text)
    {
        if (resultText != null)
        {
            resultText.text = text;
        }
        else
        {
            Debug.LogWarning("TextMeshProUGUI component is not assigned!");
        }
    }
}