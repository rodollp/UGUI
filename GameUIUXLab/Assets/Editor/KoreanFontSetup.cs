using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class KoreanFontSetup
{
    public const string Folder = "Assets/Fonts/Korean";
    public const string PrimaryPath = Folder + "/Pretendard Korean SDF.asset";
    public const string FallbackPath = Folder + "/Pretendard Korean Dynamic SDF.asset";
    public const string SourcePath = "Assets/TextMesh Pro/Fonts/Pretendard-Regular.ttf";

    [MenuItem("Tools/Fonts/Install Korean SDF")]
    public static void Configure()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Fonts")) AssetDatabase.CreateFolder("Assets", "Fonts");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Fonts", "Korean");
        Font source = AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
        if (source == null) throw new InvalidOperationException("Pretendard 원본 TTF가 없습니다.");
        uint[] characters = CollectCharacters();
        TMP_FontAsset generated = TMP_FontAsset.CreateFontAsset(source, 48, 6, GlyphRenderMode.SDFAA,
            2048, 2048, AtlasPopulationMode.Dynamic, true);
        generated.getFontFeatures = false;
        if (!generated.TryAddCharacters(characters, out uint[] missing, false))
            throw new InvalidOperationException("폰트에 없는 문자: " + string.Join(", ", missing.Select(c => "U+" + c.ToString("X4"))));
        Texture2D[] dynamicTextures = generated.atlasTextures.ToArray();
        var atlases = new List<Texture2D>();
        for (int i = 0; i < dynamicTextures.Length; i++)
        {
            Texture2D atlas = dynamicTextures[i];
            var png = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false, true);
            png.SetPixels32(atlas.GetPixels32()); png.Apply();
            string path = Folder + "/KoreanAtlas-" + i + ".png";
            File.WriteAllBytes(path, png.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            atlases.Add(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        }
        generated.atlasTextures = atlases.ToArray();
        generated.material.mainTexture = atlases[0];
        generated.atlasPopulationMode = AtlasPopulationMode.Static;
        generated.isMultiAtlasTexturesEnabled = false;
        generated.name = "Pretendard Korean SDF";
        SetNoClear(generated);
        TMP_FontAsset primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(PrimaryPath);
        if (primary == null)
        {
            primary = generated;
            AssetDatabase.CreateAsset(primary, PrimaryPath);
            AssetDatabase.AddObjectToAsset(primary.material, primary);
        }
        else
        {
            Material material = primary.material;
            EditorUtility.CopySerialized(generated, primary);
            material.CopyPropertiesFromMaterial(generated.material);
            primary.material = material;
            EditorUtility.SetDirty(material);
        }
        foreach (Texture2D texture in dynamicTextures) UnityEngine.Object.DestroyImmediate(texture);
        if (primary != generated)
        {
            UnityEngine.Object.DestroyImmediate(generated.material);
            UnityEngine.Object.DestroyImmediate(generated);
        }

        TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FallbackPath);
        if (fallback == null)
        {
            fallback = TMP_FontAsset.CreateFontAsset(source, 48, 6, GlyphRenderMode.SDFAA,
                2048, 2048, AtlasPopulationMode.Dynamic, true);
            fallback.name = "Pretendard Korean Dynamic SDF";
            fallback.getFontFeatures = false;
            SetNoClear(fallback);
            AssetDatabase.CreateAsset(fallback, FallbackPath);
            AssetDatabase.AddObjectToAsset(fallback.material, fallback);
            AssetDatabase.AddObjectToAsset(fallback.atlasTextures[0], fallback);
        }
        fallback.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fallback.isMultiAtlasTexturesEnabled = true;
        var fallbackObject = new SerializedObject(fallback);
        fallbackObject.FindProperty("m_SourceFontFile").objectReferenceValue = source;
        fallbackObject.ApplyModifiedPropertiesWithoutUndo();
        fallback.getFontFeatures = false;
        SetNoClear(fallback);
        primary.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
        primary.ReadFontAssetDefinition();
        EditorUtility.SetDirty(primary); EditorUtility.SetDirty(fallback);
        var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset");
        var serialized = new SerializedObject(settings);
        serialized.FindProperty("m_defaultFontAsset").objectReferenceValue = primary;
        var table = serialized.FindProperty("m_fallbackFontAssets");
        bool found = false;
        for (int i = 0; i < table.arraySize; i++) if (table.GetArrayElementAtIndex(i).objectReferenceValue == fallback) found = true;
        if (!found) { table.arraySize++; table.GetArrayElementAtIndex(table.arraySize - 1).objectReferenceValue = fallback; }
        serialized.FindProperty("m_UseModernHangulLineBreakingRules").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        File.WriteAllText(Folder + "/PrebakedCharacters.txt", string.Concat(characters.Select(c => char.ConvertFromUtf32((int)c))));
        AssetDatabase.ImportAsset(Folder + "/PrebakedCharacters.txt");
        AssetDatabase.SaveAssets();

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs", "Assets/GeneratedEnemyHealthBar" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                TMP_Text[] texts = prefab.GetComponentsInChildren<TMP_Text>(true);
                foreach (TMP_Text text in texts) Assign(text, primary);
                if (texts.Length > 0) PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CardGame.unity");
        foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Assign(text, primary);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"KOREAN_FONT_SETUP_OK: {characters.Length} characters, {atlases.Count} static atlas page(s).");
    }

    private static void Assign(TMP_Text text, TMP_FontAsset font)
    {
        text.font = font;
        text.fontSharedMaterial = font.material;
        EditorUtility.SetDirty(text);
    }
    private static void SetNoClear(TMP_FontAsset font)
    {
        var obj = new SerializedObject(font);
        obj.FindProperty("m_ClearDynamicDataOnBuild").boolValue = false;
        obj.ApplyModifiedPropertiesWithoutUndo();
    }
    public static uint[] CollectCharacters()
    {
        var chars = new SortedSet<uint>();
        for (uint c = 32; c <= 126; c++) chars.Add(c);
        foreach (char c in "가나다라마바사아자차카타파하취약약화손상전투보상골드카드맵이벤트공격방어체력선택휴식정예보물상인·→×%…“”‘’")
            chars.Add(c);
        foreach (string root in new[] { "Assets/Script", "Assets/Datas", "Assets/Scenes", "Assets/Prefabs", "Design" })
            foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                if (!new[] { ".cs", ".asset", ".prefab", ".unity", ".md" }.Contains(Path.GetExtension(file))) continue;
                string text = File.ReadAllText(file);
                text = Regex.Replace(text, @"\\u([0-9a-fA-F]{4})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
                foreach (char c in text)
                    if ((c >= 0xAC00 && c <= 0xD7A3) || (c >= 0x3131 && c <= 0x318E)) chars.Add(c);
            }
        return chars.ToArray();
    }

    public static void ConfigureAndRun()
    {
        Configure();
        KoreanFontValidation.Run();
    }
}
