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

    // List of UI elements to display spirit names.
    public List<Transform> SpiritNamesList;

    // Prefab used to create rows in FlavourTable1.
    public GameObject flavourRowPrefab;

    // Placeholder UI element shown when data is being fetched.
    public Transform fetching;

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

    // Updates FlavourTable2 with average data (ratings and flavours).
    public void UpdateFlavourTable2AverageData(decimal[] averageRatings, decimal[] averageFlavours)
    {
        // Get references to the specific UI elements within FlavourTable2 for ratings, flavours, and their product.
        Transform averageRatingsTransform = FlavourTable2.GetChild(1);
        Transform averageFlavoursTransform = FlavourTable2.GetChild(3);
        Transform multipliedTransform = FlavourTable2.GetChild(4);

        // Set the text values for each of the five spirit slots.
        for (int i = 0; i < 5; i++)
        {
            SetText(averageRatingsTransform.GetChild(i), averageRatings[i].ToString("F2"));
            SetText(averageFlavoursTransform.GetChild(i), averageFlavours[i].ToString("F2"));
            SetText(multipliedTransform.GetChild(i), (averageRatings[i] * averageFlavours[i]).ToString("F2"));
        }

        // Update the rankings based on the calculated products.
        UpdateFlavourTable2Ranks();
    }

    // Updates the ranking in FlavourTable2 based on multiplied values
    public void UpdateFlavourTable2Ranks()
    {
        // Get references to the multiplied values and rank UI elements.
        Transform multipliedTransform = FlavourTable2.GetChild(4);
        Transform ranksTransform = FlavourTable2.GetChild(5);

        decimal[] multipliedValues = new decimal[5];

        // Parse the multiplied values from the UI.
        for (int i = 0; i < multipliedValues.Length; i++)
        {
            if (decimal.TryParse(multipliedTransform.GetChild(i).GetComponent<TMP_Text>().text, out decimal value))
            {
                multipliedValues[i] = value;
            }
            else
            {
                multipliedValues[i] = 0;
            }
        }

        // Sort the values to determine the rankings.
        decimal[] sortedValues = multipliedValues.OrderByDescending(v => v).ToArray();

        // Update the rank UI elements with the appropriate rank strings.
        for (int i = 0; i < multipliedValues.Length; i++)
        {
            int rank = System.Array.IndexOf(sortedValues, multipliedValues[i]) + 1;
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

    public void UpdateLocalTopThree(List<DataManager.SpiritInfo> sortedSpirits)
    {
        UpdateTopThree(localTopThree, sortedSpirits, "Local");
    }

    public void UpdateGeneralTopThree(List<DataManager.SpiritInfo> sortedSpirits)
    {
        UpdateTopThree(generalTopThree, sortedSpirits, "General");
    }

    // Updates the spirit names list in the UI
    public void UpdateSpiritNamesList(List<string> spiritNames)
    {
        foreach (var transform in SpiritNamesList)
        {
            TMP_Text[] textComponents = transform.GetComponentsInChildren<TMP_Text>();
            for (int i = 0; i < textComponents.Length; i++)
            {
                textComponents[i].text = i < spiritNames.Count ? spiritNames[i] : string.Empty;
            }
        }
    }

    // Updates FlavourTable2 with local spirit data
    public void UpdateFlavourTable2LocalData(List<string> spiritNames, Dictionary<string, DataManager.SpiritInfo> spiritData)
    {
        Transform localRatingsTransform = FlavourTable2.GetChild(0);
        Transform localFlavoursTransform = FlavourTable2.GetChild(2);

        for (int i = 0; i < 5; i++)
        {
            if (i < spiritNames.Count && spiritData.ContainsKey(spiritNames[i]))
            {
                SetText(localRatingsTransform.GetChild(i), spiritData[spiritNames[i]].Rating.ToString());
                SetText(localFlavoursTransform.GetChild(i), spiritData[spiritNames[i]].SelectedFlavors.ToString());
            }
            else
            {
                SetText(localRatingsTransform.GetChild(i), "0");
                SetText(localFlavoursTransform.GetChild(i), "0");
            }
        }
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

    // Shows placeholder rows in FlavourTable1
    public void ShowFetchingPlaceholders()
    {
        Debug.Log("Showing fetching placeholders");
        ClearFlavourTable1();
        if (fetching == null)
        {
            Debug.LogError("Fetching prefab is not assigned!");
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            GameObject placeholder = Instantiate(fetching.gameObject, FlavourTable1);
            if (placeholder != null)
            {
                placeholder.transform.SetAsLastSibling();
                Debug.Log($"Created fetching placeholder {i + 1}");
            }
            else
            {
                Debug.LogError($"Failed to instantiate fetching placeholder {i + 1}");
            }
        }
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
        TMP_Text tmpText = transform.GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.text = text;
        }
        else
        {
            Text legacyText = transform.GetComponent<Text>();
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
            case 4: return "4th";
            case 5: return "5th";
            default: return rank.ToString();
        }
    }

    // Returns a color based on flavour value, between yellow and green
    private Color GetFlavorColor(int flavor)
    {
        float t = Mathf.Clamp01((float)flavor / 7f);
        return Color.Lerp(Color.yellow, Color.green, t);
    }

    // This method is what DataManager calls to update the top three spirits in UIManager
    public void UpdateTopThreeSpirits(List<DataManager.SpiritInfo> sortedSpirits)
    {
        UpdateLocalTopThree(sortedSpirits);
        UpdateGeneralTopThree(sortedSpirits);
    }
}