using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    public VideoClip introVideo;
    public GameObject GameStart;
    public GameObject ReloadButton;
    public GameObject[] Others;

    private VideoPlayer videoPlayer;
    private static bool shouldPlayVideoOnReload = true; // Static variable

    void Awake()
    {
        SetActiveForOthers(false);
        videoPlayer = gameObject.GetComponent<VideoPlayer>();
        videoPlayer.clip = introVideo;

        // Set 'Others' GameObjects active at the start
        SetActiveForOthers(true);

        // Start playing the video immediately since we are not checking the connection
        PlayVideo();
    }

    void PlayVideo()
    {
        if (shouldPlayVideoOnReload)
        {
            // Set 'Others' GameObjects inactive while the video is playing
            SetActiveForOthers(false);
            
            videoPlayer.Play();
            StartCoroutine(PrepareGameStart());
        }
    }

    IEnumerator PrepareGameStart()
    {
        // Wait until 5 seconds before the video ends
        yield return new WaitForSeconds((float)introVideo.length - 1f);

        // Enable game components
        EnableGameStart();

        // Wait for the video to finish
        yield return new WaitForSeconds(1f);

        // Disable the video player and this script
        videoPlayer.gameObject.SetActive(false);
        this.enabled = false;

        // Turn 'Others' GameObjects back on
        SetActiveForOthers(true);
    }

    void EnableGameStart()
    {
        GameStart.SetActive(true);
        ReloadButton.SetActive(false);
    }

    void SetActiveForOthers(bool isActive)
    {
        foreach (GameObject obj in Others)
        {
            obj.SetActive(isActive);
        }
    }

    public void ReloadSceneWithoutPlayingVideo()
    {
        shouldPlayVideoOnReload = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
