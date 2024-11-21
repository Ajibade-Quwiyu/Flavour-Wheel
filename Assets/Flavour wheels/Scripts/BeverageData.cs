// BeverageData.cs
using UnityEngine;
using System.Collections.Generic;

public enum BeverageType
{
    BOURBON,
    WHISKY,
    RUM,
    COGNAC,
    REDWINE,
    SPARKLING_WINE,
    GIN,
    TEQUILA,
    WHITEWINE,
    BEER
}

[CreateAssetMenu(fileName = "New Beverage Data", menuName = "Translation/Beverage Data")]
public class BeverageData : ScriptableObject
{
    [System.Serializable]
    public class BeveragePair
    {
        public string englishWord;
        public string spanishWord;
    }

    public BeverageType beverageType;

    [Header("Translation Pairs")]
    [Tooltip("Add all English-Spanish translation pairs for this beverage type")]
    public List<BeveragePair> translationPairs = new List<BeveragePair>();

#if UNITY_EDITOR
    // This will show up in the inspector as a read-only field
    [SerializeField, ReadOnly]
    private int translationCount;

    private void OnValidate()
    {
        translationCount = translationPairs.Count;
        
        // Auto-name the asset based on the category
        if (UnityEditor.AssetDatabase.Contains(this))
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
            string expectedName = $"{beverageType}_Translations";
            
            if (!assetPath.Contains(expectedName))
            {
                string directory = System.IO.Path.GetDirectoryName(assetPath);
                string newPath = System.IO.Path.Combine(directory, $"{expectedName}.asset");
                UnityEditor.AssetDatabase.RenameAsset(assetPath, expectedName);
            }
        }
    }
#endif
}

// ReadOnly attribute for editor
#if UNITY_EDITOR
public class ReadOnlyAttribute : PropertyAttribute { }

[UnityEditor.CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : UnityEditor.PropertyDrawer
{
    public override void OnGUI(Rect position, UnityEditor.SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;
        UnityEditor.EditorGUI.PropertyField(position, property, label);
        GUI.enabled = true;
    }
}
#endif