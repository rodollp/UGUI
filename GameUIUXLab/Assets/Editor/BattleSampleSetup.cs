using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>기존 CardGame 씬에 샘플 전투를 연결합니다. 다시 실행해도 에셋 GUID를 유지합니다.</summary>
public static class BattleSampleSetup
{
    private const string Folder = "Assets/Datas/BattleSamples";

    [MenuItem("Tools/Battle/Configure Sample Battles")]
    public static void Configure()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Datas", "BattleSamples");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CardGame.unity");
        BattleManager battle = Find<BattleManager>();
        RunManager run = Find<RunManager>();
        PlayHUD hud = Find<PlayHUD>();
        UIScreenFlowController flow = Find<UIScreenFlowController>();
        Enemy prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/Normal.prefab")
            .GetComponent<Enemy>();

        // 이전에 직접 배치되어 있던 적은 전투 조합에서 생성하도록 교체합니다.
        Player player = Find<Player>();
        Camera camera = Camera.main;
        Vector3 view = camera.WorldToViewportPoint(player.transform.position);
        Vector3 origin = battle.transform.InverseTransformPoint(
            camera.ViewportToWorldPoint(new Vector3(0.67f, view.y, view.z)));
        Vector3 spacing = battle.transform.InverseTransformVector(camera.transform.right * 1.3f);
        foreach (Enemy enemy in UnityEngine.Object.FindObjectsByType<Enemy>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            UnityEngine.Object.DestroyImmediate(enemy.gameObject);

        EnemyData goblin = EnemyAsset("Goblin", "고블린", 18, EnemyType.Normal,
            Action(EnemyActionType.Attack, 6));
        EnemyData guard = EnemyAsset("Guard", "수호병", 22, EnemyType.Normal,
            Action(EnemyActionType.Block, 7), Action(EnemyActionType.Attack, 8));
        EnemyData boss = EnemyAsset("Boss", "우두머리", 55, EnemyType.Boss,
            Action(EnemyActionType.Strength, 2), Action(EnemyActionType.Attack, 5, 2),
            Action(EnemyActionType.Block, 10), Action(EnemyActionType.Attack, 12));
        EncounterData solo = Encounter("GoblinEncounter", "고블린 정찰병",
            Entry(prefab, goblin, origin));
        EncounterData pair = Encounter("PairEncounter", "고블린과 수호병",
            Entry(prefab, goblin, origin - spacing),
            Entry(prefab, guard, origin + spacing));
        EncounterData final = Encounter("BossEncounter", "우두머리",
            Entry(prefab, boss, origin));
        StageData[] stages = {
            Stage("Floor1", "입구", StageType.Normal, solo),
            Stage("Floor2", "수호병의 방", StageType.Normal, pair),
            Stage("Floor3", "우두머리의 방", StageType.Boss, final)
        };
        SerializedObject runObject = new SerializedObject(run);
        SerializedProperty stageArray = runObject.FindProperty("stages");
        stageArray.arraySize = stages.Length;
        for (int i = 0; i < stages.Length; i++)
            stageArray.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
        runObject.ApplyModifiedPropertiesWithoutUndo();

        Set(battle, "screenFlowController", flow);
        Set(battle, "encounter", solo);
        Transform root = battle.transform.Find("EnemyRoot");
        if (root == null)
        {
            root = new GameObject("EnemyRoot").transform;
            root.SetParent(battle.transform, false);
        }
        root.localPosition = Vector3.zero;
        Set(battle, "enemyRoot", root);
        Set(flow, "battleManager", battle);
        Set(hud, "battleManager", battle);
        Set(hud, "runManager", run);

        SerializedObject hudObject = new SerializedObject(hud);
        TextMeshProUGUI template = (TextMeshProUGUI)hudObject.FindProperty("turnText").objectReferenceValue;
        template.rectTransform.anchorMin = template.rectTransform.anchorMax = new Vector2(0, 1);
        template.rectTransform.pivot = new Vector2(0, 1);
        template.rectTransform.anchoredPosition = new Vector2(50, -45);
        template.rectTransform.sizeDelta = new Vector2(550, 60);
        template.fontSize = 36;
        template.enableAutoSizing = false;
        Transform play = ((GameObject)new SerializedObject(flow).FindProperty("playScreen")
            .objectReferenceValue).transform;
        Transform result = ((GameObject)new SerializedObject(flow).FindProperty("resultScreen")
            .objectReferenceValue).transform;
        Set(hud, "energyText", Label(template, play, "EnergyText", new Vector2(0, 1),
            new Vector2(50, -110), new Vector2(400, 60), 32));
        Set(hud, "stageText", Label(template, play, "StageText", new Vector2(0.5f, 1),
            new Vector2(-300, -110), new Vector2(600, 60), 32));
        Set(hud, "intentText", Label(template, play, "IntentText", new Vector2(1, 1),
            new Vector2(-650, -190), new Vector2(620, 200), 27));
        Set(hud, "resultText", Label(template, result, "BattleResultText", new Vector2(0.5f, 0.5f),
            new Vector2(-350, 180), new Vector2(700, 80), 42));
        foreach (TMP_Text text in result.GetComponentsInChildren<TMP_Text>(true))
        {
            // 결과 화면은 편집 시 비활성 상태이므로 비활성 부모도 검색해야 합니다.
            bool isButtonLabel = text.GetComponentInParent<Button>(true) != null;
            text.gameObject.SetActive(text.name == "BattleResultText" || isButtonLabel);
        }

        Button endTurn = play.GetComponentsInChildren<Button>(true).First(b =>
            Enumerable.Range(0, b.onClick.GetPersistentEventCount())
                .Any(i => b.onClick.GetPersistentMethodName(i) == "EndTurn"));
        Set(hud, "endTurnButton", endTurn);
        Transform existing = result.Find("NextBattleButton");
        Button next = existing != null ? existing.GetComponent<Button>() :
            UnityEngine.Object.Instantiate(endTurn, result);
        next.name = "NextBattleButton";
        next.onClick = new Button.ButtonClickedEvent();
        UnityEventTools.AddPersistentListener(next.onClick, battle.StartNextBattle);
        RectTransform rect = next.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, -100);
        next.GetComponentInChildren<TMP_Text>(true).text = "다음 층";
        next.gameObject.SetActive(false);
        Set(hud, "nextBattleButton", next);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("BATTLE_SAMPLE_SETUP_OK");
    }

    private static T Find<T>() where T : UnityEngine.Object =>
        UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();

    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static TextMeshProUGUI Label(TextMeshProUGUI template, Transform parent, string name,
        Vector2 anchor, Vector2 position, Vector2 size, float fontSize)
    {
        Transform existing = parent.Find(name);
        TextMeshProUGUI text = existing != null ? existing.GetComponent<TextMeshProUGUI>() :
            UnityEngine.Object.Instantiate(template, parent);
        text.name = name;
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = anchor;
        text.rectTransform.pivot = new Vector2(0, 1);
        text.rectTransform.anchoredPosition = position;
        text.rectTransform.sizeDelta = size;
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;
        text.text = "";
        return text;
    }

    private static T Asset<T>(string name) where T : ScriptableObject
    {
        string path = Folder + "/" + name + ".asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
        EditorUtility.SetDirty(asset);
        return asset;
    }
    private static EnemyAction Action(EnemyActionType type, int amount, int hits = 1) =>
        new EnemyAction { Type = type, Amount = amount, Hits = hits };
    private static EnemyData EnemyAsset(string name, string title, int hp, EnemyType type, params EnemyAction[] actions)
    {
        // 샘플 재연결은 사용자가 수정한 체력과 패턴을 덮어쓰지 않습니다.
        EnemyData existing = AssetDatabase.LoadAssetAtPath<EnemyData>(Folder + "/" + name + ".asset");
        if (existing != null) return existing;
        EnemyData data = Asset<EnemyData>(name);
        data.EnemyName = title; data.MaxHp = hp; data.EnemyType = type; data.Actions = actions;
        return data;
    }
    private static EnemySpawnEntry Entry(Enemy prefab, EnemyData data, Vector3 position) =>
        new EnemySpawnEntry { Prefab = prefab, Data = data, LocalPosition = position };
    private static EncounterData Encounter(string name, string title, params EnemySpawnEntry[] entries)
    {
        EncounterData data = Asset<EncounterData>(name);
        data.EncounterName = title; data.Enemies = entries; return data;
    }
    private static StageData Stage(string name, string title, StageType type, EncounterData encounter)
    {
        StageData data = Asset<StageData>(name);
        data.StageName = title; data.Type = type; data.Encounters = new[] { encounter }; return data;
    }
}
