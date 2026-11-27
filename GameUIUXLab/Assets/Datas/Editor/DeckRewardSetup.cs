using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class DeckRewardSetup
{
    [MenuItem("Tools/Cards/Configure Deck Viewer and Reward Skip")]
    public static void Configure()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CardGame.unity");
        var flow = Find<UIScreenFlowController>();
        var run = Find<RunManager>();
        var hud = Find<PlayHUD>();
        var rewards = Find<BattleRewardUI>();
        var hudData = new SerializedObject(hud);
        var template = (TMP_Text)hudData.FindProperty("turnText").objectReferenceValue;
        var next = (Button)hudData.FindProperty("nextBattleButton").objectReferenceValue;
        // Runtime listener in BattleRewardUI owns this button, including skipping.
        next.onClick = new Button.ButtonClickedEvent();
        next.GetComponentInChildren<TMP_Text>(true).text = "다음으로";
        var rect = (RectTransform)next.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(0, -300);
        Set(rewards, "continueButton", next);
        var deck = flow.GetComponent<DeckViewUI>();
        if (deck == null) deck = flow.gameObject.AddComponent<DeckViewUI>();
        Set(deck, "flow", flow); Set(deck, "run", run); Set(deck, "textTemplate", template);
        Set(deck, "canvasRoot", template.GetComponentInParent<Canvas>().rootCanvas.transform);
        Set(deck, "cardPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/CardUI.prefab").GetComponent<CardUI>());
        EditorUtility.SetDirty(next);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("DECK_REWARD_SETUP_OK");
    }
    public static void ConfigureAndValidate()
    {
        Configure();
        RewardValidation.VerifyModels();
        var card = ScriptableObject.CreateInstance<CardData>();
        var offer = new BattleRewardOffer(10, new[] { card,
            ScriptableObject.CreateInstance<CardData>(), ScriptableObject.CreateInstance<CardData>() });
        if (!offer.TrySkip() || !offer.IsSkipped || offer.TrySkip() || offer.TryClaim(0, out _))
            throw new System.InvalidOperationException("Skipped reward must be resolved exactly once.");
        foreach (var choice in offer.Choices) Object.DestroyImmediate(choice);
        Debug.Log("DECK_REWARD_VALIDATION_OK");
    }
    private static T Find<T>() where T : Object => Object.FindObjectsByType<T>(
        FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
    private static void Set(Object target, string field, Object value)
    {
        var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
