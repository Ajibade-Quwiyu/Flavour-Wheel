using UnityEngine;
using System.Collections.Generic;
using TMPro; // Include this for TextMeshPro support
using UnityEngine.UI; // Include this for Unity UI Text support
using System.Text;

[System.Serializable]
public class WordPair
{
    public string englishWord;
    public string spanishWord;
}

public class UIToggleState : MonoBehaviour
{
    [SerializeField] private GameObject spanish;
    [SerializeField] private GameObject english;
    private bool lang;

    public List<WordPair> wordPairs = new List<WordPair>();

    public void language()
    {
        lang = !lang;
        if (lang)
        {
            english.SetActive(true);
            spanish.SetActive(false);
            TranslateToSpanish();
        }
        else
        {
            english.SetActive(false);
            spanish.SetActive(true);
            TranslateToEnglish();
        }
    }

    public void ToggleState(GameObject obj)
    {
        obj.SetActive(!obj.activeSelf);
    }

    private void TranslateToSpanish()
    {
        // Translate all Unity Text, TextMeshPro components, and Transform names to Spanish
        foreach (var pair in wordPairs)
        {
            TranslateText(pair.englishWord, pair.spanishWord);
            TranslateTransformNames(pair.englishWord, pair.spanishWord);
        }
    }

    private void TranslateToEnglish()
    {
        // Translate all Unity Text, TextMeshPro components, and Transform names to English
        foreach (var pair in wordPairs)
        {
            TranslateText(pair.spanishWord, pair.englishWord);
            TranslateTransformNames(pair.spanishWord, pair.englishWord);
        }
    }

    private void TranslateText(string from, string to)
    {
        // Translate Unity Text components
        Text[] unityTexts = Resources.FindObjectsOfTypeAll<Text>();
        foreach (var text in unityTexts)
        {
            if (text.text == from)
            {
                text.text = to;
            }
        }

        // Translate TextMeshPro components
        TextMeshProUGUI[] tmpTexts = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();
        foreach (var text in tmpTexts)
        {
            if (text.text == from)
            {
                text.text = to;
            }
        }
    }

    private void TranslateTransformNames(string from, string to)
    {
        StringBuilder fromBuilder = new StringBuilder(from.Trim());
        StringBuilder toBuilder = new StringBuilder(to.Trim());
        string fromString = fromBuilder.ToString();
        string toString = toBuilder.ToString();

        // Get all root game objects in the scene
        List<GameObject> rootObjects = new List<GameObject>();
        var scenes = UnityEngine.SceneManagement.SceneManager.GetAllScenes();
        foreach (var scene in scenes)
        {
            if (scene.isLoaded)
            {
                var rootGameObjects = scene.GetRootGameObjects();
                rootObjects.AddRange(rootGameObjects);
            }
        }

        // Traverse all root objects and their children
        foreach (var rootObject in rootObjects)
        {
            TraverseAndTranslate(rootObject.transform, fromString, toString);
        }
    }

    private void TraverseAndTranslate(Transform current, string from, string to)
    {
        if (current.name.Trim().Equals(from, System.StringComparison.OrdinalIgnoreCase))
        {
            current.name = to;
        }
        foreach (Transform child in current)
        {
            TraverseAndTranslate(child, from, to);
        }
    }
}
