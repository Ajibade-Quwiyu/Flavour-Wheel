using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static DataManager;

public class SpiritManager : MonoBehaviour
{
    private DataManager dataManager;
    private UIManager uiManager;
    private UserInputManager userInputManager;
    private string username, email, feedback;
    private int overallRating;

    private const string PlayerUsernameKey = "PlayerUsername", EmailKey = "Email";
    private const float UpdateInterval = 5f;
    public GameObject loadingPanel, overallRatingPanel, resultPanel, summaryPanel;
    private Coroutine repeatUpdateCoroutine;
    private bool isFirstLoadComplete, isReturningToOverallRating, isInitialSubmission = true, isSummaryMode=false;
    private SortedDictionary<int, (string Name, int SelectedFlavors, int Rating)> orderedSpiritData = new SortedDictionary<int, (string, int, int)>();

    private void Awake()
    {
        dataManager = GetComponent<DataManager>();
        uiManager = GetComponent<UIManager>();
        userInputManager = FindObjectOfType<UserInputManager>();

        if (dataManager == null)
            Debug.LogError("DataManager not found on SpiritManager GameObject");
        if (uiManager == null)
            Debug.LogError("UIManager not found on SpiritManager GameObject");
        if (userInputManager == null)
            Debug.LogError("UserInputManager not found in the scene");
    }

    public void SaveDataToPlayerDataTable()
    {
        DisableAllPanels();
        loadingPanel.SetActive(true);
        StopRepeatUpdateProcess();
        StartCoroutine(InitialUpdateProcess());
    }

    private IEnumerator InitialUpdateProcess()
    {
        yield return StartCoroutine(UpdatePlayerDataWithPanelManagement());
    }

    private IEnumerator UpdatePlayerDataWithPanelManagement()
    {
        if (isReturningToOverallRating)
        {
            yield break;
        }

        bool isDataProcessedSuccessfully = false;
        yield return UpdatePlayerData((success) =>
        {
            isDataProcessedSuccessfully = success;
        });

        DisableAllPanels();

        if (isDataProcessedSuccessfully && !isReturningToOverallRating)
        {
            if (isSummaryMode)
            {
                summaryPanel.SetActive(true);
            }
            else
            {
                resultPanel.SetActive(true);
            }

            if (!isFirstLoadComplete)
            {
                isFirstLoadComplete = true;
                repeatUpdateCoroutine = StartCoroutine(RepeatUpdateProcess());
            }
        }
        else
        {
            overallRatingPanel.SetActive(true);
        }
    }

    private IEnumerator RepeatUpdateProcess()
    {
        while ((resultPanel.activeSelf || summaryPanel.activeSelf) && !isReturningToOverallRating)
        {
            yield return new WaitForSeconds(UpdateInterval);
            if ((resultPanel.activeSelf || summaryPanel.activeSelf) && !isReturningToOverallRating)
            {
                yield return VerifyPasscodeAndUpdate();
            }
        }
    }

    public void ToggleSummaryMode(bool enableSummary)
    {
        isSummaryMode = enableSummary;
        if (isFirstLoadComplete)
        {
            DisableAllPanels();
            if (isSummaryMode)
            {
                summaryPanel.SetActive(true);
            }
            else
            {
                resultPanel.SetActive(true);
            }
        }
    }

    public void ReturnToOverallRating()
    {
        isReturningToOverallRating = true;
        isSummaryMode = false; // Reset summary mode
        StopAllCoroutines();
        StopRepeatUpdateProcess();

        DisableAllPanels();
        overallRatingPanel.SetActive(true);

        StartCoroutine(DeletePlayerData());
        ResetSubmissionState();
        StartCoroutine(ResetReturnFlag());
    }

    private IEnumerator VerifyPasscodeAndUpdate()
    {
        if (userInputManager == null)
        {
            Debug.LogError("UserInputManager is not set in SpiritManager");
            yield break;
        }

        bool isPasscodeValid = false;
        yield return userInputManager.VerifyPasscode((isValid) => { isPasscodeValid = isValid; });

        if (isPasscodeValid)
        {
            yield return UpdatePlayerDataWithPanelManagement();
        }
        else
        {
            userInputManager.RestartGame();
        }
    }

    private IEnumerator UpdatePlayerData(System.Action<bool> onComplete)
    {
        string username = PlayerPrefs.GetString(PlayerUsernameKey, "");
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogError("Username is not set. Please ensure the username is set before saving data.");
            onComplete?.Invoke(false);
            yield break;
        }

        var updatedData = dataManager.GeneratePlayerData(username, email, overallRating, feedback);

        bool saveAndFetchComplete = false;
        yield return dataManager.SaveAndFetchUpdatedData(updatedData, updatedPlayerDataList =>
        {
            bool updateSuccess = ProcessUpdatedPlayerData(updatedPlayerDataList);
            saveAndFetchComplete = true;
            onComplete?.Invoke(updateSuccess);
        }, error =>
        {
            Debug.LogError($"Error updating player data: {error}");
            saveAndFetchComplete = true;
            onComplete?.Invoke(false);
        });

        while (!saveAndFetchComplete)
        {
            yield return null;
        }
    }

    private IEnumerator DeletePlayerData()
    {
        int userId = PlayerPrefs.GetInt("UserId", 0);
        if (userId != 0)
        {
            yield return dataManager.DeletePlayer(userId, () => {
                Debug.Log("Player data deleted successfully");
            }, (error) => {
                Debug.LogError($"Error deleting player data: {error}");
            });
        }
    }
    public void SetUserData(string username, string email, int overallRating, string feedback)
    {
        PlayerPrefs.SetString(PlayerUsernameKey, username);
        PlayerPrefs.Save();

        this.email = email ?? PlayerPrefs.GetString(EmailKey, "DefaultEmail@example.com");
        this.overallRating = overallRating;
        this.feedback = feedback ?? "Great";
    }

    private void DisableAllPanels()
    {
        loadingPanel.SetActive(false);
        overallRatingPanel.SetActive(false);
        resultPanel.SetActive(false);
        summaryPanel.SetActive(false); 
    }

    public void StopRepeatUpdateProcess()
    {
        if (repeatUpdateCoroutine != null)
        {
            StopCoroutine(repeatUpdateCoroutine);
            repeatUpdateCoroutine = null;
        }
    }

    private bool ProcessUpdatedPlayerData(PlayerDataList updatedPlayerDataList)
    {
        if (updatedPlayerDataList?.items == null || updatedPlayerDataList.items.Count == 0)
        {
            Debug.LogError("Updated player data list is null or empty");
            return false;
        }

        string username = PlayerPrefs.GetString(PlayerUsernameKey, "");
        dataManager.UpdateSpiritNamesListFromServer(updatedPlayerDataList.items, username);

        // Replace the UpdateFlavourTable call with UpdateAllTables
        uiManager.UpdateAllTables(updatedPlayerDataList.items);

        var currentPlayer = dataManager.FindPlayerByUsername(updatedPlayerDataList, username);
        if (currentPlayer == null)
        {
            Debug.LogError($"Current player not found in updated data. Username: {username}");
            return false;
        }

        // Ensure overallRating is updated correctly
        dataManager.UpdateExistingPlayerData(currentPlayer, username, email, overallRating, feedback);
        CalculateAverages(updatedPlayerDataList.items, out float[] averageRatings, out float[] averageFlavours);
        uiManager.UpdateFlavourTable2(dataManager.spiritData, averageRatings, averageFlavours);
        uiManager.UpdateTopThreeSpirits();
        return true;
    }

    private void CalculateAverages(List<DataManager.PlayerData> players, out float[] averageRatings, out float[] averageFlavours)
    {
        averageRatings = new float[5];
        averageFlavours = new float[5];
        if (players == null || players.Count == 0) return;

        foreach (var player in players)
        {
            for (int i = 0; i < 5; i++)
            {
                averageRatings[i] += player.GetType().GetField($"spirit{i + 1}Ratings").GetValue(player) as int? ?? 0;
                averageFlavours[i] += player.GetType().GetField($"spirit{i + 1}Flavours").GetValue(player) as int? ?? 0;
            }
        }

        for (int i = 0; i < 5; i++)
        {
            averageRatings[i] /= players.Count;
            averageFlavours[i] /= players.Count;
        }
    }

    public void ReceiveSpiritData(string spiritName, int selectedFlavors, int rating, int order)
    {
        orderedSpiritData[order] = (spiritName, selectedFlavors, rating);
        UpdateDataManager();
    }

    private void UpdateDataManager()
    {
        dataManager.ClearSpiritData();
        foreach (var kvp in orderedSpiritData)
        {
            dataManager.AddSpiritData(kvp.Value.Name, kvp.Value.SelectedFlavors, kvp.Value.Rating);
        }

        // Update UI
        uiManager.UpdateFlavourTable2LocalData(dataManager.GetOrderedSpiritNames(), dataManager.GetOrderedSpiritData());
        uiManager.UpdateRankings();
    }

    private void ResetSubmissionState()
    {
        isFirstLoadComplete = false;
        isInitialSubmission = true;
    }
    private IEnumerator ResetReturnFlag()
    {
        yield return new WaitForSeconds(0.5f);
        isReturningToOverallRating = false;
    }
    private void OnApplicationQuit()
    {
        StartCoroutine(DeletePlayerDataOnQuit());
    }

    private IEnumerator DeletePlayerDataOnQuit()
    {
        int userId = PlayerPrefs.GetInt("UserId", 0);
        if (userId != 0)
        {
            bool deleteComplete = false;
            yield return dataManager.DeletePlayer(userId, () => {
                Debug.Log("Player data deleted successfully on quit");
                deleteComplete = true;
            }, (error) => {
                Debug.LogError($"Error deleting player data on quit: {error}");
                deleteComplete = true;
            });

            // Wait for the deletion to complete before quitting
            while (!deleteComplete)
            {
                yield return null;
            }
        }
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        StopRepeatUpdateProcess();
    }
}