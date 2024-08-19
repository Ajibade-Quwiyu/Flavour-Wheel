using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class DataManager : MonoBehaviour
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

    public Dictionary<string, SpiritInfo> spiritData = new Dictionary<string, SpiritInfo>();
    public List<string> spiritNames = new List<string>();

    private const string playerEndpoint = "https://flavour-wheel-server.onrender.com/api/flavourwheel";
    private const string UserIdKey = "UserId";

    public IEnumerator FetchPlayerData(System.Action<PlayerDataList> onSuccess, System.Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(playerEndpoint))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                PlayerDataList playerDataList = JsonUtility.FromJson<PlayerDataList>($"{{\"items\":{json}}}");
                if (playerDataList != null && playerDataList.items != null)
                {
                    onSuccess?.Invoke(playerDataList);
                }
                else
                {
                    onError?.Invoke("Failed to parse player data from server response.");
                }
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }

    public IEnumerator SaveAndFetchUpdatedData(PlayerData updatedData, System.Action<PlayerDataList> onSuccess, System.Action<string> onError)
    {
        int userId = GetUserId();
        string jsonData = JsonUtility.ToJson(updatedData);
        string url = userId == 0 ? playerEndpoint : $"{playerEndpoint}/{userId}";
        string method = userId == 0 ? "POST" : "PUT";

        using (UnityWebRequest request = new UnityWebRequest(url, method))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                if (userId == 0)
                {
                    PlayerData createdPlayer = JsonUtility.FromJson<PlayerData>(request.downloadHandler.text);
                    SaveUserId(createdPlayer.id);
                }

                yield return FetchPlayerData(onSuccess, onError);
            }
            else if (request.responseCode == 404 && userId != 0)
            {
                SaveUserId(0);
                yield return SaveAndFetchUpdatedData(updatedData, onSuccess, onError);
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }

    public PlayerData FindPlayerByUsername(PlayerDataList playerDataList, string username)
    {
        return playerDataList.items.FirstOrDefault(p => p.username == username);
    }

    public void UpdateExistingPlayerData(PlayerData player, string username, string email, int overallRating, string feedback)
    {
        player.username = username;
        player.email = email;
        player.overallRating = overallRating;
        player.feedback = feedback;
        player.spirit1Name = GetSpiritName(0);
        player.spirit2Name = GetSpiritName(1);
        player.spirit3Name = GetSpiritName(2);
        player.spirit4Name = GetSpiritName(3);
        player.spirit5Name = GetSpiritName(4);
        player.spirit1Flavours = GetSpiritFlavor(0);
        player.spirit2Flavours = GetSpiritFlavor(1);
        player.spirit3Flavours = GetSpiritFlavor(2);
        player.spirit4Flavours = GetSpiritFlavor(3);
        player.spirit5Flavours = GetSpiritFlavor(4);
        player.spirit1Ratings = GetSpiritRating(0);
        player.spirit2Ratings = GetSpiritRating(1);
        player.spirit3Ratings = GetSpiritRating(2);
        player.spirit4Ratings = GetSpiritRating(3);
        player.spirit5Ratings = GetSpiritRating(4);
    }

    public PlayerData GeneratePlayerData(string username, string email, int overallRating, string feedback)
    {
        return new PlayerData
        {
            id = GetUserId(),
            username = username,
            email = email,
            overallRating = overallRating,
            feedback = feedback,
            spirit1Name = GetSpiritName(0),
            spirit2Name = GetSpiritName(1),
            spirit3Name = GetSpiritName(2),
            spirit4Name = GetSpiritName(3),
            spirit5Name = GetSpiritName(4),
            spirit1Flavours = GetSpiritFlavor(0),
            spirit2Flavours = GetSpiritFlavor(1),
            spirit3Flavours = GetSpiritFlavor(2),
            spirit4Flavours = GetSpiritFlavor(3),
            spirit5Flavours = GetSpiritFlavor(4),
            spirit1Ratings = GetSpiritRating(0),
            spirit2Ratings = GetSpiritRating(1),
            spirit3Ratings = GetSpiritRating(2),
            spirit4Ratings = GetSpiritRating(3),
            spirit5Ratings = GetSpiritRating(4)
        };
    }
 public List<SpiritInfo> GetLocalSortedSpirits()
    {
        List<SpiritInfo> sortedSpirits = spiritData.Values.ToList();
        sortedSpirits.Sort((a, b) => (b.Rating * b.SelectedFlavors).CompareTo(a.Rating * a.SelectedFlavors));
        return sortedSpirits.Take(3).ToList();
    }

    public List<string> GetGeneralSortedSpirits(PlayerDataList playerDataList)
    {
        var spiritScores = new Dictionary<string, float>
        {
            { playerDataList.items[0].spirit1Name, playerDataList.items[0].spirit1Ratings * playerDataList.items[0].spirit1Flavours },
            { playerDataList.items[0].spirit2Name, playerDataList.items[0].spirit2Ratings * playerDataList.items[0].spirit2Flavours },
            { playerDataList.items[0].spirit3Name, playerDataList.items[0].spirit3Ratings * playerDataList.items[0].spirit3Flavours },
            { playerDataList.items[0].spirit4Name, playerDataList.items[0].spirit4Ratings * playerDataList.items[0].spirit4Flavours },
            { playerDataList.items[0].spirit5Name, playerDataList.items[0].spirit5Ratings * playerDataList.items[0].spirit5Flavours }
        };

        return spiritScores.OrderByDescending(pair => pair.Value)
                           .Take(3)
                           .Select(pair => pair.Key)
                           .ToList();
    }
     // Add these methods to get average ratings and flavours
    public float GetAverageRating(int index)
    {
        // Implement this method to return the average rating for the spirit at the given index
        // This should be based on the data in FlavourTable2
        return 0f; // Placeholder return
    }

    public float GetAverageFlavour(int index)
    {
        // Implement this method to return the average flavour for the spirit at the given index
        // This should be based on the data in FlavourTable2
        return 0f; // Placeholder return
    }

    // ... (rest of the methods remain the same)

    // Call this method when you want to update the UI
     public void UpdateUIWithLocalSortedSpirits(UIManager uiManager)
    {
        var localSortedSpirits = GetLocalSortedSpirits();
        uiManager.UpdateLocalTopThree(localSortedSpirits);
    }

    public string GetSpiritName(int index)
    {
        return spiritNames.Count > index ? spiritNames[index] : "";
    }

    public int GetSpiritFlavor(int index)
    {
        return spiritNames.Count > index && spiritData.ContainsKey(spiritNames[index])
            ? spiritData[spiritNames[index]].SelectedFlavors
            : 0;
    }

    public int GetSpiritRating(int index)
    {
        return spiritNames.Count > index && spiritData.ContainsKey(spiritNames[index])
            ? spiritData[spiritNames[index]].Rating
            : 0;
    }

    public void UpdateSpiritNamesListFromServer(List<PlayerData> players, string currentUsername)
    {
        PlayerData currentPlayer = players.FirstOrDefault(p => p.username == currentUsername);
        if (currentPlayer != null)
        {
            spiritNames.Clear();
            AddSpiritName(currentPlayer.spirit1Name);
            AddSpiritName(currentPlayer.spirit2Name);
            AddSpiritName(currentPlayer.spirit3Name);
            AddSpiritName(currentPlayer.spirit4Name);
            AddSpiritName(currentPlayer.spirit5Name);
            UpdateSpiritData();
        }
    }

    private void AddSpiritName(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            spiritNames.Add(name);
        }
    }

    private void UpdateSpiritData()
    {
        foreach (string spiritName in spiritNames)
        {
            if (!spiritData.ContainsKey(spiritName))
            {
                spiritData[spiritName] = new SpiritInfo(spiritName);
            }
        }

        List<string> keysToRemove = spiritData.Keys.Where(k => !spiritNames.Contains(k)).ToList();
        foreach (string key in keysToRemove)
        {
            spiritData.Remove(key);
        }
    }

    private int GetUserId()
    {
        return PlayerPrefs.GetInt(UserIdKey, 0);
    }

    private void SaveUserId(int id)
    {
        PlayerPrefs.SetInt(UserIdKey, id);
        PlayerPrefs.Save();
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