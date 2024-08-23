using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroManager : MonoBehaviour
{
    public VideoClip introVideo;
    public GameObject[] Others;

    private VideoPlayer videoPlayer;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        videoPlayer.clip = introVideo;
        ToggleObjects(false);
        videoPlayer.Play();
        StartCoroutine(StartGameAfterVideo());
        FindAnyObjectByType<UserInputManager>().StartMethod();
    }

    IEnumerator StartGameAfterVideo()
    {
        yield return new WaitForSeconds((float)introVideo.length);
        videoPlayer.gameObject.SetActive(false);
        ToggleObjects(true);
        this.enabled = false;
    }

    void ToggleObjects(bool isActive)
    {
        foreach (var obj in Others)
        {
            obj.SetActive(isActive);
        }
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
