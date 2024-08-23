using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiritManager : MonoBehaviour
{
    private DataManager dataManager;
    private UIManager uiManager;
    private string username, email, feedback;
    private int overallRating;
    private const string UsernameKey = "Username", EmailKey = "Email";
    private const float UpdateInterval = 5f;
    public GameObject loadingPanel, overallRatingPanel, resultPanel;
    private Coroutine repeatUpdateCoroutine;
    private bool isFirstLoadComplete, isReturningToOverallRating;

    private void Start()
    {
        dataManager = GetComponent<DataManager>();
        uiManager = GetComponent<UIManager>();
    }

    public void SetUserData(string username, string email, int overallRating, string feedback)
    {
        this.username = username ?? PlayerPrefs.GetString(UsernameKey, "DefaultUsername");
        this.email = email ?? PlayerPrefs.GetString(EmailKey, "DefaultEmail@example.com");
        this.overallRating = overallRating;
        this.feedback = feedback ?? "Great";
    }

    public void SaveDataToPlayerDataTable()
    {
        loadingPanel.SetActive(true);
        overallRatingPanel.SetActive(false);
        StopRepeatUpdateProcess();
        StartCoroutine(InitialUpdateProcess());
    }

    private IEnumerator InitialUpdateProcess()
    {
        yield return StartCoroutine(UpdatePlayerDataWithPanelManagement());
        if (isFirstLoadComplete && resultPanel.activeSelf)
            repeatUpdateCoroutine = StartCoroutine(RepeatUpdateProcess());
    }

    private IEnumerator RepeatUpdateProcess()
    {
        while (resultPanel.activeSelf)
        {
            yield return new WaitForSeconds(UpdateInterval);
            if (resultPanel.activeSelf)
                yield return UpdatePlayerDataWithPanelManagement();
        }
    }

    private IEnumerator UpdatePlayerDataWithPanelManagement()
    {
        bool isDataProcessedSuccessfully = false;
        yield return UpdatePlayerData(() => isDataProcessedSuccessfully = true);

        if (isDataProcessedSuccessfully && !isReturningToOverallRating)
        {
            loadingPanel.SetActive(false);
            resultPanel.SetActive(true);
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
        }
    }

    private IEnumerator UpdatePlayerData(System.Action onSuccess = null)
    {
        var updatedData = dataManager.GeneratePlayerData(username, email, overallRating, feedback);
        yield return dataManager.SaveAndFetchUpdatedData(updatedData, updatedPlayerDataList =>
        {
            dataManager.UpdateSpiritNamesListFromServer(updatedPlayerDataList.items, username);
            uiManager.UpdateFlavourTable(updatedPlayerDataList.items);
            var currentPlayer = dataManager.FindPlayerByUsername(updatedPlayerDataList, username);
            if (currentPlayer != null)
                dataManager.UpdateExistingPlayerData(currentPlayer, username, email, overallRating, feedback);

            CalculateAverages(updatedPlayerDataList.items, out float[] averageRatings, out float[] averageFlavours);
            uiManager.UpdateFlavourTable2(dataManager.spiritData, averageRatings, averageFlavours);
            uiManager.UpdateTopThreeSpirits();
            onSuccess?.Invoke();
        }, error => { });
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

    public void ReceiveSpiritData(string spiritName, int selectedFlavors, int rating)
    {
        if (!dataManager.spiritNames.Contains(spiritName))
            dataManager.spiritNames.Add(spiritName);

        dataManager.spiritData[spiritName] = new DataManager.SpiritInfo(spiritName, selectedFlavors, rating);

        uiManager.UpdateFlavourTable2LocalData(dataManager.spiritNames, dataManager.spiritData);
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

    private void OnDisable() => StopRepeatUpdateProcess();
}