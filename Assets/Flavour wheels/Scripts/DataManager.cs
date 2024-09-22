using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public class SpiritInfo
    {
        public string Name;
        public int SelectedFlavors, Rating;
        public SpiritInfo(string name, int selectedFlavors = 0, int rating = 0) =>
            (Name, SelectedFlavors, Rating) = (name, selectedFlavors, rating);
    }

    public Dictionary<string, SpiritInfo> spiritData = new Dictionary<string, SpiritInfo>();
    public List<string> spiritNames = new List<string>();
    private const string flavourWheelEndpoint = "https://flavour-wheel-server.onrender.com/api/flavourwheel";
    private const string UserIdKey = "UserId";
    public string PlayerUsernameKey = "PlayerUsername";
    private List<string> orderedSpiritNames = new List<string>();
    private Dictionary<string, SpiritInfo> orderedSpiritData = new Dictionary<string, SpiritInfo>();
    private WebGLHttpClient httpClient;

    private void Start()
    {
        httpClient = gameObject.AddComponent<WebGLHttpClient>();
    }

    public IEnumerator SaveAndFetchUpdatedData(PlayerData updatedData, System.Action<PlayerDataList> onSuccess, System.Action<string> onError)
    {
        string username = PlayerPrefs.GetString(PlayerUsernameKey, "");
        if (string.IsNullOrEmpty(username))
        {
            onError?.Invoke("Username is not set. Please set a username before saving data.");
            yield break;
        }

        int userId = GetUserId();
        updatedData.id = userId;
        updatedData.username = username;
        string jsonData = JsonUtility.ToJson(updatedData);

        if (userId != 0)
        {
            // Existing user: update
            string url = $"{flavourWheelEndpoint}/{userId}";
            yield return UpdateExistingPlayer(url, jsonData, onSuccess, onError);
        }
        else
        {
            // New user: create
            yield return CreateNewPlayer(jsonData, onSuccess, onError);
        }
    }

    private IEnumerator UpdateExistingPlayer(string url, string jsonData, System.Action<PlayerDataList> onSuccess, System.Action<string> onError)
    {
        yield return httpClient.PutAsync(url, jsonData, response =>
        {
            if (response.StartsWith("Error:"))
            {
                Debug.LogError($"Error updating player: {response}");
                onError?.Invoke(response);
            }
            else
            {
                PlayerData updatedPlayer = JsonUtility.FromJson<PlayerData>(response);
                SaveUserId(updatedPlayer.id);
                StartCoroutine(FetchPlayerData(onSuccess, onError));
            }
        });
    }

    private IEnumerator CreateNewPlayer(string jsonData, System.Action<PlayerDataList> onSuccess, System.Action<string> onError)
    {
        yield return httpClient.PostAsync(flavourWheelEndpoint, jsonData, response =>
        {
            if (!string.IsNullOrEmpty(response) && !response.StartsWith("Error:"))
            {
                PlayerData createdPlayer = JsonUtility.FromJson<PlayerData>(response);
                if (createdPlayer != null && createdPlayer.id != 0)
                {
                    SaveUserId(createdPlayer.id);
                    StartCoroutine(FetchPlayerData(onSuccess, onError));
                }
                else
                {
                    Debug.LogError($"Failed to parse player data. Response: {response}");
                    onError?.Invoke("Failed to parse player data from server response");
                }
            }
            else
            {
                Debug.LogError($"Failed to create new player. Response: {response}");
                onError?.Invoke($"Failed to create new player: {response}");
            }
        });
    }
    public int GetUserId()
    {
        return PlayerPrefs.GetInt(UserIdKey, 0);
    }

    private void SaveUserId(int id)
    {
        PlayerPrefs.SetInt(UserIdKey, id);
        PlayerPrefs.Save();
    }

    public string GetUsername()
    {
        return PlayerPrefs.GetString(PlayerUsernameKey, "");
    }
    public IEnumerator FetchPlayerData(System.Action<PlayerDataList> onSuccess, System.Action<string> onError)
    {
        yield return httpClient.GetAsync(flavourWheelEndpoint, response =>
        {
            if (!string.IsNullOrEmpty(response) && !response.StartsWith("Error:"))
            {
                PlayerDataList playerDataList = JsonUtility.FromJson<PlayerDataList>($"{{\"items\":{response}}}");
                if (playerDataList?.items != null)
                    onSuccess?.Invoke(playerDataList);
                else
                    onError?.Invoke("Failed to parse player data from server response.");
            }
            else
            {
                onError?.Invoke($"Failed to fetch player data from server. {response}");
            }
        });
    }
    public IEnumerator DeletePlayer(int playerId, System.Action onSuccess, System.Action<string> onError)
    {
        string url = $"{flavourWheelEndpoint}/{playerId}";
        yield return httpClient.DeleteAsync(url, response =>
        {
            if (response != null && !response.StartsWith("Error:"))
            {
                onSuccess?.Invoke();
                PlayerPrefs.DeleteKey(UserIdKey);
                PlayerPrefs.Save();
            }
            else
            {
                string errorMessage = $"Failed to delete player with ID {playerId}. {response}";
                Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
            }
        });
    }
    public IEnumerator DeleteAllData(System.Action onSuccess, System.Action<string> onError)
    {
        yield return httpClient.DeleteAsync(flavourWheelEndpoint, response =>
        {
            if (response.StartsWith("Error:"))
            {
                Debug.LogError($"Failed to delete all data: {response}");
                onError?.Invoke(response);
            }
            else
            {
                onSuccess?.Invoke();
            }
        });
    }
    public PlayerData FindPlayerByUsername(PlayerDataList playerDataList, string username) =>
        playerDataList.items.FirstOrDefault(p => p.username == username);

    public void UpdateExistingPlayerData(PlayerData player, string username, string email, int overallRating, string feedback)
    {
        (player.username, player.email, player.overallRating, player.feedback) = (username, email, overallRating, feedback);
        for (int i = 0; i < 5; i++)
        {
            player.GetType().GetField($"spirit{i + 1}Name").SetValue(player, GetSpiritName(i));
            player.GetType().GetField($"spirit{i + 1}Flavours").SetValue(player, GetSpiritFlavor(i));
            player.GetType().GetField($"spirit{i + 1}Ratings").SetValue(player, GetSpiritRating(i));
        }
    }

    public PlayerData GeneratePlayerData(string username, string email, int overallRating, string feedback)
    {
        var player = new PlayerData
        {
            id = GetUserId(),
            username = username,
            email = email,
            overallRating = overallRating,
            feedback = feedback
        };

        for (int i = 0; i < 5; i++)
        {
            player.GetType().GetField($"spirit{i + 1}Name").SetValue(player, GetSpiritName(i));
            player.GetType().GetField($"spirit{i + 1}Flavours").SetValue(player, GetSpiritFlavor(i));
            player.GetType().GetField($"spirit{i + 1}Ratings").SetValue(player, GetSpiritRating(i));
        }

        return player;
    }

    public List<SpiritInfo> GetLocalSortedSpirits() =>
        spiritData.Values
            .OrderByDescending(s => s.Rating * s.SelectedFlavors)
            .Take(3)
            .ToList();

    public List<string> GetGeneralSortedSpirits(PlayerDataList playerDataList)
    {
        var spiritScores = new Dictionary<string, float>();
        foreach (var player in playerDataList.items)
        {
            for (int i = 1; i <= 5; i++)
            {
                UpdateSpiritScore(spiritScores,
                    (string)player.GetType().GetField($"spirit{i}Name").GetValue(player),
                    (int)player.GetType().GetField($"spirit{i}Ratings").GetValue(player) *
                    (int)player.GetType().GetField($"spirit{i}Flavours").GetValue(player));
            }
        }
        return spiritScores.OrderByDescending(pair => pair.Value)
                           .Take(3)
                           .Select(pair => pair.Key)
                           .ToList();
    }

    private void UpdateSpiritScore(Dictionary<string, float> spiritScores, string spiritName, float score)
    {
        if (!string.IsNullOrEmpty(spiritName))
            spiritScores[spiritName] = spiritScores.TryGetValue(spiritName, out float existingScore) ? existingScore + score : score;
    }

    public void ClearSpiritData()
    {
        orderedSpiritNames.Clear();
        orderedSpiritData.Clear();
    }

    public void AddSpiritData(string name, int selectedFlavors, int rating)
    {
        if (!orderedSpiritNames.Contains(name))
        {
            orderedSpiritNames.Add(name);
        }
        orderedSpiritData[name] = new SpiritInfo(name, selectedFlavors, rating);
    }

    public List<string> GetOrderedSpiritNames()
    {
        return new List<string>(orderedSpiritNames);
    }

    public Dictionary<string, SpiritInfo> GetOrderedSpiritData()
    {
        return new Dictionary<string, SpiritInfo>(orderedSpiritData);
    }

    public string GetSpiritName(int index)
    {
        return orderedSpiritNames.Count > index ? orderedSpiritNames[index] : "";
    }

    public int GetSpiritFlavor(int index)
    {
        return orderedSpiritNames.Count > index && orderedSpiritData.ContainsKey(orderedSpiritNames[index])
            ? orderedSpiritData[orderedSpiritNames[index]].SelectedFlavors
            : 0;
    }

    public int GetSpiritRating(int index)
    {
        return orderedSpiritNames.Count > index && orderedSpiritData.ContainsKey(orderedSpiritNames[index])
            ? orderedSpiritData[orderedSpiritNames[index]].Rating
            : 0;
    }

    public void UpdateSpiritNamesListFromServer(List<PlayerData> players, string currentUsername)
    {
        var currentPlayer = players.FirstOrDefault(p => p.username == currentUsername);
        if (currentPlayer != null)
        {
            spiritNames = new List<string> { currentPlayer.spirit1Name, currentPlayer.spirit2Name, currentPlayer.spirit3Name, currentPlayer.spirit4Name, currentPlayer.spirit5Name }
                .Where(name => !string.IsNullOrEmpty(name))
                .ToList();
            UpdateSpiritData();
        }
    }

    private void UpdateSpiritData()
    {
        foreach (var spiritName in spiritNames.Where(name => !spiritData.ContainsKey(name)))
            spiritData[spiritName] = new SpiritInfo(spiritName);

        spiritData.Keys.Where(k => !spiritNames.Contains(k))
            .ToList()
            .ForEach(key => spiritData.Remove(key));
    }

    [System.Serializable]
    public class PlayerData
    {
        public int id;  // Change this from int? to int
        public string username;
        public string email;
        public string feedback = "";
        public string spirit1Name, spirit2Name, spirit3Name, spirit4Name, spirit5Name;
        public int spirit1Flavours, spirit2Flavours, spirit3Flavours, spirit4Flavours, spirit5Flavours;
        public int spirit1Ratings, spirit2Ratings, spirit3Ratings, spirit4Ratings, spirit5Ratings;
        public int overallRating;
    }

    [System.Serializable]
    public class PlayerDataList
    {
        public List<PlayerData> items;
    }
}