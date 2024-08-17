using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Collections;

public class SpiritManager : MonoBehaviour
{
    public class SpiritInfo
    {
        public string Name;
        public int SelectedFlavors;
        public int Rating;

        public SpiritInfo(string name, int selectedFlavors = 0, int rating = 0)
        {
            Name = name;
            SelectedFlavors = selectedFlavors;
            Rating = rating;
        }
    }

    public Transform localTopThree;
    public TMP_Text usernameText;
    public List<Transform> SpiritNamesList;

    public Transform FlavourTable1;
    public Transform FlavourTable2;
    public Transform generalTopThree;

    public GameObject flavourRowPrefab;
    public Transform fetching; // Placeholder prefab

    private Dictionary<string, SpiritInfo> spiritData = new Dictionary<string, SpiritInfo>();
    private string username;
    private string email;
    private int overallRating;
    private string feedback;

    private List<string> spiritNames = new List<string>();
    private bool isUpdating = false;
    private bool isDataLoaded = false; // Track if data has been loaded

    private const string UsernameKey = "Username";
    private const string EmailKey = "Email";

    private string playerEndpoint = "https://flavour-wheel-server.onrender.com/api/flavourwheel";

    public void ReceiveSpiritData(string spiritName, int selectedFlavors, int rating)
    {
        Debug.Log($"ReceiveSpiritData called with: spiritName={spiritName}, selectedFlavors={selectedFlavors}, rating={rating}");

        if (spiritData.ContainsKey(spiritName))
        {
            spiritData[spiritName].SelectedFlavors = selectedFlavors;
            spiritData[spiritName].Rating = rating;
        }
        else
        {
            spiritData.Add(spiritName, new SpiritInfo(spiritName, selectedFlavors, rating));
        }

        UpdateTopThreeSpirits();
        UpdateFlavourTable2LocalData();
        UpdateFlavourTable1();
    }

    private void UpdateFlavourTable1()
    {
        // Clear existing children
        foreach (Transform child in FlavourTable1)
        {
            Destroy(child.gameObject);
        }

        int index = 0;
        foreach (var spirit in spiritData)
        {
            GameObject row = Instantiate(flavourRowPrefab, FlavourTable1);
            row.transform.GetChild(0).GetComponent<TMP_Text>().text = (index + 1).ToString();
            row.transform.GetChild(1).GetComponent<TMP_Text>().text = spirit.Key;

            int[] flavours = new int[]
            {
                spirit.Value.SelectedFlavors,
            };

            for (int i = 0; i < flavours.Length; i++)
            {
                Transform flavourTransform = row.transform.GetChild(2 + i);
                flavourTransform.GetComponent<Image>().color = GetFlavorColor(flavours[i]);
                flavourTransform.GetChild(0).GetComponent<TMP_Text>().text = flavours[i].ToString();
            }

            Transform starRating = row.transform.GetChild(7);
            for (int i = 0; i < 5; i++)
            {
                starRating.GetChild(i).gameObject.SetActive(i < spirit.Value.Rating);
            }

            index++;
        }
    }

    public void SetUserData(string username, string email, int overallRating, string feedback)
    {
        this.username = string.IsNullOrEmpty(username) ? PlayerPrefs.GetString(UsernameKey, "DefaultUsername") : username;
        this.email = string.IsNullOrEmpty(email) ? PlayerPrefs.GetString(EmailKey, "DefaultEmail@example.com") : email;
        this.overallRating = overallRating == 0 ? 0 : overallRating;
        this.feedback = string.IsNullOrEmpty(feedback) ? "Great" : feedback;
    }

    private void UpdateTopThreeSpirits()
    {
        List<SpiritInfo> sortedSpirits = new List<SpiritInfo>(spiritData.Values);
        sortedSpirits.Sort((a, b) => b.Rating.CompareTo(a.Rating));

        for (int i = 0; i < localTopThree.childCount && i < sortedSpirits.Count; i++)
        {
            localTopThree.GetChild(i).GetComponent<Text>().text = sortedSpirits[i].Name;
        }

        for (int i = sortedSpirits.Count; i < localTopThree.childCount; i++)
        {
            localTopThree.GetChild(i).GetComponent<Text>().text = string.Empty;
        }

        UpdateSpiritNamesList();
    }

    private void UpdateSpiritNamesList()
    {
        string[] spiritNamesArray = new string[5];
        for (int i = 0; i < 5; i++)
        {
            spiritNamesArray[i] = "";
        }

        int spiritIndex = 0;
        foreach (var spirit in spiritData.Values)
        {
            if (spiritIndex < 5)
            {
                spiritNamesArray[spiritIndex] = spirit.Name;
                spiritIndex++;
            }
        }

        foreach (var transform in SpiritNamesList)
        {
            TMP_Text[] textComponents = transform.GetComponentsInChildren<TMP_Text>();
            for (int i = 0; i < textComponents.Length; i++)
            {
                if (i < spiritNamesArray.Length)
                {
                    textComponents[i].text = spiritNamesArray[i];
                }
                else
                {
                    textComponents[i].text = string.Empty;
                }
            }
        }

        spiritNames = new List<string>(spiritNamesArray);
    }

    private void UpdateFlavourTable2LocalData()
    {
        Transform localRatingsTransform = FlavourTable2.GetChild(0);
        Transform localFlavoursTransform = FlavourTable2.GetChild(2);

        int[] spiritRatings = new int[5];
        int[] spiritFlavours = new int[5];
        int spiritIndex = 0;

        foreach (var spirit in spiritData.Values)
        {
            if (spiritIndex < 5)
            {
                spiritRatings[spiritIndex] = spirit.Rating;
                spiritFlavours[spiritIndex] = spirit.SelectedFlavors;
                spiritIndex++;
            }
        }

        for (int i = 0; i < spiritRatings.Length; i++)
        {
            localRatingsTransform.GetChild(i).GetComponent<TMP_Text>().text = spiritRatings[i].ToString();
            localFlavoursTransform.GetChild(i).GetComponent<TMP_Text>().text = spiritFlavours[i].ToString();
        }
    }

    private void UpdateFlavourTable2AverageData(decimal[] averageRatings, decimal[] averageFlavours)
    {
        Transform averageRatingsTransform = FlavourTable2.GetChild(1);
        Transform averageFlavoursTransform = FlavourTable2.GetChild(3);
        Transform multipliedTransform = FlavourTable2.GetChild(4);

        for (int i = 0; i < averageRatings.Length; i++)
        {
            averageRatingsTransform.GetChild(i).GetComponent<TMP_Text>().text = averageRatings[i].ToString("F2");
            averageFlavoursTransform.GetChild(i).GetComponent<TMP_Text>().text = averageFlavours[i].ToString("F2");
            multipliedTransform.GetChild(i).GetComponent<TMP_Text>().text = (averageRatings[i] * averageFlavours[i]).ToString("F2");
        }

        UpdateFlavourTable2Ranks();
        UpdateGeneralTopThree();
    }

    private void UpdateFlavourTable2Ranks()
    {
        Transform multipliedTransform = FlavourTable2.GetChild(4);
        Transform ranksTransform = FlavourTable2.GetChild(5);

        decimal[] multipliedValues = new decimal[5];
        for (int i = 0; i < multipliedValues.Length; i++)
        {
            multipliedValues[i] = decimal.Parse(multipliedTransform.GetChild(i).GetComponent<TMP_Text>().text);
        }

        decimal[] sortedValues = multipliedValues.OrderByDescending(v => v).ToArray();

        for (int i = 0; i < multipliedValues.Length; i++)
        {
            int rank = Array.IndexOf(sortedValues, multipliedValues[i]) + 1;
            ranksTransform.GetChild(i).GetComponent<TMP_Text>().text = GetRankString(rank);
        }
    }

    private void UpdateGeneralTopThree()
    {
        Text[] generalTopThreeTexts = new Text[3];

        // Loop through the first three children and get their Text components
        for (int i = 0; i < 3; i++)
        {
            generalTopThreeTexts[i] = generalTopThree.GetChild(i).GetComponent<Text>();
        }

        // Iterate over the ranksTransform to set the corresponding spirit names
        for (int i = 0; i < generalTopThreeTexts.Length; i++)
        {
            string rankText = FlavourTable2.GetChild(5).GetChild(i).GetComponent<TMP_Text>().text;

            // Assign spirit names based on the rank
            if (rankText == "1st" && i < spiritNames.Count)
            {
                generalTopThreeTexts[0].text = spiritNames[i];
            }
            else if (rankText == "2nd" && i < spiritNames.Count)
            {
                generalTopThreeTexts[1].text = spiritNames[i];
            }
            else if (rankText == "3rd" && i < spiritNames.Count)
            {
                generalTopThreeTexts[2].text = spiritNames[i];
            }
            else
            {
                // Handle the case where there are not enough spirits in the list
                if (i < 3)
                {
                    generalTopThreeTexts[i].text = string.Empty;  // Clear the text for missing spirits
                }
            }
        }
    }


    private void ShowFetchingPlaceholders()
    {
        if (!isDataLoaded)
        {
            // Clear existing children
            foreach (Transform child in FlavourTable1)
            {
                Destroy(child.gameObject);
            }

            // Instantiate fetching placeholders
            for (int i = 0; i < 5; i++)
            {
                Instantiate(fetching, FlavourTable1);
            }
        }
    }

    private void HideFetchingPlaceholders()
    {
        if (!isDataLoaded)
        {
            foreach (Transform child in FlavourTable1)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private string GetRankString(int rank)
    {
        switch (rank)
        {
            case 1: return "1st";
            case 2: return "2nd";
            case 3: return "3rd";
            case 4: return "4th";
            case 5: return "5th";
            default: return rank.ToString();
        }
    }

    private Color GetFlavorColor(int flavor)
    {
        float t = Mathf.Clamp01((float)flavor / 7f);
        return Color.Lerp(Color.yellow, Color.green, t);
    }

    public void SaveDataToPlayerDataTable()
    {
        Debug.Log($"Saving data to PlayerDataTable: username={username}, email={email}, overallRating={overallRating}, feedback={feedback}");

        foreach (var spirit in spiritData.Values)
        {
            Debug.Log($"Spirit Name: {spirit.Name}, Selected Flavors: {spirit.SelectedFlavors}, Rating: {spirit.Rating}");
        }

        // Ensure the spiritNames list has enough elements
        string spirit1Name = spiritNames.Count > 0 ? spiritNames[0] : "";
        string spirit2Name = spiritNames.Count > 1 ? spiritNames[1] : "";
        string spirit3Name = spiritNames.Count > 2 ? spiritNames[2] : "";
        string spirit4Name = spiritNames.Count > 3 ? spiritNames[3] : "";
        string spirit5Name = spiritNames.Count > 4 ? spiritNames[4] : "";

        var data = new PlayerData
        {
            id = 0,
            username = this.username,
            email = this.email,
            overallRating = this.overallRating,
            feedback = this.feedback,
            spirit1Name = spirit1Name,
            spirit2Name = spirit2Name,
            spirit3Name = spirit3Name,
            spirit4Name = spirit4Name,
            spirit5Name = spirit5Name,
            spirit1Flavours = spiritData.ContainsKey(spirit1Name) ? spiritData[spirit1Name].SelectedFlavors : 0,
            spirit2Flavours = spiritData.ContainsKey(spirit2Name) ? spiritData[spirit2Name].SelectedFlavors : 0,
            spirit3Flavours = spiritData.ContainsKey(spirit3Name) ? spiritData[spirit3Name].SelectedFlavors : 0,
            spirit4Flavours = spiritData.ContainsKey(spirit4Name) ? spiritData[spirit4Name].SelectedFlavors : 0,
            spirit5Flavours = spiritData.ContainsKey(spirit5Name) ? spiritData[spirit5Name].SelectedFlavors : 0,
            spirit1Ratings = spiritData.ContainsKey(spirit1Name) ? spiritData[spirit1Name].Rating : 0,
            spirit2Ratings = spiritData.ContainsKey(spirit2Name) ? spiritData[spirit2Name].Rating : 0,
            spirit3Ratings = spiritData.ContainsKey(spirit3Name) ? spiritData[spirit3Name].Rating : 0,
            spirit4Ratings = spiritData.ContainsKey(spirit4Name) ? spiritData[spirit4Name].Rating : 0,
            spirit5Ratings = spiritData.ContainsKey(spirit5Name) ? spiritData[spirit5Name].Rating : 0
        };

        string jsonData = JsonUtility.ToJson(data);

        StartCoroutine(PostPlayerDataWithCheck(jsonData));
    }

    private IEnumerator PostPlayerDataWithCheck(string jsonData)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(playerEndpoint))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                List<PlayerData> players = JsonUtility.FromJson<PlayerDataList>($"{{\"items\": {json}}}").items;

                PlayerData existingPlayer = players.FirstOrDefault(p => p.username == username);

                if (existingPlayer != null)
                {
                    // Delete existing data
                    using (UnityWebRequest deleteRequest = UnityWebRequest.Delete($"{playerEndpoint}/{existingPlayer.id}"))
                    {
                        yield return deleteRequest.SendWebRequest();

                        if (deleteRequest.result == UnityWebRequest.Result.Success)
                        {
                            Debug.Log("Existing data deleted successfully.");
                        }
                        else
                        {
                            Debug.LogError($"Error deleting existing data: {deleteRequest.error}");
                        }
                    }
                }

                // Post new data
                yield return PostPlayerData(jsonData);
            }
            else
            {
                Debug.LogError($"Error fetching player data: {request.error}");
            }
        }
    }

    private IEnumerator PostPlayerData(string jsonData)
    {
        if (!isDataLoaded)
        {
            ShowFetchingPlaceholders(); // Show fetching placeholders before posting data if it's the initial load
        }

        using (UnityWebRequest request = new UnityWebRequest(playerEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Data successfully saved to the server.");
            }
            else
            {
                Debug.LogError($"Error saving data: {request.error}");
            }
        }
    }

    public void ClearPlayerDataTable()
    {
        StartCoroutine(DeleteAllPlayerData());
    }

    private IEnumerator DeleteAllPlayerData()
    {
        using (UnityWebRequest request = UnityWebRequest.Delete(playerEndpoint))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("All player data cleared successfully.");
            }
            else
            {
                Debug.LogError($"Error clearing player data: {request.error}");
            }
        }
    }

    public void TableDatas()
    {
        StartCoroutine(GetPlayerData());
    }

    private IEnumerator GetPlayerData()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(playerEndpoint))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                List<PlayerData> players = JsonUtility.FromJson<PlayerDataList>($"{{\"items\": {json}}}").items;

                ProcessAndDisplayPlayerData(players);
            }
            else
            {
                Debug.LogError($"Error loading table data: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                Debug.LogError($"Response: {request.downloadHandler.text}");
            }
        }
    }

    private void ProcessAndDisplayPlayerData(List<PlayerData> players)
    {
        int rowIndex = 0;
        decimal[] totalRatings = new decimal[5];
        decimal[] totalFlavours = new decimal[5];
        int recordCount = 0;

        foreach (Transform child in FlavourTable1)
        {
            Destroy(child.gameObject);
        }

        foreach (var player in players)
        {
            string username = player.username;
            int[] flavours = { player.spirit1Flavours, player.spirit2Flavours, player.spirit3Flavours, player.spirit4Flavours, player.spirit5Flavours };
            int overallRating = player.overallRating;

            GameObject row = Instantiate(flavourRowPrefab, FlavourTable1);
            if (row != null)
            {
                row.transform.GetChild(0).GetComponent<TMP_Text>().text = (rowIndex + 1).ToString();
                row.transform.GetChild(1).GetComponent<TMP_Text>().text = username;

                for (int i = 0; i < flavours.Length; i++)
                {
                    Transform flavourTransform = row.transform.GetChild(2 + i);
                    if (flavourTransform != null)
                    {
                        flavourTransform.GetComponent<Image>().color = GetFlavorColor(flavours[i]);
                        TMP_Text flavourText = flavourTransform.GetChild(0).GetComponent<TMP_Text>();
                        if (flavourText != null)
                        {
                            flavourText.text = flavours[i].ToString();
                        }
                    }
                }

                Transform starRating = row.transform.GetChild(7);
                if (starRating != null)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        starRating.GetChild(i).gameObject.SetActive(i < overallRating);
                    }
                }

                totalRatings[0] += player.spirit1Ratings;
                totalRatings[1] += player.spirit2Ratings;
                totalRatings[2] += player.spirit3Ratings;
                totalRatings[3] += player.spirit4Ratings;
                totalRatings[4] += player.spirit5Ratings;

                totalFlavours[0] += player.spirit1Flavours;
                totalFlavours[1] += player.spirit2Flavours;
                totalFlavours[2] += player.spirit3Flavours;
                totalFlavours[3] += player.spirit4Flavours;
                totalFlavours[4] += player.spirit5Flavours;

                recordCount++;
                rowIndex++;
            }
        }

        decimal[] averageRatings = new decimal[5];
        decimal[] averageFlavours = new decimal[5];
        if (recordCount > 0)
        {
            for (int i = 0; i < totalRatings.Length; i++)
            {
                averageRatings[i] = totalRatings[i] / recordCount;
                averageFlavours[i] = totalFlavours[i] / recordCount;
            }
        }

        UpdateFlavourTable2AverageData(averageRatings, averageFlavours);
        InvokeRepeating("TableDatas", 5f, 5f); // Update periodically every 5 seconds
    }

    public float GetLocalDataRating(int index)
    {
        Transform localRatingsTransform = FlavourTable2.GetChild(0);
        return float.Parse(localRatingsTransform.GetChild(index).GetComponent<TMP_Text>().text);
    }

    public float GetLocalDataFlavour(int index)
    {
        Transform localFlavoursTransform = FlavourTable2.GetChild(2);
        return float.Parse(localFlavoursTransform.GetChild(index).GetComponent<TMP_Text>().text);
    }

    public float GetAverageRating(int index)
    {
        Transform averageRatingsTransform = FlavourTable2.GetChild(1);
        return float.Parse(averageRatingsTransform.GetChild(index).GetComponent<TMP_Text>().text);
    }

    public float GetAverageFlavour(int index)
    {
        Transform averageFlavoursTransform = FlavourTable2.GetChild(3);
        return float.Parse(averageFlavoursTransform.GetChild(index).GetComponent<TMP_Text>().text);
    }

    [System.Serializable]
    public class PlayerData
    {
        public int id;
        public string username;
        public string email;
        public string spirit1Name;
        public string spirit2Name;
        public string spirit3Name;
        public string spirit4Name;
        public string spirit5Name;
        public int spirit1Flavours = 0;
        public int spirit2Flavours = 0;
        public int spirit3Flavours = 0;
        public int spirit4Flavours = 0;
        public int spirit5Flavours = 0;
        public int spirit1Ratings = 0;
        public int spirit2Ratings = 0;
        public int spirit3Ratings = 0;
        public int spirit4Ratings = 0;
        public int spirit5Ratings = 0;
        public string feedback = "Great";
        public int overallRating = 0;
    }

    [System.Serializable]
    public class PlayerDataList
    {
        public List<PlayerData> items;
    }
}
