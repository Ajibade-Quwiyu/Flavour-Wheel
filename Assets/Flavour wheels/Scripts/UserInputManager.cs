using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UserInputManager : MonoBehaviour
{
    [System.Serializable]
    public class SpiritTextFieldSet
    {
        public Text spirit1Text;
        public Text spirit2Text;
        public Text spirit3Text;
        public Text spirit4Text;
        public Text spirit5Text;
    }

    public TMP_InputField usernameInputField;
    public TMP_InputField emailInputField;
    public TMP_InputField passcodeKeyInputField;
    public TMP_InputField overallExperienceInputField;
    public Transform overallRatingTransform;
    public Button submitButton;
    public List<SpiritTextFieldSet> spiritTextFieldSets;
    public Text drinkCategoryText;
    public GameObject signInPage;
    public GameObject gamePanel;
    public SpiritManager spiritManager;
    public AudioClip ratingSound;
    public float particlesDuration = 5f;
    public List<Transform> drinkCategoryTransforms;

    private GameObject incorrectPasscodeIndicator;
    private int overallRating = 0;
    private List<Button> ratingButtons = new List<Button>();
    private AudioSource audioSource;
    private ParticleSystem[] particles;

    private const string UsernameKey = "PlayerUsername";
    private const string EmailKey = "PlayerEmail";
    private string adminEndpoint = "https://flavour-wheel-server.onrender.com/api/adminserver";

    void Start()
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
    }

    private void ValidateSpiritTextFieldSets()
    {
        if (spiritTextFieldSets == null)
        {
            spiritTextFieldSets = new List<SpiritTextFieldSet>();
        }

        for (int i = 0; i < spiritTextFieldSets.Count; i++)
        {
            SpiritTextFieldSet set = spiritTextFieldSets[i];
            if (set == null || set.spirit1Text == null || set.spirit2Text == null ||
                set.spirit3Text == null || set.spirit4Text == null || set.spirit5Text == null)
            {
                Debug.LogWarning($"Spirit text field set {i} is invalid. Removing from the list.");
                spiritTextFieldSets.RemoveAt(i);
                i--; // Adjust index after removal
            }
        }

        Debug.Log($"Validated {spiritTextFieldSets.Count} spirit text field sets");
    }

    private void SetupParticles()
    {
        particles = drinkCategoryTransforms
            .Select(t => t.GetChild(0).GetComponent<ParticleSystem>())
            .Where(p => p != null)
            .ToArray();

        foreach (var particle in particles)
        {
            particle.gameObject.SetActive(false);
        }

        foreach (var transform in drinkCategoryTransforms)
        {
            if (transform != null)
            {
                transform.gameObject.SetActive(false);
            }
        }
    }

    private void LoadUserData()
    {
        if (PlayerPrefs.HasKey(UsernameKey))
        {
            usernameInputField.text = PlayerPrefs.GetString(UsernameKey);
        }
        if (PlayerPrefs.HasKey(EmailKey))
        {
            emailInputField.text = PlayerPrefs.GetString(EmailKey);
        }
    }

    private void SaveUserData()
    {
        PlayerPrefs.SetString(UsernameKey, usernameInputField.text);
        PlayerPrefs.SetString(EmailKey, emailInputField.text);
        PlayerPrefs.Save();
    }

    private void CheckSubmitButtonInteractivity()
    {
        submitButton.interactable = !string.IsNullOrEmpty(usernameInputField.text) && !string.IsNullOrEmpty(passcodeKeyInputField.text);
    }

    public void OnSubmitButtonClicked()
    {
        string enteredPasscodeKey = passcodeKeyInputField.text.Trim();
        string username = usernameInputField.text.Trim();
        string email = emailInputField.text.Trim();

        Debug.Log($"Entered Passcode: '{enteredPasscodeKey}'");

        StartCoroutine(GetAdminData((adminData) =>
        {
            Debug.Log("waiting");
            if (adminData == null)
            {
                DisplayIncorrectPasscodeMessage("Unable to verify passcode. Please try again later.");
                return;
            }

            string expectedPasscodeKey = adminData.passcodeKey.Trim();
            Debug.Log($"Expected Passcode: '{expectedPasscodeKey}'");

            if (expectedPasscodeKey.Equals(enteredPasscodeKey, System.StringComparison.OrdinalIgnoreCase))
            {
                SaveUserData();
                UpdateUI(adminData);
                incorrectPasscodeIndicator.gameObject.SetActive(false);
                signInPage.SetActive(false);
                gamePanel.SetActive(true);
                PlayParticleEffects();
            }
            else
            {
                DisplayIncorrectPasscodeMessage("The key you entered is incorrect!!");
            }
        }));
    }

    private void DisplayIncorrectPasscodeMessage(string message)
    {
        incorrectPasscodeIndicator.GetComponent<TMP_Text>().text = message;
        incorrectPasscodeIndicator.SetActive(true);
    }

    private void PlayParticleEffects()
    {
        if (particles != null && particles.Length > 0)
        {
            foreach (var particle in particles)
            {
                if (particle != null)
                {
                    particle.gameObject.SetActive(true);
                    particle.Play();
                }
            }
            StartCoroutine(DeactivateParticlesAfterDelay());
        }
    }

    private IEnumerator DeactivateParticlesAfterDelay()
    {
        yield return new WaitForSeconds(particlesDuration);

        if (particles != null && particles.Length > 0)
        {
            foreach (var particle in particles)
            {
                if (particle != null)
                {
                    particle.Stop();
                    particle.gameObject.SetActive(false);
                }
            }
        }
    }

    public void Userdata()
    {
        string username = usernameInputField.text;
        string email = !string.IsNullOrEmpty(emailInputField.text) ? emailInputField.text : "example@example.com";
        string feedback = overallExperienceInputField.text;

        if (spiritManager != null)
        {
            spiritManager.SetUserData(PlayerPrefs.GetString(UsernameKey, "DefaultUsername"), PlayerPrefs.GetString(EmailKey, "DefaultEmail@example.com"), overallRating, feedback);
        }
    }

    public void Rate(int rating)
    {
        overallRating = rating;

        foreach (Button btn in ratingButtons)
        {
            btn.image.color = Color.white;
        }

        Color gold = new Color(255 / 255f, 192 / 255f, 0 / 255f);
        for (int i = 0; i < rating; i++)
        {
            ratingButtons[i].image.color = gold;
        }

        if (rating == 5)
        {
            foreach (Button btn in ratingButtons)
            {
                btn.image.color = gold;
            }
        }

        overallRatingTransform.GetChild(1).GetComponent<Text>().text = rating.ToString();

        if (audioSource != null && ratingSound != null)
        {
            audioSource.PlayOneShot(ratingSound);
        }
    }

    private IEnumerator GetAdminData(System.Action<AdminData> callback)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(adminEndpoint))
        {
            yield return request.SendWebRequest();

            Debug.Log($"Response Code: {request.responseCode}");
            Debug.Log($"Raw response: {request.downloadHandler.text}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log("Raw JSON response: " + json);

                AdminData adminData = AdminData.ParseJson(json);

                if (adminData != null && !string.IsNullOrEmpty(adminData.drinkCategory))
                {
                    Debug.Log($"Deserialized AdminData: DrinkCategory={adminData.drinkCategory}, PasscodeKey={adminData.passcodeKey}");
                    callback(adminData);
                }
                else
                {
                    Debug.LogWarning("No valid admin data received from the server.");
                    callback(null);
                }
            }
            else
            {
                Debug.LogError($"Error retrieving admin data: {request.error}");
                callback(null);
            }
        }
    }

    private void UpdateUI(AdminData data)
    {
        drinkCategoryText.text = data.drinkCategory + " WHEEL";

        // Update all sets of spirit text fields
        foreach (SpiritTextFieldSet textFieldSet in spiritTextFieldSets)
        {
            if (textFieldSet.spirit1Text != null) textFieldSet.spirit1Text.text = data.spirit1;
            if (textFieldSet.spirit2Text != null) textFieldSet.spirit2Text.text = data.spirit2;
            if (textFieldSet.spirit3Text != null) textFieldSet.spirit3Text.text = data.spirit3;
            if (textFieldSet.spirit4Text != null) textFieldSet.spirit4Text.text = data.spirit4;
            if (textFieldSet.spirit5Text != null) textFieldSet.spirit5Text.text = data.spirit5;
        }

        // Deactivate all drink category transforms
        foreach (var transform in drinkCategoryTransforms)
        {
            if (transform != null)
            {
                transform.gameObject.SetActive(false);
            }
        }

        // Activate the matching drink category transform
        Transform matchingTransform = drinkCategoryTransforms.Find(t => t.name.Equals(data.drinkCategory, System.StringComparison.OrdinalIgnoreCase));
        if (matchingTransform != null)
        {
            matchingTransform.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"No matching transform found for drink category: {data.drinkCategory}");
        }
    }

    [System.Serializable]
    public class AdminData
    {
        public int id;
        public string drinkCategory;
        public string spirit1;
        public string spirit2;
        public string spirit3;
        public string spirit4;
        public string spirit5;
        public string passcodeKey;

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