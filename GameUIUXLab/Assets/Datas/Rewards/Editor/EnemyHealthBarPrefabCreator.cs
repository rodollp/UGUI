#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class EnemyHealthBarPrefabCreator
{
    private const string Folder = "Assets/GeneratedEnemyHealthBar";

    [MenuItem("Tools/Enemy Health Bar/Create Prefab")]
    public static void CreatePrefab()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "GeneratedEnemyHealthBar");
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        texture.Apply();
        var pngPath = Folder + "/SolidWhite.png";
        System.IO.File.WriteAllBytes(pngPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(pngPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);

        var root = new GameObject("EnemyHealthBar", typeof(RectTransform), typeof(Canvas), typeof(HealthBarUI));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(210, 38);
        rect.localScale = Vector3.one * 0.01f;

        var bg = MakeImage("Background", root.transform, sprite, new Color(.12f,.12f,.14f,1), 0, 0, 170, 24);
        var fill = MakeImage("Fill", root.transform, sprite, new Color(.72f,.19f,.22f,1), 0, 0, 166, 20);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1;
        var hpText = MakeText("HPText", root.transform, "100 / 100", 17, 0, 0, 170, 24);
        hpText.color = Color.white;

        var shield = new GameObject("BlockGroup", typeof(RectTransform));
        shield.transform.SetParent(root.transform, false);
        SetRect((RectTransform)shield.transform, -99, 0, 33, 30);
        MakeImage("ShieldBackground", shield.transform, sprite, new Color(.16f,.35f,.60f,1), 0, 0, 31, 29);
        var blockText = MakeText("BlockText", shield.transform, "0", 16, 0, 0, 31, 29);
        shield.SetActive(false);

        var component = root.GetComponent<HealthBarUI>();
        var so = new SerializedObject(component);
        so.FindProperty("hpFill").objectReferenceValue = fill;
        so.FindProperty("hpText").objectReferenceValue = hpText;
        so.FindProperty("blockGroup").objectReferenceValue = shield;
        so.FindProperty("blockText").objectReferenceValue = blockText;
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "/EnemyHealthBar.prefab");
        Object.DestroyImmediate(root);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log("EnemyHealthBar prefab created: " + Folder + "/EnemyHealthBar.prefab");
    }

    private static Image MakeImage(string name, Transform parent, Sprite sprite, Color color, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, x, y, w, h);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI MakeText(
     string name, Transform parent, string value,
     float size, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        SetRect((RectTransform)go.transform, x, y, w, h);

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        return text;
    }

    private static void SetRect(RectTransform rect, float x, float y, float w, float h)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
        rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = new Vector2(x,y);
        rect.sizeDelta = new Vector2(w,h);
    }
}
#endif
