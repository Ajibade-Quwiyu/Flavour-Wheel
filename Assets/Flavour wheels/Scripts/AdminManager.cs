using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

public class AdminManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown drinkCategoryDropdown;
    [SerializeField] private List<TMP_InputField> spiritInputFields;
    [SerializeField] private TMP_InputField passkeyInputField;
    [SerializeField] private Button generateKeyButton;
    [SerializeField] private GameObject signinPage;
    [SerializeField] private UserInputManager userInputManager;

    public enum User
    {
        Admin,
        Player
    }
    public User currentUser = User.Admin;

    private string selectedDrinkCategory;
    private List<string> spiritNames = new List<string>();
    private string passkey;

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

        drinkCategoryDropdown.onValueChanged.AddListener(delegate { OnDrinkCategoryChanged(); });
        generateKeyButton.onClick.AddListener(GeneratePasskey);

        LoadPlayerPrefs();
    }

    private void LoadPlayerPrefs()
    {
        // Load drink category
        selectedDrinkCategory = PlayerPrefs.GetString("DrinkCategory", "BOURBON");
        int categoryIndex = drinkCategoryDropdown.options.FindIndex(option => option.text == selectedDrinkCategory);
        if (categoryIndex != -1)
        {
            drinkCategoryDropdown.value = categoryIndex;
        }
        else
        {
            SetDefaultDrinkCategory();
        }

        // Load spirits
        for (int i = 0; i < spiritInputFields.Count; i++)
        {
            string spirit = PlayerPrefs.GetString($"Spirit{i + 1}", "");
            spiritInputFields[i].text = spirit;
        }

        OnDrinkCategoryChanged();
    }

    private void SetDefaultDrinkCategory()
    {
        int bourbonIndex = drinkCategoryDropdown.options.FindIndex(option => option.text == "BOURBON");

        if (bourbonIndex != -1)
        {
            drinkCategoryDropdown.value = bourbonIndex;
            OnDrinkCategoryChanged();
        }
        else
        {
            Debug.LogWarning("BOURBON option not found in the dropdown. Please add it in the Unity Inspector.");
        }
    }

    private void OnDrinkCategoryChanged()
    {
        selectedDrinkCategory = drinkCategoryDropdown.options[drinkCategoryDropdown.value].text;
        if (string.IsNullOrEmpty(selectedDrinkCategory))
        {
            selectedDrinkCategory = "BOURBON";
        }
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

        SavePlayerPrefs();
        SaveDataToDatabase();
    }

    private void SavePlayerPrefs()
    {
        // Save drink category
        PlayerPrefs.SetString("DrinkCategory", selectedDrinkCategory);

        // Save spirits
        for (int i = 0; i < spiritNames.Count; i++)
        {
            PlayerPrefs.SetString($"Spirit{i + 1}", spiritNames[i]);
        }

        PlayerPrefs.Save();
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

                string deleteQuery = "DELETE FROM AdminServer;";
                MySqlCommand deleteCmd = new MySqlCommand(deleteQuery, conn);
                deleteCmd.ExecuteNonQuery();

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