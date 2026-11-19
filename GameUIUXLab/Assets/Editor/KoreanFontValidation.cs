using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class KoreanFontValidation
{
    private static int checks;
    private static void Expect(bool value, string message)
    {
        checks++;
        if (!value) throw new InvalidOperationException("Korean font: " + message);
    }
    public static void Run()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/CardGame.unity");
        try { Verify(); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); return; }
        MapValidation.Run();
    }
    public static void Verify()
    {
        checks = 0;
        TMP_FontAsset primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontSetup.PrimaryPath);
        TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontSetup.FallbackPath);
        Font source = AssetDatabase.LoadAssetAtPath<Font>(KoreanFontSetup.SourcePath);
        Expect(primary != null && fallback != null && source != null, "installed assets exist");
        Expect(primary.atlasPopulationMode == AtlasPopulationMode.Static, "primary survives domain reload and builds");
        Expect(primary.atlasTextures.All(t => t != null && t.width == 2048 && t.height == 2048), "static atlases linked");
        Expect(primary.material.mainTexture == primary.atlasTextures[0], "material uses matching SDF atlas");
        uint[] used = KoreanFontSetup.CollectCharacters();
        Expect(used.All(c => primary.HasCharacter((int)c)), "all current project characters prebaked");
        Expect(primary.fallbackFontAssetTable.Contains(fallback), "dynamic fallback connected");
        Expect(fallback.atlasPopulationMode == AtlasPopulationMode.Dynamic && fallback.isMultiAtlasTexturesEnabled
            && fallback.sourceFontFile == source, "future text has an embedded multi-atlas source");
        Expect(!new SerializedObject(primary).FindProperty("m_ClearDynamicDataOnBuild").boolValue
            && !new SerializedObject(fallback).FindProperty("m_ClearDynamicDataOnBuild").boolValue,
            "build cleanup cannot erase installed data");
        Expect(TMP_Settings.defaultFontAsset == primary && TMP_Settings.fallbackFontAssets.Contains(fallback),
            "global TMP settings connected");
        Expect(source.HasCharacter('가') && source.HasCharacter('힣'), "bundled source font readable");
        int missing = 0;
        for (uint c = 0xAC00; c <= 0xD7A3; c++)
            if (!source.HasCharacter((char)c)) missing++;
        Expect(missing == 0, "source covers all 11172 modern Hangul syllables");

        TMP_FontAsset probe = TMP_FontAsset.CreateFontAsset(source, 48, 6, GlyphRenderMode.SDFAA,
            128, 128, AtlasPopulationMode.Dynamic, true);
        try
        {
            probe.getFontFeatures = false;
            uint[] futureText = Enumerable.Range(0, 100).Select(i => (uint)(0xAC00 + i * 103)).ToArray();
            Expect(probe.TryAddCharacters(futureText, out uint[] failed, false) && (failed == null || failed.Length == 0),
                "previously unbaked Hangul can be added dynamically");
            Expect(probe.atlasTextureCount > 1, "atlas expands instead of dropping characters");
        }
        finally
        {
            foreach (Texture2D texture in probe.atlasTextures) UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(probe.material);
            UnityEngine.Object.DestroyImmediate(probe);
        }

        TMP_Text[] sceneTexts = UnityEngine.Object.FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        Expect(sceneTexts.Length > 0 && sceneTexts.All(t => t.font == primary), "all scene text uses Korean SDF");
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs", "Assets/GeneratedEnemyHealthBar" }))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            Expect(prefab.GetComponentsInChildren<TMP_Text>(true).All(t => t.font == primary),
                "prefab text uses Korean SDF: " + prefab.name);
        }
        Debug.Log($"KOREAN_FONT_VALIDATION_OK: {checks} assertions; {used.Length} prebaked characters; 11172 Hangul source glyphs.");
    }
}
