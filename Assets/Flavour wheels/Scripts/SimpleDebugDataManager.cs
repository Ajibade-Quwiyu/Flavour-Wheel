using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class SimpleDebugDataManager : MonoBehaviour
{
    public TMP_Text debugText;
    private const string apiEndpoint = "https://flavour-wheel-server.onrender.com/api/flavourwheel";

    void Start()
    {
        StartCoroutine(DebugSequence());
    }

    private IEnumerator DebugSequence()
    {
        debugText.text = "Starting debug sequence...\n";

        // Generate and display random data
        GenerateAndDisplayRandomData();

        yield return new WaitForSeconds(2f);

        // Fetch data
        yield return StartCoroutine(FetchAndDisplayData());

        yield return new WaitForSeconds(2f);

        // Add or update test data
        yield return StartCoroutine(AddTestData());

        yield return new WaitForSeconds(2f);

        // Fetch data again
        yield return StartCoroutine(FetchAndDisplayData());

        debugText.text += "Debug sequence complete.\n";
    }

    private void GenerateAndDisplayRandomData()
    {
        debugText.text += "Generating random data:\n";

        List<PlayerData> randomPlayers = GenerateRandomPlayers(5);
        foreach (var player in randomPlayers)
        {
            debugText.text += $"Player: {player.Username}\n" +
                              $"Email: {player.Email}\n" +
                              $"Spirits: {player.Spirit1Name}, {player.Spirit2Name}, {player.Spirit3Name}, {player.Spirit4Name}, {player.Spirit5Name}\n" +
                              $"Flavours: {player.Spirit1Flavours}, {player.Spirit2Flavours}, {player.Spirit3Flavours}, {player.Spirit4Flavours}, {player.Spirit5Flavours}\n" +
                              $"Ratings: {player.Spirit1Ratings}, {player.Spirit2Ratings}, {player.Spirit3Ratings}, {player.Spirit4Ratings}, {player.Spirit5Ratings}\n" +
                              $"Feedback: {player.Feedback}\n" +
                              $"Overall Rating: {player.OverallRating}\n\n";
        }

        debugText.text += "Random data generation complete.\n\n";
    }

    private IEnumerator FetchAndDisplayData()
    {
        debugText.text += "Fetching data from API:\n";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(apiEndpoint))
        {
            yield return webRequest.SendWebRequest();

            debugText.text += $"Response Code: {webRequest.responseCode}\n";

            if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                debugText.text += $"Error: {webRequest.error}\n";
            }
            else
            {
                string jsonResult = webRequest.downloadHandler.text;
                debugText.text += $"Received data: {jsonResult}\n";

                PlayerData[] playerDataArray = JsonHelper.FromJson<PlayerData>(jsonResult);
                if (playerDataArray != null && playerDataArray.Length > 0)
                {
                    debugText.text += $"Parsed {playerDataArray.Length} players from API:\n";
                    foreach (var player in playerDataArray)
                    {
                        debugText.text += $"ID: {player.Id}\n" +
                                          $"Player: {player.Username}\n" +
                                          $"Email: {player.Email}\n" +
                                          $"Spirits: {player.Spirit1Name}, {player.Spirit2Name}, {player.Spirit3Name}, {player.Spirit4Name}, {player.Spirit5Name}\n" +
                                          $"Flavours: {player.Spirit1Flavours}, {player.Spirit2Flavours}, {player.Spirit3Flavours}, {player.Spirit4Flavours}, {player.Spirit5Flavours}\n" +
                                          $"Ratings: {player.Spirit1Ratings}, {player.Spirit2Ratings}, {player.Spirit3Ratings}, {player.Spirit4Ratings}, {player.Spirit5Ratings}\n" +
                                          $"Feedback: {player.Feedback}\n" +
                                          $"Overall Rating: {player.OverallRating}\n\n";
                    }
                }
                else
                {
                    debugText.text += "No players found in API response\n";
                }
            }
        }

        debugText.text += "API data fetch complete.\n\n";
    }

    private IEnumerator AddTestData()
    {
        debugText.text += "Adding or updating test data in API:\n";

        int userId = GetOrCreateUserId();

        PlayerData testPlayer = new PlayerData
        {
            Id = userId,
            Username = "TestUser" + UnityEngine.Random.Range(1000, 9999),
            Email = "test" + UnityEngine.Random.Range(1000, 9999) + "@example.com",
            Spirit1Name = "TestSpirit1",
            Spirit2Name = "TestSpirit2",
            Spirit3Name = "TestSpirit3",
            Spirit4Name = "TestSpirit4",
            Spirit5Name = "TestSpirit5",
            Spirit1Flavours = UnityEngine.Random.Range(1, 8),
            Spirit2Flavours = UnityEngine.Random.Range(1, 8),
            Spirit3Flavours = UnityEngine.Random.Range(1, 8),
            Spirit4Flavours = UnityEngine.Random.Range(1, 8),
            Spirit5Flavours = UnityEngine.Random.Range(1, 8),
            Spirit1Ratings = UnityEngine.Random.Range(1, 6),
            Spirit2Ratings = UnityEngine.Random.Range(1, 6),
            Spirit3Ratings = UnityEngine.Random.Range(1, 6),
            Spirit4Ratings = UnityEngine.Random.Range(1, 6),
            Spirit5Ratings = UnityEngine.Random.Range(1, 6),
            Feedback = "Test feedback",
            OverallRating = UnityEngine.Random.Range(1, 6)
        };

        string jsonData = JsonUtility.ToJson(testPlayer);
        debugText.text += $"Sending data: {jsonData}\n";

        // First, try to update the existing entry
        if (userId != 0)
        {
            using (UnityWebRequest updateRequest = UnityWebRequest.Put($"{apiEndpoint}/{userId}", jsonData))
            {
                updateRequest.SetRequestHeader("Content-Type", "application/json");
                yield return updateRequest.SendWebRequest();

                if (updateRequest.result == UnityWebRequest.Result.Success)
                {
                    debugText.text += "Test data updated successfully.\n";
                    debugText.text += "Test data update complete.\n\n";
                    yield break;  // Exit the coroutine if update was successful
                }
                else
                {
                    debugText.text += $"Update failed. Attempting to create new entry.\n";
                }
            }
        }

        // If update failed or userId was 0, create a new entry
        using (UnityWebRequest createRequest = UnityWebRequest.PostWwwForm(apiEndpoint, jsonData))
        {
            createRequest.SetRequestHeader("Content-Type", "application/json");
            yield return createRequest.SendWebRequest();

            debugText.text += $"POST Response Code: {createRequest.responseCode}\n";
            debugText.text += $"POST Result: {createRequest.downloadHandler.text}\n";

            if (createRequest.result == UnityWebRequest.Result.Success)
            {
                PlayerData createdPlayer = JsonUtility.FromJson<PlayerData>(createRequest.downloadHandler.text);
                PlayerPrefs.SetInt("UserId", createdPlayer.Id);
                PlayerPrefs.Save();
                debugText.text += $"New user ID saved: {createdPlayer.Id}\n";
                debugText.text += "Test data added successfully.\n";
            }
            else
            {
                debugText.text += $"POST Error: {createRequest.error}\n";
            }
        }

        debugText.text += "Test data addition/update complete.\n\n";
    }

    private int GetOrCreateUserId()
    {
        return PlayerPrefs.GetInt("UserId", 0);
    }

    private List<PlayerData> GenerateRandomPlayers(int count)
    {
        List<PlayerData> players = new List<PlayerData>();
        string[] spiritNames = { "Whiskey", "Vodka", "Gin", "Rum", "Tequila", "Brandy", "Mezcal", "Scotch", "Bourbon", "Cognac" };
        for (int i = 0; i < count; i++)
        {
            players.Add(new PlayerData
            {
                Username = $"Player{i}",
                Email = $"player{i}@example.com",
                Spirit1Name = spiritNames[UnityEngine.Random.Range(0, spiritNames.Length)],
                Spirit2Name = spiritNames[UnityEngine.Random.Range(0, spiritNames.Length)],
                Spirit3Name = spiritNames[UnityEngine.Random.Range(0, spiritNames.Length)],
                Spirit4Name = spiritNames[UnityEngine.Random.Range(0, spiritNames.Length)],
                Spirit5Name = spiritNames[UnityEngine.Random.Range(0, spiritNames.Length)],
                Spirit1Flavours = UnityEngine.Random.Range(1, 8),
                Spirit2Flavours = UnityEngine.Random.Range(1, 8),
                Spirit3Flavours = UnityEngine.Random.Range(1, 8),
                Spirit4Flavours = UnityEngine.Random.Range(1, 8),
                Spirit5Flavours = UnityEngine.Random.Range(1, 8),
                Spirit1Ratings = UnityEngine.Random.Range(1, 6),
                Spirit2Ratings = UnityEngine.Random.Range(1, 6),
                Spirit3Ratings = UnityEngine.Random.Range(1, 6),
                Spirit4Ratings = UnityEngine.Random.Range(1, 6),
                Spirit5Ratings = UnityEngine.Random.Range(1, 6),
                Feedback = "Random feedback",
                OverallRating = UnityEngine.Random.Range(1, 6)
            });
        }
        return players;
    }

    [Serializable]
    public class PlayerData
    {
        [SerializeField] private int id;
        [SerializeField] private string username;
        [SerializeField] private string email;
        [SerializeField] private string spirit1Name;
        [SerializeField] private string spirit2Name;
        [SerializeField] private string spirit3Name;
        [SerializeField] private string spirit4Name;
        [SerializeField] private string spirit5Name;
        [SerializeField] private int spirit1Flavours;
        [SerializeField] private int spirit2Flavours;
        [SerializeField] private int spirit3Flavours;
        [SerializeField] private int spirit4Flavours;
        [SerializeField] private int spirit5Flavours;
        [SerializeField] private int spirit1Ratings;
        [SerializeField] private int spirit2Ratings;
        [SerializeField] private int spirit3Ratings;
        [SerializeField] private int spirit4Ratings;
        [SerializeField] private int spirit5Ratings;
        [SerializeField] private string feedback;
        [SerializeField] private int overallRating;

        public int Id { get => id; set => id = value; }
        public string Username { get => username; set => username = value; }
        public string Email { get => email; set => email = value; }
        public string Spirit1Name { get => spirit1Name; set => spirit1Name = value; }
        public string Spirit2Name { get => spirit2Name; set => spirit2Name = value; }
        public string Spirit3Name { get => spirit3Name; set => spirit3Name = value; }
        public string Spirit4Name { get => spirit4Name; set => spirit4Name = value; }
        public string Spirit5Name { get => spirit5Name; set => spirit5Name = value; }
        public int Spirit1Flavours { get => spirit1Flavours; set => spirit1Flavours = value; }
        public int Spirit2Flavours { get => spirit2Flavours; set => spirit2Flavours = value; }
        public int Spirit3Flavours { get => spirit3Flavours; set => spirit3Flavours = value; }
        public int Spirit4Flavours { get => spirit4Flavours; set => spirit4Flavours = value; }
        public int Spirit5Flavours { get => spirit5Flavours; set => spirit5Flavours = value; }
        public int Spirit1Ratings { get => spirit1Ratings; set => spirit1Ratings = value; }
        public int Spirit2Ratings { get => spirit2Ratings; set => spirit2Ratings = value; }
        public int Spirit3Ratings { get => spirit3Ratings; set => spirit3Ratings = value; }
        public int Spirit4Ratings { get => spirit4Ratings; set => spirit4Ratings = value; }
        public int Spirit5Ratings { get => spirit5Ratings; set => spirit5Ratings = value; }
        public string Feedback { get => feedback; set => feedback = value; }
        public int OverallRating { get => overallRating; set => overallRating = value; }
    }

    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return new T[0];
            }

            // Check if the JSON is already an array
            if (json.TrimStart().StartsWith("["))
            {
                return JsonUtility.FromJson<Wrapper<T>>("{\"Array\":" + json + "}").Array;
            }
            else
            {
                // If it's a single object, wrap it in an array
                return new T[] { JsonUtility.FromJson<T>(json) };
            }
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Array;
        }
    }
}