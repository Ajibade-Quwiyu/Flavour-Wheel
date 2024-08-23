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

    public enum User { Admin, Player }
    public User currentUser = User.Admin;

    private const string AdminEndpoint = "https://flavour-wheel-server.onrender.com/api/adminserver";
    private const string PasskeyPrefKey = "AdminPasskey";
    private const int PasskeyLength = 8;

    private string selectedDrinkCategory;
    private List<string> spiritNames = new List<string>();
    private string passkey;

    private void Start()
    {
        InitializeUI();
        LoadData();
    }

    private void InitializeUI()
    {
        SetActiveUI(currentUser == User.Admin);
        drinkCategoryDropdown.onValueChanged.AddListener(delegate { OnDrinkCategoryChanged(); });
        generateKeyButton.onClick.AddListener(GeneratePasskey);
    }

    private void SetActiveUI(bool isAdmin)
    {
        this.gameObject.SetActive(isAdmin);
        signinPage.SetActive(!isAdmin);
        userInputManager.enabled = !isAdmin;
    }

    private void LoadData()
    {
        LoadPlayerPrefs("DrinkCategory", drinkCategoryDropdown, "BOURBON");
        for (int i = 0; i < spiritInputFields.Count; i++)
        {
            LoadPlayerPrefs($"Spirit{i + 1}", spiritInputFields[i]);
        }
        LoadPlayerPrefs(PasskeyPrefKey, passkeyInputField);
        OnDrinkCategoryChanged();
    }

    private void LoadPlayerPrefs(string key, TMP_Dropdown dropdown, string defaultValue = "")
    {
        string value = PlayerPrefs.GetString(key, defaultValue);
        int index = dropdown.options.FindIndex(option => option.text == value);
        dropdown.value = index != -1 ? index : 0;
    }

    private void LoadPlayerPrefs(string key, TMP_InputField inputField, string defaultValue = "")
    {
        inputField.text = PlayerPrefs.GetString(key, defaultValue);
    }

    private void OnDrinkCategoryChanged()
    {
        selectedDrinkCategory = drinkCategoryDropdown.options[drinkCategoryDropdown.value].text;
    }

    private async void GeneratePasskey()
    {
        spiritNames = spiritInputFields.Select(inputField => inputField.text).ToList();

        if (string.IsNullOrEmpty(selectedDrinkCategory) || spiritNames.Any(string.IsNullOrEmpty))
        {
            Debug.LogError("Please fill in all fields before generating a passkey.");
            return;
        }

        passkey = GenerateRandomString(PasskeyLength);
        passkeyInputField.text = passkey;
        Debug.Log($"Generated Passkey: {passkey}");

        generateKeyButton.interactable = false;
        SaveData();
        await SaveDataToServer();
        generateKeyButton.interactable = true;
    }

    private string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[UnityEngine.Random.Range(0, s.Length)]).ToArray());
    }

    private void SaveData()
    {
        SavePlayerPrefs("DrinkCategory", selectedDrinkCategory);
        for (int i = 0; i < spiritNames.Count; i++)
        {
            SavePlayerPrefs($"Spirit{i + 1}", spiritNames[i]);
        }
        SavePlayerPrefs(PasskeyPrefKey, passkey);
    }

    private void SavePlayerPrefs(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    private async Task SaveDataToServer()
    {
        var data = new AdminData
        {
            drinkCategory = selectedDrinkCategory,
            spirit1 = spiritNames.ElementAtOrDefault(0) ?? "",
            spirit2 = spiritNames.ElementAtOrDefault(1) ?? "",
            spirit3 = spiritNames.ElementAtOrDefault(2) ?? "",
            spirit4 = spiritNames.ElementAtOrDefault(3) ?? "",
            spirit5 = spiritNames.ElementAtOrDefault(4) ?? "",
            passcodeKey = passkey
        };

        string jsonData = JsonUtility.ToJson(data);
        Debug.Log($"Sending data to server: {jsonData}");

        using (HttpClient client = new HttpClient())
        {
            try
            {
                await client.DeleteAsync(AdminEndpoint);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(AdminEndpoint, content);
                Debug.Log($"Server response: {response.StatusCode}");
            }
            catch (HttpRequestException e)
            {
                Debug.LogError($"Request error: {e.Message}");
            }
        }
    }

    [System.Serializable]
    private class AdminData
    {
        public string drinkCategory, spirit1, spirit2, spirit3, spirit4, spirit5, passcodeKey;
    }
}