using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BattleState { None, Preparing, PlayerTurn, EnemyTurn, Victory, Defeat }

public class BattleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RunManager runManager;
    [SerializeField] private Player player;
    [SerializeField] private CardManager cardManager;
    [SerializeField] private UIScreenFlowController screenFlowController;
    [SerializeField] private Transform enemyRoot;
    [Header("Encounter (used when no stage sequence is assigned)")]
    [SerializeField] private EncounterData encounter;
    [SerializeField, Min(0)] private float enemyActionDelay = 0.6f;

    private readonly List<Enemy> enemies = new();
    private PlayerStatus playerStatus;
    public IReadOnlyList<Enemy> Enemies => enemies;
    public BattleState State { get; private set; }
    public int Turn { get; private set; }
    public string EncounterName { get; private set; }
    public int BattleFloor { get; private set; }
    public bool CanPlayCards => State == BattleState.PlayerTurn
        && (screenFlowController == null || screenFlowController.IsPlaying) && Time.timeScale > 0;
    public event Action OnBattleChanged;
    public event Action<bool> OnBattleEnded;

    public void StartBattle()
    {
        if (State == BattleState.Preparing || State == BattleState.PlayerTurn || State == BattleState.EnemyTurn)
            return;
        EncounterData selected = runManager != null && runManager.HasStageSequence
            ? runManager.CurrentEncounter : encounter;
        if (runManager == null || runManager.PlayerStatus == null || runManager.PlayerStatus.IsDead
            || runManager.MasterDeck == null || player == null || cardManager == null
            || (runManager.HasStageSequence && runManager.IsRunComplete)
            || selected == null || !selected.IsValid())
        {
            Debug.LogError("전투를 시작할 수 없습니다. 플레이어, 덱, 적 조합 및 참조를 확인하세요.");
            return;
        }

        AbortBattle();
        State = BattleState.Preparing;
        BattleFloor = runManager.Floor;
        EncounterName = selected.EncounterName;
        playerStatus = runManager.PlayerStatus;
        playerStatus.ResetCombatState();
        playerStatus.OnDead += HandlePlayerDead;
        playerStatus.OnStatusChanged += NotifyChanged;
        playerStatus.OnEnergyChanged += NotifyChanged;
        player.Initialize(playerStatus);

        foreach (EnemySpawnEntry entry in selected.Enemies)
        {
            Enemy enemy = Instantiate(entry.Prefab, enemyRoot != null ? enemyRoot : transform);
            enemy.transform.localPosition = entry.LocalPosition;
            enemy.Initialize(entry.Data);
            enemy.name = entry.Data.EnemyName;
            enemies.Add(enemy);
            enemy.Status.OnDead += HandleEnemyDead;
            enemy.Status.OnStatusChanged += NotifyChanged;
            enemy.OnIntentChanged += NotifyChanged;
            enemy.PlanNextAction();
        }

        cardManager.InitializeBattle(this);
        Turn = 0;
        BeginPlayerTurn();
    }

    public bool IsValidTarget(Enemy target)
    {
        return target != null && enemies.Contains(target)
            && target.Status != null && !target.Status.IsDead;
    }

    public void EndPlayerTurn()
    {
        if (!CanPlayCards || cardManager.IsResolvingCard) return;
        State = BattleState.EnemyTurn; // 버튼 연타를 코루틴 시작 전에 차단합니다.
        cardManager.DiscardPlayerHand();
        NotifyChanged();
        StartCoroutine(EnemyTurn());
    }

    private IEnumerator EnemyTurn()
    {
        foreach (Enemy enemy in enemies)
        {
            if (!IsValidTarget(enemy)) continue;
            yield return new WaitForSeconds(enemyActionDelay);
            if (State != BattleState.EnemyTurn) yield break;
            enemy.ExecuteIntent(playerStatus);
            if (State != BattleState.EnemyTurn) yield break;
        }
        // 플레이어와 모든 적의 행동을 하나의 라운드로 처리합니다.
        playerStatus.EndRoundDebuffs();
        foreach (Enemy enemy in enemies)
            if (IsValidTarget(enemy)) enemy.Status.EndRoundDebuffs();
        foreach (Enemy enemy in enemies)
            if (IsValidTarget(enemy)) enemy.PlanNextAction();
        BeginPlayerTurn();
    }

    private void BeginPlayerTurn()
    {
        if (CheckBattleEnd()) return;
        Turn++;
        playerStatus.ResetBlock();
        playerStatus.StartTurn();
        State = BattleState.PlayerTurn;
        cardManager.BeginPlayerTurn();
        NotifyChanged();
    }

    // 다중 효과 카드는 모든 효과를 처리한 뒤 승리를 판정합니다.
    private void HandleEnemyDead()
    {
        foreach (Enemy enemy in enemies)
            if (enemy.Status.IsDead) enemy.gameObject.SetActive(false);
        if (!cardManager.IsResolvingCard) CheckBattleEnd();
        NotifyChanged();
    }

    private void HandlePlayerDead() => FinishBattle(false);

    public bool CheckBattleEnd()
    {
        if (State == BattleState.None || State == BattleState.Victory || State == BattleState.Defeat)
            return true;
        if (playerStatus.IsDead) { FinishBattle(false); return true; }
        foreach (Enemy enemy in enemies)
            if (enemy != null && !enemy.Status.IsDead) return false;
        FinishBattle(true);
        return true;
    }

    private void FinishBattle(bool won)
    {
        if (State == BattleState.None || State == BattleState.Victory || State == BattleState.Defeat)
            return;
        State = won ? BattleState.Victory : BattleState.Defeat;
        cardManager.EndBattle();
        if (won) runManager.CompleteStage();
        NotifyChanged();
        OnBattleEnded?.Invoke(won);
        if (screenFlowController != null) screenFlowController.ShowResultScreen();
    }

    public void StartNextBattle()
    {
        if (State != BattleState.Victory || !runManager.HasStageSequence || runManager.IsRunComplete)
            return;
        if (screenFlowController != null) screenFlowController.ShowPlayScreen();
        StartBattle();
    }

    public void AbortBattle()
    {
        StopAllCoroutines();
        State = BattleState.None;
        if (cardManager != null) cardManager.EndBattle();
        if (playerStatus != null)
        {
            playerStatus.OnDead -= HandlePlayerDead;
            playerStatus.OnStatusChanged -= NotifyChanged;
            playerStatus.OnEnergyChanged -= NotifyChanged;
            playerStatus = null;
        }
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null) continue;
            enemy.Status.OnDead -= HandleEnemyDead;
            enemy.Status.OnStatusChanged -= NotifyChanged;
            enemy.OnIntentChanged -= NotifyChanged;
            enemy.gameObject.SetActive(false);
            Destroy(enemy.gameObject);
        }
        enemies.Clear();
        NotifyChanged();
    }

    private void NotifyChanged() => OnBattleChanged?.Invoke();
    private void OnDestroy() => AbortBattle();
}


