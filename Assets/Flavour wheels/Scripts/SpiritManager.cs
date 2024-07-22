using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpiritManager : MonoBehaviour
{
    public class SpiritInfo
    {
        public string Name; // Name of the spirit
        public int SelectedFlavors; // Number of selected flavors
        public float AverageSelectedFlavors; // Average number of selected flavors
        public int Rating; // Rating of the spirit

        public SpiritInfo(string name)
        {
            Name = name;
            SelectedFlavors = 0;
            AverageSelectedFlavors = 0f;
            Rating = 0;
        }
    }

    public Transform topThreeSpiritsText; // Transform housing three Unity Text components
    private Text[] topThreeTexts; // Array to hold references to the Text components

    private Dictionary<FlavorWheelDataRecorder.Spirit, SpiritInfo> spiritData = new Dictionary<FlavorWheelDataRecorder.Spirit, SpiritInfo>();

    void Start()
    {
        // Initialize the spirit data
        spiritData.Add(FlavorWheelDataRecorder.Spirit.Spirit1, new SpiritInfo("Spirit 1"));
        spiritData.Add(FlavorWheelDataRecorder.Spirit.Spirit2, new SpiritInfo("Spirit 2"));
        spiritData.Add(FlavorWheelDataRecorder.Spirit.Spirit3, new SpiritInfo("Spirit 3"));
        spiritData.Add(FlavorWheelDataRecorder.Spirit.Spirit4, new SpiritInfo("Spirit 4"));
        spiritData.Add(FlavorWheelDataRecorder.Spirit.Spirit5, new SpiritInfo("Spirit 5"));

        // Initialize the top three Text components
        if (topThreeSpiritsText != null && topThreeSpiritsText.childCount >= 3)
        {
            topThreeTexts = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                topThreeTexts[i] = topThreeSpiritsText.GetChild(i).GetComponent<Text>();
            }
        }
        else
        {
            Debug.LogError("TopThreeSpiritsText is not set properly or does not have at least three children.");
        }
    }

    public void UpdateSpiritData(FlavorWheelDataRecorder.Spirit spiritType, int selectedFlavors, int rating)
    {
        // Update spirit data with new information
        if (spiritData.ContainsKey(spiritType))
        {
            var spiritInfo = spiritData[spiritType];
            spiritInfo.SelectedFlavors = selectedFlavors;
            spiritInfo.AverageSelectedFlavors = (spiritInfo.AverageSelectedFlavors + selectedFlavors) / 2.0f; // Calculate the new average
            spiritInfo.Rating = rating;

            // Update the top three spirits display
            UpdateTopThreeSpirits();
        }
    }

    public SpiritInfo GetSpiritInfo(FlavorWheelDataRecorder.Spirit spiritType)
    {
        // Get information about a specific spirit
        if (spiritData.ContainsKey(spiritType))
        {
            return spiritData[spiritType];
        }
        return null;
    }

    public void DebugAllSpiritInfo()
    {
        // Debug information for all spirits
        foreach (var spirit in spiritData)
        {
            var info = spirit.Value;
            Debug.Log($"Name: {info.Name}, Selected Flavors: {info.SelectedFlavors}, Average Selected Flavors: {info.AverageSelectedFlavors}, Rating: {info.Rating}");
        }
    }

    private void UpdateTopThreeSpirits()
    {
        // Get the top three spirits by rating
        var topThreeSpirits = new List<SpiritInfo>(spiritData.Values);
        topThreeSpirits.Sort((x, y) => y.Rating.CompareTo(x.Rating));

        // Update the Text components with the names of the top three spirits
        for (int i = 0; i < topThreeTexts.Length; i++)
        {
            if (i < topThreeSpirits.Count)
            {
                topThreeTexts[i].text = topThreeSpirits[i].Name;
            }
            else
            {
                topThreeTexts[i].text = string.Empty;
            }
        }
    }
}
