using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    public VideoClip introVideo;
    public GameObject GameStart;
    public GameObject NotConnected;
    public GameObject ReloadButton;
    public GameObject SkipVideoButton;

    private VideoPlayer videoPlayer;
    private bool isCheckingConnection = false;
    private bool shouldPlayVideo = true;

    void Awake()
    {
        videoPlayer = gameObject.GetComponent<VideoPlayer>();
        videoPlayer.clip = introVideo;

        if (shouldPlayVideo)
        {
            PlayVideo();
        }
        else
        {
            CheckInternetConnection();
        }
    }

    void PlayVideo()
    {
        videoPlayer.Play();
        SkipVideoButton.SetActive(true);
        StartCoroutine(WaitForVideoToEnd());
    }

    IEnumerator WaitForVideoToEnd()
    {
        yield return new WaitForSeconds((float)introVideo.length);
        VideoEnded();
    }

    void VideoEnded()
    {
        SkipVideoButton.SetActive(false);
        CheckInternetConnection();
    }

    void CheckInternetConnection()
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            EnableGameStart();
        }
        else
        {
            EnableNotConnected();
            if (!isCheckingConnection)
            {
                StartCoroutine(CheckConnectionPeriodically());
            }
        }
    }

    void EnableGameStart()
    {
        GameStart.SetActive(true);
        NotConnected.SetActive(false);
        ReloadButton.SetActive(false);
        SkipVideoButton.SetActive(false);
        isCheckingConnection = false;
        this.gameObject.SetActive(false);
        enabled = false; // Disable this script
    }

    void EnableNotConnected()
    {
        GameStart.SetActive(false);
        NotConnected.SetActive(true);
        ReloadButton.SetActive(true);
        SkipVideoButton.SetActive(false);
    }

    IEnumerator CheckConnectionPeriodically()
    {
        isCheckingConnection = true;
        while (Application.internetReachability == NetworkReachability.NotReachable)
        {
            yield return new WaitForSeconds(1f);
        }
        EnableGameStart();
    }

    public void ReloadScene()
    {
        ReloadButton.SetActive(false);
        shouldPlayVideo = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SkipVideo()
    {
        StopCoroutine(WaitForVideoToEnd());
        videoPlayer.Stop();
        VideoEnded();
    }
}