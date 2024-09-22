using UnityEngine;
using UnityEngine.Video;

public class WebGLVideoPlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Camera targetCamera;
    public string videoUrl = "https://raw.githubusercontent.com/Ajibade-Quwiyu/Flavour_Wheel_Video/main/Flavour%20App%20intro.mp4";
    public Material videoMaterial;

    private RenderTexture videoTexture;

    void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (targetCamera == null)
            targetCamera = Camera.main;

        SetupVideoPlayer();
    }

    void SetupVideoPlayer()
    {
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoUrl;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // Create a render texture with the video's dimensions
        videoTexture = new RenderTexture(1920, 1080, 24);
        videoTexture.Create();

        videoPlayer.targetTexture = videoTexture;

        // Set up the material for the video
        if (videoMaterial != null)
        {
            videoMaterial.mainTexture = videoTexture;
        }

        videoPlayer.Prepare();
    }

    void Update()
    {
        if (videoPlayer.isPrepared && !videoPlayer.isPlaying)
        {
            videoPlayer.Play();
        }
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (videoMaterial != null)
        {
            Graphics.Blit(source, destination, videoMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
}