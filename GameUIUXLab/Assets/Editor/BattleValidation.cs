using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>실제 CardGame 씬을 재생하며 전투 규칙을 검증합니다. 배치 모드 전용입니다.</summary>
[InitializeOnLoad]
public static class BattleValidation
{
    private const string ActiveKey = "BattleValidation.Active";
    private static IEnumerator checks;
    private static double deadline;
    private static int assertions;

    static BattleValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                assertions = 0;
                deadline = EditorApplication.timeSinceStartup + 40;
                checks = VerifyScene();
                EditorApplication.update += Tick;
            }
        };
    }

    public static void Run()
    {
        try { VerifyModels(); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); return; }
        EditorSceneManager.OpenScene("Assets/Scenes/CardGame.unity");
        SessionState.SetBool(ActiveKey, true);
        EditorApplication.isPlaying = true;
    }

    public static void ConfigureAndRun()
    {
        BattleSampleSetup.Configure();
        Run();
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
                throw new TimeoutException("Battle validation timed out.");
            if (checks.MoveNext()) return;
            Debug.Log($"BATTLE_VALIDATION_OK: model rules and {assertions} scene assertions passed.");
            End(0);
        }
        catch (Exception error) { Debug.LogException(error); End(1); }
    }

    private static void End(int code)
    {
        SessionState.SetBool(ActiveKey, false);
        EditorApplication.update -= Tick;
        EditorApplication.Exit(code);
    }

    private static void Expect(bool value, string message)
    {
        assertions++;
        if (!value) throw new InvalidOperationException(message);
    }

    private static T Find<T>() where T : UnityEngine.Object =>
        UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();

    private static CardUI[] Hand() => UnityEngine.Object.FindObjectsByType<CardUI>(
        FindObjectsSortMode.None).Where(c => c.CardInstance != null).ToArray();

    private static void VerifyModels()
    {
        DebuffValidation.VerifyModels();
        PlayerStatus player = new PlayerStatus(80);
        int notifications = 0;
        player.OnEnergyChanged += () => notifications++;
        Expect(player.CurrentEnergy == 3 && player.MaxEnergy == 3, "Initial energy");
        Expect(!player.TryUseEnergy(-1) && !player.TryUseEnergy(4), "Invalid costs rejected");
        Expect(player.TryUseEnergy(0) && notifications == 0, "Free card does not change energy");
        Expect(player.TryUseEnergy(2) && player.CurrentEnergy == 1, "Cost consumed");
        Expect(player.GainEnergy(5) == 5 && player.CurrentEnergy == 6, "Overflow energy allowed");
        player.StartTurn();
        Expect(player.CurrentEnergy == 3, "Turn resets bonus energy");
        player.GainEnergy(int.MaxValue);
        Expect(player.CurrentEnergy == int.MaxValue, "Energy integer overflow guarded");
        player.TakeDamage(20);
        player.Heal(int.MaxValue);
        Expect(player.CurrentHp == 80, "Healing clamps without integer overflow");
        player.AddBlock(8); player.AddAttackPower(3); player.AddDexterity(2);
        player.TakeDamage(10);
        int hp = player.CurrentHp;
        player.ResetCombatState();
        Expect(player.CurrentHp == hp && player.CurrentBlock == 0
            && player.AttackPower == 0 && player.Dexterity == 0, "Combat reset preserves HP");
        player.TakeDamage(int.MaxValue);
        player.Heal(100);
        Expect(player.IsDead && !player.TryUseEnergy(0) && player.GainEnergy(2) == 0, "Death is terminal");

        StarterDeckData starter = ScriptableObject.CreateInstance<StarterDeckData>();
        CardData normal = ScriptableObject.CreateInstance<CardData>();
        CardData power = ScriptableObject.CreateInstance<CardData>();
        power.Type = CardType.Power;
        starter.StartingCards.Add(new StartingCardEntry { CardData = normal, Count = 1 });
        starter.StartingCards.Add(new StartingCardEntry { CardData = power, Count = 1 });
        MasterDeck master = new MasterDeck(starter);
        BattleDeck deck = new BattleDeck(master);
        CardInstance first = deck.Draw();
        CardInstance second = deck.Draw();
        CardInstance powerCard = first.CardData.Type == CardType.Power ? first : second;
        CardInstance normalCard = first == powerCard ? second : first;
        Expect(deck.PlayCard(powerCard) && !deck.PlayCard(powerCard), "Power card plays once");
        deck.PlayCard(normalCard);
        Expect(deck.Draw() == normalCard && deck.Draw() == null, "Refill excludes powers");
        deck.DiscardHand();
        Expect(deck.Draw() == normalCard && master.Cards.Count == 2, "Master deck unchanged");
        UnityEngine.Object.DestroyImmediate(starter);
        UnityEngine.Object.DestroyImmediate(normal);
        UnityEngine.Object.DestroyImmediate(power);

        EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>();
        enemyData.MaxHp = 20;
        enemyData.Actions = new[] {
            new EnemyAction { Type = EnemyActionType.Strength, Amount = 2 },
            new EnemyAction { Type = EnemyActionType.Attack, Amount = 3, Hits = 2 }
        };
        GameObject enemyObject = new GameObject("ValidationEnemy");
        Enemy enemy = enemyObject.AddComponent<Enemy>();
        enemy.Initialize(enemyData);
        PlayerStatus defender = new PlayerStatus(30);
        enemy.PlanNextAction();
        enemy.ExecuteIntent(defender);
        Expect(enemy.Status.AttackPower == 2 && defender.CurrentHp == 30, "Enemy strength intent");
        enemy.PlanNextAction();
        defender.AddBlock(6);
        enemy.ExecuteIntent(defender);
        Expect(defender.CurrentHp == 26 && defender.CurrentBlock == 0, "Multi-hit damage consumes block per hit");
        enemy.PlanNextAction();
        Expect(enemy.Intent.Type == EnemyActionType.Strength, "Intent pattern cycles");
        UnityEngine.Object.DestroyImmediate(enemyObject);
        UnityEngine.Object.DestroyImmediate(enemyData);
        Debug.Log($"BATTLE_MODEL_VALIDATION_OK: {assertions} assertions passed.");
    }

    private static IEnumerator VerifyScene()
    {
        yield return null; // MonoBehaviour.Start 및 카드 풀 초기화 이후 진행합니다.
        RunManager run = Find<RunManager>();
        // 기존 순차 전투 검증은 별도 테스트 설정으로 유지합니다. 실제 씬은 맵 모드를 사용합니다.
        var runSetup = new SerializedObject(run);
        runSetup.FindProperty("mapData").objectReferenceValue = null;
        runSetup.ApplyModifiedPropertiesWithoutUndo();
        BattleManager battle = Find<BattleManager>();
        CardManager cards = Find<CardManager>();
        UIScreenFlowController flow = Find<UIScreenFlowController>();
        run.StartNewRun();
        flow.ShowPlayScreen();
        battle.StartBattle();
        PlayerStatus status = run.PlayerStatus;
        Expect(battle.State == BattleState.PlayerTurn && battle.Turn == 1, "Initial player turn");
        Expect(battle.Enemies.Count == 1 && Hand().Length == 5, "Enemy spawned and five cards drawn");
        Enemy target = battle.Enemies[0];
        Expect(target.Intent != null && target.Intent.Type == EnemyActionType.Attack, "Intent planned");
        // 그래픽 장치가 있는 배치 실행에서는 실제 게임 화면도 저장합니다.
        if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            double settleUntil = EditorApplication.timeSinceStartup + 1;
            while (EditorApplication.timeSinceStartup < settleUntil) yield return null;
            CapturePreview();
            CapturePreview("cards-preview.png");
        }
        CardUI attack = Hand().FirstOrDefault(c => c.CardInstance.CardData.TargetType == CardTargetType.Enemy);
        if (attack == null)
        {
            cards.EndTurn();
            while (battle.State == BattleState.EnemyTurn) yield return null;
            attack = Hand().FirstOrDefault(c => c.CardInstance.CardData.TargetType == CardTargetType.Enemy);
        }
        Expect(attack != null, "Attack available within first ten draws");
        {
            int enemyHp = target.Status.CurrentHp;
            status.TryUseEnergy(status.CurrentEnergy);
            cards.UseCard(attack, target);
            Expect(target.Status.CurrentHp == enemyHp && attack.gameObject.activeSelf, "Unaffordable card rejected");
            status.RestoreEnergy();
            cards.UseCard(attack);
            Expect(status.CurrentEnergy == 3 && target.Status.CurrentHp == enemyHp, "Missing target rejected");
            int cost = attack.CardInstance.CurrentCost;
            cards.UseCard(attack, target);
            Expect(status.CurrentEnergy == 3 - cost && target.Status.CurrentHp < enemyHp, "Card costs and damage applied");
            int energy = status.CurrentEnergy;
            enemyHp = target.Status.CurrentHp;
            cards.UseCard(attack, target);
            Expect(status.CurrentEnergy == energy && target.Status.CurrentHp == enemyHp, "Duplicate use rejected");
        }
        status.AddBlock(5);
        int hp = status.CurrentHp;
        int previousTurn = battle.Turn;
        cards.EndTurn();
        Expect(battle.State == BattleState.EnemyTurn && Hand().Length == 0, "Hand discarded before enemy actions");
        cards.EndTurn();
        Expect(battle.State == BattleState.EnemyTurn, "Duplicate turn-end rejected");
        Time.timeScale = 0;
        yield return null;
        Expect(!battle.CanPlayCards && status.CurrentHp == hp, "Pause freezes enemy turn");
        Time.timeScale = 1;
        while (battle.State == BattleState.EnemyTurn) yield return null;
        Expect(battle.Turn == previousTurn + 1 && status.CurrentHp == hp - 1, "Enemy attacks through block once");
        Expect(status.CurrentBlock == 0 && status.CurrentEnergy == 3 && Hand().Length == 5, "New turn resets resources");

        foreach (DebuffType type in Enum.GetValues(typeof(DebuffType)))
        {
            status.ApplyDebuff(type, 1);
            target.Status.ApplyDebuff(type, 2);
        }
        Expect(target.GetIntentDescription(status) == "공격 6 × 1", "Intent incorporates weak and vulnerable");
        TMP_Text playerDebuffs = Find<Player>().GetComponentsInChildren<TMP_Text>(true)
            .Single(t => t.name == "DebuffText");
        Expect(playerDebuffs.gameObject.activeInHierarchy && playerDebuffs.text.Contains("취약 1")
            && playerDebuffs.text.Contains("약화 1") && playerDebuffs.text.Contains("손상 1"), "Player debuffs visible");
        TMP_Text enemyDebuffs = target.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "DebuffText");
        Expect(enemyDebuffs.gameObject.activeInHierarchy && enemyDebuffs.text.Contains("약화 2"), "Enemy debuffs visible");
        if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            yield return null;
            CapturePreview("debuff-preview.png");
        }
        status.AddBlock(100);
        cards.EndTurn();
        while (battle.State == BattleState.EnemyTurn) yield return null;
        Expect(status.WeakTurns == 0 && status.FrailTurns == 0 && status.VulnerableTurns == 0,
            "Player durations decay once per round");
        Expect(target.Status.WeakTurns == 1 && target.Status.FrailTurns == 1 && target.Status.VulnerableTurns == 1,
            "Enemy durations decay once per round");
        Expect(!playerDebuffs.gameObject.activeSelf && enemyDebuffs.text.Contains("약화 1"),
            "Debuff labels refresh after round");
        status.AddBlock(100);
        cards.EndTurn();
        while (battle.State == BattleState.EnemyTurn) yield return null;
        Expect(target.Status.GetDebuffDescription() == "" && !enemyDebuffs.gameObject.activeSelf,
            "Expired enemy debuffs hidden");
        status.ApplyDebuff(DebuffType.Weak, 3);
        status.AddAttackPower(3); status.AddDexterity(2);
        target.Status.TakeDamage(int.MaxValue);
        Expect(battle.State == BattleState.Victory && run.StageIndex == 1, "Victory advances one floor");
        VerifyResultText("전투 승리!", false);
        if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            yield return null;
            CapturePreview("battle-result-preview.png");
        }
        RewardValidation.VerifyAndClaim(battle, run, 0);
        hp = status.CurrentHp;
        battle.StartNextBattle();
        Expect(battle.State == BattleState.PlayerTurn && battle.Enemies.Count == 2, "Next encounter spawns two enemies");
        Expect(ReferenceEquals(status, run.PlayerStatus) && status.CurrentHp == hp, "HP persists across floors");
        Expect(status.AttackPower == 0 && status.Dexterity == 0 && status.CurrentEnergy == 3, "Combat buffs reset");
        Expect(status.GetDebuffDescription() == "", "Next battle clears all debuffs");
        int goldBeforeDefeat = run.Gold;
        int deckBeforeDefeat = run.MasterDeck.Cards.Count;
        foreach (Enemy enemy in battle.Enemies.Take(1).ToArray()) enemy.Status.TakeDamage(int.MaxValue);
        Expect(run.Gold == goldBeforeDefeat && battle.RewardOffer == null, "Single kill does not grant victory rewards");
        status.TakeDamage(int.MaxValue);
        Expect(battle.State == BattleState.Defeat && run.StageIndex == 1 && Hand().Length == 0, "Defeat stops progression");
        Expect(run.Gold == goldBeforeDefeat && run.MasterDeck.Cards.Count == deckBeforeDefeat
            && battle.RewardOffer == null && !battle.TrySelectRewardCard(0), "Defeat has no gold or card reward");
        VerifyResultText("패배", false);
        battle.StartNextBattle();
        Expect(battle.State == BattleState.Defeat, "Cannot advance after defeat");
        flow.ShowTitleScreen();
        Expect(battle.State == BattleState.None && battle.Enemies.Count == 0, "Title cleans up combat");
        run.StartNewRun(); flow.ShowPlayScreen(); battle.StartBattle();
        status = run.PlayerStatus;
        hp = status.CurrentHp;
        cards.EndTurn();
        flow.ShowTitleScreen();
        run.StartNewRun(); flow.ShowPlayScreen(); battle.StartBattle();
        double waitUntil = EditorApplication.timeSinceStartup + 1;
        while (EditorApplication.timeSinceStartup < waitUntil) yield return null;
        Expect(battle.State == BattleState.PlayerTurn && battle.Turn == 1
            && run.PlayerStatus.CurrentHp == hp, "Aborted enemy coroutine cannot affect new run");

        for (int floor = 0; floor < 3; floor++)
        {
            int goldBeforeVictory = run.Gold;
            foreach (Enemy enemy in battle.Enemies.ToArray()) enemy.Status.TakeDamage(int.MaxValue);
            Expect(battle.State == BattleState.Victory && run.StageIndex == floor + 1, "Floor victory recorded once");
            RewardValidation.VerifyAndClaim(battle, run, goldBeforeVictory);
            if (floor < 2)
            {
                battle.StartNextBattle();
                Expect(run.MasterDeck.Cards.Count == 11 + floor, "Reward cards persist into next battle");
            }
        }
        Expect(run.IsRunComplete, "Final floor completes run");
        VerifyResultText("모든 층을 클리어했습니다!", false);
        battle.StartNextBattle();
        Expect(battle.State == BattleState.Victory && run.StageIndex == 3, "Final floor cannot be restarted");
    }

    private static void VerifyResultText(string expected, bool nextVisible)
    {
        var serialized = new SerializedObject(Find<PlayHUD>());
        TMP_Text result = (TMP_Text)serialized.FindProperty("resultText").objectReferenceValue;
        var next = (UnityEngine.UI.Button)serialized.FindProperty("nextBattleButton").objectReferenceValue;
        Expect(result != null && result.gameObject.activeInHierarchy && result.text == expected,
            "Result heading visible: " + expected);
        Expect(next.gameObject.activeInHierarchy == nextVisible, "Next-floor visibility");
        Transform screen = result.transform.parent;
        foreach (var button in screen.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            Expect(label != null && label.gameObject.activeSelf && label.enabled
                && !string.IsNullOrWhiteSpace(label.text), "Result button has visible label: " + button.name);
        }
    }

    private static void CapturePreview(string fileName = "battle-preview.png")
    {
        Camera camera = Camera.main;
        // 미리보기에서만 손패를 들어 올려 카드 설명까지 한눈에 확인합니다.
        RectTransform[] cards = fileName == "cards-preview.png"
            ? Hand().Select(c => c.GetComponent<RectTransform>()).ToArray()
            : Array.Empty<RectTransform>();
        Vector2[] positions = cards.Select(c => c.anchoredPosition).ToArray();
        foreach (RectTransform card in cards) card.anchoredPosition += new Vector2(0, 180);
        Canvas[] overlays = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        RenderTexture render = new RenderTexture(1920, 1080, 24);
        RenderTexture previous = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        Texture2D image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = render;
            foreach (Canvas canvas in overlays)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + 1;
            }
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None))
                text.ForceMeshUpdate();
            camera.Render();
            RenderTexture.active = render;
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            string path = System.IO.Path.GetFullPath(Application.dataPath + "/../../../" + fileName);
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            Debug.Log("BATTLE_PREVIEW_SAVED: " + path);
        }
        finally
        {
            for (int i = 0; i < cards.Length; i++) cards[i].anchoredPosition = positions[i];
            foreach (Canvas canvas in overlays) canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            camera.targetTexture = previousTarget;
            RenderTexture.active = previous;
            UnityEngine.Object.Destroy(image);
            UnityEngine.Object.Destroy(render);
        }
    }
}
