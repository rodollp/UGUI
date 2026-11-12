using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MapSetup
{
    private const string Folder = "Assets/Datas/Adventure";
    [MenuItem("Tools/Map/Configure Adventure")]
    public static void Configure()
    {
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Datas", "Adventure");
        BattleRewardData regularRewards = AssetDatabase.LoadAssetAtPath<BattleRewardData>(
            "Assets/Datas/Rewards/DefaultBattleRewards.asset");
        CardData bash = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Datas/Rewards/Bash.asset");
        MapEventChoice leave = new MapEventChoice { Label = "지나간다", Result = "아무 일 없이 길을 떠납니다." };
        MapEventData shrine = Event("Shrine", "수상한 제단",
            "낡은 제단 위에 금빛 동전이 놓여 있습니다. 작은 대가를 치르면 가져갈 수 있을 것 같습니다.",
            new MapEventChoice { Label = "피를 바치고 동전을 얻는다", HealthCost = 6, GoldGain = 35,
                Result = "제단은 당신의 피를 받아들이고 금화를 내어 줍니다." },
            new MapEventChoice { Label = "조용히 기도한다", HealPercent = 10, Result = "잠시 마음을 가다듬고 기력을 회복합니다." },
            leave);
        MapEventData merchant = Event("Merchant", "떠돌이 상인",
            "어둠 속에서 상인이 나타납니다. 강력한 카드 한 장을 금화와 교환하겠다고 합니다.",
            new MapEventChoice { Label = "강타 카드를 구매한다", GoldCost = 25, Card = bash,
                Result = "상인은 카드를 건네고 안개 속으로 사라집니다." }, leave);
        MapEventData cache = Event("Cache", "버려진 가방",
            "누군가 두고 간 가방을 발견했습니다. 한 가지 물건만 가져가기로 합니다.",
            new MapEventChoice { Label = "금화 주머니를 챙긴다", GoldGain = 18, Result = "가방 안에서 금화를 찾아냈습니다." },
            new MapEventChoice { Label = "치료 약품을 사용한다", HealAmount = 12, Result = "남아 있던 약품으로 상처를 치료합니다." }, leave);
        MapEventData rest = Event("Rest", "조용한 야영지",
            "모닥불의 온기가 남아 있습니다. 다음 싸움을 앞두고 쉬어 갈 수 있습니다.",
            new MapEventChoice { Label = "휴식한다 · 최대 체력의 30% 회복", HealPercent = 30,
                Result = "모닥불 곁에서 충분히 쉬었습니다." }, leave);
        MapEventData treasure = Event("Treasure", "잠겨 있지 않은 상자",
            "먼지 쌓인 상자를 발견했습니다. 안쪽에서 금속이 부딪히는 소리가 들립니다.",
            new MapEventChoice { Label = "상자를 연다 · 골드 25 획득", GoldGain = 25,
                Result = "상자 속 금화를 챙겼습니다." }, leave);
        EnemyData elite = Asset<EnemyData>("EliteGuard", e => {
            e.EnemyName = "정예 수호병"; e.EnemyType = EnemyType.Elite; e.MaxHp = 40;
            e.Actions = new[] {
                new EnemyAction { Type = EnemyActionType.Strength, Amount = 1 },
                new EnemyAction { Type = EnemyActionType.Attack, Amount = 6, Hits = 2 },
                new EnemyAction { Type = EnemyActionType.Block, Amount = 8 }
            };
        });
        EncounterData boss = AssetDatabase.LoadAssetAtPath<EncounterData>("Assets/Datas/BattleSamples/BossEncounter.asset");
        BattleRewardData eliteRewards = Asset<BattleRewardData>("EliteRewards", r => {
            r.MinGold = 25; r.MaxGold = 35; r.CardPool.AddRange(regularRewards.CardPool);
        });
        BattleRewardData bossRewards = Asset<BattleRewardData>("BossRewards", r => {
            r.MinGold = 35; r.MaxGold = 50; r.CardPool.AddRange(regularRewards.CardPool);
        });
        EncounterData eliteEncounter = Asset<EncounterData>("EliteEncounter", e => {
            e.EncounterName = "정예 수호병"; e.Rewards = eliteRewards;
            e.Enemies = new[] { new EnemySpawnEntry { Prefab = boss.Enemies[0].Prefab,
                Data = elite, LocalPosition = boss.Enemies[0].LocalPosition } };
        });
        EncounterData mapBoss = Asset<EncounterData>("MapBossEncounter", e => {
            e.EncounterName = boss.EncounterName; e.Enemies = boss.Enemies; e.Rewards = bossRewards;
        });
        Asset<RunMapData>("DefaultMap", m => {
            m.Battles = new[] {
                AssetDatabase.LoadAssetAtPath<EncounterData>("Assets/Datas/BattleSamples/GoblinEncounter.asset"),
                AssetDatabase.LoadAssetAtPath<EncounterData>("Assets/Datas/BattleSamples/PairEncounter.asset")
            };
            m.Elites = new[] { eliteEncounter }; m.Bosses = new[] { mapBoss };
            m.Events = new[] { shrine, merchant, cache }; m.Rest = rest; m.Treasure = treasure;
        });
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CardGame.unity");
        RunManager run = Find<RunManager>();
        BattleManager battle = Find<BattleManager>();
        UIScreenFlowController flow = Find<UIScreenFlowController>();
        PlayHUD hud = Find<PlayHUD>();
        AdventureController adventure = run.GetComponent<AdventureController>();
        if (adventure == null) adventure = run.gameObject.AddComponent<AdventureController>();
        Set(adventure, "runManager", run); Set(adventure, "battleManager", battle); Set(adventure, "screenFlow", flow);
        Set(battle, "adventure", adventure);
        var runObject = new SerializedObject(run);
        if (runObject.FindProperty("mapData").objectReferenceValue == null)
        {
            runObject.FindProperty("mapData").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<RunMapData>(Folder + "/DefaultMap.asset");
            runObject.ApplyModifiedPropertiesWithoutUndo();
        }
        Set(flow, "worldPlayer", Find<Player>().gameObject);
        Transform enemyRoot = (Transform)new SerializedObject(battle).FindProperty("enemyRoot").objectReferenceValue;
        Set(flow, "worldEnemies", enemyRoot.gameObject);
        TMP_Text template = (TMP_Text)new SerializedObject(hud).FindProperty("turnText").objectReferenceValue;
        Button buttonTemplate = (Button)new SerializedObject(hud).FindProperty("endTurnButton").objectReferenceValue;
        Transform parent = hud.transform;
        GameObject map = Screen(parent, "Screen_Map", new Color(0.055f, 0.075f, 0.10f));
        GameObject events = Screen(parent, "Screen_Event", new Color(0.075f, 0.065f, 0.055f));
        Set(flow, "mapScreen", map); Set(flow, "eventScreen", events);

        RunMapUI mapUI = map.GetComponent<RunMapUI>();
        if (mapUI == null) mapUI = map.AddComponent<RunMapUI>();
        Set(mapUI, "adventure", adventure);
        Set(mapUI, "titleText", Label(map.transform, template, "Title", new Vector2(0, 440), new Vector2(1000, 70), 40));
        Set(mapUI, "statusText", Label(map.transform, template, "Status", new Vector2(0, 365), new Vector2(1200, 50), 26));
        GameObject graph = Rect(map.transform, "Graph", new Vector2(0, -10), new Vector2(1200, 650));
        Set(mapUI, "graphRoot", graph.GetComponent<RectTransform>());
        Button nodeTemplate = Button(map.transform, buttonTemplate, "NodeTemplate", Vector2.zero, new Vector2(120, 52), "전투", 22);
        nodeTemplate.gameObject.SetActive(false);
        Set(mapUI, "nodeTemplate", nodeTemplate);
        Label(map.transform, template, "Hint", new Vector2(0, -400), new Vector2(1200, 50), 23).text =
            "밝게 표시된 노드를 클릭해 이동하세요 · 연결된 다음 층만 선택할 수 있습니다";
        Button mapTitle = Button(map.transform, buttonTemplate, "ReturnTitle", new Vector2(0, -475),
            new Vector2(260, 60), "타이틀로", 24);
        UnityEventTools.AddPersistentListener(mapTitle.onClick, flow.ShowTitleScreen);

        MapEventUI eventUI = events.GetComponent<MapEventUI>();
        if (eventUI == null) eventUI = events.AddComponent<MapEventUI>();
        Set(eventUI, "adventure", adventure);
        Set(eventUI, "titleText", Label(events.transform, template, "Title", new Vector2(0, 350), new Vector2(1100, 80), 42));
        Set(eventUI, "statusText", Label(events.transform, template, "Status", new Vector2(0, 260), new Vector2(1200, 55), 25));
        Set(eventUI, "descriptionText", Label(events.transform, template, "Description",
            new Vector2(0, 80), new Vector2(1150, 250), 28));
        var eventObject = new SerializedObject(eventUI);
        var choices = eventObject.FindProperty("choiceButtons"); choices.arraySize = 3;
        for (int i = 0; i < 3; i++)
            choices.GetArrayElementAtIndex(i).objectReferenceValue = Button(events.transform, buttonTemplate,
                "Choice" + i, new Vector2(0, -140 - i * 100), new Vector2(1050, 78), "", 26);
        eventObject.ApplyModifiedPropertiesWithoutUndo();
        Set(eventUI, "continueButton", Button(events.transform, buttonTemplate, "Continue",
            new Vector2(0, -330), new Vector2(320, 78), "맵으로", 26));

        foreach (Button button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            bool start = Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                .Any(i => button.onClick.GetPersistentMethodName(i) == "StartNewRun");
            if (!start) continue;
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, adventure.BeginRun);
        }
        map.SetActive(false); events.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("MAP_SETUP_OK");
    }

    public static void ConfigureAndRun() { Configure(); MapValidation.Run(); }

    private static MapEventData Event(string name, string title, string description, params MapEventChoice[] choices) =>
        Asset<MapEventData>(name, e => { e.Title = title; e.Description = description; e.Choices = choices; });
    private static T Asset<T>(string name, Action<T> configure) where T : ScriptableObject
    {
        string path = Folder + "/" + name + ".asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>(); configure(asset); AssetDatabase.CreateAsset(asset, path); return asset;
    }
    private static T Find<T>() where T : UnityEngine.Object =>
        UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo();
    }
    private static GameObject Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        Transform existing = parent.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        return go;
    }
    private static GameObject Screen(Transform parent, string name, Color color)
    {
        GameObject go = Rect(parent, name, Vector2.zero, Vector2.zero);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = go.GetComponent<Image>(); if (image == null) image = go.AddComponent<Image>();
        image.color = color;
        return go;
    }
    private static TMP_Text Label(Transform parent, TMP_Text template, string name, Vector2 position, Vector2 size, float fontSize)
    {
        Transform existing = parent.Find(name);
        TMP_Text text = existing != null ? existing.GetComponent<TMP_Text>() : UnityEngine.Object.Instantiate(template, parent);
        text.name = name;
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = text.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchoredPosition = position; text.rectTransform.sizeDelta = size;
        text.fontSize = fontSize; text.enableAutoSizing = false; text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false; text.color = new Color(0.94f, 0.88f, 0.72f); text.text = "";
        return text;
    }
    private static Button Button(Transform parent, Button template, string name, Vector2 position, Vector2 size, string label, float fontSize)
    {
        Transform existing = parent.Find(name);
        Button button = existing != null ? existing.GetComponent<Button>() : UnityEngine.Object.Instantiate(template, parent);
        button.name = name; button.onClick = new Button.ButtonClickedEvent();
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
        button.GetComponent<Image>().color = new Color(0.7f, 0.53f, 0.29f);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        text.text = label; text.fontSize = fontSize; text.enableAutoSizing = false;
        text.color = new Color(0.10f, 0.075f, 0.04f);
        return button;
    }
}
