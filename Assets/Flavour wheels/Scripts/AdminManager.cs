using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

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

    private string adminEndpoint = "https://flavour-wheel-server.onrender.com/api/adminserver";
    private const string PasskeyPrefKey = "AdminPasskey";

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

        // Add listener to clear passkey when dropdown is clicked
        drinkCategoryDropdown.onValueChanged.AddListener(delegate { ClearPasskey(); });

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

        // Load passkey
        passkey = PlayerPrefs.GetString(PasskeyPrefKey, "");
        passkeyInputField.text = passkey;

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

    private void ClearPasskey()
    {
        passkey = "";
        passkeyInputField.text = "";
        PlayerPrefs.DeleteKey(PasskeyPrefKey);
        PlayerPrefs.Save();
        Debug.Log("Passkey cleared.");
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

    private async void GeneratePasskey()
    {
        GetSpiritNames();

        // Input validation
        if (string.IsNullOrEmpty(selectedDrinkCategory) || spiritNames.Any(string.IsNullOrEmpty))
        {
            Debug.LogError("Please fill in all fields before generating a passkey.");
            return;
        }

        passkey = GenerateRandomAlphanumericString(8);
        passkeyInputField.text = passkey;
        Debug.Log($"Generated Passkey: {passkey}");
        generateKeyButton.interactable = false;
        SavePlayerPrefs();
        await SaveDataToServer();
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

        // Save passkey
        PlayerPrefs.SetString(PasskeyPrefKey, passkey);

        PlayerPrefs.Save();
        Debug.Log("Data saved to PlayerPrefs.");
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

    private async Task SaveDataToServer()
    {
        var data = new AdminData
        {
            drinkCategory = selectedDrinkCategory,
            spirit1 = spiritNames.Count > 0 ? spiritNames[0] : "",
            spirit2 = spiritNames.Count > 1 ? spiritNames[1] : "",
            spirit3 = spiritNames.Count > 2 ? spiritNames[2] : "",
            spirit4 = spiritNames.Count > 3 ? spiritNames[3] : "",
            spirit5 = spiritNames.Count > 4 ? spiritNames[4] : "",
            passcodeKey = passkey
        };

        string jsonData = JsonUtility.ToJson(data);
        Debug.Log($"Sending data to server: {jsonData}");

        using (HttpClient client = new HttpClient())
        {
            try
            {
                // Send DELETE request to clear existing data
                HttpResponseMessage deleteResponse = await client.DeleteAsync(adminEndpoint);
                Debug.Log($"DELETE response: {deleteResponse.StatusCode}");

                if (deleteResponse.IsSuccessStatusCode)
                {
                    Debug.Log("Existing data deleted successfully.");
                }
                else
                {
                    Debug.LogError($"Error deleting data: {deleteResponse.ReasonPhrase}");
                }

                // Send POST request to save new data
                StringContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage postResponse = await client.PostAsync(adminEndpoint, content);
                Debug.Log($"POST response: {postResponse.StatusCode}");

                if (postResponse.IsSuccessStatusCode)
                {
                    string responseContent = await postResponse.Content.ReadAsStringAsync();
                    Debug.Log($"Data successfully saved to the server. Response: {responseContent}");
                }
                else
                {
                    Debug.LogError($"Error saving data: {postResponse.ReasonPhrase}");
                }
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"Request error: {e.Message}");
            }
            finally
            {
                generateKeyButton.interactable = true;
            }
        }
    }

    [System.Serializable]
    private class AdminData
    {
        public string drinkCategory;
        public string spirit1;
        public string spirit2;
        public string spirit3;
        public string spirit4;
        public string spirit5;
        public string passcodeKey;
    }
}