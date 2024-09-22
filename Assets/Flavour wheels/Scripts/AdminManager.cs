using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections;

public class AdminManager : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown drinkCategoryDropdown;
    [SerializeField] private List<TMP_InputField> spiritInputFields;
    [SerializeField] private TMP_InputField passkeyInputField;
    [SerializeField] private Button generateKeyButton;
    [SerializeField] private GameObject signinPage;
    [SerializeField] private UserInputManager userInputManager;
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private Button goToUserButton;

    public enum User { Admin, Player }
    public User currentUser = User.Admin;

    private const string AdminEndpoint = "https://flavour-wheel-server.onrender.com/api/adminserver";
    private const string FlavourWheelEndpoint = "https://flavour-wheel-server.onrender.com/api/flavourwheel";
    private const string PasskeyPrefKey = "AdminPasskey";
    private const int PasskeyLength = 4;

    private string selectedDrinkCategory;
    private List<string> spiritNames = new List<string>();
    private string passkey;

    private WebGLHttpClient httpClient;

    private void Start()
    {
        httpClient = gameObject.AddComponent<WebGLHttpClient>();
        InitializeUI();
        LoadData();
    }

    private void InitializeUI()
    {
        SetActiveUI(currentUser == User.Admin);
        drinkCategoryDropdown.onValueChanged.AddListener(delegate { OnDrinkCategoryChanged(); });
        generateKeyButton.onClick.AddListener(GeneratePasskey);
        goToUserButton.onClick.AddListener(GoToUser);
    }

    private void SetActiveUI(bool isAdmin)
    {
        this.gameObject.SetActive(isAdmin);
        signinPage.SetActive(!isAdmin);
        userInputManager.enabled = !isAdmin;
    }

    public void SetAdmin()
    {
        currentUser = User.Admin;
    }

    public void SetPlayer()
    {
        currentUser = User.Player;
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
            return;
        }

        passkey = GenerateRandomNumericString(PasskeyLength);
        passkeyInputField.text = passkey;

        generateKeyButton.interactable = false;
        goToUserButton.interactable = false;
        SaveData();

        StartCoroutine(UpdateDisplayTextCoroutine());

        await SaveDataToServer();

        // Update UI on the main thread
        generateKeyButton.interactable = true;
        goToUserButton.interactable = true;
        displayText.text = "Data ready...";
        userInputManager.StartMethod();
        signinPage.SetActive(false);
    }

    private string GenerateRandomNumericString(int length)
    {
        const string digits = "0123456789";
        return new string(Enumerable.Repeat(digits, length)
            .Select(s => s[UnityEngine.Random.Range(0, s.Length)]).ToArray());
    }

    private IEnumerator UpdateDisplayTextCoroutine()
    {
        string[] loadingMessages = { "Loading the server.....", "Receiving.....", "Please wait.." };
        int messageIndex = 0;

        while (!generateKeyButton.interactable)
        {
            displayText.text = loadingMessages[messageIndex];
            messageIndex = (messageIndex + 1) % loadingMessages.Length;
            yield return new WaitForSeconds(2f);
        }
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

        try
        {
            // Delete all content from the flavour wheel server
            string deleteFlavorWheelResponse = await DeleteAsyncWrapper(FlavourWheelEndpoint);

            // Delete all existing admin data
            string deleteAdminResponse = await DeleteAsyncWrapper(AdminEndpoint);

            // Post new admin data
            string postResponse = await PostAsyncWrapper(AdminEndpoint, jsonData);
            if (string.IsNullOrEmpty(postResponse))
            {
                Debug.LogError("Failed to update admin server.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveDataToServer Exception: {e.Message}\nStack Trace: {e.StackTrace}");
        }
    }

    private Task<string> DeleteAsyncWrapper(string url)
    {
        var tcs = new TaskCompletionSource<string>();
        StartCoroutine(httpClient.DeleteAsync(url, result => tcs.SetResult(result)));
        return tcs.Task;
    }

    private Task<string> PostAsyncWrapper(string url, string jsonData)
    {
        var tcs = new TaskCompletionSource<string>();
        StartCoroutine(httpClient.PostAsync(url, jsonData, result => tcs.SetResult(result)));
        return tcs.Task;
    }

    private void GoToUser()
    {
        SetPlayer();
        SetActiveUI(false);
    }

    [System.Serializable]
    private class AdminData
    {
        public string drinkCategory, spirit1, spirit2, spirit3, spirit4, spirit5, passcodeKey;
    }
}