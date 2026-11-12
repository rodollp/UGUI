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
    private int upgradeChoice = -1;
    public bool IsChoosingUpgrade => upgradeChoice >= 0;
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
        if (node.IsCombat)
        {
            screenFlow.ShowPlayScreen();
            battleManager.StartBattle();
            if (battleManager.State != BattleState.PlayerTurn)
            {
                runManager.Map.CancelActiveNode();
                screenFlow.ShowMapScreen();
                return false;
            }
        }
        else
        {
            ResetEvent();
            CurrentEvent = node.Event;
            EventResolved = false;
            EventResult = "";
            screenFlow.ShowEventScreen();
            OnEventChanged?.Invoke();
        }
        return true;
    }

    public bool CanChooseEvent(int index)
    {
        if (!screenFlow.IsEvent || EventResolved || IsChoosingUpgrade || CurrentEvent == null || !runManager.HasMap
            || runManager.Map.ActiveNode == null || runManager.PlayerStatus == null || runManager.PlayerStatus.IsDead
            || index < 0 || index >= CurrentEvent.Choices.Length) return false;
        MapEventChoice choice = CurrentEvent.Choices[index];
        return choice.GoldCost <= runManager.Gold && choice.HealthCost < runManager.PlayerStatus.CurrentHp
            && (!choice.UpgradeCard || UpgradeCandidates.Length > 0);
    }

    public bool ChooseEvent(int index)
    {
        if (!CanChooseEvent(index)) return false;
        MapEventChoice choice = CurrentEvent.Choices[index];
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
        if (choice.GoldCost > runManager.Gold || choice.HealthCost >= runManager.PlayerStatus.CurrentHp) return false;
        return ResolveChoice(choice, card);
    }

    private bool ResolveChoice(MapEventChoice choice, CardInstance card)
    {
        EventResolved = true;
        int hp = runManager.PlayerStatus.CurrentHp;
        int gold = runManager.Gold;
        if (!runManager.TrySpendGold(choice.GoldCost)) { EventResolved = false; return false; }
        string originalName = card?.CardData.CardName;
        if (card != null) card.TryUpgrade();
        upgradeChoice = -1;
        runManager.PlayerStatus.TakeDamage(choice.HealthCost);
        runManager.AddGold(choice.GoldGain);
        int heal = (int)Math.Min(int.MaxValue, (long)choice.HealAmount
            + (long)runManager.PlayerStatus.MaxHp * choice.HealPercent / 100);
        runManager.PlayerStatus.Heal(heal);
        if (choice.Card != null) runManager.MasterDeck.AddCard(new CardInstance(choice.Card));
        runManager.CompleteStage();
        EventResult = choice.Result + $"\n\n체력 {runManager.PlayerStatus.CurrentHp - hp:+0;-0;0} · 골드 {runManager.Gold - gold:+0;-0;0}"
            + (choice.Card != null ? $"\n{choice.Card.CardName} 카드를 획득했습니다." : "");
        if (card != null) EventResult += $"\n{originalName} → {card.CardData.CardName} 강화 완료";
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
    }
}

