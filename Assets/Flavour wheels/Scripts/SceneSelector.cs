using UnityEngine.SceneManagement;
using UnityEngine;

public class SceneSelector : MonoBehaviour
{
    public void SelectScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
