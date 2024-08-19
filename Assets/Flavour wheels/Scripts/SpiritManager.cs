using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class SpiritManager : MonoBehaviour
{
    private DataManager dataManager;
    private UIManager uiManager;

    private string username;
    private string email;
    private int overallRating;
    private string feedback;

    private const string UsernameKey = "Username";
    private const string EmailKey = "Email";
    private const float UpdateInterval = 5f;

    private void Start()
    {
        Debug.Log("SpiritManager: Start method called");
        dataManager = GetComponent<DataManager>();
        uiManager = GetComponent<UIManager>();

        if (dataManager == null || uiManager == null)
        {
            Debug.LogError("SpiritManager: DataManager or UIManager not found!");
        }
    }

    public void SetUserData(string username, string email, int overallRating, string feedback)
    {
        Debug.Log($"SpiritManager: Setting user data - Username: {username}, Email: {email}, Rating: {overallRating}");
        this.username = string.IsNullOrEmpty(username) ? PlayerPrefs.GetString(UsernameKey, "DefaultUsername") : username;
        this.email = string.IsNullOrEmpty(email) ? PlayerPrefs.GetString(EmailKey, "DefaultEmail@example.com") : email;
        this.overallRating = overallRating;
        this.feedback = string.IsNullOrEmpty(feedback) ? "Great" : feedback;
    }

    public void SaveDataToPlayerDataTable()
    {
        Debug.Log("SpiritManager: Starting data update process");
        StartCoroutine(RepeatUpdateProcess());
    }

    private IEnumerator RepeatUpdateProcess()
    {
        while (true)
        {
            Debug.Log("SpiritManager: Updating player data");
            yield return StartCoroutine(UpdatePlayerData());
            yield return new WaitForSeconds(UpdateInterval);
        }
    }

    private IEnumerator UpdatePlayerData()
    {
        Debug.Log("SpiritManager: Generating and updating player data");
        var updatedData = dataManager.GeneratePlayerData(username, email, overallRating, feedback);
        
        yield return StartCoroutine(dataManager.SaveAndFetchUpdatedData(
            updatedData,
            updatedPlayerDataList =>
            {
                Debug.Log($"SpiritManager: Data updated successfully. Updating UI with {updatedPlayerDataList.items.Count} items");
                
                dataManager.UpdateSpiritNamesListFromServer(updatedPlayerDataList.items, username);
                
                uiManager.UpdateFlavourTable(updatedPlayerDataList.items);
                
                var currentPlayer = dataManager.FindPlayerByUsername(updatedPlayerDataList, username);
                if (currentPlayer != null)
                {
                    dataManager.UpdateExistingPlayerData(currentPlayer, username, email, overallRating, feedback);
                }
                
                var sortedSpirits = dataManager.GetSortedSpirits();
                uiManager.UpdateTopThreeSpirits(sortedSpirits);
                
                float[] averageRatings = new float[5];
                float[] averageFlavours = new float[5];
                CalculateAverages(updatedPlayerDataList.items, out averageRatings, out averageFlavours);
                
                Debug.Log("SpiritManager: Updating FlavourTable2");
                uiManager.UpdateFlavourTable2(dataManager.spiritNames, dataManager.spiritData, averageRatings, averageFlavours);
            },
            error =>
            {
                Debug.LogError($"SpiritManager: Error updating player data: {error}");
            }
        ));
    }

    private void CalculateAverages(List<DataManager.PlayerData> players, out float[] averageRatings, out float[] averageFlavours)
    {
        Debug.Log("SpiritManager: Calculating averages");
        averageRatings = new float[5];
        averageFlavours = new float[5];

        if (players == null || players.Count == 0)
        {
            Debug.LogWarning("SpiritManager: No player data available for average calculation");
            return;
        }

        foreach (var player in players)
        {
            averageRatings[0] += player.spirit1Ratings;
            averageRatings[1] += player.spirit2Ratings;
            averageRatings[2] += player.spirit3Ratings;
            averageRatings[3] += player.spirit4Ratings;
            averageRatings[4] += player.spirit5Ratings;

            averageFlavours[0] += player.spirit1Flavours;
            averageFlavours[1] += player.spirit2Flavours;
            averageFlavours[2] += player.spirit3Flavours;
            averageFlavours[3] += player.spirit4Flavours;
            averageFlavours[4] += player.spirit5Flavours;
        }

        for (int i = 0; i < 5; i++)
        {
            averageRatings[i] /= players.Count;
            averageFlavours[i] /= players.Count;
        }

        Debug.Log($"SpiritManager: Averages calculated - Ratings: [{string.Join(", ", averageRatings)}], Flavours: [{string.Join(", ", averageFlavours)}]");
    }

    public void ReceiveSpiritData(string spiritName, int selectedFlavors, int rating)
    {
        Debug.Log($"SpiritManager: ReceiveSpiritData called with: spiritName={spiritName}, selectedFlavors={selectedFlavors}, rating={rating}");

        if (!dataManager.spiritNames.Contains(spiritName))
        {
            dataManager.spiritNames.Add(spiritName);
        }

        if (dataManager.spiritData.ContainsKey(spiritName))
        {
            dataManager.spiritData[spiritName].SelectedFlavors = selectedFlavors;
            dataManager.spiritData[spiritName].Rating = rating;
        }
        else
        {
            dataManager.spiritData.Add(spiritName, new DataManager.SpiritInfo(spiritName, selectedFlavors, rating));
        }

        Debug.Log("SpiritManager: Updating UI with new spirit data");
        uiManager.UpdateFlavourTable2LocalData(dataManager.spiritNames, dataManager.spiritData);

        var sortedSpirits = dataManager.GetSortedSpirits();
        uiManager.UpdateTopThreeSpirits(sortedSpirits);
    }
}