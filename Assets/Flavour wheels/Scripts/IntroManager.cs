using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject choosePanel;
    public Button adminButton;
    public Button guestButton;
    public GameObject adminPasswordPanel;
    public InputField adminPasswordInput;
    public Button adminLoginButton;
    public TextMeshProUGUI incorrectPasswordText;

    private GameObject mainPanel;
    public UnityEvent OnAdminLogin;
    public UnityEvent OnGuestLogin;
    public UnityEvent OnAdminLoginFail;

    private const string AdminPassword = "Cesar";
    private const string LoginPrefKey = "LoginType";

    void Awake()
    {
        mainPanel = choosePanel.transform.parent.gameObject;
        mainPanel.SetActive(true);
        choosePanel.SetActive(false);

        adminButton.onClick.AddListener(AdminLogin);
        guestButton.onClick.AddListener(GuestLogin);
        adminLoginButton.onClick.AddListener(ValidateAdminPassword);

        if (incorrectPasswordText != null)
        {
            incorrectPasswordText.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        // Show UI immediately
        LoadLoginPreference();

        // Start API calls in parallel in background
        StartCoroutine(CallAPIEndpoints());
    }

    IEnumerator CallAPIEndpoints()
    {
        // Start both requests simultaneously
        var request1 = UnityWebRequest.Get("https://flavour-wheel-server.onrender.com/api/adminserver");
        var request2 = UnityWebRequest.Get("https://flavour-wheel-server.onrender.com/api/flavourwheel");

        var operation1 = request1.SendWebRequest();
        var operation2 = request2.SendWebRequest();

        // Wait for both to complete
        while (!operation1.isDone || !operation2.isDone)
        {
            yield return null;
        }

        // Handle results
        HandleAPIResponse(request1, "adminserver");
        HandleAPIResponse(request2, "flavourwheel");

        request1.Dispose();
        request2.Dispose();
    }

    private void HandleAPIResponse(UnityWebRequest request, string endpoint)
    {
        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"Error calling {endpoint}: {request.error}");
        }
        else
        {
            Debug.Log($"Successfully called {endpoint}");
        }
    }

    void LoadLoginPreference()
    {
        string savedLoginType = PlayerPrefs.GetString(LoginPrefKey, "");
        if (string.IsNullOrEmpty(savedLoginType))
        {
            choosePanel.SetActive(true);
        }
        else
        {
            if (savedLoginType == "Admin")
                InvokeAdminEvent();
            else if (savedLoginType == "Guest")
                InvokeGuestEvent();
        }
    }

    void AdminLogin()
    {
        adminPasswordPanel.SetActive(true);
        if (incorrectPasswordText != null)
        {
            incorrectPasswordText.gameObject.SetActive(false);
        }
    }

    void GuestLogin()
    {
        SaveGuestPreference();
        InvokeGuestEvent();
    }

    void ValidateAdminPassword()
    {
        if (adminPasswordInput.text == AdminPassword)
        {
            SaveAdminPreference();
            InvokeAdminEvent();
            adminPasswordPanel.SetActive(false);
        }
        else
        {
            Debug.Log("Incorrect password.");
            if (incorrectPasswordText != null)
            {
                incorrectPasswordText.text = "Incorrect password. Please login as a guest!";
                incorrectPasswordText.gameObject.SetActive(true);
            }
            OnAdminLoginFail.Invoke();
        }
    }

    void InvokeAdminEvent()
    {
        OnAdminLogin.Invoke();
    }

    void InvokeGuestEvent()
    {
        OnGuestLogin.Invoke();
    }

    void SaveAdminPreference()
    {
        PlayerPrefs.SetString(LoginPrefKey, "Admin");
        PlayerPrefs.Save();
    }

    public void SaveGuestPreference()
    {
        PlayerPrefs.SetString(LoginPrefKey, "Guest");
        PlayerPrefs.Save();
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ClearPrefsAndRestart()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        mainPanel.SetActive(true);
        choosePanel.SetActive(true);
    }
}