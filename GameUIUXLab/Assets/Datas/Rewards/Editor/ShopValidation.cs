using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ShopValidation
{
    private static int checks;
    private static void Expect(bool value, string message)
    {
        checks++;
        if (!value) throw new InvalidOperationException("Shop: " + message);
    }
    public static void VerifyModels()
    {
        checks = 0;
        ShopData data = AssetDatabase.LoadAssetAtPath<ShopData>("Assets/Datas/Adventure/DefaultShop.asset");
        Expect(data != null && data.IsValid(), "configured shop valid");
        for (int seed = 0; seed < 20; seed++)
        {
            var inventory = new ShopInventory(data, new System.Random(seed));
            Expect(inventory.Cards.Count == 5 && inventory.Cards.Distinct().Count() == 5, "five distinct offers");
            Expect(inventory.Cards.All(c => !data.ExcludedCards.Contains(c)), "basic cards excluded");
            Expect(Enumerable.Range(0, 5).Count(i => inventory.Price(i) == data.CardPrice / 2) == 1,
                "exactly one half-price offer");
            Expect(inventory.TryMarkPurchased(0) && inventory.IsSold(0) && !inventory.TryMarkPurchased(0),
                "one purchase per slot");
            Expect(!inventory.TryMarkPurchased(-1) && !inventory.TryMarkPurchased(5), "invalid slots rejected");
        }
        var starter = ScriptableObject.CreateInstance<StarterDeckData>();
        starter.StartingCards.Add(new StartingCardEntry { CardData = data.CardPool[0], Count = 3 });
        var player = ScriptableObject.CreateInstance<PlayerData>(); player.MaxHp = 80; player.StarterDeck = starter;
        var go = new GameObject("ShopWalletValidation");
        try
        {
            var run = go.AddComponent<RunManager>();
            var serialized = new SerializedObject(run);
            serialized.FindProperty("playerData").objectReferenceValue = player;
            serialized.ApplyModifiedPropertiesWithoutUndo(); run.StartNewRun();
            var first = run.MasterDeck.Cards[0]; var second = run.MasterDeck.Cards[1];
            Expect(run.CardRemovalPrice == 50 && !run.TryRemoveCard(first) && run.CardsRemoved == 0,
                "insufficient funds do not remove card or increase price");
            run.AddGold(500);
            Expect(!run.TryRemoveCard(new CardInstance(first.CardData)) && run.Gold == 500, "foreign copy rejected");
            Expect(run.TryRemoveCard(first) && run.Gold == 450 && run.CardRemovalPrice == 100
                && run.MasterDeck.Cards.Contains(second), "first removal costs fifty and preserves other copies");
            Expect(!run.TryRemoveCard(first) && run.Gold == 450 && run.CardRemovalPrice == 100, "cannot remove same card twice");
            Expect(run.TryRemoveCard(second) && run.Gold == 350 && run.CardRemovalPrice == 150, "second removal costs one hundred");
            run.StartNewRun();
            Expect(run.CardRemovalPrice == 50 && run.CardsRemoved == 0 && run.Gold == 0, "new run resets removal price");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(starter);
        }
        Debug.Log($"SHOP_MODEL_VALIDATION_OK: {checks} assertions passed.");
    }
}
