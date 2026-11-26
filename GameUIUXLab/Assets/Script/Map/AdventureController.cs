using System;
using System.Linq;
using UnityEngine;

public class AdventureController : MonoBehaviour
{
    [SerializeField] private RunManager runManager;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private UIScreenFlowController screenFlow;
    public RunManager Run => runManager;
    public MapEventData CurrentEvent { get; private set; }
    public bool EventResolved { get; private set; }
    public string EventResult { get; private set; }
    public ShopInventory CurrentShop { get; private set; }
    public bool IsShopping => CurrentShop != null && !EventResolved;
    public string ShopMessage { get; private set; }
    private readonly System.Random treasureRandom = new();
    private bool shopTransaction;
    private int upgradeChoice = -1;
    public bool IsChoosingUpgrade => upgradeChoice >= 0;
    public bool HasUpgradeableCards => runManager.MasterDeck.Cards.Any(card => card.CanUpgrade);
    public CardInstance[] UpgradeCandidates => runManager.MasterDeck.Cards.Where(c => c.CanUpgrade).ToArray();
    public event Action OnEventChanged;

    public void BeginRun()
    {
        battleManager.AbortBattle();
        ResetEvent();
        int version = runManager.RunVersion;
        runManager.StartNewRun();
        if (runManager.RunVersion == version) return;
        if (runManager.HasMap) screenFlow.ShowMapForNewRun();
        else { screenFlow.ShowPlayScreen(); battleManager.StartBattle(); }
    }

    public bool SelectNode(int id)
    {
        if (!screenFlow.IsMap || !runManager.TryEnterMapNode(id)) return false;
        MapNode node = runManager.Map.ActiveNode;
        if (node.IsCombat) return EnterBattle();

        ResetEvent();
        CurrentEvent = node.Event;
        if (node.Shop != null) OpenShop(node.Shop);
        screenFlow.ShowEventScreen();
        OnEventChanged?.Invoke();
        return true;
    }

    private bool EnterBattle()
    {
        screenFlow.ShowPlayScreen();
        battleManager.StartBattle();
        if (battleManager.State == BattleState.PlayerTurn) return true;

        runManager.Map.CancelActiveNode();
        screenFlow.ShowMapScreen();
        return false;
    }

    private bool CanInteractWithVisit => screenFlow.IsEvent && !screenFlow.IsDeckOpen
        && runManager.Map?.ActiveNode != null && runManager.PlayerStatus is { IsDead: false };

    public bool CanChooseEvent(int index)
    {
        if (!CanInteractWithVisit || EventResolved || IsChoosingUpgrade || IsShopping || CurrentEvent == null
            || index < 0 || index >= CurrentEvent.Choices.Length) return false;
        MapEventChoice choice = CurrentEvent.Choices[index];
        return CanResolveChoice(choice) && (!choice.UpgradeCard || HasUpgradeableCards);
    }

    private bool CanResolveChoice(MapEventChoice choice) =>
        choice.GoldCost <= runManager.Gold && choice.HealthCost < runManager.PlayerStatus.CurrentHp
        && (choice.Relic == null || !runManager.Relics.Contains(choice.Relic));

    public bool ChooseEvent(int index)
    {
        if (!CanChooseEvent(index)) return false;
        MapEventChoice choice = CurrentEvent.Choices[index];
        if (choice.Shop != null)
        {
            if (!choice.Shop.IsValid()) return false;
            OpenShop(choice.Shop);
            OnEventChanged?.Invoke();
            return true;
        }
        if (choice.UpgradeCard)
        {
            upgradeChoice = index;
            OnEventChanged?.Invoke();
            return true;
        }
        return ResolveChoice(choice, null);
    }

    public void CancelUpgrade()
    {
        if (!screenFlow.IsEvent || !IsChoosingUpgrade || EventResolved) return;
        upgradeChoice = -1;
        OnEventChanged?.Invoke();
    }

    public bool ConfirmUpgrade(CardInstance card)
    {
        if (!screenFlow.IsEvent || !IsChoosingUpgrade || EventResolved || card == null
            || !runManager.MasterDeck.Cards.Contains(card) || !card.CanUpgrade
            || runManager.Map.ActiveNode == null || runManager.PlayerStatus.IsDead) return false;
        MapEventChoice choice = CurrentEvent.Choices[upgradeChoice];
        return CanResolveChoice(choice) && ResolveChoice(choice, card);
    }

    private bool ResolveChoice(MapEventChoice choice, CardInstance card)
    {
        EventResolved = true;
        int hp = runManager.PlayerStatus.CurrentHp;
        int gold = runManager.Gold;
        if (!runManager.TrySpendGold(choice.GoldCost)) { EventResolved = false; return false; }
        RelicData relic = choice.Relic;
        if (runManager.Map.ActiveNode.Type == MapNodeType.Treasure && CurrentEvent.TreasureRelicRewards != null)
            relic = CurrentEvent.TreasureRelicRewards.RollRelic(treasureRandom, runManager.Relics);
        string originalName = card?.Version.CardName;
        if (card != null) card.TryUpgrade();
        upgradeChoice = -1;
        runManager.PlayerStatus.TakeDamage(choice.HealthCost);
        runManager.AddGold(choice.GoldGain);
        int heal = (int)Math.Min(int.MaxValue, (long)choice.HealAmount
            + (long)runManager.PlayerStatus.MaxHp * choice.HealPercent / 100);
        runManager.PlayerStatus.Heal(heal);
        if (choice.Card != null) runManager.MasterDeck.AddCard(new CardInstance(choice.Card));
        bool acquiredRelic = runManager.TryAddRelic(relic);
        runManager.CompleteStage();
        EventResult = choice.Result + $"\n\n체력 {runManager.PlayerStatus.CurrentHp - hp:+0;-0;0} · 골드 {runManager.Gold - gold:+0;-0;0}"
            + (choice.Card != null ? $"\n{choice.Card.CardName} 카드를 획득했습니다." : "");
        if (acquiredRelic) EventResult += $"\n{relic.DisplayName} 유물을 획득했습니다.";
        if (card != null) EventResult += $"\n{originalName} → {card.Version.CardName} 강화 완료";
        OnEventChanged?.Invoke();
        return true;
    }

    public void ReturnFromEvent()
    {
        if (!screenFlow.IsEvent || !EventResolved) return;
        ResetEvent();
        screenFlow.ShowMapScreen();
    }

    public void ReturnToMap()
    {
        if (!runManager.HasMap || battleManager.State != BattleState.Victory
            || battleManager.HasPendingCardReward) return;
        battleManager.AbortBattle();
        screenFlow.ShowMapScreen();
    }

    private void ResetEvent()
    {
        CurrentEvent = null; EventResolved = false; EventResult = ""; upgradeChoice = -1;
        CurrentShop = null; ShopMessage = "";
    }

    private void OpenShop(ShopData data)
    {
        CurrentShop = new ShopInventory(data, new System.Random());
        ShopMessage = "카드를 선택해 구매할 수 있습니다.";
    }

    private bool CanUseShop => CanInteractWithVisit && IsShopping;
    public bool CanRemoveShopCard => CanUseShop && !CurrentShop.CardRemovalUsed
        && runManager.Gold >= runManager.CardRemovalPrice && runManager.MasterDeck.Cards.Count > 0;

    public bool BuyShopCard(int index)
    {
        if (!CanUseShop || shopTransaction || CurrentShop.IsSold(index)
            || runManager.Gold < CurrentShop.Price(index)) return false;
        shopTransaction = true;
        try
        {
            if (!runManager.TrySpendGold(CurrentShop.Price(index))) return false;
            CurrentShop.TryMarkPurchased(index);
            CardData card = CurrentShop.Cards[index];
            runManager.MasterDeck.AddCard(new CardInstance(card));
            ShopMessage = $"{card.CardName} 카드를 구매했습니다.";
            OnEventChanged?.Invoke();
            return true;
        }
        finally { shopTransaction = false; }
    }

    public bool RemoveShopCard(CardInstance card)
    {
        if (shopTransaction || !CanRemoveShopCard) return false;
        ShopInventory shop = CurrentShop;
        shopTransaction = true;
        try
        {
            int price = runManager.CardRemovalPrice;
            if (!runManager.TryRemoveCard(card)) return false;
            shop.MarkCardRemovalUsed();
            ShopMessage = $"{card.Version.CardName} 카드 제거 · {price}골드 사용";
            OnEventChanged?.Invoke();
            return true;
        }
        finally { shopTransaction = false; }
    }

    public void LeaveShop()
    {
        if (!CanUseShop || shopTransaction) return;
        EventResolved = true;
        EventResult = "상인에게 인사하고 길을 떠납니다.";
        runManager.CompleteStage();
        ResetEvent();
        screenFlow.ShowMapScreen();
    }
}

