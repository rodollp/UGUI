using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MapValidation
{
    private const string Active = "MapValidation.Active";
    private const string Legacy = "MapValidation.Legacy";
    private static int checks;
    private static IEnumerator routine;
    private static double deadline;
    static MapValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Active, false))
            {
                checks = 0; deadline = EditorApplication.timeSinceStartup + 60;
                routine = VerifyScene(); EditorApplication.update += Tick;
            }
            else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Legacy, false))
            {
                SessionState.SetBool(Legacy, false);
                EditorApplication.delayCall += BattleValidation.Run;
            }
        };
    }

    public static void Run()
    {
        try { VerifyModels(); RewardValidation.VerifyModels(); UpgradeValidation.VerifyModels(); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); return; }
        EditorSceneManager.OpenScene("Assets/Scenes/CardGame.unity");
        SessionState.SetBool(Active, true); EditorApplication.isPlaying = true;
    }
    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Map validation timed out");
            if (routine.MoveNext()) return;
            Debug.Log($"MAP_SCENE_VALIDATION_OK: {checks} assertions passed.");
            EditorApplication.update -= Tick;
            SessionState.SetBool(Active, false); SessionState.SetBool(Legacy, true);
            EditorApplication.isPlaying = false;
        }
        catch (Exception error)
        {
            Debug.LogException(error); SessionState.SetBool(Active, false);
            EditorApplication.update -= Tick; EditorApplication.Exit(1);
        }
    }
    private static void Expect(bool value, string message)
    {
        checks++; if (!value) throw new InvalidOperationException("Map: " + message);
    }
    private static T Find<T>() where T : UnityEngine.Object =>
        UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
    private static void VerifyModels()
    {
        checks = 0;
        RunMapData data = AssetDatabase.LoadAssetAtPath<RunMapData>("Assets/Datas/Adventure/DefaultMap.asset");
        Expect(data != null && data.IsValid(), "default map configuration valid");
        bool sawElite = false;
        bool sawFourthFloorRest = false;
        for (int seed = 0; seed < 16; seed++)
        {
            RunMap map = RunMapGenerator.Generate(data, seed);
            Expect(map.Rows == 12, "every route has twelve floors including the boss");
            Expect(map.Nodes.Where(n => n.Row == 5).Count() == 3
                && map.Nodes.All(n => (n.Row == 5) == (n.Type == MapNodeType.Treasure)),
                "treasure appears on every sixth-floor node and nowhere else");
            foreach (MapNode start in map.Nodes.Where(n => n.Row == 0))
                VerifyRoutes(map, start, 1, 0, data.TreasureFloor);
            Expect(map.Nodes.Count == (data.Rows - 1) * 3 + 1, "node count");
            Expect(map.Nodes.All(n => n.Type != MapNodeType.Elite || n.Row + 1 >= 5), "no elites before floor five");
            Expect(map.Nodes.All(n => n.Type != MapNodeType.Rest || n.Row + 1 >= 4), "no rest before floor four");
            Expect(map.Nodes.Where(n => n.Row == map.Rows - 2).All(n => n.Type == MapNodeType.Rest), "rest before boss");
            sawElite |= map.Nodes.Any(n => n.Type == MapNodeType.Elite);
            sawFourthFloorRest |= map.Nodes.Any(n => n.Row == 3 && n.Type == MapNodeType.Rest);
            Expect(map.Nodes.All(n => n.Outgoing.All(id => map.Find(id).Row == n.Row + 1)),
                "edges advance one floor without cycles");
            var reachable = map.Nodes.Where(n => n.Row == 0).Select(n => n.Id).ToList();
            for (int row = 0; row < data.Rows - 1; row++)
                foreach (MapNode node in map.Nodes.Where(n => n.Row == row && reachable.Contains(n.Id)))
                    foreach (int id in node.Outgoing) if (!reachable.Contains(id)) reachable.Add(id);
            Expect(reachable.Count == map.Nodes.Count, "every node reachable");
            Expect(!map.TryEnter(-1) && !map.TryEnter(map.Nodes.Last().Id), "cannot skip floors");
            while (!map.IsComplete)
            {
                MapNode node = map.Nodes.First(n => map.CanEnter(n.Id));
                Expect(map.TryEnter(node.Id) && !map.TryEnter(node.Id), "one active node locks other selections");
                Expect(map.CompleteActiveNode() && !map.CompleteActiveNode(), "completion applies once");
                Expect(!map.CanEnter(node.Id), "cannot replay completed node");
            }
            Expect(map.Nodes.Count(n => n.Completed) == data.Rows && !map.Nodes.Any(n => map.CanEnter(n.Id)),
                "one node per row and boss ends route");
        }
        Expect(sawElite && sawFourthFloorRest, "elite and fourth-floor rest both reachable across seeds");
        RunMap first = RunMapGenerator.Generate(data, 42);
        RunMap same = RunMapGenerator.Generate(data, 42);
        Expect(Signature(first) == Signature(same), "seed reproduces graph and encounters");
        first.TryEnter(first.Nodes[0].Id); first.CancelActiveNode();
        Expect(first.CanEnter(first.Nodes[0].Id), "failed entry can be retried without consuming node");
        var invalid = ScriptableObject.CreateInstance<MapEventData>();
        invalid.Choices = new[] { new MapEventChoice { Label = "유료", GoldCost = 10 } };
        Expect(!invalid.IsValid(), "events require a free way to leave");
        invalid.Choices = new[] { new MapEventChoice { Label = "떠난다" } };
        Expect(invalid.IsValid(), "free event choice valid");
        invalid.Choices[0].HealthCost = -1;
        Expect(!invalid.IsValid(), "negative event costs rejected");
        UnityEngine.Object.DestroyImmediate(invalid);
        Debug.Log($"MAP_MODEL_VALIDATION_OK: {checks} assertions passed.");
    }
    private static string Signature(RunMap map) => string.Join("|", map.Nodes.Select(n =>
        $"{n.Id}:{n.Type}:{n.IsUnknown}:{n.Encounter?.name}:{n.Event?.name}:{string.Join(",", n.Outgoing)}"));

    private static void VerifyRoutes(RunMap map, MapNode node, int visits, int treasures, int treasureFloor)
    {
        if (node.Type == MapNodeType.Treasure)
        {
            treasures++;
            Expect(visits == treasureFloor, "every possible route reaches treasure on its configured visit");
        }
        if (node.Outgoing.Count == 0)
            Expect(visits == map.Rows && treasures == 1 && node.Type == MapNodeType.Boss,
                "every possible route ends at the twelfth node after exactly one treasure");
        foreach (int id in node.Outgoing) VerifyRoutes(map, map.Find(id), visits + 1, treasures, treasureFloor);
    }

    private static IEnumerator VerifyScene()
    {
        yield return null;
        RunManager run = Find<RunManager>();
        BattleManager battle = Find<BattleManager>();
        AdventureController adventure = Find<AdventureController>();
        UIScreenFlowController flow = Find<UIScreenFlowController>();
        var configObject = new SerializedObject(run);
        var config = UnityEngine.Object.Instantiate((RunMapData)configObject.FindProperty("mapData").objectReferenceValue);
        config.UseFixedSeed = true; config.Seed = 42;
        config.UnknownBattlePercent = 0; // Event-choice checks use deterministic event outcomes.
        config.Events = new[] { AssetDatabase.LoadAssetAtPath<MapEventData>("Assets/Datas/Adventure/Merchant.asset") };
        configObject.FindProperty("mapData").objectReferenceValue = config;
        configObject.ApplyModifiedPropertiesWithoutUndo();
        Button start = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(b => Enumerable.Range(0, b.onClick.GetPersistentEventCount())
                .Any(i => b.onClick.GetPersistentMethodName(i) == "BeginRun"));
        start.onClick.Invoke();
        Expect(flow.IsMap && run.HasMap && battle.State == BattleState.None, "START opens map before battle");
        foreach (MapNode node in run.Map.Nodes.Where(n => n.Row < run.Map.Rows - 1))
        {
            var rect = NodeButton(node.Id).GetComponent<RectTransform>();
            var above = NodeButton(node.Outgoing[0]).GetComponent<RectTransform>();
            Expect(above.anchoredPosition.y - rect.anchoredPosition.y > rect.sizeDelta.y,
                "twelve-floor node buttons do not overlap");
        }
        Expect(run.CurrentEncounter == null && run.Gold == 0 && run.MasterDeck.Cards.Count == 10, "fresh run resources");
        Expect(!Find<Player>().gameObject.activeInHierarchy && battle.Enemies.Count == 0, "map hides combat world");
        RunMap originalMap = run.Map;
        battle.StartBattle();
        Expect(battle.State == BattleState.None, "battle cannot start without a selected combat node");
        yield return null;
        UIUpdateValidation.VerifyMap(Find<RunMapUI>(), run);
        Capture("map-preview.png");
        var previewScroll = Find<RunMapUI>().GetComponentInChildren<ScrollRect>(true);
        previewScroll.verticalNormalizedPosition = 1;
        yield return null;
        Expect(previewScroll.verticalNormalizedPosition > .99f, "map can scroll to the boss and rest floor");
        Capture("map-upper-preview.png");
        previewScroll.verticalNormalizedPosition = 0;
        yield return null;

        MapNode story = run.Map.Nodes.Single(n => n.Row == 1 && n.Type == MapNodeType.Event);
        MapNode entry = run.Map.Nodes.First(n => n.Row == 0 && n.Column == story.Column);
        Button locked = NodeButton(run.Map.Nodes.Last().Id);
        Expect(!locked.interactable, "future floor button disabled");
        locked.onClick.Invoke();
        Expect(run.Map.ActiveNode == null, "forced click still cannot skip route");
        NodeButton(entry.Id).onClick.Invoke();
        Expect(flow.IsPlaying && battle.State == BattleState.PlayerTurn && run.Map.ActiveNode == entry,
            "node click starts selected encounter");
        UIUpdateValidation.VerifyHUD(Find<PlayHUD>(), run);
        Expect(run.CurrentEncounter == entry.Encounter && Find<Player>().gameObject.activeInHierarchy,
            "node encounter and world enabled");
        foreach (Enemy enemy in battle.Enemies.ToArray()) enemy.Status.TakeDamage(int.MaxValue);
        Expect(battle.State == BattleState.Victory && battle.HasPendingCardReward && entry.Completed,
            "victory completes node and awaits reward");
        Expect(!adventure.SelectNode(story.Id), "cannot leave result before reward");
        battle.StartNextBattle();
        Expect(!flow.IsMap, "pending reward blocks return");
        ClaimCard();
        int gold = run.Gold; int hp = run.PlayerStatus.CurrentHp; int deck = run.MasterDeck.Cards.Count;
        NextButton().onClick.Invoke();
        Expect(flow.IsMap && battle.State == BattleState.None && ReferenceEquals(originalMap, run.Map),
            "reward return keeps same map and does not auto-start battle");
        Expect(NodeButton(entry.Id).GetComponentInChildren<MapNodeIcon>().Completed,
            "cached node icon updates its visited state after battle");
        Expect(run.Gold == gold && run.PlayerStatus.CurrentHp == hp && run.MasterDeck.Cards.Count == deck,
            "resources persist on map");
        Expect(!adventure.SelectNode(entry.Id), "completed node cannot replay");
        yield return null;
        NodeButton(story.Id).onClick.Invoke();
        Expect(flow.IsEvent && battle.Enemies.Count == 0 && adventure.CurrentEvent == story.Event,
            "event node opens event screen");
        var eventUI = new SerializedObject(Find<MapEventUI>());
        Button paid = (Button)eventUI.FindProperty("choiceButtons").GetArrayElementAtIndex(0).objectReferenceValue;
        Expect(!paid.interactable && !adventure.ChooseEvent(0), "unaffordable choice disabled and guarded");
        flow.ShowPauseScreen(); Expect(flow.IsPaused, "event can pause");
        flow.ResumeGame(); Expect(flow.IsEvent && adventure.CurrentEvent == story.Event, "resume returns to event");
        yield return null;
        Capture("event-preview.png");
        Button leave = (Button)eventUI.FindProperty("choiceButtons").GetArrayElementAtIndex(1).objectReferenceValue;
        leave.onClick.Invoke(); leave.onClick.Invoke();
        Expect(adventure.EventResolved && story.Completed && run.Gold == gold, "free choice completes once");
        ((Button)eventUI.FindProperty("continueButton").objectReferenceValue).onClick.Invoke();
        Expect(flow.IsMap, "event confirmation returns to map");
        flow.ShowPauseScreen(); flow.ResumeGame();
        Expect(flow.IsMap && battle.State == BattleState.None, "map pause resumes map");

        adventure.BeginRun();
        Expect(!ReferenceEquals(originalMap, run.Map) && run.Map.Nodes.All(n => !n.Completed)
            && run.Gold == 0 && run.MasterDeck.Cards.Count == 10, "new run resets progress and resources");
        run.AddGold(100); run.PlayerStatus.TakeDamage(20);
        int completed = 0;
        int treasuresOpened = 0;
        bool rested = false;
        bool paidForCard = false;
        while (!run.IsRunComplete)
        {
            MapNode node;
            node = run.Map.Nodes.Where(n => run.Map.CanEnter(n.Id))
                .OrderBy(n => n.Type == MapNodeType.Event ? 0 : n.Type == MapNodeType.Rest ? 1 : 2).First();
            yield return null;
            NodeButton(node.Id).onClick.Invoke();
            if (node.IsCombat)
            {
                foreach (Enemy enemy in battle.Enemies.ToArray()) enemy.Status.TakeDamage(int.MaxValue);
                Expect(battle.HasPendingCardReward, "all combat node types award rewards");
                ClaimCard();
                NextButton().onClick.Invoke();
            }
            else
            {
                int beforeGold = run.Gold; int beforeHp = run.PlayerStatus.CurrentHp;
                int beforeDeck = run.MasterDeck.Cards.Count;
                MapEventChoice choice = adventure.CurrentEvent.Choices[0];
                Expect(adventure.ChooseEvent(0), "affordable event choice accepted");
                Expect(!adventure.ChooseEvent(0), "event effects cannot duplicate");
                Expect(run.Gold == beforeGold - choice.GoldCost + choice.GoldGain, "event gold costs and gains exact");
                Expect(run.MasterDeck.Cards.Count == beforeDeck + (choice.Card != null ? 1 : 0), "event card reward persists");
                if (node.Type == MapNodeType.Rest)
                {
                    rested = true;
                    Expect(run.PlayerStatus.CurrentHp == Math.Min(run.PlayerStatus.MaxHp, beforeHp + run.PlayerStatus.MaxHp * 30 / 100), "campfire heals persistent HP");
                }
                if (choice.Card != null) paidForCard = true;
                if (node.Type == MapNodeType.Treasure)
                {
                    treasuresOpened++;
                    Expect(completed == config.TreasureRow && run.Gold == beforeGold + 25,
                        "sixth clicked node grants treasure gold");
                }
                adventure.ReturnFromEvent();
            }
            completed++;
            Expect(flow.IsMap && run.Map.LastCompletedNode == node && run.Map.ActiveNode == null,
                "node resolves back to map");
        }
        Expect(completed == config.Rows && rested && treasuresOpened == 1,
            "twelve-node route includes rest and exactly one treasure");
        Expect(run.Map.Nodes.Count(n => n.Completed) == config.Rows && !run.Map.Nodes.Any(n => run.Map.CanEnter(n.Id)),
            "boss ends route without replayable nodes");
        // 상인 노드가 선택된 경로에 없을 수도 있으므로 별도로 구매 경로를 확인합니다.
        if (!paidForCard)
        {
            adventure.BeginRun(); run.AddGold(100);
            MapNode merchant = run.Map.Nodes.Single(n => n.Row == 1 && n.Type == MapNodeType.Event);
            MapNode firstNode = run.Map.Nodes.First(n => n.Row == 0 && n.Column == merchant.Column);
            adventure.SelectNode(firstNode.Id);
            foreach (Enemy enemy in battle.Enemies.ToArray()) enemy.Status.TakeDamage(int.MaxValue);
            ClaimCard(); battle.StartNextBattle(); adventure.SelectNode(merchant.Id);
            int beforeGold = run.Gold, beforeDeck = run.MasterDeck.Cards.Count;
            Expect(adventure.ChooseEvent(0) && run.Gold == beforeGold - 25
                && run.MasterDeck.Cards.Count == beforeDeck + 1, "merchant exchanges gold for a card");
            adventure.ReturnFromEvent();
        }

        var upgradeChecks = UpgradeValidation.VerifyScene(config);
        while (upgradeChecks.MoveNext()) yield return upgradeChecks.Current;
        config.Events = new[] { AssetDatabase.LoadAssetAtPath<MapEventData>("Assets/Datas/Adventure/Shrine.asset") };
        adventure.BeginRun();
        MapNode shrine = run.Map.Nodes.Single(n => n.Row == 1 && n.Type == MapNodeType.Event);
        adventure.SelectNode(run.Map.Nodes.First(n => n.Row == 0 && n.Column == shrine.Column).Id);
        foreach (Enemy enemy in battle.Enemies.ToArray()) enemy.Status.TakeDamage(int.MaxValue);
        ClaimCard(); battle.StartNextBattle();
        run.PlayerStatus.ResetBlock();
        run.PlayerStatus.TakeDamage(run.PlayerStatus.CurrentHp - 6);
        adventure.SelectNode(shrine.Id);
        int goldBeforeShrine = run.Gold;
        Expect(!adventure.CanChooseEvent(0) && !adventure.ChooseEvent(0)
            && run.PlayerStatus.CurrentHp == 6 && run.Gold == goldBeforeShrine,
            "HP-cost event cannot kill player or charge a rejected choice");
        Expect(adventure.ChooseEvent(1) && run.PlayerStatus.CurrentHp == 14,
            "healing event applies max-HP percentage correctly");
        adventure.ReturnFromEvent();

        adventure.BeginRun();
        adventure.SelectNode(run.Map.Nodes.First(n => n.Row == 0).Id);
        MapNode failed = run.Map.ActiveNode;
        run.PlayerStatus.TakeDamage(int.MaxValue);
        Expect(battle.State == BattleState.Defeat && !failed.Completed && run.Gold == 0, "defeat grants no map progress or reward");
        adventure.ReturnToMap();
        Expect(!flow.IsMap && !adventure.SelectNode(run.Map.Nodes.Last().Id), "defeat cannot resume route");
        flow.ShowTitleScreen();
        adventure.BeginRun();
        Expect(flow.IsMap && run.PlayerStatus.CurrentHp == run.PlayerStatus.MaxHp && run.Gold == 0,
            "new game after defeat works");
        config.UnknownBattlePercent = 100;
        adventure.BeginRun();
        MapNode mystery = run.Map.Nodes.Single(n => n.Row == 1 && n.IsUnknown);
        MapNode approach = run.Map.Nodes.Single(n => n.Row == 0 && n.Column == mystery.Column);
        var mysteryIcon = NodeButton(mystery.Id).GetComponentInChildren<MapNodeIcon>();
        Expect(mysteryIcon.Unknown && mystery.Label == "미지의 장소", "mystery battle hidden before entry");
        adventure.SelectNode(approach.Id);
        foreach (var enemy in battle.Enemies.ToArray()) enemy.Status.TakeDamage(int.MaxValue);
        ClaimCard(); battle.StartNextBattle();
        Expect(adventure.SelectNode(mystery.Id) && flow.IsPlaying && battle.State == BattleState.PlayerTurn,
            "question-mark battle enters normal combat");
        foreach (var enemy in battle.Enemies.ToArray()) enemy.Status.TakeDamage(int.MaxValue);
        ClaimCard(); battle.StartNextBattle();
        Expect(mystery.Completed && !NodeButton(mystery.Id).GetComponentInChildren<MapNodeIcon>().Unknown,
            "resolved mystery reveals visited battle icon");
        var scroll = Find<RunMapUI>().GetComponentInChildren<ScrollRect>(true);
        Expect(scroll != null && scroll.vertical && !scroll.horizontal && scroll.content.rect.height > scroll.viewport.rect.height,
            "map uses a vertical scroll viewport with spacious rows");
        UnityEngine.Object.Destroy(config);
    }
    private static Button NodeButton(int id) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
        .Single(b => b.name == "MapNode_" + id);
    private static Button NextButton() => (Button)new SerializedObject(Find<PlayHUD>())
        .FindProperty("nextBattleButton").objectReferenceValue;
    private static void ClaimCard()
    {
        var rewardUI = new SerializedObject(Find<BattleRewardUI>());
        var card = (CardUI)rewardUI.FindProperty("rewardCards").GetArrayElementAtIndex(0).objectReferenceValue;
        card.GetComponent<Button>().onClick.Invoke();
        Expect(!Find<BattleManager>().HasPendingCardReward, "actual reward card click completes selection");
    }
    private static void Capture(string name)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) return;
        typeof(BattleValidation).GetMethod("CapturePreview", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { name });
    }
}
