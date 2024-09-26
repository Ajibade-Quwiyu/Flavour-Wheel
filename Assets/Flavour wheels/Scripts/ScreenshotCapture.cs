using System.IO;
using UnityEngine;
using System.Collections;
#if UNITY_IOS || UNITY_ANDROID
using NativeShareNamespace; // Make sure to include Native Share
#endif

public class ScreenshotCapture : MonoBehaviour
{
    public string screenshotFileName = "screenshot.png";
    private Texture2D screenshotTexture; // Store screenshot texture here

    // Method to capture screenshot and share
    public void CaptureAndShareScreenshot()
    {
        StartCoroutine(CaptureScreenshot());
    }

    // Coroutine to capture the screenshot
    private IEnumerator CaptureScreenshot()
    {
        // Wait for the end of the frame to ensure the screenshot captures the current UI
        yield return new WaitForEndOfFrame();

        // Capture the screenshot and save it to a Texture2D
        screenshotTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenshotTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshotTexture.Apply();

        // Save screenshot to file
        string filePath = Path.Combine(Application.persistentDataPath, screenshotFileName);
        File.WriteAllBytes(filePath, screenshotTexture.EncodeToPNG());
        Debug.Log("Screenshot saved to: " + filePath);

#if UNITY_IOS || UNITY_ANDROID
        // Use Native Share to share the screenshot on mobile
        new NativeShare().AddFile(filePath)
                         .SetSubject("Check out my screenshot!")
                         .SetText("Here's a screenshot I captured.")
                         .Share();
#elif UNITY_WEBGL
        // For WebGL, trigger a download of the image file
        StartCoroutine(DownloadScreenshotWebGL());
#endif

        // Clean up memory (optional, depends on usage)
        // Destroy(screenshotTexture); - commented out for now to prevent early disposal
    }

#if UNITY_WEBGL
    // Coroutine to download screenshot in WebGL
    private IEnumerator DownloadScreenshotWebGL()
    {
        // Wait until the next frame in case of timing issues
        yield return new WaitForEndOfFrame();

        // Ensure the screenshotTexture is not null
        if (screenshotTexture == null)
        {
            Debug.LogError("Screenshot texture is null, cannot download screenshot.");
            yield break;
        }

        // Convert the screenshot texture to PNG format
        byte[] screenshotBytes = screenshotTexture.EncodeToPNG();
        string base64String = System.Convert.ToBase64String(screenshotBytes);

        // Trigger JavaScript download in WebGL
        string jsDownloadCommand = string.Format("var link = document.createElement('a');" +
                                                 "link.href = 'data:image/png;base64,{0}';" +
                                                 "link.download = '{1}';" +
                                                 "document.body.appendChild(link);" +
                                                 "link.click();" +
                                                 "document.body.removeChild(link);",
                                                 base64String, screenshotFileName);

        Application.ExternalEval(jsDownloadCommand);
    }
#endif
}
