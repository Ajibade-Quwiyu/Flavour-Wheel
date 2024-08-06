using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using MySql.Data.MySqlClient;





public class AdminManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown drinkCategoryDropdown; // Dropdown for drink categories
    [SerializeField] private List<TMP_InputField> spiritInputFields; // List of input fields for spirits
    [SerializeField] private TMP_InputField passkeyInputField; // Input field for displaying the passkey
    [SerializeField] private Button generateKeyButton; // Button to generate the passkey
    [SerializeField] private GameObject signinPage; // Sign-in page GameObject
    [SerializeField] private UserInputManager userInputManager; // User input manager
    public enum User
    {
        Admin,
        Player
    }
    public User currentUser = User.Admin; // Default user type
    private string selectedDrinkCategory; // Selected drink category
    private List<string> spiritNames = new List<string>(); // List to store spirit names
    private string passkey; // Generated passkey

    private string connectionString = "Server=sql8.freesqldatabase.com; Database=sql8721580; User=sql8721580; Password=6wdc5VDnaQ; Charset=utf8;";

    void Start()
    {
        if (currentUser == User.Player)
        {
            this.gameObject.SetActive(false);
            signinPage.SetActive(true);
            userInputManager.enabled = true;
        }
        else
        {
            this.gameObject.SetActive(true);
            signinPage.SetActive(false);
            userInputManager.enabled = false;
        }

        // Add a listener to the dropdown
        drinkCategoryDropdown.onValueChanged.AddListener(delegate { OnDrinkCategoryChanged(); });

        // Add a listener to the generate key button
        generateKeyButton.onClick.AddListener(GeneratePasskey);
    }

    private void OnDrinkCategoryChanged()
    {
        selectedDrinkCategory = drinkCategoryDropdown.options[drinkCategoryDropdown.value].text;
        Debug.Log($"Selected Drink Category: {selectedDrinkCategory}");
    }

    private void GetSpiritNames()
    {
        spiritNames.Clear();
        foreach (var inputField in spiritInputFields)
        {
            spiritNames.Add(inputField.text);
        }

        for (int i = 0; i < spiritNames.Count; i++)
        {
            Debug.Log($"Spirit {i + 1}: {spiritNames[i]}");
        }
    }

    private void GeneratePasskey()
    {
        GetSpiritNames();
        passkey = GenerateRandomAlphanumericString(8);
        passkeyInputField.text = passkey;
        Debug.Log($"Generated Passkey: {passkey}");

        // Save the data to the database
        SaveDataToDatabase();
    }

    private string GenerateRandomAlphanumericString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] stringChars = new char[length];
        System.Random random = new System.Random();

        for (int i = 0; i < length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }

        return new string(stringChars);
    }

    private void SaveDataToDatabase()
    {
        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                conn.Open();

                // Delete existing record
                string deleteQuery = "DELETE FROM AdminServer;";
                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn);
                deleteCmd.ExecuteNonQuery();

                // Insert new data
                string insertQuery = $"INSERT INTO AdminServer (DrinkCategory, Spirit1, Spirit2, Spirit3, Spirit4, Spirit5, PasscodeKey) " +
                                     $"VALUES ('{selectedDrinkCategory}', '{spiritNames[0]}', '{spiritNames[1]}', '{spiritNames[2]}', '{spiritNames[3]}', '{spiritNames[4]}', '{passkey}');";

                MySqlCommand insertCmd = new MySqlCommand(insertQuery, conn);
                insertCmd.ExecuteNonQuery();
            }
            catch (MySqlException e)
            {
                Debug.LogError($"Error saving data: {e.Message}");
            }
        }
    }
}
