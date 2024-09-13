using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.SceneManagement;

public class UserInputManager : MonoBehaviour
{
    [System.Serializable]
    public class SpiritTextFieldSet
    {
        public Text spirit1Text, spirit2Text, spirit3Text, spirit4Text, spirit5Text;
    }

    // UI Elements
    public TMP_InputField usernameInputField, emailInputField, passcodeKeyInputField, overallExperienceInputField;
    public TMP_Text myName;
    public Transform overallRatingTransform;
    public List<Transform> SpiritNamesList, drinkCategoryTransforms;
    public Button submitButton;
    public GameObject signInPage, gamePanel, loadingPanel;
    private GameObject incorrectPasscodeIndicator;
    public Text drinkCategoryText;
    public List<SpiritTextFieldSet> spiritTextFieldSets;
    private List<Button> ratingButtons = new List<Button>();

    // Audio
    public AudioClip ratingSound;
    private AudioSource audioSource;

    // Particle Systems
    private ParticleSystem[] particles;

    // Managers
    public SpiritManager spiritManager;
    public UIToggleState uIToggleState;

    // Constants
    private const int maxRetries = 3;
    private const float retryDelay = 2f;
    public float particlesDuration = 5f;
    private const string UsernameKey = "PlayerUsername";
    private const string EmailKey = "PlayerEmail";
    private const string adminEndpoint = "https://flavour-wheel-server.onrender.com/api/adminserver";
    private const float passcodeCheckInterval = 1f;

    // Data-related variables
    private AdminData cachedAdminData;
    private bool isDataLoaded = false, isOfflineMode = false, isDataRetrievalInProgress = false;
    private Coroutine dataRetrievalCoroutine;
    private Coroutine continuousPasscodeCheckCoroutine;
    private bool isGameActive = false;

    private int overallRating = 0;

    public void StartMethod()
    {
        incorrectPasscodeIndicator = submitButton.transform.GetChild(0).gameObject;

        usernameInputField.onValueChanged.AddListener(delegate { CheckSubmitButtonInteractivity(); });
        emailInputField.onValueChanged.AddListener(delegate { CheckSubmitButtonInteractivity(); });
        passcodeKeyInputField.onValueChanged.AddListener(delegate { CheckSubmitButtonInteractivity(); });

        submitButton.onClick.AddListener(OnSubmitButtonClicked);

        incorrectPasscodeIndicator.SetActive(false);

        signInPage.SetActive(true);
        gamePanel.SetActive(false);

        SetupRatingButtons();

        audioSource = Camera.main.GetComponent<AudioSource>() ?? Camera.main.gameObject.AddComponent<AudioSource>();

        LoadUserData();
        SetupParticles();
        ValidateSpiritTextFieldSets();

        LoadCachedAdminData();
        dataRetrievalCoroutine = StartCoroutine(LoadAdminDataAtStart());
    }

    private void SetupRatingButtons()
    {
        Transform ratingGroup = overallRatingTransform.Find("Rating Group");

        ratingButtons.Clear();
        for (int i = 0; i < ratingGroup.childCount; i++)
        {
            int rating = i + 1;
            Button ratingButton = ratingGroup.GetChild(i).GetComponent<Button>();
            ratingButton.onClick.AddListener(() => Rate(rating));
            ratingButtons.Add(ratingButton);
        }
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

    private IEnumerator LoadAdminDataAtStart()
    {
        isDataRetrievalInProgress = true;
        yield return GetAdminDataWithRetry(0);
        isDataRetrievalInProgress = false;
    }

    private IEnumerator GetAdminDataWithRetry(int retryCount)
    {
        while (retryCount < maxRetries)
        {
            yield return GetAdminData(data =>
            {
                if (data != null)
                {
                    cachedAdminData = data;
                    isDataLoaded = true;
                    isOfflineMode = false;
                    PlayerPrefs.SetString("CachedAdminData", JsonUtility.ToJson(data));
                    PlayerPrefs.Save();
                }
            });

            if (isDataLoaded)
                yield break;

            retryCount++;
            if (retryCount < maxRetries)
                yield return new WaitForSeconds(retryDelay);
        }

        LoadCachedAdminData();
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
            StartCoroutine(GetAdminDataWithRetry(0));
    }

    private IEnumerator WaitForDataRetrieval(string enteredPasscodeKey)
    {
        yield return ShowLoadingIndicator();
        ValidatePasscode(enteredPasscodeKey);
    }

    private IEnumerator ShowLoadingIndicator()
    {
        incorrectPasscodeIndicator.SetActive(true);
        TMP_Text indicatorText = incorrectPasscodeIndicator.GetComponent<TMP_Text>();
        indicatorText.color = Color.white;
        while (isDataRetrievalInProgress)
        {
            indicatorText.text = "Please wait.....";
            yield return new WaitForSeconds(4f);
            indicatorText.text = "Loading......";
            yield return new WaitForSeconds(2f);
        }
        incorrectPasscodeIndicator.SetActive(false);
    }

    private void ValidatePasscode(string enteredPasscodeKey)
    {
        if (cachedAdminData == null)
        {
            DisplayConnectionError("No data available. Please check your internet connection and try again.");
            return;
        }
        myName.text = usernameInputField.text;
        if (cachedAdminData.passcodeKey.Trim().Equals(enteredPasscodeKey, StringComparison.OrdinalIgnoreCase))
        {
            if (isOfflineMode)
            {
                DisplayConnectionError("No internet connection. Please connect to the internet to access the game.");
                signInPage.SetActive(true);
            }
            else
            {
                UpdateUI(cachedAdminData);
                incorrectPasscodeIndicator.SetActive(false);
                signInPage.SetActive(false);
                gamePanel.SetActive(true);
                PlayParticleEffects();
                StartGame();
            }
        }
        else
        {
            DisplayConnectionError("The key you entered is incorrect!!");
        }
    }

    private void StartGame()
    {
        isGameActive = true;
        if (continuousPasscodeCheckCoroutine != null)
        {
            StopCoroutine(continuousPasscodeCheckCoroutine);
        }
        continuousPasscodeCheckCoroutine = StartCoroutine(ContinuousPasscodeCheck());
    }

    private IEnumerator ContinuousPasscodeCheck()
    {
        while (isGameActive)
        {
            yield return new WaitForSeconds(passcodeCheckInterval);
            yield return GetAdminData(newData =>
            {
                if (newData != null && !newData.passcodeKey.Trim().Equals(cachedAdminData.passcodeKey.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    StartCoroutine(RestartGameProcess());
                }
            });
        }
    }

    private IEnumerator RestartGameProcess()
    {
        isGameActive = false;
        if (continuousPasscodeCheckCoroutine != null)
        {
            StopCoroutine(continuousPasscodeCheckCoroutine);
        }

        yield return new WaitForSeconds(.1f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void DisplayConnectionError(string message)
    {
        loadingPanel.SetActive(false);
        signInPage.SetActive(true);
        incorrectPasscodeIndicator.GetComponent<TMP_Text>().text = message;
        incorrectPasscodeIndicator.GetComponent<TMP_Text>().color = Color.red;
        incorrectPasscodeIndicator.SetActive(true);
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
        Color gold = new Color(1f, 0.84f, 0f);
        Color white = Color.white;

        // Find the "Rating Group" child
        Transform ratingGroup = overallRatingTransform.Find("Rating Group");
       
        // Update colors of button images in the Rating Group
        for (int i = 0; i < ratingGroup.childCount; i++)
        {
            Image buttonImage = ratingGroup.GetChild(i).GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = i < rating ? gold : white;
            }
        }

        // Update the rating text
        Text ratingText = overallRatingTransform.GetChild(1).GetComponent<Text>();
        ratingText.text = rating.ToString();
        audioSource.PlayOneShot(ratingSound);
        Handheld.Vibrate();
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
        if (System.Enum.TryParse(data.drinkCategory, true, out FlavorCategory category))
        {
            uIToggleState.SetActiveFlavorCategory(category);
        }
        else
        {
            Debug.LogError($"Invalid flavor category: {data.drinkCategory}");
        }
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
                if (json == "[]")
                {
                    Debug.LogWarning("Received an empty array from the server.");
                    return null;
                }

                if (json.StartsWith("[") && json.EndsWith("]"))
                {
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