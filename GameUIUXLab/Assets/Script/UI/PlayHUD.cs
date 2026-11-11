using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI stageText;
    [SerializeField] private TextMeshProUGUI intentText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button nextBattleButton;
    [SerializeField] private RunTimer runTimer;
    [SerializeField] private UIScreenFlowController screenFlowController;
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private RunManager runManager;

    private void OnEnable()
    {
        if (battleManager != null) battleManager.OnBattleChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        if (battleManager != null) battleManager.OnBattleChanged -= RefreshUI;
    }

    private void Update()
    {
        if (timeText != null && runTimer != null)
            timeText.text = $"Time : {System.TimeSpan.FromSeconds(runTimer.PlayTime):mm\\:ss}";
        if (endTurnButton != null)
            endTurnButton.interactable = battleManager != null && battleManager.CanPlayCards;
    }

    public void ResetHUD() => RefreshUI();
    public void NextTurn() => RefreshUI();

    private void RefreshUI()
    {
        if (battleManager == null || runManager == null) return;
        if (turnText != null)
            turnText.text = $"Turn : {battleManager.Turn} / " +
                (battleManager.State == BattleState.EnemyTurn ? "적 턴" : "플레이어 턴");
        if (energyText != null)
            energyText.text = runManager.PlayerStatus == null ? "" :
                $"에너지 {runManager.PlayerStatus.CurrentEnergy} / {runManager.PlayerStatus.MaxEnergy}";
        if (stageText != null)
            stageText.text = $"{battleManager.BattleFloor}층 · {battleManager.EncounterName}";
        if (intentText != null)
        {
            StringBuilder text = new StringBuilder();
            foreach (Enemy enemy in battleManager.Enemies)
                if (enemy != null && enemy.Status != null && !enemy.Status.IsDead)
                    text.AppendLine($"{enemy.Data.EnemyName}  HP {enemy.Status.CurrentHp}/{enemy.Status.MaxHp}  " +
                        $"방어 {enemy.Status.CurrentBlock}  → {enemy.IntentDescription}");
            intentText.text = text.ToString();
        }
        bool won = battleManager.State == BattleState.Victory;
        if (resultText != null)
            resultText.text = won ? (runManager.IsRunComplete ? "모든 층을 클리어했습니다!" : "전투 승리!") : "패배";
        if (nextBattleButton != null)
            nextBattleButton.gameObject.SetActive(won && runManager.HasStageSequence && !runManager.IsRunComplete);
    }
}

