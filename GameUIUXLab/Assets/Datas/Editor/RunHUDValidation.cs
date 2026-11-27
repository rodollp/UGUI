using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class RunHUDValidation
{
    private const string Active = "RunHUDValidation.Active";
    private static IEnumerator routine;
    private static double deadline;
    private static int checks;
    static RunHUDValidation()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Active, false)) return;
            checks = 0; deadline = EditorApplication.timeSinceStartup + 90;
            routine = Verify(); EditorApplication.update += Tick;
        };
    }

    [MenuItem("Tools/UI/Validate Common Run HUD")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorSceneManager.OpenScene("Assets/Scenes/CardGame.unity");
        SessionState.SetBool(Active, true); EditorApplication.isPlaying = true;
    }

    private static void Tick()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("HUD validation timed out");
            if (routine.MoveNext()) return;
            Debug.Log($"RUN_HUD_VALIDATION_OK: {checks} assertions passed");
            Finish();
        }
        catch (Exception error) { Debug.LogException(error); Finish(); }
    }
    private static void Finish()
    {
        EditorApplication.update -= Tick; SessionState.SetBool(Active, false); EditorApplication.isPlaying = false;
    }

    private static IEnumerator Verify()
    {
        yield return null;
        RunHUD hud = Find<RunHUD>(); RunManager run = Find<RunManager>();
        UIScreenFlowController flow = Find<UIScreenFlowController>();
        AdventureController adventure = Find<AdventureController>(); BattleManager battle = Find<BattleManager>();
        DeckViewUI deck = Find<DeckViewUI>();
        Transform originalParent = hud.transform.parent;
        Expect(!hud.gameObject.activeInHierarchy, "title hides run HUD");
        adventure.BeginRun(); yield return null; hud.Refresh();
        Expect(flow.IsMap && hud.gameObject.activeInHierarchy, "new run map keeps HUD");
        Expect(Find<RunMapUI>().transform.Find("RelicBar") == null, "map no longer creates duplicate relic bar");
        int hp = run.PlayerStatus.CurrentHp;
        run.PlayerStatus.TakeDamage(7); run.AddGold(21); hud.Refresh();
        Expect(Ref<TMP_Text>(hud, "healthText").text == $"{hp - 7} / {run.PlayerStatus.MaxHp}", "map damage refreshes health");
        Expect(Ref<TMP_Text>(hud, "goldText").text == run.Gold.ToString(), "gold refreshes on map");
        run.PlayerStatus.Heal(7);
        int count = run.Relics.Count;
        RelicData extra = AssetDatabase.FindAssets("t:RelicData").Select(g => AssetDatabase.LoadAssetAtPath<RelicData>(AssetDatabase.GUIDToAssetPath(g)))
            .First(r => !run.OwnsRelic(r));
        run.TryAddRelic(extra); hud.Refresh();
        Expect(hud.GetComponentsInChildren<RunRelicItem>().Length == count + 1, "new relic appears without screen transition");
        var item = hud.GetComponentsInChildren<RunRelicItem>().Last();
        item.OnPointerEnter(new PointerEventData(EventSystem.current));
        Expect(Ref<RectTransform>(hud, "tooltip").gameObject.activeSelf, "relic tooltip opens on map");
        item.OnPointerExit(new PointerEventData(EventSystem.current));
        Expect(!Ref<RectTransform>(hud, "tooltip").gameObject.activeSelf, "relic tooltip closes");
        Click(Ref<Button>(hud, "deckButton")); yield return null;
        Expect(deck.IsOpen && hud.gameObject.activeInHierarchy, "deck overlay retains HUD");
        Click(Ref<Button>(hud, "deckButton"));
        Expect(!deck.IsOpen, "HUD deck button toggles deck closed");
        Click(Ref<Button>(hud, "settingsButton"));
        Expect(flow.IsPaused && hud.gameObject.activeInHierarchy && Time.timeScale == 0, "settings pause map without hiding HUD");
        Click(Ref<Button>(hud, "settingsButton"));
        Expect(flow.IsMap && Time.timeScale == 1, "settings return to map");
        MapNode story = run.Map.Nodes.First(n => n.Row == 1 && n.Type == MapNodeType.Event);
        MapNode entry = run.Map.Nodes.First(n => n.Row == 0 && n.Column == story.Column);
        Expect(adventure.SelectNode(entry.Id), "enter battle through a real node");
        yield return null; hud.Refresh();
        Expect(hud.gameObject.activeInHierarchy && flow.IsPlaying, "battle retains same HUD");
        Click(Ref<Button>(hud, "mapButton")); yield return null;
        Expect(flow.IsMapPreviewOpen && hud.gameObject.activeInHierarchy, "minimap preview retains HUD");
        Click(Ref<Button>(hud, "mapButton"));
        foreach (Enemy enemy in battle.Enemies.ToArray()) enemy.Status.TakeDamage(int.MaxValue);
        yield return null; hud.Refresh();
        Expect(flow.IsResult && battle.HasPendingCardReward && hud.gameObject.activeInHierarchy, "victory rewards retain HUD");
        var reward = Find<BattleRewardUI>();
        var cardReward = reward.transform.Find("RewardSummary/CardReward");
        // Reward rows may use a different display name; invoke the existing private selector through its button.
        if (cardReward == null)
            cardReward = reward.GetComponentsInChildren<Button>(true).First(b => b.name.Contains("CardReward")).transform;
        cardReward.GetComponent<Button>().onClick.Invoke(); yield return null;
        Expect(Ref<GameObject>(reward, "choicesRoot").activeSelf && hud.gameObject.activeInHierarchy, "card selection retains HUD");
        hud.Refresh(); Click(Ref<Button>(hud, "settingsButton"));
        Expect(flow.IsPaused && hud.gameObject.activeInHierarchy, "settings work during rewards");
        Click(Ref<Button>(hud, "settingsButton"));
        Expect(flow.IsResult && battle.HasPendingCardReward, "resume preserves unclaimed rewards");
        Expect(battle.TrySelectRewardCard(0), "card reward selection still works");
        battle.TryClaimGoldReward(); battle.TryClaimRelicReward(); battle.StartNextBattle();
        yield return null;
        Expect(adventure.SelectNode(story.Id), "enter event through connected node");
        yield return null; hud.Refresh();
        Expect(flow.IsEvent && hud.gameObject.activeInHierarchy, "event retains HUD");
        Expect(Ref<TMP_Text>(hud, "nodeText").text.Contains("이벤트"), "HUD shows current event node");
        Expect(hud.transform.parent == originalParent && Find<RunHUD>() == hud, "all screens reuse one HUD instance");
        flow.ShowTitleScreen();
        Expect(!hud.gameObject.activeInHierarchy, "return to title hides HUD");
        adventure.BeginRun(); yield return null; hud.Refresh();
        Expect(hud.GetComponentsInChildren<RunRelicItem>().Length == run.Relics.Count, "new run removes previous relic entries");
    }

    private static T Ref<T>(Object target, string field) where T : Object =>
        (T)new SerializedObject(target).FindProperty(field).objectReferenceValue;
    private static void Click(Button button)
    {
        Find<RunHUD>().Refresh();
        Canvas.ForceUpdateCanvases();
        Expect(button.IsInteractable() && button.targetGraphic.raycastTarget, button.name + " accepts pointer input");
        var rect = (RectTransform)button.transform;
        var pointer = new PointerEventData(EventSystem.current)
        {
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)),
            button = PointerEventData.InputButton.Left
        };
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Expect(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button,
            button.name + " is the frontmost pointer target");
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
    private static T Find<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
    private static void Expect(bool value, string message)
    {
        checks++; if (!value) throw new InvalidOperationException("Run HUD: " + message);
    }
}
