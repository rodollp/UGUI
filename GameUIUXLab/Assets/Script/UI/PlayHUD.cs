using System.Text;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI intentText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button nextBattleButton;
    [SerializeField] private RunTimer runTimer;
    [SerializeField] private UIScreenFlowController screenFlowController;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private RunManager runManager;
    private bool uiDirty;
    private int displayedSecond = -1;
    private int displayedGold = int.MinValue;
    private (int, bool) displayedTurn = (-1, false);
    private (int, int) displayedEnergy = (int.MinValue, int.MinValue);
    private (int, string) displayedStage = (-1, null);
    private readonly StringBuilder intentBuilder = new StringBuilder();
    private readonly List<(Enemy, int, int, int, int, int, int, int, EnemyAction, string)> enemyDisplay =
        new List<(Enemy, int, int, int, int, int, int, int, EnemyAction, string)>();
    private int displayedPlayerVulnerable = -1;
    private bool intentInitialized;
    private TMP_Text nextBattleLabel;

    private void OnEnable()
    {
        if (battleManager != null) battleManager.OnBattleChanged += MarkDirty;
        if (runManager != null) runManager.OnGoldChanged += MarkDirty;
        displayedSecond = -1;
        if (nextBattleButton != null) nextBattleLabel = nextBattleButton.GetComponentInChildren<TMP_Text>(true);
        RefreshUI();
    }

    private void OnDisable()
    {
        if (battleManager != null) battleManager.OnBattleChanged -= MarkDirty;
        if (runManager != null) runManager.OnGoldChanged -= MarkDirty;
    }

    private void Update()
    {
        if (timeText != null && runTimer != null)
        {
            int second = (int)runTimer.PlayTime;
            if (displayedSecond != second)
            {
                displayedSecond = second;
                timeText.text = $"Time : {System.TimeSpan.FromSeconds(second):mm\\:ss}";
            }
        }
        if (endTurnButton != null)
        {
            bool canEnd = battleManager != null && battleManager.CanPlayCards;
            if (endTurnButton.interactable != canEnd) endTurnButton.interactable = canEnd;
        }
    }

    private void MarkDirty()
    {
        uiDirty = true;
        // Result/next-action controls must be correct immediately, including reward clicks.
        RefreshControls();
    }

    private void LateUpdate()
    {
        if (uiDirty) RefreshUI();
    }

    public void ResetHUD() => RefreshUI();
    public void NextTurn() => RefreshUI();

    private void RefreshUI()
    {
        uiDirty = false;
        if (battleManager == null || runManager == null) return;
        if (goldText != null && displayedGold != runManager.Gold)
        {
            displayedGold = runManager.Gold;
            goldText.text = $"골드 {displayedGold}";
        }
        var turn = (battleManager.Turn, battleManager.State == BattleState.EnemyTurn);
        if (turnText != null && !displayedTurn.Equals(turn))
        {
            displayedTurn = turn;
            turnText.text = $"Turn : {battleManager.Turn} / " +
                (battleManager.State == BattleState.EnemyTurn ? "적 턴" : "플레이어 턴");
        }
        var energy = (runManager.PlayerStatus?.CurrentEnergy ?? -1, runManager.PlayerStatus?.MaxEnergy ?? -1);
        if (energyText != null && !displayedEnergy.Equals(energy))
        {
            displayedEnergy = energy;
            energyText.text = runManager.PlayerStatus == null ? "" :
                $"에너지 {runManager.PlayerStatus.CurrentEnergy} / {runManager.PlayerStatus.MaxEnergy}";
        }
        var stage = (battleManager.BattleFloor, battleManager.EncounterName);
        if (stageText != null && !displayedStage.Equals(stage))
        {
            displayedStage = stage;
            stageText.text = $"{battleManager.BattleFloor}층 · {battleManager.EncounterName}";
        }
        if (intentText != null && IntentChanged())
        {
            intentBuilder.Clear();
            foreach (Enemy enemy in battleManager.Enemies)
                if (enemy != null && enemy.Status != null && !enemy.Status.IsDead)
                {
                    string debuffs = enemy.Status.GetDebuffDescription();
                    intentBuilder.AppendLine($"{enemy.Data.EnemyName}  HP {enemy.Status.CurrentHp}/{enemy.Status.MaxHp}  " +
                        $"방어 {enemy.Status.CurrentBlock}  → {enemy.GetIntentDescription(runManager.PlayerStatus)}" +
                        (debuffs.Length > 0 ? $" [{debuffs}]" : ""));
                }
            intentText.text = intentBuilder.ToString();
        }
        RefreshControls();
    }

    private bool IntentChanged()
    {
        int vulnerable = runManager.PlayerStatus?.VulnerableTurns ?? 0;
        bool changed = !intentInitialized || displayedPlayerVulnerable != vulnerable
            || enemyDisplay.Count != battleManager.Enemies.Count;
        intentInitialized = true; displayedPlayerVulnerable = vulnerable;
        int index = 0;
        foreach (Enemy enemy in battleManager.Enemies)
        {
            var status = enemy != null ? enemy.Status : null;
            var stamp = (enemy, status?.CurrentHp ?? 0, status?.MaxHp ?? 0, status?.CurrentBlock ?? 0,
                status?.AttackPower ?? 0, status?.WeakTurns ?? 0, status?.VulnerableTurns ?? 0,
                status?.FrailTurns ?? 0, enemy != null ? enemy.Intent : null, enemy != null ? enemy.Data?.EnemyName : null);
            if (index >= enemyDisplay.Count) { enemyDisplay.Add(stamp); changed = true; }
            else if (!enemyDisplay[index].Equals(stamp)) { enemyDisplay[index] = stamp; changed = true; }
            index++;
        }
        if (index < enemyDisplay.Count) enemyDisplay.RemoveRange(index, enemyDisplay.Count - index);
        return changed;
    }

    private void RefreshControls()
    {
        if (battleManager == null || runManager == null) return;
        bool won = battleManager.State == BattleState.Victory;
        if (resultText != null)
        {
            string text = won ? (runManager.IsRunComplete ? "모든 층을 클리어했습니다!" : "전투 승리!") : "패배";
            if (resultText.text != text) resultText.text = text;
        }
        if (nextBattleButton != null)
        {
            bool visible = won && !battleManager.HasPendingCardReward
                && (runManager.HasMap || (runManager.HasStageSequence && !runManager.IsRunComplete));
            if (nextBattleButton.gameObject.activeSelf != visible) nextBattleButton.gameObject.SetActive(visible);
            if (nextBattleLabel != null)
            {
                string text = runManager.HasMap ? "맵으로" : "다음 층";
                if (nextBattleLabel.text != text) nextBattleLabel.text = text;
            }
        }
    }
}


