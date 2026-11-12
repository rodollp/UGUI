using System;
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
        if (!screenFlow.IsEvent || EventResolved || CurrentEvent == null || !runManager.HasMap
            || runManager.Map.ActiveNode == null || runManager.PlayerStatus == null || runManager.PlayerStatus.IsDead
            || index < 0 || index >= CurrentEvent.Choices.Length) return false;
        MapEventChoice choice = CurrentEvent.Choices[index];
        return choice.GoldCost <= runManager.Gold && choice.HealthCost < runManager.PlayerStatus.CurrentHp;
    }

    public bool ChooseEvent(int index)
    {
        if (!CanChooseEvent(index)) return false;
        MapEventChoice choice = CurrentEvent.Choices[index];
        EventResolved = true;
        int hp = runManager.PlayerStatus.CurrentHp;
        int gold = runManager.Gold;
        if (!runManager.TrySpendGold(choice.GoldCost)) { EventResolved = false; return false; }
        runManager.PlayerStatus.TakeDamage(choice.HealthCost);
        runManager.AddGold(choice.GoldGain);
        int heal = (int)Math.Min(int.MaxValue, (long)choice.HealAmount
            + (long)runManager.PlayerStatus.MaxHp * choice.HealPercent / 100);
        runManager.PlayerStatus.Heal(heal);
        if (choice.Card != null) runManager.MasterDeck.AddCard(new CardInstance(choice.Card));
        runManager.CompleteStage();
        EventResult = choice.Result + $"\n\n체력 {runManager.PlayerStatus.CurrentHp - hp:+0;-0;0} · 골드 {runManager.Gold - gold:+0;-0;0}"
            + (choice.Card != null ? $"\n{choice.Card.CardName} 카드를 획득했습니다." : "");
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
        CurrentEvent = null; EventResolved = false; EventResult = "";
    }
}

