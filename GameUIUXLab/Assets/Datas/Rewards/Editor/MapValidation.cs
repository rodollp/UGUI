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

    [MenuItem("Tools/Map/Validate Adventure")]
    private static void RunInEditor()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) Run();
    }

    public static void Run()
    {
        try { VerifyModels(); RewardValidation.VerifyModels(); UpgradeValidation.VerifyModels(); }
        catch (Exception error)
        {
            Debug.LogException(error);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
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
            SessionState.SetBool(Active, false); SessionState.SetBool(Legacy, Application.isBatchMode);
            EditorApplication.isPlaying = false;
        }
        catch (Exception error)
        {
            Debug.LogException(error); SessionState.SetBool(Active, false);
            EditorApplication.update -= Tick;
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else EditorApplication.isPlaying = false;
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
        Expect(data.Shop != null && data.Events.All(story => story.Choices.All(choice => choice.Shop == null)),
            "default map has a dedicated shop and no shop events");
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
            var shops = map.Nodes.Where(n => n.Type == MapNodeType.Shop).ToArray();
            Expect(shops.Length == 1, "one shop per default map");
            MapNode shop = shops[0];
            Expect(shop.Row + 1 >= 4 && shop.Row < map.Rows - 2 && shop.Row != data.TreasureRow
                && !shop.IsUnknown && !shop.IsCombat && shop.Event == null && shop.Shop == data.Shop,
                "visible dedicated shop starts on floor four and leaves reserved floors intact");
            sawElite |= map.Nodes.Any(n => n.Type == MapNodeType.Elite);
            sawFourthFloorRest |= map.Nodes.Any(n => n.Row == 3 && n.Type == MapNodeType.Rest);
            Expect(map.Nodes.All(n => n.Outgoing.All(id => map.Find(id).Row == n.Row + 1)),
                "edges advance one floor without cycles");
            Expect(map.Nodes.Where(n => n.Type is MapNodeType.Rest or MapNodeType.Elite or MapNodeType.Shop)
                .All(n => n.Outgoing.All(id => map.Find(id).Type != n.Type)),
                "no path connects two rest, elite or shop nodes of the same type");
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
        var paidEvent = ScriptableObject.CreateInstance<MapEventData>();
        paidEvent.Title = "여행자의 거래";
        paidEvent.Description = "여행자가 카드를 팝니다.";
        paidEvent.Choices = new[] {
            new MapEventChoice { Label = "카드를 구매한다", GoldCost = 25, Card = config.Shop.CardPool[0] },
            new MapEventChoice { Label = "지나간다" }
        };
        config.Events = new[] { paidEvent };
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
        VerifyRelicBar(Find<RunMapUI>().transform, run);
        Expect(!Find<Player>().gameObject.activeInHierarchy && battle.Enemies.Count == 0, "map hides combat world");
        VerifyShopNode(run, adventure, flow);
        config.Shop = null; // Keep the regular event and battle scenarios focused on their own paths.
        start.onClick.Invoke();
        RunMap originalMap = run.Map;
        battle.StartBattle();
        Expect(battle.State == BattleState.None, "battle cannot start without a selected combat node");
        yield return null;
        var mapUI = Find<RunMapUI>();
        UIUpdateValidation.VerifyMap(mapUI, run);
        VerifyMapHealth(mapUI, run.PlayerStatus);
        run.PlayerStatus.TakeDamage(7);
        VerifyMapHealth(mapUI, run.PlayerStatus);
        run.PlayerStatus.Heal(7);
        VerifyMapHealth(mapUI, run.PlayerStatus);
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
        var playHud = Find<PlayHUD>();
        UIUpdateValidation.VerifyHUD(playHud, run);
        var turnLabel = (TMP_Text)new SerializedObject(playHud).FindProperty("turnText").objectReferenceValue;
        VerifyRelicBar(turnLabel.transform.parent, run);
        yield return null;
        var shortcuts = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        Button mapShortcut = shortcuts.Single(b => b.name == "OpenMapPreview");
        Button deckShortcut = shortcuts.Single(b => b.name == "OpenDeck");
        Expect(mapShortcut.GetComponentInChildren<AdventureShortcutIcon>().ShowMap
            && !deckShortcut.GetComponentInChildren<AdventureShortcutIcon>().ShowMap,
            "battle shows distinct map and card shortcut icons");
        int turn = battle.Turn;
        run.PlayerStatus.TakeDamage(4);
        mapShortcut.onClick.Invoke();
        VerifyMapHealth(mapUI, run.PlayerStatus);
        MapNodeIcon currentIcon = NodeButton(entry.Id).GetComponentInChildren<MapNodeIcon>();
        Expect(flow.IsMapPreviewOpen && flow.IsPlaying && !flow.IsMap && Time.timeScale == 0
            && currentIcon.Available && !NodeButton(entry.Id).interactable,
            "map preview pauses the battle and marks the current node without making it selectable");
        var timer = Find<RunTimer>();
        float pausedTime = timer.PlayTime;
        NodeButton(story.Id).onClick.Invoke();
        battle.EndPlayerTurn();
        deckShortcut.onClick.Invoke();
        yield return null;
        Expect(timer.PlayTime == pausedTime && !flow.IsDeckOpen && run.Map.ActiveNode == entry
            && battle.Turn == turn && battle.State == BattleState.PlayerTurn,
            "map preview blocks travel, turn changes and overlapping deck while the timer is paused");
        UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .Single(b => b.name == "CloseMapPreview").onClick.Invoke();
        Expect(!flow.IsMapPreviewOpen && flow.IsPlaying && battle.CanPlayCards && Time.timeScale == 1,
            "close button resumes the same battle");
        yield return null;
        mapShortcut.onClick.Invoke();
        Expect(flow.TryCloseMapPreview() && battle.CanPlayCards && run.Map.ActiveNode == entry,
            "escape dismissal also resumes the same turn");
        Expect(run.CurrentEncounter == entry.Encounter && Find<Player>().gameObject.activeInHierarchy,
            "node encounter and world enabled");
        run.PlayerStatus.Heal(4);
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
        var defaultMap = AssetDatabase.LoadAssetAtPath<RunMapData>("Assets/Datas/Adventure/DefaultMap.asset");
        config.Events = new[] { MapSetup.FindEvent(defaultMap, "Shrine") };
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
        Expect(run.PlayerStatus.CurrentHp == 0 && battle.HpRestoredByRelics == 0, "Battle-end healing does not revive a defeated player");
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
        var relicChecks = VerifyRelics(run, adventure, battle);
        while (relicChecks.MoveNext()) yield return relicChecks.Current;
        VerifyStrikeRelic(config, run, adventure, battle);
        UnityEngine.Object.Destroy(config);
        UnityEngine.Object.Destroy(paidEvent);
    }

    private static IEnumerator VerifyRelics(RunManager run, AdventureController adventure, BattleManager battle)
    {
        RelicData crest = AssetDatabase.LoadAssetAtPath<RelicData>("Assets/Datas/Relics/WarriorCrest.asset");
        RelicData talisman = AssetDatabase.LoadAssetAtPath<RelicData>("Assets/Datas/Relics/WornTalisman.asset");
        adventure.BeginRun();
        Expect(crest != null && talisman != null && run.Relics.SequenceEqual(new[] { crest }),
            "The warrior starts with its configured relic");
        Expect(crest.GetEffectDescription() == "전투 종료 시 체력을 최대 6 회복합니다.",
            "The warrior relic describes its battle-end trigger");
        MapNode treasure = run.Map.Nodes.First(node => node.Type == MapNodeType.Treasure);
        foreach (MapNode previous in run.Map.Nodes.Where(node => node.Row < treasure.Row && node.Column == treasure.Column))
        {
            Expect(run.TryEnterMapNode(previous.Id), "The route reaches the treasure");
            run.CompleteStage();
        }
        Expect(adventure.SelectNode(treasure.Id), "The treasure opens its event screen");
        Transform eventScreen = Find<MapEventUI>().transform;
        Button open = eventScreen.Find("Choice0").GetComponent<Button>();
        Expect(adventure.CurrentEvent.Choices.Length == 1 && open.gameObject.activeInHierarchy && open.interactable
            && !eventScreen.Find("Choice1").gameObject.activeSelf && !eventScreen.Find("Choice2").gameObject.activeSelf,
            "The treasure presents one open action");
        int gold = run.Gold;
        open.onClick.Invoke();
        Expect(adventure.EventResolved && run.Gold == gold + 25
            && run.Relics.SequenceEqual(new[] { crest, talisman })
            && adventure.EventResult.Contains("골드 +25") && adventure.EventResult.Contains(talisman.DisplayName),
            "Opening the treasure awards and shows both gold and relic");
        Expect(!adventure.ChooseEvent(0) && !run.TryAddRelic(talisman) && run.Gold == gold + 25,
            "The treasure cannot grant rewards twice");
        adventure.ReturnFromEvent();
        Expect(!adventure.SelectNode(treasure.Id), "The opened treasure cannot be revisited");
        VerifyRelicBar(Find<RunMapUI>().transform, run);

        int combats = 0;
        while (!run.IsRunComplete && combats < 2)
        {
            MapNode next = run.Map.Nodes.Where(node => run.Map.CanEnter(node.Id))
                .OrderBy(node => node.IsCombat ? 0 : 1).First();
            if (!next.IsCombat)
            {
                Expect(run.TryEnterMapNode(next.Id), "The route continues after treasure");
                run.CompleteStage();
                continue;
            }
            Expect(adventure.SelectNode(next.Id) && battle.Turn == 1 && run.PlayerStatus.CurrentBlock == 4,
                "The talisman grants four block on the first turn of combat");
            if (combats == 0)
            {
                battle.EndPlayerTurn();
                while (battle.State == BattleState.EnemyTurn) yield return null;
                Expect(battle.Turn == 2 && run.PlayerStatus.CurrentBlock == 0,
                    "The talisman does not trigger on the second turn");
            }
            int missingHp = combats == 0 ? 2 : 10;
            run.PlayerStatus.ResetBlock();
            run.PlayerStatus.Heal(run.PlayerStatus.MaxHp);
            run.PlayerStatus.TakeDamage(missingHp);
            foreach (Enemy enemy in battle.Enemies.ToArray()) enemy.Status.TakeDamage(int.MaxValue);
            battle.CheckBattleEnd();
            int expectedHealing = combats == 0 ? 2 : 6;
            Expect(battle.HpRestoredByRelics == expectedHealing
                && run.PlayerStatus.CurrentHp == run.PlayerStatus.MaxHp - missingHp + expectedHealing,
                "Warrior battle-end healing applies once for up to six HP");
            ClaimCard();
            NextButton().onClick.Invoke();
            combats++;
        }
        Expect(combats == 2, "The talisman also applies in the following combat");
        adventure.BeginRun();
        Expect(run.Relics.SequenceEqual(new[] { crest }), "A new run replaces acquired relics with the starter relic");
        VerifyRelicBar(Find<RunMapUI>().transform, run);
    }

    private static void VerifyStrikeRelic(RunMapData config, RunManager run, AdventureController adventure, BattleManager battle)
    {
        var defaultMap = AssetDatabase.LoadAssetAtPath<RunMapData>("Assets/Datas/Adventure/DefaultMap.asset");
        var smith = defaultMap.Events.Single(data => data.name == "Smith");
        var hammer = AssetDatabase.LoadAssetAtPath<RelicData>("Assets/Datas/Relics/SmithHammer.asset");
        var strike = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Datas/CardDis/ATK.asset");
        var bash = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Datas/Rewards/Bash.asset");
        config.Events = new[] { smith };
        config.UnknownBattlePercent = 0;
        adventure.BeginRun();
        Expect(strike.Base.GetDescription(run.PlayerStatus) == "피해를 6 줍니다.", "Strike starts at its base damage");

        MapNode eventNode = run.Map.Nodes.Single(node => node.Row == 1 && node.Event == smith);
        MapNode approach = run.Map.Nodes.Single(node => node.Row == 0 && node.Column == eventNode.Column);
        Expect(run.TryEnterMapNode(approach.Id), "Route reaches the smith");
        run.CompleteStage();
        Expect(adventure.SelectNode(eventNode.Id), "The smith event opens");
        int choice = Array.FindIndex(smith.Choices, option => option.Relic == hammer);
        Expect(hammer != null && choice >= 0, "The smith offers the hammer relic");
        Find<MapEventUI>().transform.Find("Choice" + choice).GetComponent<Button>().onClick.Invoke();
        Expect(adventure.EventResolved && run.Relics.Contains(hammer) && !adventure.ChooseEvent(choice),
            "The smith grants the hammer once");
        adventure.ReturnFromEvent();
        VerifyRelicBar(Find<RunMapUI>().transform, run);

        foreach (CardInstance card in run.MasterDeck.Cards.ToArray()) run.MasterDeck.RemoveCard(card);
        var upgradedStrike = new CardInstance(strike);
        upgradedStrike.TryUpgrade();
        run.MasterDeck.AddCard(new CardInstance(strike));
        run.MasterDeck.AddCard(upgradedStrike);
        run.MasterDeck.AddCard(new CardInstance(bash));

        MapNode next = run.Map.Nodes.First(node => run.Map.CanEnter(node.Id));
        while (!next.IsCombat)
        {
            Expect(run.TryEnterMapNode(next.Id), "The route continues after the smith");
            run.CompleteStage();
            next = run.Map.Nodes.First(node => run.Map.CanEnter(node.Id));
        }
        Expect(adventure.SelectNode(next.Id), "Combat starts after acquiring the hammer");
        CardUI[] hand = UnityEngine.Object.FindObjectsByType<CardUI>(FindObjectsSortMode.None)
            .Where(view => view.CardInstance != null).ToArray();
        CardUI basic = hand.Single(view => view.CardInstance.CardData == strike && !view.CardInstance.IsUpgraded);
        string Description(CardUI view) =>
            ((TMP_Text)new SerializedObject(view).FindProperty("descriptionText").objectReferenceValue).text;
        Expect(Description(basic) == "피해를 9 줍니다."
            && Description(hand.Single(view => view.CardInstance.IsUpgraded)) == "피해를 12 줍니다."
            && Description(hand.Single(view => view.CardInstance.CardData == bash)).StartsWith("피해를 8 줍니다."),
            "The hand shows the bonus on Strike and Strike+ and the base damage on Bash");

        Enemy enemy = battle.Enemies.First();
        int hp = enemy.Status.CurrentHp;
        Find<CardManager>().UseCard(basic, enemy);
        Expect(enemy.Status.CurrentHp == hp - 9, "Playing Strike deals the damage shown on the card");

        var multiHit = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Datas/Rewards/ManyAttack.asset")
            .Base.Effects.OfType<DamageEffect>().Single();
        var practiceTarget = new EnemyStatus(30);
        const string multiStrikeName = "연속 타격";
        multiHit.Execute(new CardEffectContext(run.PlayerStatus, practiceTarget, cardName: multiStrikeName));
        Expect(practiceTarget.CurrentHp == 18
            && multiHit.GetDescription(run.PlayerStatus, null, multiStrikeName) == "피해를 6만큼 2번 줍니다.",
            "A named multi-hit attack receives the bonus per hit");

        adventure.BeginRun();
        Expect(!run.Relics.Contains(hammer) && strike.Base.GetDescription(run.PlayerStatus) == "피해를 6 줍니다.",
            "A new run clears the hammer bonus");
    }

    private static void VerifyMapHealth(RunMapUI map, PlayerStatus player)
    {
        var health = map.transform.Find("MapHealth") as RectTransform;
        Expect(health != null && health.gameObject.activeInHierarchy
            && health.anchorMin == Vector2.up && health.anchorMax == Vector2.up,
            "The map shows health in the upper-left HUD");
        var value = health.Find("Value").GetComponent<TMP_Text>();
        var fill = (RectTransform)health.Find("Track/Fill");
        Expect(value.text == $"{player.CurrentHp} / {player.MaxHp}"
            && Mathf.Approximately(fill.anchorMax.x, (float)player.CurrentHp / player.MaxHp),
            "The map health value and fill match the current player status");
    }

    private static void VerifyRelicBar(Transform screen, RunManager run)
    {
        var bar = screen.Find("RelicBar") as RectTransform;
        Expect(bar != null && bar.gameObject.activeInHierarchy && bar.anchorMin == Vector2.up
            && bar.anchorMax == Vector2.up && bar.pivot == Vector2.up, "Relic bar is visible at the upper left");
        var targets = bar.GetComponentsInChildren<RelicTooltipTarget>();
        Expect(targets.Length == run.Relics.Count && !bar.GetComponent<Image>().raycastTarget,
            "There is one hover target per owned relic");
        Transform tooltip = bar.Find("RelicTooltip");
        var title = tooltip.Find("Title").GetComponent<TMP_Text>();
        var description = tooltip.Find("Description").GetComponent<TMP_Text>();
        Expect(!tooltip.gameObject.activeSelf && !tooltip.GetComponent<Image>().raycastTarget,
            "The tooltip starts hidden and does not intercept pointer input");
        for (int i = 0; i < targets.Length; i++)
        {
            var relic = run.Relics[i];
            var target = targets[i];
            target.OnPointerEnter(null);
            Expect(tooltip.gameObject.activeInHierarchy && title.text == relic.DisplayName
                && description.text == relic.GetEffectDescription(), "Hover identifies the relic and explains its effect");
            if (i > 0)
            {
                targets[i - 1].OnPointerExit(null);
                Expect(tooltip.gameObject.activeInHierarchy, "Leaving a previous relic does not hide the current tooltip");
            }
        }
        targets.Last().OnPointerExit(null);
        Expect(!tooltip.gameObject.activeSelf, "Leaving the current relic hides the tooltip");
    }

    private static void VerifyShopNode(RunManager run, AdventureController adventure, UIScreenFlowController flow)
    {
        MapNode node = run.Map.Nodes.Single(n => n.Type == MapNodeType.Shop);
        Button button = NodeButton(node.Id);
        MapNodeIcon icon = button.GetComponentInChildren<MapNodeIcon>();
        Expect(icon.Kind == MapNodeType.Shop && !icon.Unknown && !button.interactable,
            "shop icon is visible and the distant shop is locked");
        foreach (MapNode previous in run.Map.Nodes.Where(n => n.Row < node.Row && n.Column == node.Column))
        {
            Expect(run.TryEnterMapNode(previous.Id), "same-column route leads to shop");
            run.CompleteStage();
        }
        Expect(button.interactable, "adjacent shop becomes selectable");
        run.AddGold(250);
        button.onClick.Invoke();
        ShopUI shop = Find<ShopUI>();
        Expect(flow.IsEvent && adventure.IsShopping && adventure.CurrentEvent == null
            && shop.gameObject.activeInHierarchy && shop.OfferViews.Count == 5,
            "shop button immediately opens card offers");
        var inventory = adventure.CurrentShop;
        flow.ShowPauseScreen(); flow.ResumeGame();
        Expect(ReferenceEquals(adventure.CurrentShop, inventory) && shop.gameObject.activeInHierarchy,
            "pause and resume preserve shop inventory");
        int cards = run.MasterDeck.Cards.Count;
        shop.OfferViews[inventory.DiscountIndex].GetComponent<Button>().onClick.Invoke();
        shop.ConfirmButton.onClick.Invoke();
        Expect(run.Gold == 225 && run.MasterDeck.Cards.Count == cards + 1
            && inventory.IsSold(inventory.DiscountIndex), "discounted shop purchase updates gold, deck and stock");
        Button remove = shop.transform.Find("RemoveCard").GetComponent<Button>();
        remove.onClick.Invoke();
        shop.RemovalViews[0].GetComponent<Button>().onClick.Invoke();
        shop.ConfirmButton.onClick.Invoke();
        Expect(inventory.CardRemovalUsed && !remove.interactable && run.Gold == 175
            && run.MasterDeck.Cards.Count == cards && run.CardRemovalPrice == 100,
            "successful removal closes the picker and disables the service");
        Expect(!adventure.RemoveShopCard(run.MasterDeck.Cards[0]) && run.Gold == 175
            && run.MasterDeck.Cards.Count == cards && run.CardRemovalPrice == 100,
            "second removal is rejected even when the player can afford it");
        shop.transform.Find("LeaveShop").GetComponent<Button>().onClick.Invoke();
        Expect(flow.IsMap && node.Completed && run.Map.ActiveNode == null && !adventure.SelectNode(node.Id),
            "leaving closes the shop and completes its node once");
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
