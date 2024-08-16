using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UserInputManager : MonoBehaviour
{
    public TMP_InputField usernameInputField;
    public TMP_InputField emailInputField;
    public TMP_InputField passcodeKeyInputField;
    public TMP_InputField overallExperienceInputField;
    public Transform overallRatingTransform;
    public Button submitButton;
    public List<Text> spiritTextFields;
    public Text drinkCategoryText;
    public GameObject signInPage;
    public GameObject gamePanel;
    public SpiritManager spiritManager;
    public AudioClip ratingSound;
    public float particlesDuration = 5f;
    public List<Transform> drinkCategoryTransforms;

    private GameObject incorrectPasscodeIndicator;
    private string connectionString = "Server=sql8.freesqldatabase.com; Database=sql8721580; User=sql8721580; Password=6wdc5VDnaQ; Charset=utf8;";
    private int overallRating = 0;
    private List<Button> ratingButtons = new List<Button>();
    private AudioSource audioSource;
    private ParticleSystem[] particles;

    private const string UsernameKey = "PlayerUsername";
    private const string EmailKey = "PlayerEmail";
    

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

        StartCoroutine(CheckForUpdates());
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

        // Disable all drink category transforms at start
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
    string enteredPasscodeKey = passcodeKeyInputField.text;
    AdminData adminData = null;

    try
    {
        adminData = GetAdminDataFromDatabase();

        // Check for a null result, which might indicate a failed database query
        if (adminData == null)
        {
            // If adminData is null, it could be a network issue or another error
            DisplayIncorrectPasscodeMessage("No Connections");
            return;
        }
    }
    catch (MySqlException e)
    {
        // Handle specific network-related errors
        if (e.Number == 0) // 0 is the common error code for network-related MySQL exceptions
        {
            DisplayIncorrectPasscodeMessage("No Connections");
        }
        else
        {
            // Log the error for other types of MySQL exceptions
            Debug.LogError($"Error retrieving data: {e.Message}");
            DisplayIncorrectPasscodeMessage("An unexpected error occurred.");
        }
        return;
    }

    // Check if the passcode is correct after a successful database connection
    if (adminData.PasscodeKey == enteredPasscodeKey)
    {
        UpdateUI(adminData);
        incorrectPasscodeIndicator.gameObject.SetActive(false);

        SaveUserData();
        
        PlayParticleEffects();

        signInPage.SetActive(false);
        gamePanel.SetActive(true);
    }
    else
    {
        DisplayIncorrectPasscodeMessage("The key you entered is incorrect!!");
    }
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

    private AdminData GetAdminDataFromDatabase()
{
    AdminData adminData = null;

    using (MySqlConnection conn = new MySqlConnection(connectionString))
    {
        try
        {
            conn.Open();

            string query = "SELECT * FROM AdminServer LIMIT 1;";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            MySqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                adminData = new AdminData
                {
                    DrinkCategory = reader.GetString("DrinkCategory"),
                    Spirit1 = reader.GetString("Spirit1"),
                    Spirit2 = reader.GetString("Spirit2"),
                    Spirit3 = reader.GetString("Spirit3"),
                    Spirit4 = reader.GetString("Spirit4"),
                    Spirit5 = reader.GetString("Spirit5"),
                    PasscodeKey = reader.GetString("PasscodeKey")
                };
            }
        }
        catch (MySqlException e)
        {
            // Log the error to the console
            Debug.LogError($"Error retrieving data: {e.Message}");

        }
    }

    return adminData;
}


    private IEnumerator CheckForUpdates()
    {
        while (true)
        {
            AdminData newData = GetAdminDataFromDatabase();
            if (newData != null)
            {
                UpdateUI(newData);
            }
            yield return new WaitForSeconds(60); // Check every minute
        }
    }

    private void UpdateUI(AdminData data)
    {
        drinkCategoryText.text = data.DrinkCategory + " WHEEL";
        spiritTextFields[0].text = data.Spirit1;
        spiritTextFields[1].text = data.Spirit2;
        spiritTextFields[2].text = data.Spirit3;
        spiritTextFields[3].text = data.Spirit4;
        spiritTextFields[4].text = data.Spirit5;

        // Disable all drink category transforms
        foreach (var transform in drinkCategoryTransforms)
        {
            if (transform != null)
            {
                transform.gameObject.SetActive(false);
            }
        }

        // Enable the matching drink category transform
        Transform matchingTransform = drinkCategoryTransforms.Find(t => t.name.Equals(data.DrinkCategory, System.StringComparison.OrdinalIgnoreCase));
        if (matchingTransform != null)
        {
            matchingTransform.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"No matching transform found for drink category: {data.DrinkCategory}");
        }
    }

    public class AdminData
    {
        public string DrinkCategory { get; set; }
        public string Spirit1 { get; set; }
        public string Spirit2 { get; set; }
        public string Spirit3 { get; set; }
        public string Spirit4 { get; set; }
        public string Spirit5 { get; set; }
        public string PasscodeKey { get; set; }
    }
}