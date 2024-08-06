using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MySql.Data.MySqlClient;
using System;
using System.Linq;
using UnityEngine.UI;

public class SpiritManager : MonoBehaviour
{
    public class SpiritInfo
    {
        public string Name;
        public int SelectedFlavors;
        public int Rating;

        public SpiritInfo(string name, int selectedFlavors = 0, int rating = 0)
        {
            Name = name;
            SelectedFlavors = selectedFlavors;
            Rating = rating;
        }
    }

    public Transform localTopThree;
    public TMP_Text usernameText;
    public List<Transform> SpiritNamesList;

    public Transform FlavourTable1; // Transform with several children, each having 7 TMP text components
    public Transform FlavourTable2; // Transform with 6 children, each having 5 TMP text components
    public Transform generalTopThree; // Transform with three children, each having one TMP text component

    private Dictionary<string, SpiritInfo> spiritData = new Dictionary<string, SpiritInfo>();
    private string connectionString = "Server=sql8.freesqldatabase.com; Database=sql8721580; User=sql8721580; Password=6wdc5VDnaQ; Charset=utf8;";
    private string username;
    private string email;
    private int overallRating;
    private string feedback;

    private List<string> spiritNames = new List<string>();
    private bool isUpdating = false;

    private const string UsernameKey = "Username";
    private const string EmailKey = "Email";

    public void ReceiveSpiritData(string spiritName, int selectedFlavors, int rating)
    {
        if (spiritData.ContainsKey(spiritName))
        {
            spiritData[spiritName].SelectedFlavors = selectedFlavors;
            spiritData[spiritName].Rating = rating;
        }
        else
        {
            spiritData.Add(spiritName, new SpiritInfo(spiritName, selectedFlavors, rating));
        }

        UpdateTopThreeSpirits();
        UpdateFlavourTable2LocalData();
    }

    public void SetUserData(string username, string email, int overallRating, string feedback)
    {
        Debug.Log($"SetUserData called with: username={username}, email={email}, overallRating={overallRating}, feedback={feedback}");
        this.username = string.IsNullOrEmpty(username) ? PlayerPrefs.GetString(UsernameKey, "DefaultUsername") : username;
        this.email = string.IsNullOrEmpty(email) ? PlayerPrefs.GetString(EmailKey, "DefaultEmail@example.com") : email;
        this.overallRating = overallRating == 0 ? 0 : overallRating;
        this.feedback = string.IsNullOrEmpty(feedback) ? "N/A" : feedback;

        Debug.Log($"After checking PlayerPrefs: username={this.username}, email={this.email}, overallRating={this.overallRating}, feedback={this.feedback}");
    }

    private void UpdateTopThreeSpirits()
    {
        List<SpiritInfo> sortedSpirits = new List<SpiritInfo>(spiritData.Values);
        sortedSpirits.Sort((a, b) => b.Rating.CompareTo(a.Rating));

        for (int i = 0; i < localTopThree.childCount && i < sortedSpirits.Count; i++)
        {
            localTopThree.GetChild(i).GetComponent<Text>().text = sortedSpirits[i].Name;
        }

        for (int i = sortedSpirits.Count; i < localTopThree.childCount; i++)
        {
            localTopThree.GetChild(i).GetComponent<Text>().text = string.Empty;
        }

        UpdateSpiritNamesList();
    }

    private void UpdateSpiritNamesList()
    {
        string[] spiritNamesArray = new string[5];
        for (int i = 0; i < 5; i++)
        {
            spiritNamesArray[i] = "";
        }

        // Populate spiritNames array with actual values
        int spiritIndex = 0;
        foreach (var spirit in spiritData.Values)
        {
            if (spiritIndex < 5)
            {
                spiritNamesArray[spiritIndex] = spirit.Name;
                spiritIndex++;
            }
        }

        // Update the TMP_Text components in each transform in SpiritNamesList
        foreach (var transform in SpiritNamesList)
        {
            TMP_Text[] textComponents = transform.GetComponentsInChildren<TMP_Text>();
            for (int i = 0; i < textComponents.Length; i++)
            {
                if (i < spiritNamesArray.Length)
                {
                    textComponents[i].text = spiritNamesArray[i];
                }
                else
                {
                    textComponents[i].text = string.Empty;
                }
            }
        }

        // Save the spirit names for later use
        spiritNames = new List<string>(spiritNamesArray);
    }

    private void UpdateFlavourTable2LocalData()
    {
        Transform localRatingsTransform = FlavourTable2.GetChild(0);
        Transform localFlavoursTransform = FlavourTable2.GetChild(2);

        int[] spiritRatings = new int[5];
        int[] spiritFlavours = new int[5];
        int spiritIndex = 0;

        foreach (var spirit in spiritData.Values)
        {
            if (spiritIndex < 5)
            {
                spiritRatings[spiritIndex] = spirit.Rating;
                spiritFlavours[spiritIndex] = spirit.SelectedFlavors;
                spiritIndex++;
            }
        }

        for (int i = 0; i < spiritRatings.Length; i++)
        {
            localRatingsTransform.GetChild(i).GetComponent<TMP_Text>().text = spiritRatings[i].ToString();
            localFlavoursTransform.GetChild(i).GetComponent<TMP_Text>().text = spiritFlavours[i].ToString();
        }
    }

    private void UpdateFlavourTable2AverageData(decimal[] averageRatings, decimal[] averageFlavours)
    {
        Transform averageRatingsTransform = FlavourTable2.GetChild(1);
        Transform averageFlavoursTransform = FlavourTable2.GetChild(3);
        Transform multipliedTransform = FlavourTable2.GetChild(4);

        for (int i = 0; i < averageRatings.Length; i++)
        {
            averageRatingsTransform.GetChild(i).GetComponent<TMP_Text>().text = averageRatings[i].ToString("F2");
            averageFlavoursTransform.GetChild(i).GetComponent<TMP_Text>().text = averageFlavours[i].ToString("F2");
            multipliedTransform.GetChild(i).GetComponent<TMP_Text>().text = (averageRatings[i] * averageFlavours[i]).ToString("F2");
        }

        UpdateFlavourTable2Ranks();
        UpdateGeneralTopThree();
    }

    private void UpdateFlavourTable2Ranks()
    {
        Transform multipliedTransform = FlavourTable2.GetChild(4);
        Transform ranksTransform = FlavourTable2.GetChild(5);

        decimal[] multipliedValues = new decimal[5];
        for (int i = 0; i < multipliedValues.Length; i++)
        {
            multipliedValues[i] = decimal.Parse(multipliedTransform.GetChild(i).GetComponent<TMP_Text>().text);
        }

        decimal[] sortedValues = multipliedValues.OrderByDescending(v => v).ToArray();

        for (int i = 0; i < multipliedValues.Length; i++)
        {
            int rank = Array.IndexOf(sortedValues, multipliedValues[i]) + 1;
            ranksTransform.GetChild(i).GetComponent<TMP_Text>().text = GetRankString(rank);
        }
    }

    private void UpdateGeneralTopThree()
    {
        Transform ranksTransform = FlavourTable2.GetChild(5);
        TMP_Text[] generalTopThreeTexts = new TMP_Text[3];

        for (int i = 0; i < 3; i++)
        {
            generalTopThreeTexts[i] = generalTopThree.GetChild(i).GetComponent<TMP_Text>();
        }

        for (int i = 0; i < ranksTransform.childCount; i++)
        {
            string rankText = ranksTransform.GetChild(i).GetComponent<TMP_Text>().text;
            if (rankText == "1st")
            {
                generalTopThreeTexts[0].text = spiritNames[i];
            }
            else if (rankText == "2nd")
            {
                generalTopThreeTexts[1].text = spiritNames[i];
            }
            else if (rankText == "3rd")
            {
                generalTopThreeTexts[2].text = spiritNames[i];
            }
        }
    }

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

    private Color GetFlavorColor(int flavor)
    {
        float t = Mathf.Clamp01((float)flavor / 7f);
        return Color.Lerp(Color.yellow, Color.green, t);
    }

    public void SaveDataToPlayerDataTable()
    {
        Debug.Log($"Saving data to PlayerDataTable: username={username}, email={email}, overallRating={overallRating}, feedback={feedback}");

        // Log all spirit data being sent
        foreach (var spirit in spiritData.Values)
        {
            Debug.Log($"Spirit Name: {spirit.Name}, Selected Flavors: {spirit.SelectedFlavors}, Rating: {spirit.Rating}");
        }

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();

                // Check if username exists
                string checkQuery = "SELECT COUNT(*) FROM PlayerData WHERE Username = @Username";
                MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@Username", username);
                int userExists = Convert.ToInt32(checkCmd.ExecuteScalar());

                if (userExists > 0)
                {
                    // Delete existing record
                    string deleteQuery = "DELETE FROM PlayerData WHERE Username = @Username";
                    MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn);
                    deleteCmd.Parameters.AddWithValue("@Username", username);
                    deleteCmd.ExecuteNonQuery();
                }

                string query = "INSERT INTO PlayerData (Username, Email, Spirit1Name, Spirit2Name, Spirit3Name, Spirit4Name, Spirit5Name, " +
                               "Spirit1Flavours, Spirit2Flavours, Spirit3Flavours, Spirit4Flavours, Spirit5Flavours, " +
                               "Spirit1Ratings, Spirit2Ratings, Spirit3Ratings, Spirit4Ratings, Spirit5Ratings, " +
                               "OverallRating, Feedback) VALUES " +
                               "(@Username, @Email, @Spirit1Name, @Spirit2Name, @Spirit3Name, @Spirit4Name, @Spirit5Name, " +
                               "@Spirit1Flavours, @Spirit2Flavours, @Spirit3Flavours, @Spirit4Flavours, @Spirit5Flavours, " +
                               "@Spirit1Ratings, @Spirit2Ratings, @Spirit3Ratings, @Spirit4Ratings, @Spirit5Ratings, " +
                               "@OverallRating, @Feedback) " +
                               "ON DUPLICATE KEY UPDATE " +
                               "Email = VALUES(Email), Spirit1Name = VALUES(Spirit1Name), Spirit2Name = VALUES(Spirit2Name), Spirit3Name = VALUES(Spirit3Name), " +
                               "Spirit4Name = VALUES(Spirit4Name), Spirit5Name = VALUES(Spirit5Name), " +
                               "Spirit1Flavours = VALUES(Spirit1Flavours), Spirit2Flavours = VALUES(Spirit2Flavours), " +
                               "Spirit3Flavours = VALUES(Spirit3Flavours), Spirit4Flavours = VALUES(Spirit4Flavours), " +
                               "Spirit5Flavours = VALUES(Spirit5Flavours), " +
                               "Spirit1Ratings = VALUES(Spirit1Ratings), Spirit2Ratings = VALUES(Spirit2Ratings), " +
                               "Spirit3Ratings = VALUES(Spirit3Ratings), Spirit4Ratings = VALUES(Spirit4Ratings), " +
                               "Spirit5Ratings = VALUES(Spirit5Ratings), " +
                               "OverallRating = VALUES(OverallRating), Feedback = VALUES(Feedback);";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@OverallRating", overallRating);
                cmd.Parameters.AddWithValue("@Feedback", feedback);

                // Initialize the spirit parameters to null or default values
                for (int i = 1; i <= 5; i++)
                {
                    cmd.Parameters.Add(new MySqlParameter($"@Spirit{i}Name", DBNull.Value));
                    cmd.Parameters.Add(new MySqlParameter($"@Spirit{i}Flavours", 0));
                    cmd.Parameters.Add(new MySqlParameter($"@Spirit{i}Ratings", 0));
                }

                // Populate the spirit parameters with actual values
                int spiritIndex = 1;
                foreach (var spirit in spiritData.Values)
                {
                    cmd.Parameters[$"@Spirit{spiritIndex}Name"].Value = string.IsNullOrEmpty(spirit.Name) ? "" : spirit.Name;
                    cmd.Parameters[$"@Spirit{spiritIndex}Flavours"].Value = spirit.SelectedFlavors;
                    cmd.Parameters[$"@Spirit{spiritIndex}Ratings"].Value = spirit.Rating;
                    spiritIndex++;
                }

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Debug.LogError($"Error saving player data: {e.Message}");
            }
        }
    }

    public void ClearPlayerDataTable()
    {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();

                string query = "TRUNCATE TABLE PlayerData";
                MySqlCommand cmd = new MySqlCommand(query, conn);

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Debug.LogError($"Error clearing player data: {e.Message}");
            }
        }
    }

    public void TableDatas()
    {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();

                string query = "SELECT * FROM PlayerData;";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                MySqlDataReader reader = cmd.ExecuteReader();

                int rowIndex = 0;
                decimal[] totalRatings = new decimal[5];
                decimal[] totalFlavours = new decimal[5];
                int recordCount = 0;

                while (reader.Read())
                {
                    string username = reader.IsDBNull(reader.GetOrdinal("Username")) ? "" : reader.GetString("Username");
                    int spirit1Flavours = reader.IsDBNull(reader.GetOrdinal("Spirit1Flavours")) ? 0 : reader.GetInt32("Spirit1Flavours");
                    int spirit2Flavours = reader.IsDBNull(reader.GetOrdinal("Spirit2Flavours")) ? 0 : reader.GetInt32("Spirit2Flavours");
                    int spirit3Flavours = reader.IsDBNull(reader.GetOrdinal("Spirit3Flavours")) ? 0 : reader.GetInt32("Spirit3Flavours");
                    int spirit4Flavours = reader.IsDBNull(reader.GetOrdinal("Spirit4Flavours")) ? 0 : reader.GetInt32("Spirit4Flavours");
                    int spirit5Flavours = reader.IsDBNull(reader.GetOrdinal("Spirit5Flavours")) ? 0 : reader.GetInt32("Spirit5Flavours");
                    int overallRating = reader.IsDBNull(reader.GetOrdinal("OverallRating")) ? 0 : reader.GetInt32("OverallRating");

                    Transform row = FlavourTable1.GetChild(rowIndex);
                    row.GetChild(0).GetComponent<TMP_Text>().text = username;

                    // Update flavor texts and image colors
                    int[] spiritFlavours = { spirit1Flavours, spirit2Flavours, spirit3Flavours, spirit4Flavours, spirit5Flavours };
                    for (int i = 0; i < spiritFlavours.Length; i++)
                    {
                        Transform flavorTransform = row.GetChild(i + 1);
                        flavorTransform.GetChild(0).GetComponent<TMP_Text>().text = spiritFlavours[i].ToString();
                        flavorTransform.GetComponent<Image>().color = GetFlavorColor(spiritFlavours[i]);
                    }

                    // Update rating stars
                    Transform ratingTransform = row.GetChild(6);
                    for (int i = 0; i < ratingTransform.childCount; i++)
                    {
                        ratingTransform.GetChild(i).gameObject.SetActive(i < overallRating);
                    }

                    totalRatings[0] += reader.IsDBNull(reader.GetOrdinal("Spirit1Ratings")) ? 0 : reader.GetDecimal("Spirit1Ratings");
                    totalRatings[1] += reader.IsDBNull(reader.GetOrdinal("Spirit2Ratings")) ? 0 : reader.GetDecimal("Spirit2Ratings");
                    totalRatings[2] += reader.IsDBNull(reader.GetOrdinal("Spirit3Ratings")) ? 0 : reader.GetDecimal("Spirit3Ratings");
                    totalRatings[3] += reader.IsDBNull(reader.GetOrdinal("Spirit4Ratings")) ? 0 : reader.GetDecimal("Spirit4Ratings");
                    totalRatings[4] += reader.IsDBNull(reader.GetOrdinal("Spirit5Ratings")) ? 0 : reader.GetDecimal("Spirit5Ratings");

                    totalFlavours[0] += spirit1Flavours;
                    totalFlavours[1] += spirit2Flavours;
                    totalFlavours[2] += spirit3Flavours;
                    totalFlavours[3] += spirit4Flavours;
                    totalFlavours[4] += spirit5Flavours;

                    recordCount++;
                    rowIndex++;
                }

                decimal[] averageRatings = new decimal[5];
                decimal[] averageFlavours = new decimal[5];
                if (recordCount > 0)
                {
                    for (int i = 0; i < totalRatings.Length; i++)
                    {
                        averageRatings[i] = totalRatings[i] / recordCount;
                        averageFlavours[i] = totalFlavours[i] / recordCount;
                    }
                }

                UpdateFlavourTable2AverageData(averageRatings, averageFlavours);

                // Start the repeating update if it hasn't been started yet
                if (!isUpdating)
                {
                    isUpdating = true;
                    InvokeRepeating("TableDatas", 5f, 5f); // Start after 5 seconds, repeat every 5 seconds
                }
            }
            catch (MySqlException e)
            {
                Debug.LogError($"Error loading table data: {e.Message}");
            }
        }
    }

    // Methods to get data for the radar chart
    public float GetLocalDataRating(int index)
    {
        Transform localRatingsTransform = FlavourTable2.GetChild(0);
        return float.Parse(localRatingsTransform.GetChild(index).GetComponent<TMP_Text>().text);
    }

    public float GetLocalDataFlavour(int index)
    {
        Transform localFlavoursTransform = FlavourTable2.GetChild(2);
        return float.Parse(localFlavoursTransform.GetChild(index).GetComponent<TMP_Text>().text);
    }

    public float GetAverageRating(int index)
    {
        Transform averageRatingsTransform = FlavourTable2.GetChild(1);
        return float.Parse(averageRatingsTransform.GetChild(index).GetComponent<TMP_Text>().text);
    }

    public float GetAverageFlavour(int index)
    {
        Transform averageFlavoursTransform = FlavourTable2.GetChild(3);
        return float.Parse(averageFlavoursTransform.GetChild(index).GetComponent<TMP_Text>().text);
    }
}
