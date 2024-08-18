using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class DataManager : MonoBehaviour
{
    // This class represents a spirit, including its name, selected flavors, and rating.
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

    // A dictionary to store the spirit data, keyed by the spirit's name.
    public Dictionary<string, SpiritInfo> spiritData = new Dictionary<string, SpiritInfo>();

    // A list to store the names of the spirits.
    public List<string> spiritNames = new List<string>();

    // The endpoint for the API that handles player data.
    private const string playerEndpoint = "https://flavour-wheel-server.onrender.com/api/flavourwheel";

    // Fetches player data from the server and returns it via callbacks.
    public IEnumerator FetchPlayerData(System.Action<PlayerDataList> onSuccess, System.Action<string> onError)
    {
        Debug.Log("Fetching player data...");
        using (UnityWebRequest request = UnityWebRequest.Get(playerEndpoint))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log($"Received JSON: {json}");
                PlayerDataList playerDataList = JsonUtility.FromJson<PlayerDataList>($"{{\"items\":{json}}}");
                if (playerDataList != null && playerDataList.items != null)
                {
                    Debug.Log($"Successfully parsed {playerDataList.items.Count} player(s)");
                    onSuccess?.Invoke(playerDataList);
                }
                else
                {
                    Debug.LogError("Failed to parse player data from server response.");
                    onError?.Invoke("Failed to parse player data from server response.");
                }
            }
            else
            {
                Debug.LogError($"Error fetching player data: {request.error}");
                onError?.Invoke(request.error);
            }
        }
    }

    // Saves updated player data to the server and then fetches the updated data.
    public IEnumerator SaveAndFetchUpdatedData(PlayerData updatedData, System.Action<PlayerDataList> onSuccess, System.Action<string> onError)
    {
        string jsonData = JsonUtility.ToJson(updatedData);

        using (UnityWebRequest postRequest = new UnityWebRequest(playerEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            postRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            postRequest.downloadHandler = new DownloadHandlerBuffer();
            postRequest.SetRequestHeader("Content-Type", "application/json");

            yield return postRequest.SendWebRequest();

            if (postRequest.result == UnityWebRequest.Result.Success)
            {
                // If the save is successful, fetch the updated data.
                yield return FetchPlayerData(onSuccess, onError);
            }
            else
            {
                // Call the onError callback if the save request fails.
                onError?.Invoke(postRequest.error);
            }
        }
    }
    public IEnumerator CheckAndUpdatePlayerData(string username, string email, int overallRating, string feedback, System.Action<PlayerDataList> onSuccess, System.Action<string> onError)
    {
        Debug.Log("Checking and updating player data...");

        // First, fetch the current data
        yield return StartCoroutine(FetchPlayerData(
            playerDataList =>
            {
                PlayerData playerData = FindPlayerByUsername(playerDataList, username);

                if (playerData == null)
                {
                    // Player doesn't exist, create new data
                    playerData = new PlayerData
                    {
                        username = username,
                        email = email,
                        overallRating = overallRating,
                        feedback = feedback
                        // Initialize other fields as necessary
                    };
                    playerDataList.items.Add(playerData);
                }
                else
                {
                    // Player exists, update data
                    playerData.email = email;
                    playerData.overallRating = overallRating;
                    playerData.feedback = feedback;
                    // Update other fields as necessary
                }

                // Now save the updated data
                StartCoroutine(SavePlayerData(playerDataList, onSuccess, onError));
            },
            error =>
            {
                Debug.LogError($"Error fetching player data: {error}");
                onError?.Invoke(error);
            }
        ));
    }

    private IEnumerator SavePlayerData(PlayerDataList playerDataList, System.Action<PlayerDataList> onSuccess, System.Action<string> onError)
    {
        string jsonData = JsonUtility.ToJson(playerDataList);

        using (UnityWebRequest postRequest = new UnityWebRequest(playerEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            postRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
            postRequest.downloadHandler = new DownloadHandlerBuffer();
            postRequest.SetRequestHeader("Content-Type", "application/json");

            yield return postRequest.SendWebRequest();

            if (postRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Player data saved successfully");
                onSuccess?.Invoke(playerDataList);
            }
            else
            {
                Debug.LogError($"Error saving player data: {postRequest.error}");
                onError?.Invoke(postRequest.error);
            }
        }
    }

    // Finds a player by their username in the player data list.
    public PlayerData FindPlayerByUsername(PlayerDataList playerDataList, string username)
    {
        return playerDataList.items.FirstOrDefault(p => p.username == username);
    }

    // Updates the existing player data with new information.
    public void UpdateExistingPlayerData(PlayerData player, string username, string email, int overallRating, string feedback)
    {
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

    // Generates a new PlayerData object based on the current state of the game.
    public PlayerData GeneratePlayerData(string username, string email, int overallRating, string feedback)
    {
        return new PlayerData
        {
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

    // Retrieves and sorts the spirits based on their rating and flavor.
    public List<SpiritInfo> GetSortedSpirits()
    {
        List<SpiritInfo> sortedSpirits = spiritData.Values.ToList();
        sortedSpirits.Sort((a, b) => (b.Rating * b.SelectedFlavors).CompareTo(a.Rating * a.SelectedFlavors));
        return sortedSpirits;
    }

    // Retrieves the spirit name at a specific index from the spiritNames list.
    public string GetSpiritName(int index)
    {
        return spiritNames.Count > index ? spiritNames[index] : "";
    }

    // Retrieves the flavor count for a spirit at a specific index from the spiritData dictionary.
    public int GetSpiritFlavor(int index)
    {
        return spiritNames.Count > index && spiritData.ContainsKey(spiritNames[index])
            ? spiritData[spiritNames[index]].SelectedFlavors
            : 0;
    }

    // Retrieves the rating for a spirit at a specific index from the spiritData dictionary.
    public int GetSpiritRating(int index)
    {
        return spiritNames.Count > index && spiritData.ContainsKey(spiritNames[index])
            ? spiritData[spiritNames[index]].Rating
            : 0;
    }

    // Updates the top three spirits based on rating and flavor selection and updates the UI through UIManager.
    public void UpdateTopThreeSpirits(UIManager uiManager)
    {
        List<SpiritInfo> sortedSpirits = spiritData.Values.ToList();
        sortedSpirits.Sort((a, b) => (b.Rating * b.SelectedFlavors).CompareTo(a.Rating * a.SelectedFlavors));

        // Updates the local and general top three spirits in the UIManager.
        uiManager.UpdateLocalTopThree(sortedSpirits);
        uiManager.UpdateGeneralTopThree(sortedSpirits);
    }

    // Updates the spirit names list from server data for the current player.
    public void UpdateSpiritNamesListFromServer(List<PlayerData> players, string currentUsername)
    {
        PlayerData currentPlayer = players.FirstOrDefault(p => p.username == currentUsername);
        if (currentPlayer != null)
        {
            // Clears the existing spirit names and adds the new names from the server data.
            spiritNames.Clear();
            AddSpiritName(currentPlayer.spirit1Name);
            AddSpiritName(currentPlayer.spirit2Name);
            AddSpiritName(currentPlayer.spirit3Name);
            AddSpiritName(currentPlayer.spirit4Name);
            AddSpiritName(currentPlayer.spirit5Name);
            UpdateSpiritData();
        }
    }

    // Adds a spirit name to the spiritNames list if it is not null or empty.
    private void AddSpiritName(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            spiritNames.Add(name);
        }
    }

    // Updates the spiritData dictionary to include only the spirits that are in the spiritNames list.
    private void UpdateSpiritData()
    {
        foreach (string spiritName in spiritNames)
        {
            if (!spiritData.ContainsKey(spiritName))
            {
                spiritData[spiritName] = new SpiritInfo(spiritName);
            }
        }

        // Remove any spirits from the dictionary that are no longer in the spiritNames list.
        List<string> keysToRemove = spiritData.Keys.Where(k => !spiritNames.Contains(k)).ToList();
        foreach (string key in keysToRemove)
        {
            spiritData.Remove(key);
        }
    }

    // Classes to represent the structure of player data and the list of player data items.
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
