using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;

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
    public ParticleSystem barley;
    public float barleyDuration = 5f;

    private GameObject incorrectPasscodeIndicator;
    private string connectionString = "Server=sql8.freesqldatabase.com; Database=sql8721580; User=sql8721580; Password=6wdc5VDnaQ; Charset=utf8;";
    private int overallRating = 0;
    private List<Button> ratingButtons = new List<Button>();
    private AudioSource audioSource;

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

        if (barley != null)
        {
            barley.gameObject.SetActive(false);
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

        AdminData adminData = GetAdminDataFromDatabase();
        if (adminData != null && adminData.PasscodeKey == enteredPasscodeKey)
        {
            drinkCategoryText.text = adminData.DrinkCategory + " WHEEL";
            spiritTextFields[0].text = adminData.Spirit1;
            spiritTextFields[1].text = adminData.Spirit2;
            spiritTextFields[2].text = adminData.Spirit3;
            spiritTextFields[3].text = adminData.Spirit4;
            spiritTextFields[4].text = adminData.Spirit5;

            incorrectPasscodeIndicator.SetActive(false);

            SaveUserData();

            PlayBarleyEffect();

            signInPage.SetActive(false);
            gamePanel.SetActive(true);
        }
        else
        {
            incorrectPasscodeIndicator.SetActive(true);
        }
    }

    private void PlayBarleyEffect()
    {
        if (barley != null)
        {
            barley.gameObject.SetActive(true);
            barley.Play();
            StartCoroutine(DeactivateBarleyAfterDelay());
        }
    }

    private IEnumerator DeactivateBarleyAfterDelay()
    {
        yield return new WaitForSeconds(barleyDuration);

        if (barley != null)
        {
            barley.Stop();
            barley.gameObject.SetActive(false);
        }
    }

    public void Userdata()
    {
        string username = usernameInputField.text;
        string email = !string.IsNullOrEmpty(emailInputField.text) ? emailInputField.text : "example@example.com";
        string feedback = overallExperienceInputField.text;

        if (spiritManager != null)
        {
            spiritManager.SetUserData(username, email, overallRating, feedback);
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
                Debug.LogError($"Error retrieving data: {e.Message}");
            }
        }

        return adminData;
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