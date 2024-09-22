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
    public string webGLVideoUrl = "https://raw.githubusercontent.com/Ajibade-Quwiyu/Flavour_Wheel_Video/main/Flavour%20App%20intro.mp4";
    public RawImage videoRawImage;
    public AspectRatioFitter aspectRatioFitter;
    public GameObject loadingObject;
    public GameObject choosePanel;
    public Button adminButton;
    public Button guestButton;
    public GameObject adminPasswordPanel;
    public TMP_InputField adminPasswordInput;
    public Button adminLoginButton;
    public TextMeshProUGUI incorrectPasswordText;

    private VideoPlayer videoPlayer;
    private GameObject mainPanel;
    private RenderTexture videoTexture;

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

        StartCoroutine(CallAPIEndpoints());
    }

    void SetupVideoPlayer()
    {
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.isLooping = false;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = webGLVideoUrl;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        videoTexture = new RenderTexture(1080, 2048, 24, RenderTextureFormat.ARGB32);
        videoPlayer.targetTexture = videoTexture;

        if (videoRawImage != null)
        {
            videoRawImage.texture = videoTexture;
            videoRawImage.gameObject.SetActive(true);
        }

        if (aspectRatioFitter != null)
        {
            aspectRatioFitter.aspectRatio = 9f / 16f; // Adjust this ratio if your video has a different aspect ratio
        }

        if (loadingObject != null)
        {
            loadingObject.SetActive(true);
        }

        videoPlayer.prepareCompleted += VideoPlayer_PrepareCompleted;
        videoPlayer.errorReceived += VideoPlayer_ErrorReceived;
        videoPlayer.loopPointReached += VideoPlayer_LoopPointReached;

        videoPlayer.Prepare();
    }

    private void VideoPlayer_PrepareCompleted(VideoPlayer source)
    {
        Debug.Log("Video prepared successfully");
        videoPlayer.Play();
        if (loadingObject != null)
        {
            loadingObject.SetActive(false);
        }
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
        videoPlayer.Stop();
        videoPlayer.gameObject.SetActive(false);
        if (videoRawImage != null)
        {
            videoRawImage.gameObject.SetActive(false);
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
            videoPlayer.prepareCompleted -= VideoPlayer_PrepareCompleted;
            videoPlayer.errorReceived -= VideoPlayer_ErrorReceived;
            videoPlayer.loopPointReached -= VideoPlayer_LoopPointReached;
        }
        if (videoTexture != null)
        {
            videoTexture.Release();
            Destroy(videoTexture);
        }
    }
}