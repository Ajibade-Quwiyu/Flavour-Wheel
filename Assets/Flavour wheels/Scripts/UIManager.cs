using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public class UIManager : MonoBehaviour
{
    public Transform localTopThree, generalTopThree, FlavourTable1_Flavour, FlavourTable1_Rating, FlavourTable2, summaryTable;
    public GameObject flavourRowPrefab, summaryPrefab;
    private Dictionary<string, int> spiritRanks = new Dictionary<string, int>();
    private Dictionary<string, int> localFlavors = new Dictionary<string, int>();
    private Dictionary<string, float> localCalculatedValues = new Dictionary<string, float>();
    public void UpdateAllTables(List<DataManager.PlayerData> players)
    {
        UpdateFlavourTable(players);
        UpdateSummaryTable(players);
    }
    public void UpdateFlavourTable(List<DataManager.PlayerData> players)
    {
        ClearTable(FlavourTable1_Flavour);
        ClearTable(FlavourTable1_Rating);
        int maxRowsToShow = Mathf.Min(players.Count, 30);
        for (int i = 0; i < maxRowsToShow; i++)
        {
            CreateFlavourTableRow(FlavourTable1_Flavour, i, players[i], true);
            CreateFlavourTableRow(FlavourTable1_Rating, i, players[i], false);
        }
    }
    public void UpdateSummaryTable(List<DataManager.PlayerData> players)
    {
        ClearTable(summaryTable);
        int maxRowsToShow = Mathf.Min(players.Count, 30);
        for (int i = 0; i < maxRowsToShow; i++)
        {
            CreateSummaryTableRow(summaryTable, i, players[i]);
        }
    }

    private void ClearTable(Transform table)
    {
        foreach (Transform child in table)
            Destroy(child.gameObject);
    }

    private void CreateSummaryTableRow(Transform parent, int rowIndex, DataManager.PlayerData player)
    {
        var row = Instantiate(summaryPrefab, parent);
        if (row != null) SetSummaryRowData(row, rowIndex, player);
    }

    private void SetSummaryRowData(GameObject row, int rowIndex, DataManager.PlayerData player)
    {
        // Set id, username, email, spirit names as before
        SetText(row.transform.GetChild(1), (rowIndex + 1).ToString());
        SetText(row.transform.GetChild(2), player.username);
        SetText(row.transform.GetChild(3), player.email);
        SetText(row.transform.GetChild(4), player.spirit1Name);
        SetText(row.transform.GetChild(5), player.spirit2Name);
        SetText(row.transform.GetChild(6), player.spirit3Name);
        SetText(row.transform.GetChild(7), player.spirit4Name);
        SetText(row.transform.GetChild(8), player.spirit5Name);

        // Set spirit flavours with colored backgrounds
        SetColoredCell(row.transform.GetChild(9), player.spirit1Flavours, true);
        SetColoredCell(row.transform.GetChild(10), player.spirit2Flavours, true);
        SetColoredCell(row.transform.GetChild(11), player.spirit3Flavours, true);
        SetColoredCell(row.transform.GetChild(12), player.spirit4Flavours, true);
        SetColoredCell(row.transform.GetChild(13), player.spirit5Flavours, true);

        // Set spirit ratings with colored backgrounds
        SetColoredCell(row.transform.GetChild(14), player.spirit1Ratings, false);
        SetColoredCell(row.transform.GetChild(15), player.spirit2Ratings, false);
        SetColoredCell(row.transform.GetChild(16), player.spirit3Ratings, false);
        SetColoredCell(row.transform.GetChild(17), player.spirit4Ratings, false);
        SetColoredCell(row.transform.GetChild(18), player.spirit5Ratings, false);

        // Set feedback
        SetText(row.transform.GetChild(19), player.feedback);

        // Set overall rating
        SetOverallRating(row.transform.GetChild(20), player.overallRating);
    }

    private void SetColoredCell(Transform cellTransform, int value, bool isFlavour)
    {
        // Set the background color of the Image (which is the direct child)
        Image backgroundImage = cellTransform.GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.color = GetCellColor(value, isFlavour);
        }

        // Set the text of the TMP_Text (which is the child of the Image)
        TMP_Text tmpText = cellTransform.GetComponentInChildren<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.text = value.ToString();
        }
    }

    private void SetOverallRating(Transform ratingTransform, int rating)
    {
        for (int i = 0; i < ratingTransform.childCount; i++)
        {
            ratingTransform.GetChild(i).gameObject.SetActive(i < rating);
        }
    }

    private void CreateFlavourTableRow(Transform parent, int rowIndex, DataManager.PlayerData player, bool isFlavourTable)
    {
        var row = Instantiate(flavourRowPrefab, parent);
        if (row != null) SetRowData(row, rowIndex, player, isFlavourTable);
    }

    private void SetRowData(GameObject row, int rowIndex, DataManager.PlayerData player, bool isFlavourTable)
    {
        SetText(row.transform.GetChild(0), (rowIndex + 1).ToString());
        SetText(row.transform.GetChild(1), player.username);

        int[] values = isFlavourTable
            ? new int[] { player.spirit1Flavours, player.spirit2Flavours, player.spirit3Flavours, player.spirit4Flavours, player.spirit5Flavours }
            : new int[] { player.spirit1Ratings, player.spirit2Ratings, player.spirit3Ratings, player.spirit4Ratings, player.spirit5Ratings };

        for (int i = 0; i < values.Length; i++)
            SetCellData(row.transform.GetChild(2 + i), values[i], isFlavourTable);

        // Ensure the overall rating is set correctly
        SetStarRating(row.transform.GetChild(7), player.overallRating);
    }

    private void SetStarRating(Transform starRating, int rating)
    {
        for (int i = 0; i < starRating.childCount && i < 5; i++)
            starRating.GetChild(i).gameObject.SetActive(i < rating);
    }

    private void SetCellData(Transform cellTransform, int value, bool isFlavourTable)
    {
        cellTransform.GetComponent<Image>().color = GetCellColor(value, isFlavourTable);
        SetText(cellTransform.GetChild(0), value.ToString());
    }

    private Color GetCellColor(int value, bool isFlavour)
    {
        float normalizedValue = isFlavour ? Mathf.Clamp01(value / 7f) : Mathf.Clamp01(value / 5f);
        return Color.Lerp(Color.yellow, Color.green, normalizedValue);
    }

    public void UpdateFlavourTable2AverageData(float[] averageRatings, float[] averageFlavours)
    {
        var multipliedValues = new float[5];

        for (int i = 0; i < 5; i++)
        {
            SetText(FlavourTable2.GetChild(1).GetChild(i), averageRatings[i].ToString("F2"));
            SetText(FlavourTable2.GetChild(3).GetChild(i), averageFlavours[i].ToString("F2"));
            multipliedValues[i] = averageRatings[i] * averageFlavours[i];
            SetText(FlavourTable2.GetChild(4).GetChild(i), multipliedValues[i].ToString("F2"));
        }

        UpdateFlavourTable2Ranks(multipliedValues);
    }

    private void UpdateFlavourTable2Ranks(float[] multipliedValues)
    {
        var ranksTransform = FlavourTable2.GetChild(5);
        var sortedIndices = Enumerable.Range(0, multipliedValues.Length)
                                      .OrderByDescending(i => multipliedValues[i])
                                      .ToArray();

        for (int i = 0; i < multipliedValues.Length; i++)
            SetText(ranksTransform.GetChild(i), GetRankString(Array.IndexOf(sortedIndices, i) + 1));
    }

    public void UpdateFlavourTable2(Dictionary<string, DataManager.SpiritInfo> spiritData, float[] averageRatings, float[] averageFlavours)
    {
        UpdateAverageData(averageRatings, averageFlavours);
        UpdateMultipliedValues();
        UpdateRankings();
    }

    private void UpdateMultipliedValues()
    {
        var averageRatings = FlavourTable2.GetChild(1);
        var averageFlavours = FlavourTable2.GetChild(3);
        var multiplied = FlavourTable2.GetChild(4);

        for (int i = 0; i < 5; i++)
        {
            float avgRating = float.Parse(GetText(averageRatings.GetChild(i)));
            float avgFlavour = float.Parse(GetText(averageFlavours.GetChild(i)));
            SetText(multiplied.GetChild(i), (avgRating * avgFlavour).ToString("F2"));
        }
    }

    private void UpdateAverageData(float[] averageRatings, float[] averageFlavours)
    {
        var averageRatingsTransform = FlavourTable2.GetChild(1);
        var averageFlavoursTransform = FlavourTable2.GetChild(3);

        for (int i = 0; i < 5; i++)
        {
            SetText(averageRatingsTransform.GetChild(i), averageRatings[i].ToString("F2"));
            SetText(averageFlavoursTransform.GetChild(i), averageFlavours[i].ToString("F2"));
        }
    }

    private List<string> GetSpiritNames()
    {
        var spiritsTransform = FlavourTable2.parent.Find("Spirits");
        return spiritsTransform != null
            ? spiritsTransform.GetComponentsInChildren<TMP_Text>().Select(tmp => tmp.text).ToList()
            : new List<string>();
    }

    public void UpdateRankings()
    {
        var spiritNames = GetSpiritNames();
        var multipliedTransform = FlavourTable2.GetChild(4);
        var ranksTransform = FlavourTable2.GetChild(5);
        var multipliedValues = Enumerable.Range(0, 5)
            .Select(i => float.Parse(GetText(multipliedTransform.GetChild(i))))
            .ToArray();

        var sortedIndices = Enumerable.Range(0, multipliedValues.Length)
                                      .OrderByDescending(i => multipliedValues[i])
                                      .ToArray();

        spiritRanks.Clear();
        for (int i = 0; i < multipliedValues.Length && i < spiritNames.Count; i++)
        {
            int rank = Array.IndexOf(sortedIndices, i) + 1;
            spiritRanks[spiritNames[i]] = rank;
            SetText(ranksTransform.GetChild(i), GetRankString(rank));
        }

        UpdateGeneralTopThree();
    }
    private void UpdateGeneralTopThree()
    {
        var sortedSpirits = spiritRanks.OrderBy(pair => pair.Value).Take(3).Select(pair => pair.Key).ToList();
        for (int i = 0; i < generalTopThree.childCount; i++)
            SetText(generalTopThree.GetChild(i), i < sortedSpirits.Count ? sortedSpirits[i] : string.Empty);
    }
    private void UpdateLocalTopThree()
    {
        var sortedLocalSpirits = localCalculatedValues
            .OrderByDescending(pair => pair.Value)
            .Take(3)
            .Select(pair => pair.Key)
            .ToList();

        for (int i = 0; i < localTopThree.childCount; i++)
        {
            SetText(localTopThree.GetChild(i), i < sortedLocalSpirits.Count ? sortedLocalSpirits[i] : string.Empty);
        }
    }
    public void UpdateFlavourTable2LocalData(List<string> spiritNames, Dictionary<string, DataManager.SpiritInfo> spiritData)
    {
        var localRatingsTransform = FlavourTable2.GetChild(0);
        var localFlavoursTransform = FlavourTable2.GetChild(2);

        localFlavors.Clear();
        localCalculatedValues.Clear();

        for (int i = 0; i < 5; i++)
        {
            string ratingText = "0", flavourText = "0";
            if (i < spiritNames.Count && spiritData.TryGetValue(spiritNames[i], out var spirit))
            {
                ratingText = spirit.Rating.ToString();
                flavourText = spirit.SelectedFlavors.ToString();
                localFlavors[spiritNames[i]] = spirit.SelectedFlavors;
                
                // Calculate and store the local rating * local flavors
                float calculatedValue = spirit.Rating * spirit.SelectedFlavors;
                localCalculatedValues[spiritNames[i]] = calculatedValue;
            }

            SetText(localRatingsTransform.GetChild(i), ratingText);
            SetText(localFlavoursTransform.GetChild(i), flavourText);
        }

        UpdateLocalTopThree();
    }
    public float GetLocalDataRating(int index) => GetFloatFromChild(FlavourTable2.GetChild(0), index);
    public float GetLocalDataFlavour(int index) => GetFloatFromChild(FlavourTable2.GetChild(2), index);
    public float GetAverageRating(int index) => GetFloatFromChild(FlavourTable2.GetChild(1), index);
    public float GetAverageFlavour(int index) => GetFloatFromChild(FlavourTable2.GetChild(3), index);

    private float GetFloatFromChild(Transform parent, int index) =>
        float.TryParse(parent.GetChild(index).GetComponent<TMP_Text>().text, out float result) ? result : 0f;

    public void HideFetchingPlaceholders()
    {
        ClearTable(FlavourTable1_Flavour);
        ClearTable(FlavourTable1_Rating);
        ClearTable(summaryTable);
    }

    private void SetText(Transform textTransform, string text)
    {
        var tmpText = textTransform.GetComponent<TMP_Text>();
        if (tmpText != null)
            tmpText.text = text;
        else
        {
            var legacyText = textTransform.GetComponent<Text>();
            if (legacyText != null)
                legacyText.text = text;
            else
                Debug.LogError("No text component found on " + textTransform.name);
        }
    }

    private string GetText(Transform transform)
    {
        var tmpText = transform.GetComponent<TMP_Text>();
        return tmpText != null ? tmpText.text : transform.GetComponent<Text>().text;
    }

    private string GetRankString(int rank) =>
        rank switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => rank + "th"
        };

    public void UpdateTopThreeSpirits()
    {
        UpdateRankings();
    }
}