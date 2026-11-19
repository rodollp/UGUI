using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CardVisualSetup
{
    private const string Folder = "Assets/Images/Cards";

    [MenuItem("Tools/Cards/Apply Generated Visuals")]
    public static void Configure()
    {
        Sprite frame = Import("card-frame-v1.png");
        Sprite strike = Import("strike-v1.png");
        Sprite defend = Import("defend-v1.png");
        foreach (string guid in AssetDatabase.FindAssets("t:CardData", new[] { "Assets/Datas" }))
        {
            CardData data = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid));
            if (data.Type == CardType.Attack) data.Artwork = strike;
            else if (data.Type == CardType.Skill) data.Artwork = defend;
            EditorUtility.SetDirty(data);
        }

        const string path = "Assets/Prefabs/UI/CardUI.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Image background = prefab.transform.Find("BackGround").GetComponent<Image>();
            background.sprite = frame;
            background.color = Color.white;
            background.raycastTarget = false;
            background.transform.SetAsFirstSibling();
            Image art = prefab.transform.Find("Artwork").GetComponent<Image>();
            art.sprite = strike;
            art.color = Color.white;
            art.preserveAspect = false;
            art.raycastTarget = false;
            SerializedObject card = new SerializedObject(prefab.GetComponent<CardUI>());
            card.FindProperty("artworkImage").objectReferenceValue = art;
            card.ApplyModifiedPropertiesWithoutUndo();
            Text(prefab, "NameText", 18, new Color(1, 0.94f, 0.78f), TextAlignmentOptions.Center);
            Text(prefab, "CostText", 24, Color.white, TextAlignmentOptions.Center);
            Text(prefab, "TypeText", 13, new Color(0.25f, 0.15f, 0.07f), TextAlignmentOptions.Center);
            Text(prefab, "DescriptionText", 16, new Color(0.16f, 0.11f, 0.07f), TextAlignmentOptions.Top);
            RectTransform name = prefab.transform.Find("NameText").GetComponent<RectTransform>();
            name.anchoredPosition = new Vector2(27, -10);
            name.sizeDelta = new Vector2(145, 38);
            RectTransform type = prefab.transform.Find("TypeText").GetComponent<RectTransform>();
            type.anchoredPosition = new Vector2(0, 101);
            type.sizeDelta = new Vector2(180, 25);
            PrefabUtility.SaveAsPrefabAsset(prefab, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();
        Debug.Log("CARD_VISUAL_SETUP_OK");
    }

    private static void Text(GameObject prefab, string name, float size, Color color, TextAlignmentOptions alignment)
    {
        TMP_Text text = prefab.transform.Find(name).GetComponent<TMP_Text>();
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = size - 3;
        text.fontSizeMax = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
    }

    private static Sprite Import(string file)
    {
        string path = Folder + "/" + file;
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static void ConfigureAndValidate()
    {
        Configure();
        BattleValidation.Run();
    }
}
