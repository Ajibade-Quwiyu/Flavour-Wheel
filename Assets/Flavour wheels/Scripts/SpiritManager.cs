using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MySql.Data.MySqlClient;

public class SpiritManager : MonoBehaviour
{
    public class SpiritInfo
    {
        public string Name; // Name of the spirit
        public int SelectedFlavors; // Number of selected flavors
        public int Rating; // Rating of the spirit

        public SpiritInfo(string name)
        {
            Name = name;
            SelectedFlavors = 0;
            Rating = 0;
        }
    }

    public Transform topThreeSpiritsText; // Transform housing three Unity Text components
    private Text[] topThreeTexts; // Array to hold references to the Text components
    public string playerName; // Player's name
    public TextMeshProUGUI allPlayerDataText; // TextMeshPro component to display all player data
    public Transform Playerdata; // Transform housing player data children

    private Dictionary<FlavorWheelDataRecorder.Spirit, SpiritInfo> spiritData = new Dictionary<FlavorWheelDataRecorder.Spirit, SpiritInfo>();
    private string connectionString = "Server=sql8.freesqldatabase.com; Database=sql8721580; User=sql8721580; Password=6wdc5VDnaQ; Charset=utf8;";

    void Start()
    {
        // Initialize the spirit data
        spiritData.Add(FlavorWheelDataRecorder.Spirit.Spirit1, new SpiritInfo("Spirit1"));
        spiritData.Add(FlavorWheelDataRecorder.Spirit.Spirit2, new SpiritInfo("Spirit2"));
        spiritData.Add(FlavorWheelDataRecorder.Spirit.Spirit3, new SpiritInfo("Spirit3"));
        spiritData.Add(FlavorWheelDataRecorder.Spirit.Spirit4, new SpiritInfo("Spirit4"));
        spiritData.Add(FlavorWheelDataRecorder.Spirit.Spirit5, new SpiritInfo("Spirit5"));

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

        // Initialize the TextMeshPro component with a default message
        if (allPlayerDataText != null)
        {
            allPlayerDataText.text = "No data available yet.";
        }

        // Get and display all player data from the database
        GetAllPlayerData();
    }

    public void UpdateSpiritData(FlavorWheelDataRecorder.Spirit spiritType, int selectedFlavors, int rating)
    {
        // Update spirit data with new information
        if (spiritData.ContainsKey(spiritType))
        {
            var spiritInfo = spiritData[spiritType];
            spiritInfo.SelectedFlavors = selectedFlavors;
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
            Debug.Log($"Name: {info.Name}, Selected Flavors: {info.SelectedFlavors}, Rating: {info.Rating}");
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

    public void SendDataToDatabaseAndRefresh()
    {
        // Send player data to the database
        SendDataToDatabase();

        // Get and display all player data from the database
        GetAllPlayerData();
    }

    private void SendDataToDatabase()
    {
        // Send player data to the database
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            string query = "INSERT INTO Douglas (PlayerName, Spirit1, Spirit2, Spirit3, Spirit4, Spirit5, Rating) VALUES (@PlayerName, @Spirit1, @Spirit2, @Spirit3, @Spirit4, @Spirit5, @Rating)";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@PlayerName", playerName);
                command.Parameters.AddWithValue("@Spirit1", spiritData[FlavorWheelDataRecorder.Spirit.Spirit1].SelectedFlavors);
                command.Parameters.AddWithValue("@Spirit2", spiritData[FlavorWheelDataRecorder.Spirit.Spirit2].SelectedFlavors);
                command.Parameters.AddWithValue("@Spirit3", spiritData[FlavorWheelDataRecorder.Spirit.Spirit3].SelectedFlavors);
                command.Parameters.AddWithValue("@Spirit4", spiritData[FlavorWheelDataRecorder.Spirit.Spirit4].SelectedFlavors);
                command.Parameters.AddWithValue("@Spirit5", spiritData[FlavorWheelDataRecorder.Spirit.Spirit5].SelectedFlavors);
                command.Parameters.AddWithValue("@Rating", CalculateOverallRating());
                command.ExecuteNonQuery();
            }
        }
    }

    private int CalculateOverallRating()
    {
        // Calculate the overall rating based on individual ratings
        int totalRating = 0;
        foreach (var spirit in spiritData)
        {
            totalRating += spirit.Value.Rating;
        }
        return totalRating / spiritData.Count;
    }

    public void GetAllPlayerData()
    {
        // Get all player data from the database
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            string query = "SELECT * FROM Douglas";
            using (var command = new MySqlCommand(query, connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    string allData = "";
                    if (reader.HasRows)
                    {
                        // Log available columns
                        List<string> columns = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            columns.Add(reader.GetName(i));
                        }

                        int playerIndex = 0;
                        while (reader.Read() && playerIndex < Playerdata.childCount)
                        {
                            try
                            {
                                string playerName = columns.Contains("PlayerName") ? reader.GetString(reader.GetOrdinal("PlayerName")) : "N/A";
                                int spirit1 = columns.Contains("Spirit1") ? reader.GetInt32(reader.GetOrdinal("Spirit1")) : 0;
                                int spirit2 = columns.Contains("Spirit2") ? reader.GetInt32(reader.GetOrdinal("Spirit2")) : 0;
                                int spirit3 = columns.Contains("Spirit3") ? reader.GetInt32(reader.GetOrdinal("Spirit3")) : 0;
                                int spirit4 = columns.Contains("Spirit4") ? reader.GetInt32(reader.GetOrdinal("Spirit4")) : 0;
                                int spirit5 = columns.Contains("Spirit5") ? reader.GetInt32(reader.GetOrdinal("Spirit5")) : 0;
                                int rating = columns.Contains("Rating") ? reader.GetInt32(reader.GetOrdinal("Rating")) : 0;

                                Transform playerDataChild = Playerdata.GetChild(playerIndex);
                                playerDataChild.GetChild(0).GetComponent<TextMeshProUGUI>().text = playerName;
                                playerDataChild.GetChild(1).GetComponent<TextMeshProUGUI>().text = spirit1.ToString();
                                playerDataChild.GetChild(2).GetComponent<TextMeshProUGUI>().text = spirit2.ToString();
                                playerDataChild.GetChild(3).GetComponent<TextMeshProUGUI>().text = spirit3.ToString();
                                playerDataChild.GetChild(4).GetComponent<TextMeshProUGUI>().text = spirit4.ToString();
                                playerDataChild.GetChild(5).GetComponent<TextMeshProUGUI>().text = spirit5.ToString();
                                playerDataChild.GetChild(6).GetComponent<TextMeshProUGUI>().text = rating.ToString();

                                playerIndex++;

                                allData += $"Player: {playerName}, Spirit1: {spirit1}, Spirit2: {spirit2}, Spirit3: {spirit3}, Spirit4: {spirit4}, Spirit5: {spirit5}, Rating: {rating}\n";
                            }
                            catch (MySqlException e)
                            {
                                Debug.LogError($"Error reading data: {e.Message}");
                            }
                        }
                    }
                    else
                    {
                        allData = "No data available yet.";
                    }

                    if (allPlayerDataText != null)
                    {
                        allPlayerDataText.text = allData;
                    }
                }
            }
        }
    }
}
