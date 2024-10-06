using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoClip introVideo;

    [Header("UI Elements")]
    public GameObject choosePanel;
    public Button adminButton;
    public Button guestButton;
    public GameObject adminPasswordPanel;
    public InputField adminPasswordInput;
    public Button adminLoginButton;
    public TextMeshProUGUI incorrectPasswordText;

    private VideoPlayer videoPlayer;
    private GameObject mainPanel;

    public UnityEvent OnAdminLogin;
    public UnityEvent OnGuestLogin;
    public UnityEvent OnAdminLoginFail;

    private const string AdminPassword = "Cesar";
    private const string LoginPrefKey = "LoginType";

    void Awake()
    {
        mainPanel = choosePanel.transform.parent.gameObject;
        mainPanel.SetActive(false);
        choosePanel.SetActive(false);

        adminButton.onClick.AddListener(AdminLogin);
        guestButton.onClick.AddListener(GuestLogin);
        adminLoginButton.onClick.AddListener(ValidateAdminPassword);

        if (incorrectPasswordText != null)
        {
            incorrectPasswordText.gameObject.SetActive(false);
        }

        StartCoroutine(CallAPIEndpoints());

        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // For WebGL, skip video and start the game immediately
            TransitionToMainGame();
        }
        else
        {
            SetupVideoPlayer();
        }
    }

    void SetupVideoPlayer()
    {
        if (introVideo == null)
        {
            Debug.LogError("Intro video clip is not assigned!");
            TransitionToMainGame();
            return;
        }
        videoPlayer = gameObject.GetComponent<VideoPlayer>();
        videoPlayer.loopPointReached += VideoPlayer_LoopPointReached;
        videoPlayer.errorReceived += VideoPlayer_ErrorReceived;
        videoPlayer.Play();

        Debug.Log("Starting video playback");
    }

    private void VideoPlayer_ErrorReceived(VideoPlayer source, string message)
    {
        Debug.LogError($"Video Player Error: {message}");
        TransitionToMainGame();
    }

    private void VideoPlayer_LoopPointReached(VideoPlayer source)
    {
        Debug.Log("Video playback completed");
        TransitionToMainGame();
    }

    void TransitionToMainGame()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            Destroy(videoPlayer);
        }

        mainPanel.SetActive(true);
        LoadLoginPreference();
    }

    IEnumerator CallAPIEndpoints()
    {
        yield return StartCoroutine(CallAPI("https://flavour-wheel-server.onrender.com/api/adminserver"));
        yield return StartCoroutine(CallAPI("https://flavour-wheel-server.onrender.com/api/flavourwheel"));
    }

    IEnumerator CallAPI(string url)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Error calling {url}: {webRequest.error}");
            }
            else
            {
                Debug.Log($"Successfully called {url}");
            }
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

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= VideoPlayer_LoopPointReached;
            videoPlayer.errorReceived -= VideoPlayer_ErrorReceived;
        }
    }
}