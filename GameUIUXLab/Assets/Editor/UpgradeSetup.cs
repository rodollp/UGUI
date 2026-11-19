using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class UpgradeSetup
{
    private const string Folder = "Assets/Datas/Upgrades";
    [MenuItem("Tools/Cards/Configure Upgrades")]
    public static void Configure()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Datas", "Upgrades");
        Upgrade("Assets/Datas/CardDis/ATK.asset", "StrikePlus", 9, 0);
        Upgrade("Assets/Datas/CardDis/BLO.asset", "DefendPlus", 8, 0);
        Upgrade("Assets/Datas/Rewards/Bash.asset", "BashPlus", 12, 3);
        Upgrade("Assets/Datas/Rewards/Intimidate.asset", "IntimidatePlus", 0, 3);
        Upgrade("Assets/Datas/Rewards/IronGuard.asset", "IronGuardPlus", 11, 0);
        var rest = AssetDatabase.LoadAssetAtPath<MapEventData>("Assets/Datas/Adventure/Rest.asset");
        if (!rest.Choices.Any(c => c.UpgradeCard))
        {
            var choices = rest.Choices.ToList();
            choices.Insert(Math.Min(1, choices.Count), UpgradeChoice());
            rest.Choices = choices.ToArray(); EditorUtility.SetDirty(rest);
        }
        string eventPath = "Assets/Datas/Adventure/Smith.asset";
        var smith = AssetDatabase.LoadAssetAtPath<MapEventData>(eventPath);
        if (smith == null)
        {
            smith = ScriptableObject.CreateInstance<MapEventData>();
            smith.Title = "길가의 대장장이";
            smith.Description = "대장장이가 장비를 손봐 주겠다고 합니다. 덱에서 카드 한 장을 강화할 수 있습니다.";
            smith.Choices = new[] { UpgradeChoice(), new MapEventChoice {
                Label = "지나간다", Result = "대장장이에게 인사하고 길을 떠납니다." } };
            AssetDatabase.CreateAsset(smith, eventPath);
        }
        var map = AssetDatabase.LoadAssetAtPath<RunMapData>("Assets/Datas/Adventure/DefaultMap.asset");
        if (!map.Events.Contains(smith))
        {
            map.Events = map.Events.Concat(new[] { smith }).ToArray(); EditorUtility.SetDirty(map);
        }
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CardGame.unity");
        var ui = UnityEngine.Object.FindObjectsByType<MapEventUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        var adventure = UnityEngine.Object.FindObjectsByType<AdventureController>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        var data = new SerializedObject(ui);
        TMP_Text text = (TMP_Text)data.FindProperty("titleText").objectReferenceValue;
        Button template = (Button)data.FindProperty("continueButton").objectReferenceValue;
        Transform existing = ui.transform.Find("UpgradePanel");
        GameObject panel = existing != null ? existing.gameObject : new GameObject("UpgradePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(ui.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.055f, 0.065f, 0.08f, 1);
        var upgradeUI = panel.GetComponent<CardUpgradeUI>();
        if (upgradeUI == null) upgradeUI = panel.AddComponent<CardUpgradeUI>();
        Set(upgradeUI, "adventure", adventure);
        Set(upgradeUI, "prompt", Label(panel.transform, text, "Prompt", new Vector2(0, 350), new Vector2(1450, 230), 26));
        var serialized = new SerializedObject(upgradeUI);
        var cards = serialized.FindProperty("cards"); cards.arraySize = 3;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/CardUI.prefab");
        for (int i = 0; i < 3; i++)
        {
            Transform child = panel.transform.Find("UpgradeCard" + i);
            if (child == null)
            {
                child = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, panel.transform)).transform;
                child.name = "UpgradeCard" + i;
            }
            Center(child.GetComponent<RectTransform>(), new Vector2((i - 1) * 340, -10), new Vector2(240, 380));
            child.localScale = Vector3.one * 1.1f;
            cards.GetArrayElementAtIndex(i).objectReferenceValue = child.GetComponent<CardUI>();
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Set(upgradeUI, "previous", Button(panel.transform, template, "Previous", new Vector2(-430, -300), "이전"));
        Set(upgradeUI, "next", Button(panel.transform, template, "Next", new Vector2(430, -300), "다음"));
        Set(upgradeUI, "confirm", Button(panel.transform, template, "Confirm", new Vector2(0, -300), "강화 확정"));
        Set(upgradeUI, "cancel", Button(panel.transform, template, "Cancel", new Vector2(0, -410), "돌아가기"));
        Set(ui, "upgradePanel", panel);
        panel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log("UPGRADE_SETUP_OK");
    }
    public static void ConfigureAndRun() { Configure(); KoreanFontSetup.ConfigureAndRun(); }
    private static MapEventChoice UpgradeChoice() => new MapEventChoice {
        Label = "카드 한 장을 강화한다", UpgradeCard = true, Result = "카드를 강화했습니다." };
    private static void Upgrade(string path, string name, int amount, int turns)
    {
        CardData source = AssetDatabase.LoadAssetAtPath<CardData>(path);
        if (source.UpgradedCard != null) return;
        CardData target = UnityEngine.Object.Instantiate(source);
        target.name = name; target.CardName = source.CardName + "+"; target.CardID = source.CardID + 1000;
        target.UpgradedCard = null;
        target.Effects = source.Effects.Select((effect, index) => {
            CardEffect copy = UnityEngine.Object.Instantiate(effect);
            copy.name = name + "Effect" + index;
            var serialized = new SerializedObject(copy);
            if (effect is DamageEffect) serialized.FindProperty("damage").intValue = amount;
            if (effect is BlockEffect) serialized.FindProperty("block").intValue = amount;
            if (effect is ApplyDebuffEffect) serialized.FindProperty("turns").intValue = turns;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(copy, Folder + "/" + copy.name + ".asset");
            return copy;
        }).ToList();
        AssetDatabase.CreateAsset(target, Folder + "/" + name + ".asset");
        source.UpgradedCard = target; EditorUtility.SetDirty(source);
    }
    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target); serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Center(RectTransform rect, Vector2 pos, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos; rect.sizeDelta = size;
    }
    private static TMP_Text Label(Transform parent, TMP_Text template, string name, Vector2 pos, Vector2 size, int fontSize)
    {
        Transform old = parent.Find(name);
        TMP_Text label = old != null ? old.GetComponent<TMP_Text>() : UnityEngine.Object.Instantiate(template, parent);
        label.name = name; label.text = ""; label.fontSize = fontSize; label.enableAutoSizing = false;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        Center(label.rectTransform, pos, size); return label;
    }
    private static Button Button(Transform parent, Button template, string name, Vector2 pos, string caption)
    {
        Transform old = parent.Find(name);
        Button button = old != null ? old.GetComponent<Button>() : UnityEngine.Object.Instantiate(template, parent);
        button.name = name; button.onClick = new Button.ButtonClickedEvent(); button.gameObject.SetActive(true);
        Center(button.GetComponent<RectTransform>(), pos, new Vector2(270, 70));
        button.GetComponentInChildren<TMP_Text>(true).text = caption; return button;
    }
}
