using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    
    public GameObject loadingPanel;
    public GameObject overallRatingPanel;
    public GameObject resultPanel;

    private Coroutine repeatUpdateCoroutine;
    private bool isFirstLoadComplete = false;

    private bool isReturningToOverallRating = false;

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
        loadingPanel.SetActive(true);
        overallRatingPanel.SetActive(false);
        
        StopRepeatUpdateProcess();
        StartCoroutine(InitialUpdateProcess());
    }

    private IEnumerator InitialUpdateProcess()
    {
        yield return StartCoroutine(UpdatePlayerDataWithPanelManagement());

        if (isFirstLoadComplete && resultPanel.activeSelf)
        {
            repeatUpdateCoroutine = StartCoroutine(RepeatUpdateProcess());
        }
    }

    private IEnumerator RepeatUpdateProcess()
    {
        while (resultPanel.activeSelf)
        {
            yield return new WaitForSeconds(UpdateInterval);
            
            if (resultPanel.activeSelf)
            {
                Debug.Log("SpiritManager: Updating player data");
                yield return StartCoroutine(UpdatePlayerDataWithPanelManagement());
            }
        }
    }

    private IEnumerator UpdatePlayerDataWithPanelManagement()
    {
        Debug.Log("SpiritManager: Updating player data");
        bool isDataProcessedSuccessfully = false;

        yield return StartCoroutine(UpdatePlayerData(() => {
            isDataProcessedSuccessfully = true;
        }));

        if (isDataProcessedSuccessfully && !isReturningToOverallRating)
        {
            loadingPanel.SetActive(false);
            resultPanel.SetActive(true);
            Debug.Log("SpiritManager: Data processed successfully, showing result panel");

            if (!isFirstLoadComplete)
            {
                isFirstLoadComplete = true;
                repeatUpdateCoroutine = StartCoroutine(RepeatUpdateProcess());
            }
        }
        else if (!isReturningToOverallRating)
        {
            loadingPanel.SetActive(false);
            overallRatingPanel.SetActive(true);
            Debug.LogError("SpiritManager: Failed to process data, returned to overall rating panel");
        }
    }

    private IEnumerator UpdatePlayerData(System.Action onSuccess = null)
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
            
            float[] averageRatings = new float[5];
            float[] averageFlavours = new float[5];
            CalculateAverages(updatedPlayerDataList.items, out averageRatings, out averageFlavours);
            
            Debug.Log("SpiritManager: Updating FlavourTable2");
            uiManager.UpdateFlavourTable2(dataManager.spiritNames, dataManager.spiritData, averageRatings, averageFlavours);

            // Get local and general sorted spirits
            var localSortedSpirits = dataManager.GetLocalSortedSpirits();
            var generalSortedSpirits = dataManager.GetGeneralSortedSpirits(updatedPlayerDataList);

            // Update both local and general top three spirits
            uiManager.UpdateTopThreeSpirits();

            onSuccess?.Invoke();
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

    // Update FlavourTable2 with new average data
    float[] averageRatings = new float[5];
    float[] averageFlavours = new float[5];
    for (int i = 0; i < 5; i++)
    {
        averageRatings[i] = dataManager.GetAverageRating(i);
        averageFlavours[i] = dataManager.GetAverageFlavour(i);
    }
    uiManager.UpdateFlavourTable2AverageData(averageRatings, averageFlavours);

    // Update local top three spirits
    dataManager.UpdateUIWithLocalSortedSpirits(uiManager);

    // Update rankings in FlavourTable2
    uiManager.UpdateRankings();
}

    public void StopRepeatUpdateProcess()
    {
        if (repeatUpdateCoroutine != null)
        {
            StopCoroutine(repeatUpdateCoroutine);
            repeatUpdateCoroutine = null;
        }
    }

    public void ReturnToOverallRating()
    {
        isReturningToOverallRating = true;
        StopRepeatUpdateProcess();
        resultPanel.SetActive(false);
        overallRatingPanel.SetActive(true);
        isReturningToOverallRating = false;
    }

    private void OnDisable()
    {
        StopRepeatUpdateProcess();
    }
}