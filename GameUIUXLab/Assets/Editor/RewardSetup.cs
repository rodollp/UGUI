using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RewardSetup
{
    private const string Folder = "Assets/Datas/Rewards";
    [MenuItem("Tools/Battle/Configure Victory Rewards")]
    public static void Configure()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Datas", "Rewards");
        DamageEffect damage = Asset<DamageEffect>("BashDamage", e => Int(e, "damage", 8));
        BlockEffect block = Asset<BlockEffect>("IronBlock", e => Int(e, "block", 8));
        ApplyDebuffEffect vulnerable = Asset<ApplyDebuffEffect>("BashVulnerable", e => {
            Int(e, "debuff", (int)DebuffType.Vulnerable); Int(e, "turns", 2);
            Int(e, "target", (int)DebuffTarget.Enemy);
        });
        ApplyDebuffEffect weak = Asset<ApplyDebuffEffect>("IntimidateWeak", e => {
            Int(e, "debuff", (int)DebuffType.Weak); Int(e, "turns", 2);
            Int(e, "target", (int)DebuffTarget.Enemy);
        });
        Sprite strike = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Cards/strike-v1.png");
        Sprite defend = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/Cards/defend-v1.png");
        CardData bash = Card("Bash", 100, "강타", CardType.Attack, CardTargetType.Enemy, 2, strike, damage, vulnerable);
        CardData intimidate = Card("Intimidate", 101, "위협", CardType.Skill, CardTargetType.Enemy, 1, defend, weak);
        CardData iron = Card("IronGuard", 102, "철벽", CardType.Skill, CardTargetType.None, 1, defend, block);
        BattleRewardData rewards = Asset<BattleRewardData>("DefaultBattleRewards", r => {
            r.MinGold = 10; r.MaxGold = 20; r.CardPool.AddRange(new[] { bash, intimidate, iron });
        });

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CardGame.unity");
        rewards = AssetDatabase.LoadAssetAtPath<BattleRewardData>(Folder + "/DefaultBattleRewards.asset");
        BattleManager battle = Find<BattleManager>();
        RunManager run = Find<RunManager>();
        PlayHUD hud = Find<PlayHUD>();
        UIScreenFlowController flow = Find<UIScreenFlowController>();
        SerializedObject battleObject = new SerializedObject(battle);
        if (battleObject.FindProperty("defaultRewards").objectReferenceValue == null)
        {
            battleObject.FindProperty("defaultRewards").objectReferenceValue = rewards;
            battleObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(battle);
        }
        GameObject result = (GameObject)new SerializedObject(flow).FindProperty("resultScreen").objectReferenceValue;
        Transform play = ((GameObject)new SerializedObject(flow).FindProperty("playScreen").objectReferenceValue).transform;
        TMP_Text template = (TMP_Text)new SerializedObject(hud).FindProperty("turnText").objectReferenceValue;
        Set(hud, "goldText", Label(play, template, "GoldText", new Vector2(0, 1), new Vector2(50, -170),
            new Vector2(400, 55), TextAlignmentOptions.TopLeft, 30));
        var rewardUI = result.GetComponent<BattleRewardUI>();
        if (rewardUI == null) rewardUI = result.AddComponent<BattleRewardUI>();
        Set(rewardUI, "battleManager", battle);
        Set(rewardUI, "runManager", run);
        Set(rewardUI, "goldText", Label(result.transform, template, "RewardGoldText",
            new Vector2(0.5f, 0.5f), new Vector2(0, 335), new Vector2(1000, 55), TextAlignmentOptions.Center, 28));
        Set(rewardUI, "promptText", Label(result.transform, template, "RewardPromptText",
            new Vector2(0.5f, 0.5f), new Vector2(0, 275), new Vector2(1000, 55), TextAlignmentOptions.Center, 26));
        Transform choices = result.transform.Find("RewardChoices");
        if (choices == null)
        {
            choices = new GameObject("RewardChoices", typeof(RectTransform)).transform;
            choices.SetParent(result.transform, false);
        }
        RectTransform choicesRect = choices.GetComponent<RectTransform>();
        choicesRect.anchorMin = choicesRect.anchorMax = choicesRect.pivot = new Vector2(0.5f, 0.5f);
        choicesRect.anchoredPosition = new Vector2(0, 30);
        choicesRect.sizeDelta = new Vector2(1000, 400);
        Set(rewardUI, "choicesRoot", choices.gameObject);
        var rewardObject = new SerializedObject(rewardUI);
        var cards = rewardObject.FindProperty("rewardCards");
        cards.arraySize = BattleRewardOffer.ChoiceCount;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/CardUI.prefab");
        for (int i = 0; i < BattleRewardOffer.ChoiceCount; i++)
        {
            Transform child = choices.Find("RewardCard" + i);
            if (child == null)
            {
                GameObject card = (GameObject)PrefabUtility.InstantiatePrefab(prefab, choices);
                card.name = "RewardCard" + i; child = card.transform;
            }
            RectTransform rect = child.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2((i - 1) * 310, 0);
            rect.localScale = Vector3.one * 1.15f;
            cards.GetArrayElementAtIndex(i).objectReferenceValue = child.GetComponent<CardUI>();
        }
        rewardObject.ApplyModifiedPropertiesWithoutUndo();
        TMP_Text heading = (TMP_Text)new SerializedObject(hud).FindProperty("resultText").objectReferenceValue;
        Center(heading.rectTransform, new Vector2(0, 415));
        heading.rectTransform.sizeDelta = new Vector2(1100, 70);
        heading.alignment = TextAlignmentOptions.Center;
        var next = (Button)new SerializedObject(hud).FindProperty("nextBattleButton").objectReferenceValue;
        Center(next.GetComponent<RectTransform>(), new Vector2(0, -300));
        foreach (Button button in result.GetComponentsInChildren<Button>(true))
            if (Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                .Any(i => button.onClick.GetPersistentMethodName(i) == "ShowTitleScreen"))
                Center(button.GetComponent<RectTransform>(), new Vector2(0, -415));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("REWARD_SETUP_OK");
    }

    public static void ConfigureAndRun()
    {
        Configure();
        RewardValidation.VerifyModels();
        BattleValidation.Run();
    }

    private static T Find<T>() where T : UnityEngine.Object =>
        UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
    private static T Asset<T>(string name, Action<T> configure) where T : ScriptableObject
    {
        string path = Folder + "/" + name + ".asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        configure(asset);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
    private static CardData Card(string file, uint id, string name, CardType type, CardTargetType target,
        int cost, Sprite art, params CardEffect[] effects) => Asset<CardData>(file, c => {
            c.CardID = id; c.CardName = name; c.Type = type; c.TargetType = target;
            c.EnergyCost = cost; c.Artwork = art; c.Effects = effects.ToList();
        });
    private static void Int(UnityEngine.Object target, string name, int value)
    {
        var obj = new SerializedObject(target);
        obj.FindProperty(name).intValue = value;
        obj.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var obj = new SerializedObject(target);
        obj.FindProperty(field).objectReferenceValue = value;
        obj.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Center(RectTransform rect, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
    }
    private static TMP_Text Label(Transform parent, TMP_Text template, string name, Vector2 anchor,
        Vector2 position, Vector2 size, TextAlignmentOptions alignment, float fontSize)
    {
        Transform existing = parent.Find(name);
        TMP_Text text = existing != null ? existing.GetComponent<TMP_Text>() :
            UnityEngine.Object.Instantiate(template, parent);
        text.name = name;
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = anchor;
        text.rectTransform.pivot = anchor.x == 0 ? new Vector2(0, 1) : new Vector2(0.5f, 0.5f);
        text.rectTransform.anchoredPosition = position;
        text.rectTransform.sizeDelta = size;
        text.fontSize = fontSize; text.alignment = alignment;
        text.enableAutoSizing = false; text.raycastTarget = false; text.text = "";
        return text;
    }
}
