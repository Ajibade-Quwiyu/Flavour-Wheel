using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System;

public class UserInputManager : MonoBehaviour
{
    [System.Serializable]
    public class SpiritTextFieldSet
    {
        public Text spirit1Text, spirit2Text, spirit3Text, spirit4Text, spirit5Text;
    }

    // InputFields
    public TMP_InputField usernameInputField, emailInputField, passcodeKeyInputField, overallExperienceInputField;

    // Transforms
    public Transform overallRatingTransform;
    public List<Transform> SpiritNamesList, drinkCategoryTransforms;

    // Buttons
    public Button submitButton;

    // GameObjects
    public GameObject signInPage, gamePanel, loadingPanel;
    private GameObject incorrectPasscodeIndicator;

    // Text components
    public Text drinkCategoryText;

    // Lists
    public List<SpiritTextFieldSet> spiritTextFieldSets;
    private List<Button> ratingButtons = new List<Button>();

    // Audio components
    public AudioClip ratingSound;
    private AudioSource audioSource;

    // Particle systems
    private ParticleSystem[] particles;

    // Managers
    public SpiritManager spiritManager;

    // Constants
    private const int maxRetries = 3;
    private const float retryDelay = 2f;
    public float particlesDuration = 5f;
    private const string UsernameKey = "PlayerUsername";
    private const string EmailKey = "PlayerEmail";
    private string adminEndpoint = "https://flavour-wheel-server.onrender.com/api/adminserver";

    // Data-related variables
    private AdminData cachedAdminData;
    private bool isDataLoaded = false, isOfflineMode = false, isDataRetrievalInProgress = false;
    private Coroutine dataRetrievalCoroutine;

    // Other variables
    private int overallRating = 0;
    public void StartMethod()
    {
        incorrectPasscodeIndicator = submitButton.transform.GetChild(0).gameObject;

        usernameInputField.onValueChanged.AddListener(delegate { CheckSubmitButtonInteractivity(); });
        emailInputField.onValueChanged.AddListener(delegate { CheckSubmitButtonInteractivity(); });
        passcodeKeyInputField.onValueChanged.AddListener(delegate { CheckSubmitButtonInteractivity(); });

        submitButton.onClick.AddListener(OnSubmitButtonClicked);

        submitButton.interactable = false;

        incorrectPasscodeIndicator.SetActive(false);

        signInPage.SetActive(true);
        gamePanel.SetActive(false);

        for (int i = 2; i < 7; i++)
        {
            int rating = i - 1;
            Button ratingButton = overallRatingTransform.GetChild(i).GetComponent<Button>();
            ratingButton.onClick.AddListener(() => Rate(rating));
            ratingButtons.Add(ratingButton);
        }

        audioSource = Camera.main.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
        }

        LoadUserData();
        SetupParticles();
        ValidateSpiritTextFieldSets();

        LoadCachedAdminData();
        dataRetrievalCoroutine = StartCoroutine(LoadAdminDataAtStart());
    }

    private void ValidateSpiritTextFieldSets()
    {
        spiritTextFieldSets ??= new List<SpiritTextFieldSet>();
        spiritTextFieldSets.RemoveAll(set => set == null || set.spirit1Text == null || set.spirit2Text == null ||
                                             set.spirit3Text == null || set.spirit4Text == null || set.spirit5Text == null);
    }

    private void SetupParticles()
    {
        particles = drinkCategoryTransforms
            .Select(t => t.GetChild(0).GetComponent<ParticleSystem>())
            .Where(p => p != null)
            .ToArray();

        foreach (var particle in particles)
            particle.gameObject.SetActive(false);

        foreach (var transform in drinkCategoryTransforms)
            transform?.gameObject.SetActive(false);
    }

    private void LoadUserData()
    {
        usernameInputField.text = PlayerPrefs.GetString(UsernameKey, usernameInputField.text);
        emailInputField.text = PlayerPrefs.GetString(EmailKey, emailInputField.text);
    }

    private void SaveUserData()
    {
        PlayerPrefs.SetString(UsernameKey, usernameInputField.text);
        PlayerPrefs.SetString(EmailKey, emailInputField.text);
        PlayerPrefs.Save();
    }

    private void CheckSubmitButtonInteractivity() =>
        submitButton.interactable = !string.IsNullOrEmpty(usernameInputField.text) && !string.IsNullOrEmpty(passcodeKeyInputField.text);


    private void LoadCachedAdminData()
    {
        string cachedJson = PlayerPrefs.GetString("CachedAdminData", "");
        if (!string.IsNullOrEmpty(cachedJson))
        {
            cachedAdminData = AdminData.ParseJson(cachedJson);
            isDataLoaded = cachedAdminData != null;
            isOfflineMode = true;
        }
    }

    private void SaveCachedAdminData(AdminData data)
    {
        if (data != null)
        {
            PlayerPrefs.SetString("CachedAdminData", JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }

    private IEnumerator LoadAdminDataAtStart()
    {
        isDataRetrievalInProgress = true;
        yield return GetAdminDataWithRetry(0);
        isDataRetrievalInProgress = false;
    }

    private void DisplayOfflineModeMessage() =>
        DisplayConnectionError("No internet connection. Please connect to the internet to access the game.");

    private IEnumerator GetAdminDataWithRetry(int retryCount)
    {
        while (retryCount < maxRetries)
        {
            yield return TryGetAdminData();
            if (isDataLoaded)
                yield break;

            retryCount++;
            if (retryCount < maxRetries)
                yield return new WaitForSeconds(retryDelay);
        }

        LoadCachedAdminData();
    }

    private IEnumerator TryGetAdminData()
    {
        yield return GetAdminData(data =>
        {
            if (data != null)
            {
                cachedAdminData = data;
                isDataLoaded = true;
                isOfflineMode = false;
                SaveCachedAdminData(data);
            }
        });
    }

    public void OnSubmitButtonClicked()
    {
        string enteredPasscodeKey = passcodeKeyInputField.text.Trim();
        SaveUserData();

        if (isDataRetrievalInProgress)
            StartCoroutine(WaitForDataRetrieval(enteredPasscodeKey));
        else if (isDataLoaded)
            ValidatePasscode(enteredPasscodeKey);
        else
            StartCoroutine(RetryAdminDataFetch(enteredPasscodeKey));
    }

    private IEnumerator WaitForDataRetrieval(string enteredPasscodeKey)
    {
        yield return ShowLoadingIndicator();
        ProcessDataValidation(enteredPasscodeKey);
    }

    private IEnumerator ShowLoadingIndicator()
    {
        incorrectPasscodeIndicator.SetActive(true);
        TMP_Text indicatorText = incorrectPasscodeIndicator.GetComponent<TMP_Text>();
        while (isDataRetrievalInProgress)
        {
            indicatorText.text = "Please wait.....";
            yield return new WaitForSeconds(1f);
            indicatorText.text = "Loading......";
            yield return new WaitForSeconds(1f);
        }
        incorrectPasscodeIndicator.SetActive(false);
    }

    private void ProcessDataValidation(string enteredPasscodeKey)
    {
        if (isDataLoaded)
            ValidatePasscode(enteredPasscodeKey);
        else
            StartCoroutine(RetryAdminDataFetch(enteredPasscodeKey));
    }

    private void ValidatePasscode(string enteredPasscodeKey)
    {
        if (cachedAdminData == null)
        {
            DisplayConnectionError("No data available. Please check your internet connection and try again.");
            return;
        }

        if (cachedAdminData.passcodeKey.Trim().Equals(enteredPasscodeKey, System.StringComparison.OrdinalIgnoreCase))
        {
            if (isOfflineMode)
            {
                DisplayOfflineModeMessage();
                signInPage.SetActive(true);
            }
            else
            {
                UpdateUI(cachedAdminData);
                incorrectPasscodeIndicator.SetActive(false);
                signInPage.SetActive(false);
                gamePanel.SetActive(true);
                PlayParticleEffects();
            }
        }
        else
        {
            DisplayIncorrectPasscodeMessage("The key you entered is incorrect!!");
        }
    }

    private IEnumerator RetryAdminDataFetch(string enteredPasscodeKey)
    {
        incorrectPasscodeIndicator.SetActive(true);
        incorrectPasscodeIndicator.GetComponent<TMP_Text>().text = "Retrieving data...";
        yield return GetAdminDataWithRetry(0);
        ProcessDataValidation(enteredPasscodeKey);
    }

    private void DisplayConnectionError(string message)
    {
        loadingPanel.SetActive(false);
        signInPage.SetActive(true);
        incorrectPasscodeIndicator.GetComponent<TMP_Text>().text = message;
        incorrectPasscodeIndicator.SetActive(true);
    }

    private void DisplayIncorrectPasscodeMessage(string message)
    {
        incorrectPasscodeIndicator.GetComponent<TMP_Text>().text = message;
        incorrectPasscodeIndicator.SetActive(true);
        loadingPanel.SetActive(false);
        signInPage.SetActive(true);
    }
    private void PlayParticleEffects()
    {
        if (particles?.Length > 0)
        {
            foreach (var particle in particles.Where(p => p != null))
            {
                particle.gameObject.SetActive(true);
                particle.Play();
            }
            StartCoroutine(DeactivateParticlesAfterDelay());
        }
    }

    private IEnumerator DeactivateParticlesAfterDelay()
    {
        yield return new WaitForSeconds(particlesDuration);
        foreach (var particle in particles?.Where(p => p != null) ?? Enumerable.Empty<ParticleSystem>())
        {
            particle.Stop();
            particle.gameObject.SetActive(false);
        }
    }

    public void Userdata()
    {
        spiritManager?.SetUserData(
            PlayerPrefs.GetString(UsernameKey, "DefaultUsername"),
            PlayerPrefs.GetString(EmailKey, "DefaultEmail@example.com"),
            overallRating,
            overallExperienceInputField.text
        );
    }

    public void Rate(int rating)
    {
        overallRating = rating;
        Color gold = new Color(1f, 0.75f, 0f);

        for (int i = 0; i < ratingButtons.Count; i++)
            ratingButtons[i].image.color = i < rating ? gold : Color.white;

        overallRatingTransform.GetChild(1).GetComponent<Text>().text = rating.ToString();
        audioSource?.PlayOneShot(ratingSound);
    }

    private IEnumerator GetAdminData(System.Action<AdminData> callback)
    {
        using UnityWebRequest request = UnityWebRequest.Get(adminEndpoint);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            AdminData adminData = AdminData.ParseJson(request.downloadHandler.text);
            callback(adminData?.drinkCategory != null ? adminData : null);
        }
        else
        {
            callback(null);
        }
    }
    private void UpdateUI(AdminData data)
    {
        drinkCategoryText.text = $"{data.drinkCategory} WHEEL";

        var spirits = new[] { data.spirit1, data.spirit2, data.spirit3, data.spirit4, data.spirit5 };
        var textFields = new[] { "spirit1Text", "spirit2Text", "spirit3Text", "spirit4Text", "spirit5Text" };

        for (int i = 0; i < 5; i++)
        {
            string spirit = spirits[i];
            foreach (var set in spiritTextFieldSets)
            {
                var field = typeof(SpiritTextFieldSet).GetField(textFields[i]);
                if (field?.GetValue(set) is Text text) text.text = spirit;
            }

            foreach (var transform in SpiritNamesList)
            {
                if (transform.childCount > i)
                    transform.GetChild(i).GetComponent<TMP_Text>().text = spirit;
            }
        }

        foreach (var t in drinkCategoryTransforms)
            t.gameObject.SetActive(t.name.Equals(data.drinkCategory, StringComparison.OrdinalIgnoreCase));
    }

    [System.Serializable]
    public class AdminData
    {
        public int id;
        public string drinkCategory, spirit1, spirit2, spirit3, spirit4, spirit5, passcodeKey;

        [System.Serializable]
        private class AdminDataWrapper
        {
            public AdminData[] data;
        }

        public static AdminData ParseJson(string json)
        {
            try
            {
                // Check if the JSON is an empty array
                if (json == "[]")
                {
                    Debug.LogWarning("Received an empty array from the server.");
                    return null;
                }

                // Check if the JSON is an array
                if (json.StartsWith("[") && json.EndsWith("]"))
                {
                    // Wrap the array in an object
                    json = "{\"data\":" + json + "}";
                    AdminDataWrapper wrapper = JsonUtility.FromJson<AdminDataWrapper>(json);
                    if (wrapper != null && wrapper.data != null && wrapper.data.Length > 0)
                    {
                        return wrapper.data[0];
                    }
                    else
                    {
                        Debug.LogWarning("Parsed array is empty or null.");
                        return null;
                    }
                }
                else
                {
                    // Try parsing as a single object
                    AdminData adminData = JsonUtility.FromJson<AdminData>(json);
                    if (adminData != null && !string.IsNullOrEmpty(adminData.drinkCategory))
                    {
                        return adminData;
                    }
                    else
                    {
                        Debug.LogWarning("Parsed object is null or invalid.");
                        return null;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error parsing JSON: {e.Message}");
                Debug.LogError($"JSON content: {json}");
                return null;
            }
        }
    }
}