using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class UIManager : MonoBehaviour
{
    // UI elements for displaying the top three spirits locally and globally.
    public Transform localTopThree;
    public Transform generalTopThree;

    // UI elements for displaying flavour data in table format.
    public Transform FlavourTable1;
    public Transform FlavourTable2;

    // Prefab used to create rows in FlavourTable1.
    public GameObject flavourRowPrefab;

    // Updates FlavourTable1 with player data
    public void UpdateFlavourTable(List<DataManager.PlayerData> players)
    {
        if (players == null)
        {
            Debug.LogError("Players list is null");
            return;
        }

        Debug.Log($"Updating FlavourTable with {players.Count} players");
        ClearFlavourTable1();

        int maxRowsToShow = Mathf.Min(players.Count, 10); // Limit to 10 rows for performance
        for (int i = 0; i < maxRowsToShow; i++)
        {
            CreateFlavourTableRow(i, players[i]);
        }

        if (players.Count > maxRowsToShow)
        {
            Debug.Log($"Showing only first {maxRowsToShow} players out of {players.Count}");
        }
    }

    private void ClearFlavourTable1()
    {
        if (FlavourTable1 == null)
        {
            Debug.LogError("FlavourTable1 is not assigned!");
            return;
        }

        foreach (Transform child in FlavourTable1)
        {
            Destroy(child.gameObject);
        }
        Debug.Log("Cleared FlavourTable1");
    }

      public void UpdateFlavourTable2AverageData(float[] averageRatings, float[] averageFlavours)
    {
        if (FlavourTable2 == null)
        {
            Debug.LogError("FlavourTable2 is not assigned!");
            return;
        }

        Debug.Log("Updating FlavourTable2 average data");

        Transform averageRatingsTransform = FlavourTable2.GetChild(1);
        Transform averageFlavoursTransform = FlavourTable2.GetChild(3);
        Transform multipliedTransform = FlavourTable2.GetChild(4);

        float[] multipliedValues = new float[5];

        for (int i = 0; i < 5; i++)
        {
            SetText(averageRatingsTransform.GetChild(i), averageRatings[i].ToString("F2"));
            SetText(averageFlavoursTransform.GetChild(i), averageFlavours[i].ToString("F2"));
            multipliedValues[i] = averageRatings[i] * averageFlavours[i];
            SetText(multipliedTransform.GetChild(i), multipliedValues[i].ToString("F2"));
        }

        UpdateFlavourTable2Ranks(multipliedValues);
    }

      private void UpdateFlavourTable2Ranks(float[] multipliedValues)
    {
        if (FlavourTable2 == null)
        {
            Debug.LogError("FlavourTable2 is not assigned!");
            return;
        }

        Debug.Log("Updating FlavourTable2 ranks");

        Transform ranksTransform = FlavourTable2.GetChild(5);

        var sortedIndices = Enumerable.Range(0, multipliedValues.Length)
                                      .OrderByDescending(i => multipliedValues[i])
                                      .ToArray();

        for (int i = 0; i < multipliedValues.Length; i++)
        {
            int rank = System.Array.IndexOf(sortedIndices, i) + 1;
            SetText(ranksTransform.GetChild(i), GetRankString(rank));
        }
    }


       private void UpdateTopThree(Transform topThreeTransform, List<DataManager.SpiritInfo> sortedSpirits, string debugPrefix)
    {
        Debug.Log($"Updating {debugPrefix}TopThree with {sortedSpirits.Count} spirits");
        for (int i = 0; i < topThreeTransform.childCount; i++)
        {
            string spiritName = i < sortedSpirits.Count ? sortedSpirits[i].Name : string.Empty;
            SetText(topThreeTransform.GetChild(i), spiritName);
            Debug.Log($"Set {debugPrefix}TopThree[{i}] to {spiritName}");
        }
    }
public void UpdateFlavourTable2(List<string> spiritNames, Dictionary<string, DataManager.SpiritInfo> spiritData, float[] averageRatings, float[] averageFlavours)
    {
        if (FlavourTable2 == null)
        {
            Debug.LogError("FlavourTable2 is not assigned!");
            return;
        }

        Debug.Log("Starting comprehensive update of FlavourTable2");

        UpdateLocalData(spiritNames, spiritData);
        UpdateAverageData(averageRatings, averageFlavours);
        UpdateMultipliedValues();
        UpdateRankings();

        Debug.Log("FlavourTable2 update completed");
    }
 private void UpdateMultipliedValues()
    {
        Debug.Log("Updating FlavourTable2 multiplied values");

        Transform averageRatingsTransform = FlavourTable2.GetChild(1);
        Transform averageFlavoursTransform = FlavourTable2.GetChild(3);
        Transform multipliedTransform = FlavourTable2.GetChild(4);

        for (int i = 0; i < 5; i++)
        {
            float avgRating = float.Parse(GetText(averageRatingsTransform.GetChild(i)));
            float avgFlavour = float.Parse(GetText(averageFlavoursTransform.GetChild(i)));
            float multipliedValue = avgRating * avgFlavour;

            string multipliedText = multipliedValue.ToString("F2");

            Debug.Log($"Setting multiplied value for spirit {i}: {multipliedText}");
            SetText(multipliedTransform.GetChild(i), multipliedText);
            Debug.Log($"Multiplied value after set: {GetText(multipliedTransform.GetChild(i))}");
        }
    }
    private void UpdateLocalData(List<string> spiritNames, Dictionary<string, DataManager.SpiritInfo> spiritData)
    {
        Debug.Log("Updating FlavourTable2 local data");

        Transform localRatingsTransform = FlavourTable2.GetChild(0);
        Transform localFlavoursTransform = FlavourTable2.GetChild(2);

        for (int i = 0; i < 5; i++)
        {
            string ratingText = "0";
            string flavourText = "0";

            if (i < spiritNames.Count && spiritData.ContainsKey(spiritNames[i]))
            {
                var spirit = spiritData[spiritNames[i]];
                ratingText = spirit.Rating.ToString();
                flavourText = spirit.SelectedFlavors.ToString();
            }

            Debug.Log($"Setting local rating for spirit {i}: {ratingText}");
            SetText(localRatingsTransform.GetChild(i), ratingText);
            Debug.Log($"Local rating after set: {GetText(localRatingsTransform.GetChild(i))}");

            Debug.Log($"Setting local flavour for spirit {i}: {flavourText}");
            SetText(localFlavoursTransform.GetChild(i), flavourText);
            Debug.Log($"Local flavour after set: {GetText(localFlavoursTransform.GetChild(i))}");
        }
    }
    private void UpdateAverageData(float[] averageRatings, float[] averageFlavours)
    {
        Debug.Log("Updating FlavourTable2 average data");

        Transform averageRatingsTransform = FlavourTable2.GetChild(1);
        Transform averageFlavoursTransform = FlavourTable2.GetChild(3);

        for (int i = 0; i < 5; i++)
        {
            string avgRatingText = averageRatings[i].ToString("F2");
            string avgFlavourText = averageFlavours[i].ToString("F2");

            Debug.Log($"Setting average rating for spirit {i}: {avgRatingText}");
            SetText(averageRatingsTransform.GetChild(i), avgRatingText);
            Debug.Log($"Average rating after set: {GetText(averageRatingsTransform.GetChild(i))}");

            Debug.Log($"Setting average flavour for spirit {i}: {avgFlavourText}");
            SetText(averageFlavoursTransform.GetChild(i), avgFlavourText);
            Debug.Log($"Average flavour after set: {GetText(averageFlavoursTransform.GetChild(i))}");
        }
    }

     public void UpdateRankings()
    {
        Debug.Log("Updating FlavourTable2 rankings");

        Transform multipliedTransform = FlavourTable2.GetChild(4);
        Transform ranksTransform = FlavourTable2.GetChild(5);

        float[] multipliedValues = new float[5];
        for (int i = 0; i < 5; i++)
        {
            multipliedValues[i] = float.Parse(GetText(multipliedTransform.GetChild(i)));
        }

        var sortedIndices = Enumerable.Range(0, multipliedValues.Length)
                                      .OrderByDescending(i => multipliedValues[i])
                                      .ToArray();

        for (int i = 0; i < multipliedValues.Length; i++)
        {
            int rank = System.Array.IndexOf(sortedIndices, i) + 1;
            string rankText = GetRankString(rank);

            Debug.Log($"Setting rank for spirit {i}: {rankText}");
            SetText(ranksTransform.GetChild(i), rankText);
            Debug.Log($"Rank after set: {GetText(ranksTransform.GetChild(i))}");
        }
    }
     public void UpdateLocalTopThree(List<DataManager.SpiritInfo> sortedSpirits)
    {
        for (int i = 0; i < localTopThree.childCount; i++)
        {
            string spiritName = i < sortedSpirits.Count ? sortedSpirits[i].Name : string.Empty;
            SetText(localTopThree.GetChild(i), spiritName);
        }
    }

    public void UpdateGeneralTopThree(List<string> sortedSpirits)
    {
        for (int i = 0; i < generalTopThree.childCount; i++)
        {
            string spiritName = i < sortedSpirits.Count ? sortedSpirits[i] : string.Empty;
            SetText(generalTopThree.GetChild(i), spiritName);
        }
    }

   public void UpdateFlavourTable2LocalData(List<string> spiritNames, Dictionary<string, DataManager.SpiritInfo> spiritData)
{
    if (FlavourTable2 == null)
    {
        Debug.LogError("FlavourTable2 is not assigned!");
        return;
    }

    Debug.Log("Updating FlavourTable2 local data");

    Transform localRatingsTransform = FlavourTable2.GetChild(0);
    Transform localFlavoursTransform = FlavourTable2.GetChild(2);

    if (localRatingsTransform == null || localFlavoursTransform == null)
    {
        Debug.LogError("FlavourTable2 child transforms are not set up correctly!");
        return;
    }

    for (int i = 0; i < 5; i++)
    {
        string ratingText = "0";
        string flavourText = "0";

        if (i < spiritNames.Count && spiritData.ContainsKey(spiritNames[i]))
        {
            var spirit = spiritData[spiritNames[i]];
            ratingText = spirit.Rating.ToString();
            flavourText = spirit.SelectedFlavors.ToString();
        }

        if (i < localRatingsTransform.childCount && i < localFlavoursTransform.childCount)
        {
            SetText(localRatingsTransform.GetChild(i), ratingText);
            SetText(localFlavoursTransform.GetChild(i), flavourText);
        }
        else
        {
            Debug.LogWarning($"FlavourTable2 is missing child at index {i}");
        }
    }

    Debug.Log("FlavourTable2 local data updated successfully");
}
    // Retrieves local data rating from FlavourTable2
    public float GetLocalDataRating(int index)
    {
        Transform localRatingsTransform = FlavourTable2.GetChild(0);
        return float.TryParse(localRatingsTransform.GetChild(index).GetComponent<TMP_Text>().text, out float result) ? result : 0f;
    }

    // Retrieves local data flavor from FlavourTable2
    public float GetLocalDataFlavour(int index)
    {
        Transform localFlavoursTransform = FlavourTable2.GetChild(2);
        return float.TryParse(localFlavoursTransform.GetChild(index).GetComponent<TMP_Text>().text, out float result) ? result : 0f;
    }

    // Retrieves average rating from FlavourTable2
    public float GetAverageRating(int index)
    {
        Transform averageRatingsTransform = FlavourTable2.GetChild(1);
        return float.TryParse(averageRatingsTransform.GetChild(index).GetComponent<TMP_Text>().text, out float result) ? result : 0f;
    }

    // Retrieves average flavor from FlavourTable2
    public float GetAverageFlavour(int index)
    {
        Transform averageFlavoursTransform = FlavourTable2.GetChild(3);
        return float.TryParse(averageFlavoursTransform.GetChild(index).GetComponent<TMP_Text>().text, out float result) ? result : 0f;
    }
    // Hides placeholder rows in FlavourTable1
    public void HideFetchingPlaceholders()
    {
        ClearFlavourTable1();
    }

    private void CreateFlavourTableRow(int rowIndex, DataManager.PlayerData player)
    {
        if (flavourRowPrefab == null)
        {
            Debug.LogError("flavourRowPrefab is null");
            return;
        }

        GameObject row = Instantiate(flavourRowPrefab, FlavourTable1);
        if (row != null)
        {
            SetRowData(row, rowIndex, player);
        }
    }

    private void SetRowData(GameObject row, int rowIndex, DataManager.PlayerData player)
    {
        SetText(row.transform.GetChild(0), (rowIndex + 1).ToString()); // Row number
        SetText(row.transform.GetChild(1), player.username); // Username

        int[] flavours = { player.spirit1Flavours, player.spirit2Flavours, player.spirit3Flavours, player.spirit4Flavours, player.spirit5Flavours };
        for (int i = 0; i < flavours.Length; i++)
        {
            SetFlavourData(row.transform.GetChild(2 + i), flavours[i]);
        }

        SetStarRating(row.transform.GetChild(7), player.overallRating);
    }

    // Sets the text of a UI element
      private void SetText(Transform transform, string text)
    {
        var tmpText = transform.GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.text = text;
        }
        else
        {
            var legacyText = transform.GetComponent<Text>();
            if (legacyText != null)
            {
                legacyText.text = text;
            }
            else
            {
                Debug.LogWarning($"No text component found on {transform.name}");
            }
        }
    }

    private string GetText(Transform transform)
    {
        var tmpText = transform.GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            return tmpText.text;
        }
        else
        {
            var legacyText = transform.GetComponent<Text>();
            if (legacyText != null)
            {
                return legacyText.text;
            }
            else
            {
                Debug.LogWarning($"No text component found on {transform.name}");
                return string.Empty;
            }
        }
    }

    // Sets the flavour data (color and text) for a flavour UI element
    private void SetFlavourData(Transform flavourTransform, int flavour)
    {
        Image flavourImage = flavourTransform.GetComponent<Image>();
        if (flavourImage != null)
        {
            flavourImage.color = GetFlavorColor(flavour);
        }

        SetText(flavourTransform.GetChild(0), flavour.ToString());
    }

    // Sets the star rating UI elements based on rating value
    private void SetStarRating(Transform starRating, int rating)
    {
        for (int i = 0; i < 5; i++)
        {
            if (i < starRating.childCount)
            {
                starRating.GetChild(i).gameObject.SetActive(i < rating);
            }
        }
    }

    // Converts an integer rank to its ordinal representation
      private string GetRankString(int rank)
    {
        switch (rank)
        {
            case 1: return "1st";
            case 2: return "2nd";
            case 3: return "3rd";
            default: return rank + "th";
        }
    }

    // Returns a color based on flavour value, between yellow and green
    private Color GetFlavorColor(int flavor)
    {
        float t = Mathf.Clamp01((float)flavor / 7f);
        return Color.Lerp(Color.yellow, Color.green, t);
    }

    // This method is what DataManager calls to update the top three spirits in UIManager
      public void UpdateTopThreeSpirits(List<DataManager.SpiritInfo> localSortedSpirits, List<string> generalSortedSpirits)
    {
        UpdateLocalTopThree(localSortedSpirits);
        UpdateGeneralTopThree(generalSortedSpirits);
    }
}