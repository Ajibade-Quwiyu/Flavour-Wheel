using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.Collections;

public class SceneSelector : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoClip introVideo;
    private VideoPlayer videoPlayer;

    [Header("Events")]
    public UnityEvent OnVideoComplete;

    void Awake()
    {
        StartCoroutine(CallAPIEndpoints());

        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            OnVideoComplete?.Invoke();
            return;
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
            OnVideoComplete?.Invoke();
            return;
        }
        videoPlayer = gameObject.GetComponent<VideoPlayer>();
        videoPlayer.clip = introVideo;
        videoPlayer.loopPointReached += VideoPlayer_LoopPointReached;
        videoPlayer.errorReceived += VideoPlayer_ErrorReceived;
        videoPlayer.Play();

        Debug.Log("Starting video playback");
    }

    private void VideoPlayer_ErrorReceived(VideoPlayer source, string message)
    {
        Debug.LogError($"Video Player Error: {message}");
        CleanupVideo();
        OnVideoComplete?.Invoke();
    }

    private void VideoPlayer_LoopPointReached(VideoPlayer source)
    {
        Debug.Log("Video playback completed");
        CleanupVideo();
        OnVideoComplete?.Invoke();
    }

    void CleanupVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            Destroy(videoPlayer);
        }
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

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= VideoPlayer_LoopPointReached;
            videoPlayer.errorReceived -= VideoPlayer_ErrorReceived;
        }
    }

    public void LoadScene(string sceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}