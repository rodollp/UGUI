using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UpgradeValidation
{
    private static int checks;
    private static void Expect(bool condition, string message)
    {
        checks++; if (!condition) throw new InvalidOperationException("Upgrade: " + message);
    }
    [MenuItem("Tools/Cards/Validate Card Assets")]
    public static void VerifyModels()
    {
        checks = 0;
        string[] paths = { "CardDis/ATK", "CardDis/BLO", "Rewards/Bash", "Rewards/Intimidate", "Rewards/IronGuard",
            "Rewards/WildShot", "Rewards/WhirlSlash", "Rewards/StrengthTraining", "Rewards/BalanceTraining" };
        foreach (string path in paths)
        {
            string assetPath = "Assets/Datas/" + path + ".asset";
            CardData data = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            Expect(data != null && data.HasUpgrade && data.Base.Effects.Count > 0 && data.Upgrade.Effects.Count > 0
                && data.Base.Effects.Concat(data.Upgrade.Effects).All(effect =>
                effect != null && AssetDatabase.IsSubAsset(effect) && AssetDatabase.GetAssetPath(effect) == assetPath),
                "both versions load their effects from the same card file");
            var card = new CardInstance(data); var sibling = new CardInstance(data);
            CardInstance preview = card.CopyForPreview(true);
            Expect(preview.IsUpgraded && preview.Version == data.Upgrade && card.Version == data.Base,
                "preview reads the upgrade without editing the owned card");
            Expect(card.TryUpgrade() && card.CardData == data && card.Version == data.Upgrade
                && card.Version.CardName.EndsWith("+"), "upgrade selects the second version of the same asset");
            Expect(!card.TryUpgrade() && sibling.Version == data.Base && sibling.CanUpgrade, "one copy upgrades only once");
            Expect(card.CopyForBattle().IsUpgraded && card.CopyForBattle().CardData == card.CardData, "battle copy retains upgrade");
            var player = new PlayerStatus(100); var enemy = new EnemyStatus(100);
            foreach (var effect in card.Version.Effects) effect.Execute(new CardEffectContext(player, enemy));
            if (path.EndsWith("ATK")) Expect(enemy.CurrentHp == 91, "strike plus deals nine damage");
            if (path.EndsWith("BLO")) Expect(player.CurrentBlock == 8, "defend plus gives eight block");
            if (path.EndsWith("Bash"))
            {
                Expect(enemy.CurrentHp == 88 && enemy.GetDebuffTurns(DebuffType.Vulnerable) == 3, "bash plus upgrades both effects in order");
                player.AddAttackPower(3);
                Expect(sibling.Version.GetDescription(player).Contains("피해를 11")
                    && card.Version.GetDescription(player, enemy).Contains("피해를 22"),
                    "bash previews the current version with strength and target vulnerability");
            }
            if (path.EndsWith("Intimidate")) Expect(enemy.GetDebuffTurns(DebuffType.Weak) == 3, "intimidate plus weak lasts three turns");
            if (path.EndsWith("IronGuard")) Expect(player.CurrentBlock == 11, "iron guard plus gives eleven block");
        }
        var invalid = ScriptableObject.CreateInstance<MapEventData>();
        invalid.Choices = new[] { new MapEventChoice { Label = "강화", UpgradeCard = true } };
        Expect(!invalid.IsValid(), "upgrade-only event cannot trap a fully upgraded deck");
        UnityEngine.Object.DestroyImmediate(invalid);
        Debug.Log($"UPGRADE_MODEL_VALIDATION_OK: {checks} assertions passed.");
    }
    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
    private static Button Button(CardUpgradeUI ui, string field) => (Button)new SerializedObject(ui).FindProperty(field).objectReferenceValue;
    private static void Win()
    {
        BattleManager battle = Find<BattleManager>();
        foreach (var enemy in battle.Enemies.ToArray()) enemy.Status.TakeDamage(int.MaxValue);
        battle.TrySelectRewardCard(0); battle.StartNextBattle();
    }
    public static IEnumerator VerifyScene(RunMapData config)
    {
        checks = 0;
        var adventure = Find<AdventureController>(); var run = adventure.Run;
        var defaultMap = AssetDatabase.LoadAssetAtPath<RunMapData>("Assets/Datas/Adventure/DefaultMap.asset");
        var smith = UnityEngine.Object.Instantiate(MapSetup.FindEvent(defaultMap, "Smith"));
        smith.Choices[0].GoldCost = 10;
        config.Events = new[] { smith };
        adventure.BeginRun(); run.AddGold(100);
        var node = run.Map.Nodes.Single(n => n.Row == 1 && n.Type == MapNodeType.Event);
        adventure.SelectNode(run.Map.Nodes.First(n => n.Row == 0 && n.Column == node.Column).Id); Win();
        adventure.SelectNode(node.Id);
        var target = run.MasterDeck.Cards[0]; var sibling = run.MasterDeck.Cards[1]; CardData original = target.CardData;
        int gold = run.Gold, hp = run.PlayerStatus.CurrentHp;
        Expect(adventure.ChooseEvent(0) && adventure.IsChoosingUpgrade && !node.Completed && run.Gold == gold,
            "opening picker does not resolve or charge event");
        yield return null;
        var ui = Find<CardUpgradeUI>();
        Button(ui, "cancel").onClick.Invoke();
        Expect(!adventure.IsChoosingUpgrade && !adventure.EventResolved && run.Gold == gold && target.CardData == original,
            "cancel returns to choices without costs or upgrades");
        adventure.ChooseEvent(0);
        Expect(!adventure.ConfirmUpgrade(new CardInstance(original)), "foreign card rejected");
        Expect(ui.CandidateViews.Count == adventure.UpgradeCandidates.Length && ui.CandidateViews.Count > 3,
            "picker displays every eligible card without pagination");
        Expect(ui.CandidateViews.All(v => v.CardInstance.CanUpgrade && !v.CardInstance.IsUpgraded),
            "list shows current unupgraded cards");
        var cardView = ui.CandidateViews[0];
        cardView.GetComponent<Button>().onClick.Invoke();
        Expect(ui.IsInspectingCard && Button(ui, "confirm").interactable && !target.IsUpgraded,
            "card click opens enlarged preview without upgrading");
        var detail = ui.GetComponentsInChildren<CardUI>(true)
            .Single(v => v.transform.parent.name == "EnlargedCard");
        var toggle = ui.GetComponentInChildren<Toggle>(true);
        Expect(toggle.isOn && detail.CardInstance.CardData == original && detail.CardInstance.IsUpgraded,
            "detail initially shows upgraded card");
        toggle.isOn = false;
        Expect(!detail.CardInstance.IsUpgraded && detail.CardInstance.CardData == original
            && target.CardData == original && run.Gold == gold,
            "unchecking shows current card without mutation or payment");
        toggle.isOn = true;
        Expect(detail.CardInstance.CardData == original && detail.CardInstance.IsUpgraded && !target.IsUpgraded,
            "rechecking restores upgrade preview only");
        ui.HandleBack();
        Expect(!ui.IsInspectingCard && adventure.IsChoosingUpgrade && !target.IsUpgraded && run.Gold == gold,
            "back closes detail without cancelling the picker or paying costs");
        cardView.GetComponent<Button>().onClick.Invoke();
        yield return null;
        if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
            typeof(BattleValidation).GetMethod("CapturePreview", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { "upgrade-preview.png" });
        Button(ui, "confirm").onClick.Invoke(); Button(ui, "confirm").onClick.Invoke();
        Expect(target.IsUpgraded && !sibling.IsUpgraded && sibling.CardData == original
            && run.Gold == gold - 10 && node.Completed,
            "confirm upgrades only selected copy and charges once");
        Expect(!adventure.ConfirmUpgrade(target) && run.PlayerStatus.CurrentHp == hp, "resolved event cannot repeat");
        adventure.ReturnFromEvent();
        var deck = new BattleDeck(run.MasterDeck); bool found = false;
        for (int i = 0; i < run.MasterDeck.Cards.Count; i++)
        {
            CardInstance drawn = deck.Draw();
            if (drawn.CardData == target.CardData && drawn.IsUpgraded) found = true;
            deck.PlayCard(drawn);
        }
        Expect(found, "next battle receives upgraded master-deck card");
        // The pre-boss rest is guaranteed regardless of optional earlier camps.
        while (run.Map.LastCompletedNode.Row < config.Rows - 3)
        {
            var next = run.Map.Nodes.First(n => run.Map.CanEnter(n.Id)); adventure.SelectNode(next.Id);
            if (next.IsCombat) Win();
            else { adventure.ChooseEvent(adventure.CurrentEvent.Choices.Length - 1); adventure.ReturnFromEvent(); }
        }
        var rest = run.Map.Nodes.First(n => run.Map.CanEnter(n.Id)); adventure.SelectNode(rest.Id);
        run.PlayerStatus.ResetBlock(); run.PlayerStatus.TakeDamage(20);
        hp = run.PlayerStatus.CurrentHp;
        int choice = Array.FindIndex(adventure.CurrentEvent.Choices, c => c.UpgradeCard);
        Expect(rest.Type == MapNodeType.Rest && choice >= 0 && adventure.ChooseEvent(choice), "rest offers upgrade choice");
        Expect(adventure.ConfirmUpgrade(adventure.UpgradeCandidates[0]) && run.PlayerStatus.CurrentHp == hp && rest.Completed,
            "rest upgrade resolves node without healing");
        adventure.ReturnFromEvent();
        adventure.BeginRun();
        Expect(run.MasterDeck.Cards.All(c => !c.IsUpgraded), "new run resets upgrades without changing source assets");
        node = run.Map.Nodes.Single(n => n.Row == 1 && n.Type == MapNodeType.Event);
        adventure.SelectNode(run.Map.Nodes.First(n => n.Row == 0 && n.Column == node.Column).Id); Win();
        foreach (var card in run.MasterDeck.Cards) card.TryUpgrade();
        adventure.SelectNode(node.Id);
        Expect(!adventure.CanChooseEvent(0) && !adventure.ChooseEvent(0) && adventure.ChooseEvent(1),
            "fully upgraded deck can leave event with upgrade disabled");
        adventure.ReturnFromEvent(); UnityEngine.Object.Destroy(smith);
        Debug.Log($"UPGRADE_SCENE_VALIDATION_OK: {checks} assertions passed.");
    }
}
