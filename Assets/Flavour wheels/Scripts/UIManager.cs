using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public class UIManager : MonoBehaviour
{
    public Transform localTopThree, generalTopThree, FlavourTable1a, FlavourTable1b, FlavourTable2;
    public GameObject flavourRowPrefab;
    private Dictionary<string, int> spiritRanks = new Dictionary<string, int>();
    private Dictionary<string, int> localFlavors = new Dictionary<string, int>();


    public void UpdateFlavourTable(List<DataManager.PlayerData> players)
    {
        ClearFlavourTables();
        int maxRowsToShow = Mathf.Min(players.Count, 10);
        for (int i = 0; i < maxRowsToShow; i++)
        {
            CreateFlavourTableRow(FlavourTable1a, i, players[i], true);
            CreateFlavourTableRow(FlavourTable1b, i, players[i], false);
        }
    }

    private void ClearFlavourTables()
    {
        foreach (Transform child in FlavourTable1a)
            Destroy(child.gameObject);
        foreach (Transform child in FlavourTable1b)
            Destroy(child.gameObject);
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

        SetStarRating(row.transform.GetChild(7), player.overallRating);
    }

    private void SetCellData(Transform cellTransform, int value, bool isFlavourTable)
    {
        cellTransform.GetComponent<Image>().color = GetCellColor(value, isFlavourTable);
        SetText(cellTransform.GetChild(0), value.ToString());
    }

    private Color GetCellColor(int value, bool isFlavourTable)
    {
        float normalizedValue = isFlavourTable ? Mathf.Clamp01(value / 7f) : Mathf.Clamp01(value / 5f);
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
        var sortedLocalSpirits = localFlavors.OrderByDescending(pair => pair.Value).Take(3).Select(pair => pair.Key).ToList();
        for (int i = 0; i < localTopThree.childCount; i++)
            SetText(localTopThree.GetChild(i), i < sortedLocalSpirits.Count ? sortedLocalSpirits[i] : string.Empty);
    }

    public void UpdateFlavourTable2LocalData(List<string> spiritNames, Dictionary<string, DataManager.SpiritInfo> spiritData)
    {
        var localRatingsTransform = FlavourTable2.GetChild(0);
        var localFlavoursTransform = FlavourTable2.GetChild(2);

        localFlavors.Clear(); // Clear previous local flavors

        for (int i = 0; i < 5; i++)
        {
            string ratingText = "0", flavourText = "0";
            if (i < spiritNames.Count && spiritData.TryGetValue(spiritNames[i], out var spirit))
            {
                ratingText = spirit.Rating.ToString();
                flavourText = spirit.SelectedFlavors.ToString();
                localFlavors[spiritNames[i]] = spirit.SelectedFlavors; // Store local flavors
            }

            SetText(localRatingsTransform.GetChild(i), ratingText);
            SetText(localFlavoursTransform.GetChild(i), flavourText);
        }

        UpdateLocalTopThree(); // Update local top three after updating local data
    }
    public float GetLocalDataRating(int index) => GetFloatFromChild(FlavourTable2.GetChild(0), index);
    public float GetLocalDataFlavour(int index) => GetFloatFromChild(FlavourTable2.GetChild(2), index);
    public float GetAverageRating(int index) => GetFloatFromChild(FlavourTable2.GetChild(1), index);
    public float GetAverageFlavour(int index) => GetFloatFromChild(FlavourTable2.GetChild(3), index);

    private float GetFloatFromChild(Transform parent, int index) =>
        float.TryParse(parent.GetChild(index).GetComponent<TMP_Text>().text, out float result) ? result : 0f;

    public void HideFetchingPlaceholders() => ClearFlavourTables();

    private void SetRowData(GameObject row, int rowIndex, DataManager.PlayerData player)
    {
        SetText(row.transform.GetChild(0), (rowIndex + 1).ToString());
        SetText(row.transform.GetChild(1), player.username);

        int[] flavours = { player.spirit1Flavours, player.spirit2Flavours, player.spirit3Flavours, player.spirit4Flavours, player.spirit5Flavours };
        for (int i = 0; i < flavours.Length; i++)
            SetFlavourData(row.transform.GetChild(2 + i), flavours[i]);

        SetStarRating(row.transform.GetChild(7), player.overallRating);
    }

    private void SetText(Transform transform, string text)
    {
        var tmpText = transform.GetComponent<TMP_Text>();
        if (tmpText != null)
            tmpText.text = text;
        else
            transform.GetComponent<Text>().text = text;
    }

    private string GetText(Transform transform)
    {
        var tmpText = transform.GetComponent<TMP_Text>();
        return tmpText != null ? tmpText.text : transform.GetComponent<Text>().text;
    }

    private void SetFlavourData(Transform flavourTransform, int flavour)
    {
        flavourTransform.GetComponent<Image>().color = GetFlavorColor(flavour);
        SetText(flavourTransform.GetChild(0), flavour.ToString());
    }

    private void SetStarRating(Transform starRating, int rating)
    {
        for (int i = 0; i < starRating.childCount && i < 5; i++)
            starRating.GetChild(i).gameObject.SetActive(i < rating);
    }

    private string GetRankString(int rank) =>
        rank switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => rank + "th"
        };

    private Color GetFlavorColor(int flavor) =>
        Color.Lerp(Color.yellow, Color.green, Mathf.Clamp01(flavor / 7f));

    public void UpdateTopThreeSpirits()
    {
        UpdateRankings();
    }
}