using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class RewardValidation
{
    private static int checks;
    private static void Expect(bool value, string message)
    {
        checks++;
        if (!value) throw new InvalidOperationException("Reward: " + message);
    }

    public static void VerifyModels()
    {
        checks = 0;
        var data = ScriptableObject.CreateInstance<BattleRewardData>();
        CardData[] cards = Enumerable.Range(0, 4).Select(i => ScriptableObject.CreateInstance<CardData>()).ToArray();
        try
        {
            data.MinGold = 10; data.MaxGold = 20;
            data.CardPool.AddRange(cards); data.CardPool.Add(cards[0]); data.CardPool.Add(null);
            Expect(data.IsValid(), "duplicate and null pool entries filtered");
            var combinations = new HashSet<string>();
            for (int seed = 0; seed < 16; seed++)
            {
                BattleRewardOffer offer = data.CreateOffer(new System.Random(seed));
                Expect(offer.Choices.Count == 3 && offer.Choices.Distinct().Count() == 3,
                    "three distinct choices");
                Expect(offer.Gold >= 10 && offer.Gold <= 20, "inclusive gold range");
                combinations.Add(string.Join(",", offer.Choices.Select(c => Array.IndexOf(cards, c))));
            }
            Expect(combinations.Count > 1, "selection varies across seeded rolls");
            var selected = data.CreateOffer(new System.Random(5));
            Expect(!selected.TryClaim(-1, out _) && !selected.TryClaim(3, out _) && !selected.IsClaimed,
                "invalid indices do not consume choice");
            Expect(selected.TryClaim(1, out CardData card) && card == selected.Choices[1], "one card selected");
            Expect(!selected.TryClaim(0, out _) && selected.SelectedCard == card, "claim cannot repeat");
            data.CardPool.Clear(); data.CardPool.AddRange(new[] { cards[0], cards[0], cards[1] });
            Expect(!data.IsValid(), "at least three unique definitions required");
            data.CardPool.Add(cards[2]);
            data.MinGold = int.MaxValue; data.MaxGold = int.MaxValue;
            Expect(data.CreateOffer(new System.Random(0)).Gold == int.MaxValue, "large gold range safe");
            data.MaxGold = 0;
            Expect(!data.IsValid(), "invalid gold range rejected");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(data);
            foreach (CardData card in cards) UnityEngine.Object.DestroyImmediate(card);
        }

        var go = new GameObject("RewardWalletValidation");
        var player = ScriptableObject.CreateInstance<PlayerData>();
        var starter = ScriptableObject.CreateInstance<StarterDeckData>();
        try
        {
            RunManager run = go.AddComponent<RunManager>();
            Expect(run.AddGold(10) == 0, "uninitialized run cannot receive gold");
            player.MaxHp = 80; player.StarterDeck = starter;
            var setup = new SerializedObject(run);
            setup.FindProperty("playerData").objectReferenceValue = player;
            setup.ApplyModifiedPropertiesWithoutUndo();
            run.StartNewRun();
            Expect(run.Gold == 0 && run.AddGold(-5) == 0, "new run starts at zero");
            Expect(run.AddGold(12) == 12 && run.Gold == 12, "wallet gains gold");
            Expect(run.AddGold(int.MaxValue) == int.MaxValue - 12 && run.Gold == int.MaxValue,
                "wallet saturates without overflow");
            Expect(run.AddGold(1) == 0, "full wallet does not overflow");
            run.StartNewRun();
            Expect(run.Gold == 0, "new run resets gold");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(starter);
        }
        Debug.Log($"REWARD_MODEL_VALIDATION_OK: {checks} assertions passed.");
    }

    public static void VerifyAndClaim(BattleManager battle, RunManager run, int previousGold)
    {
        int startChecks = checks;
        int floor = run.StageIndex;
        int deckCount = run.MasterDeck.Cards.Count;
        BattleRewardOffer offer = battle.RewardOffer;
        Expect(battle.State == BattleState.Victory && offer != null && battle.HasPendingCardReward,
            "victory has a pending offer");
        Expect(run.Gold == previousGold + battle.GoldAwarded && battle.GoldAwarded == offer.Gold,
            "gold is awarded once on victory");
        battle.CheckBattleEnd();
        Expect(run.Gold == previousGold + offer.Gold && ReferenceEquals(offer, battle.RewardOffer),
            "reopening victory does not reroll or pay again");
        Expect(offer.Choices.Count == 3 && offer.Choices.Distinct().Count() == 3, "three distinct cards displayed");
        battle.StartNextBattle();
        battle.StartBattle();
        Expect(battle.State == BattleState.Victory && run.StageIndex == floor, "pending choice blocks next battle");
        Expect(!battle.TrySelectRewardCard(-1) && !battle.TrySelectRewardCard(3), "invalid selection rejected");

        BattleRewardUI ui = UnityEngine.Object.FindObjectsByType<BattleRewardUI>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        var serialized = new SerializedObject(ui);
        var choicesRoot = (GameObject)serialized.FindProperty("choicesRoot").objectReferenceValue;
        var rewardCards = serialized.FindProperty("rewardCards");
        Expect(ui.gameObject.activeInHierarchy && choicesRoot.activeInHierarchy && rewardCards.arraySize == 3,
            "reward panel visible");
        for (int i = 0; i < 3; i++)
        {
            CardUI choice = (CardUI)rewardCards.GetArrayElementAtIndex(i).objectReferenceValue;
            Expect(choice.CardInstance.CardData == offer.Choices[i], "display matches offered card");
            choice.OnBeginDrag(null); // 보상 모드는 드래그를 시작하지 않습니다.
        }
        var next = (Button)new SerializedObject(UnityEngine.Object.FindObjectsByType<PlayHUD>(
            FindObjectsInactive.Include, FindObjectsSortMode.None).Single())
            .FindProperty("nextBattleButton").objectReferenceValue;
        Expect(!next.gameObject.activeSelf, "next-floor button hidden while selecting");
        CardData expected = offer.Choices[1];
        CardUI clicked = (CardUI)rewardCards.GetArrayElementAtIndex(1).objectReferenceValue;
        clicked.GetComponent<Button>().onClick.Invoke();
        Expect(!battle.HasPendingCardReward && offer.SelectedCard == expected, "click claims requested choice");
        Expect(run.MasterDeck.Cards.Count == deckCount + 1 && run.MasterDeck.Cards.Last().CardData == expected,
            "chosen card added to persistent deck");
        Expect(!battle.TrySelectRewardCard(0) && run.MasterDeck.Cards.Count == deckCount + 1,
            "double click cannot add another card");
        Expect(!choicesRoot.activeSelf && run.Gold == previousGold + offer.Gold, "selection closes without extra gold");
        Expect(next.gameObject.activeSelf == !run.IsRunComplete, "next floor unlocked only after choice");
        Debug.Log($"REWARD_SCENE_VALIDATION_OK: {checks - startChecks} assertions passed.");
    }
}
