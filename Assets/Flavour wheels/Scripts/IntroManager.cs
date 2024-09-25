using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class IntroManager : MonoBehaviour
{
    public VideoClip introVideo;
    public GameObject choosePanel;
    public Button adminButton;
    public Button guestButton;
    public GameObject adminPasswordPanel;
    public TMP_InputField adminPasswordInput;
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
        SetupVideoPlayer();

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
    }

    void SetupVideoPlayer()
    {
        videoPlayer = gameObject.GetComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.clip = introVideo;
        videoPlayer.isLooping = false;
        videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
        videoPlayer.targetCamera = Camera.main;

        videoPlayer.loopPointReached += VideoPlayer_LoopPointReached;
        videoPlayer.Play();
    }

    private void VideoPlayer_LoopPointReached(VideoPlayer source)
    {
        Debug.Log("Video playback completed");
        TransitionToMainGame();
    }

    void TransitionToMainGame()
    {
        videoPlayer.Stop();
        Destroy(videoPlayer);
        mainPanel.SetActive(true);
        LoadLoginPreference();
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
        }
    }
}