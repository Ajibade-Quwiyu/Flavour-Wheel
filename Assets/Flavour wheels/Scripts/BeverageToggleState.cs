// BeverageToggleState.cs
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Text;
using System.Linq;

[System.Serializable]
public class TranslationPair
{
    public string englishWord;
    public string spanishWord;
}

public class BeverageToggleState : MonoBehaviour
{
    [SerializeField] private GameObject spanish;
    [SerializeField] private GameObject english;
    private bool isEnglish; // true for English, false for Spanish

    public List<TranslationPair> wordPairs = new List<TranslationPair>();
    [SerializeField] private List<BeverageData> beverageDataList = new List<BeverageData>();
    [SerializeField] private BeverageType activeBeverageType;

    private const string LanguagePrefsKey = "SelectedLanguage";

    private void Start()
    {
        LoadLanguagePreference();
        SetActiveLanguage();
    }

    public void SetActiveBeverageType(BeverageType newType)
    {
        activeBeverageType = newType;
        SetActiveLanguage();
    }

    private void LoadLanguagePreference()
    {
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

    private List<BeverageData.BeveragePair> GetActiveTranslationPairs()
    {
        // Find the beverage data that matches the active type by name
        BeverageData activeData = beverageDataList.FirstOrDefault(data => data != null &&
            data.name.StartsWith(activeBeverageType.ToString(), System.StringComparison.OrdinalIgnoreCase));

        if (activeData != null)
        {
            return activeData.translationPairs;
        }
        return new List<BeverageData.BeveragePair>();
    }

    private void TranslateToSpanish()
    {
        foreach (var pair in wordPairs)
        {
            TranslateText(pair.englishWord, pair.spanishWord);
        }
        foreach (var pair in GetActiveTranslationPairs())
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
        foreach (var pair in GetActiveTranslationPairs())
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