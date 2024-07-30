using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MySql.Data.MySqlClient;
using System.Collections.Generic;

public class UserInputManager : MonoBehaviour
{
    public TMP_InputField usernameInputField; // Input field for the username
    public TMP_InputField emailInputField; // Input field for the email
    public TMP_InputField passcodeKeyInputField; // Input field for the passcode key
    public TMP_InputField overallExperienceInputField; // Input field for overall experience
    public Transform overallRatingTransform; // Transform for overall rating buttons and display
    public Button submitButton; // Button to submit the input
    public List<Text> spiritTextFields; // List of Text components to display spirits
    public Text drinkCategoryText; // Text component to display drink category
    public GameObject signInPage; // GameObject for the sign-in page
    public GameObject gamePanel; // GameObject for the game panel
    public SpiritManager spiritManager; // Reference to the SpiritManager
    public AudioClip ratingSound; // Sound clip to play when rating buttons are pressed

    private GameObject incorrectPasscodeIndicator; // GameObject to indicate incorrect passcode
    private string connectionString = "Server=sql8.freesqldatabase.com; Database=sql8721580; User=sql8721580; Password=6wdc5VDnaQ; Charset=utf8;";
    private int overallRating = 0;
    private List<Button> ratingButtons = new List<Button>();
    private AudioSource audioSource; // Audio source component to play sound

    void Start()
    {
        // Get the incorrect passcode indicator as a child of the submit button
        incorrectPasscodeIndicator = submitButton.transform.GetChild(0).gameObject;

        // Add listeners to input fields to check if submit button should be interactable
        usernameInputField.onValueChanged.AddListener(delegate { CheckSubmitButtonInteractivity(); });
        emailInputField.onValueChanged.AddListener(delegate { CheckSubmitButtonInteractivity(); });
        passcodeKeyInputField.onValueChanged.AddListener(delegate { CheckSubmitButtonInteractivity(); });

        // Add a listener to the submit button
        submitButton.onClick.AddListener(OnSubmitButtonClicked);

        // Initially set the submit button as not interactable
        submitButton.interactable = false;

        // Hide the incorrect passcode indicator
        incorrectPasscodeIndicator.SetActive(false);

        // Ensure the correct initial state of the pages
        signInPage.SetActive(true);
        gamePanel.SetActive(false);

        // Set up rating buttons
        for (int i = 2; i < 7; i++)
        {
            int rating = i - 1; // Buttons are 2-6, ratings are 1-5
            Button ratingButton = overallRatingTransform.GetChild(i).GetComponent<Button>();
            ratingButton.onClick.AddListener(() => Rate(rating));
            ratingButtons.Add(ratingButton);
        }

        // Get the AudioSource component from the Camera
        audioSource = Camera.main.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
        }
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
            // Passcode matches, update the UI elements
            drinkCategoryText.text = adminData.DrinkCategory + " WHEEL";
            spiritTextFields[0].text = adminData.Spirit1;
            spiritTextFields[1].text = adminData.Spirit2;
            spiritTextFields[2].text = adminData.Spirit3;
            spiritTextFields[3].text = adminData.Spirit4;
            spiritTextFields[4].text = adminData.Spirit5;

            // Hide the incorrect passcode indicator
            incorrectPasscodeIndicator.SetActive(false);

            // Switch to game panel
            signInPage.SetActive(false);
            gamePanel.SetActive(true);

            
        }
        else
        {
            // Show the incorrect passcode indicator
            incorrectPasscodeIndicator.SetActive(true);
        }
    }
    public void Userdata()
    {
        // Capture user data and submit
        string username = usernameInputField.text;
        string email = !string.IsNullOrEmpty(emailInputField.text) ? emailInputField.text : "example@example.com"; // Check for null email
        string feedback = overallExperienceInputField.text;

        if (spiritManager != null)
        {
            spiritManager.SetUserData(username, email, overallRating, feedback);
        }
    }
    public void Rate(int rating)
    {
        overallRating = rating; // Update current rating

        // Reset all buttons to their default color
        foreach (Button btn in ratingButtons)
        {
            btn.image.color = Color.white;
        }

        // Change the color of the buttons based on the rating
        Color gold = new Color(255 / 255f, 192 / 255f, 0 / 255f); // Gold color in RGB
        for (int i = 0; i < rating; i++)
        {
            ratingButtons[i].image.color = gold;
        }

        // If the last button is pressed, change the color of all buttons
        if (rating == 5)
        {
            foreach (Button btn in ratingButtons)
            {
                btn.image.color = gold;
            }
        }

        // Update the score display
        overallRatingTransform.GetChild(1).GetComponent<Text>().text = rating.ToString();

        // Play the rating sound
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
