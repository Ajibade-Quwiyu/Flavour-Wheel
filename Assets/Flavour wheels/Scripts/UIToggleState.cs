using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Text;
using System.Linq;

[System.Serializable]
public class WordPair
{
    public string englishWord;
    public string spanishWord;
}

public enum FlavorCategory
{
    BOURBON,
    WHISKEY,
    RUM,
    GIN,
    REDWINE,
    SPARKLING_WINE,
    COGNAC,
    TEQUILA,
    BEER
}

[System.Serializable]
public class FlavorPair
{
    public string englishWord;
    public string spanishWord;
    public FlavorCategory category;
}

public class UIToggleState : MonoBehaviour
{
    [SerializeField] private GameObject spanish;
    [SerializeField] private GameObject english;
    private bool isEnglish; // true for English, false for Spanish

    public List<WordPair> wordPairs = new List<WordPair>();
    public List<FlavorPair> flavorPairs = new List<FlavorPair>();

    [SerializeField] private FlavorCategory activeFlavorCategory;

    private const string LanguagePrefsKey = "SelectedLanguage";

    private void Start()
    {
        LoadLanguagePreference();
        SetActiveLanguage();
    }

    private void LoadLanguagePreference()
    {
        // Load the saved language preference (default to English if not set)
        isEnglish = PlayerPrefs.GetInt(LanguagePrefsKey, 1) == 1;
    }

    private void SaveLanguagePreference()
    {
        PlayerPrefs.SetInt(LanguagePrefsKey, isEnglish ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void SetActiveLanguage()
    {
        english.SetActive(isEnglish);
        spanish.SetActive(!isEnglish);

        if (isEnglish)
        {
            TranslateToEnglish();
        }
        else
        {
            TranslateToSpanish();
        }
    }

    public void language()
    {
        isEnglish = !isEnglish;
        SetActiveLanguage();
        SaveLanguagePreference();
    }

    public void ToggleState(GameObject obj)
    {
        obj.SetActive(!obj.activeSelf);
    }

    private List<FlavorPair> GetActiveFlavorPairs()
    {
        return flavorPairs.Where(fp => fp.category == activeFlavorCategory).ToList();
    }

    private void TranslateToSpanish()
    {
        foreach (var pair in wordPairs)
        {
            TranslateText(pair.englishWord, pair.spanishWord);
        }
        foreach (var pair in GetActiveFlavorPairs())
        {
            TranslateText(pair.englishWord, pair.spanishWord);
            TranslateTransformNames(pair.englishWord, pair.spanishWord);
        }
    }

    private void TranslateToEnglish()
    {
        foreach (var pair in wordPairs)
        {
            TranslateText(pair.spanishWord, pair.englishWord);
        }
        foreach (var pair in GetActiveFlavorPairs())
        {
            TranslateText(pair.spanishWord, pair.englishWord);
            TranslateTransformNames(pair.spanishWord, pair.englishWord);
        }
    }

    private void TranslateText(string from, string to)
    {
        Text[] unityTexts = Resources.FindObjectsOfTypeAll<Text>();
        foreach (var text in unityTexts)
        {
            if (text.text == from)
            {
                text.text = to;
            }
        }

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