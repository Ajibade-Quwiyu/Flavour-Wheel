using System.Collections;
using UnityEngine;
using System.Collections.Generic;
public class SpiritManager : MonoBehaviour
{
    // References to DataManager and UIManager components.
    private DataManager dataManager;
    private UIManager uiManager;

    // Variables to store user data.
    private string username;
    private string email;
    private int overallRating;
    private string feedback;

    // Constants for PlayerPrefs keys and the update interval.
    private const string UsernameKey = "Username";
    private const string EmailKey = "Email";
    private const float UpdateInterval = 5f;

    // Called when the script is first initialized.
    private void Start()
    {
        // Get references to the DataManager and UIManager components attached to the same GameObject.
        dataManager = GetComponent<DataManager>();
        uiManager = GetComponent<UIManager>();
    }

    // Sets the user data, either from provided parameters or from PlayerPrefs if no data is provided.
    public void SetUserData(string username, string email, int overallRating, string feedback)
    {
        // Use the provided username or fallback to the stored value in PlayerPrefs.
        this.username = string.IsNullOrEmpty(username) ? PlayerPrefs.GetString(UsernameKey, "DefaultUsername") : username;
        // Use the provided email or fallback to the stored value in PlayerPrefs.
        this.email = string.IsNullOrEmpty(email) ? PlayerPrefs.GetString(EmailKey, "DefaultEmail@example.com") : email;
        this.overallRating = overallRating; // Set the overall rating.
        this.feedback = string.IsNullOrEmpty(feedback) ? "Great" : feedback; // Set feedback, defaulting to "Great" if not provided.
    }

    // Initiates the process of saving and updating data every 5 seconds.
    public void SaveDataToPlayerDataTable()
    {
        // Start the coroutine to continuously update data.
        StartCoroutine(RepeatUpdateProcess());
    }

    // Continuously updates player data every 5 seconds.
    private IEnumerator RepeatUpdateProcess()
    {
        while (true)
        {
            // Perform data check and update.
            yield return StartCoroutine(CheckAndUpdatePlayerData());
            // Wait for the specified interval before repeating the process.
            yield return new WaitForSeconds(UpdateInterval);
        }
    }

    // Checks for existing data and updates it if necessary.
    private IEnumerator CheckAndUpdatePlayerData()
    {
        Debug.Log("Checking and updating player data...");

        var updatedData = dataManager.GeneratePlayerData(username, email, overallRating, feedback);
        
        yield return StartCoroutine(dataManager.SaveAndFetchUpdatedData(
            updatedData,
            updatedPlayerDataList =>
            {
                Debug.Log($"Data saved and fetched. Updating UI with {updatedPlayerDataList.items.Count} items");
                uiManager.UpdateFlavourTable(updatedPlayerDataList.items);
                dataManager.UpdateSpiritNamesListFromServer(updatedPlayerDataList.items, username);
                var sortedSpirits = dataManager.GetSortedSpirits();
                uiManager.UpdateTopThreeSpirits(sortedSpirits);
            },
            error =>
            {
                Debug.LogError($"Error saving/fetching data: {error}");
            }
        ));
    }
    private IEnumerator UpdatePlayerData()
    {
        var updatedData = dataManager.GeneratePlayerData(username, email, overallRating, feedback);
        
        yield return StartCoroutine(dataManager.SaveAndFetchUpdatedData(
            updatedData,
            updatedPlayerDataList =>
            {
                Debug.Log($"Data updated successfully. Updating UI with {updatedPlayerDataList.items.Count} items");
                
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
                
                uiManager.UpdateFlavourTable2(dataManager.spiritNames, dataManager.spiritData, averageRatings, averageFlavours);
            },
            error =>
            {
                Debug.LogError($"Error updating player data: {error}");
            }
        ));
    }

    private void CalculateAverages(List<DataManager.PlayerData> players, out float[] averageRatings, out float[] averageFlavours)
    {
        averageRatings = new float[5];
        averageFlavours = new float[5];

        if (players == null || players.Count == 0)
        {
            Debug.LogWarning("No player data available for average calculation");
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
    }

    // This method updates spirit data with new information and refreshes the UI.
    public void ReceiveSpiritData(string spiritName, int selectedFlavors, int rating)
    {
        Debug.Log($"ReceiveSpiritData called with: spiritName={spiritName}, selectedFlavors={selectedFlavors}, rating={rating}");

        // Add the spirit name if it's not already in the list.
        if (!dataManager.spiritNames.Contains(spiritName))
        {
            dataManager.spiritNames.Add(spiritName);
        }

        // Update or add the spirit data.
        if (dataManager.spiritData.ContainsKey(spiritName))
        {
            dataManager.spiritData[spiritName].SelectedFlavors = selectedFlavors;
            dataManager.spiritData[spiritName].Rating = rating;
        }
        else
        {
            dataManager.spiritData.Add(spiritName, new DataManager.SpiritInfo(spiritName, selectedFlavors, rating));
        }

        // Update the UI with the new data.
        uiManager.UpdateFlavourTable2LocalData(dataManager.spiritNames, dataManager.spiritData);
        uiManager.UpdateSpiritNamesList(dataManager.spiritNames);

        // Get the sorted spirits from the dataManager.
        var sortedSpirits = dataManager.GetSortedSpirits();

        // Update the top three spirits in the UI.
        uiManager.UpdateTopThreeSpirits(sortedSpirits);
    }
}
