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
        public int SelectedFlavors, Rating;
        public SpiritInfo(string name, int selectedFlavors = 0, int rating = 0) =>
            (Name, SelectedFlavors, Rating) = (name, selectedFlavors, rating);
    }

    public Dictionary<string, SpiritInfo> spiritData = new Dictionary<string, SpiritInfo>();
    public List<string> spiritNames = new List<string>();
    private const string playerEndpoint = "https://flavour-wheel-server.onrender.com/api/flavourwheel", UserIdKey = "UserId";


    public IEnumerator FetchPlayerData(System.Action<PlayerDataList> onSuccess, System.Action<string> onError)
    {
        using UnityWebRequest request = UnityWebRequest.Get(playerEndpoint);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            PlayerDataList playerDataList = JsonUtility.FromJson<PlayerDataList>($"{{\"items\":{request.downloadHandler.text}}}");
            if (playerDataList?.items != null)
                onSuccess?.Invoke(playerDataList);
            else
                onError?.Invoke("Failed to parse player data from server response.");
        }
        else
            onError?.Invoke(request.error);
    }
    public IEnumerator SaveAndFetchUpdatedData(PlayerData updatedData, System.Action<PlayerDataList> onSuccess, System.Action<string> onError)
    {
        int userId = GetUserId();
        string url = userId == 0 ? playerEndpoint : $"{playerEndpoint}/{userId}";
        using UnityWebRequest request = new UnityWebRequest(url, userId == 0 ? "POST" : "PUT");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(updatedData)));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            if (userId == 0)
                SaveUserId(JsonUtility.FromJson<PlayerData>(request.downloadHandler.text).id);
            yield return FetchPlayerData(onSuccess, onError);
        }
        else if (request.responseCode == 404 && userId != 0)
        {
            SaveUserId(0);
            yield return SaveAndFetchUpdatedData(updatedData, onSuccess, onError);
        }
        else
            onError?.Invoke(request.error);
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

    private int GetUserId() => PlayerPrefs.GetInt(UserIdKey, 0);

    private void SaveUserId(int id)
    {
        PlayerPrefs.SetInt(UserIdKey, id);
        PlayerPrefs.Save();
    }

    [System.Serializable]
    public class PlayerData
    {
        public int id;
        public string username, email, feedback = "Great";
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