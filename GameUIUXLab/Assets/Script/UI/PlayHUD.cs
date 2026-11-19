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
    [SerializeField] private RectTransform enemyIntentRoot;
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
    private (int, string) displayedStage = (-1, null);
    private readonly List<EnemyIntentView> enemyIntents = new();
    private Canvas intentCanvas;
    private Camera battleCamera;
    private TMP_Text nextBattleLabel;
    private BattleResourcesView resources;

    private void Awake()
    {
        if (energyText != null) resources = new BattleResourcesView(energyText);
        if (enemyIntentRoot != null) intentCanvas = enemyIntentRoot.GetComponentInParent<Canvas>(true);
    }

    public void SetPileCounts(int draw, int discard) => resources?.SetPileCounts(draw, discard);

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
        foreach (EnemyIntentView view in enemyIntents) view.Refresh(null, null);
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
        if (enemyIntents.Count == 0) return;

        bool show = enemyIntentRoot.gameObject.activeInHierarchy
            && (screenFlowController == null || (screenFlowController.IsPlaying && !screenFlowController.IsOverlayOpen));
        if (show && battleCamera == null) battleCamera = Camera.main;
        Camera uiCamera = intentCanvas != null && intentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? intentCanvas.worldCamera : null;
        foreach (EnemyIntentView view in enemyIntents)
        {
            if (show && battleCamera != null) view.FollowEnemy(battleCamera, uiCamera);
            else view.Hide();
        }
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
        var playerStatus = runManager.PlayerStatus;
        resources?.SetEnergy(playerStatus?.CurrentEnergy, playerStatus?.MaxEnergy);
        var stage = (battleManager.BattleFloor, battleManager.EncounterName);
        if (stageText != null && !displayedStage.Equals(stage))
        {
            displayedStage = stage;
            stageText.text = $"{battleManager.BattleFloor}층 · {battleManager.EncounterName}";
        }
        RefreshEnemyIntents(playerStatus);
        RefreshControls();
    }

    private void RefreshEnemyIntents(PlayerStatus playerStatus)
    {
        if (enemyIntentRoot == null) return;
        var enemies = battleManager.Enemies;
        int count = battleManager.State is BattleState.PlayerTurn or BattleState.EnemyTurn ? enemies.Count : 0;
        for (int i = 0; i < count; i++)
        {
            if (i == enemyIntents.Count)
                enemyIntents.Add(new EnemyIntentView(enemyIntentRoot, stageText != null ? stageText : turnText));
            enemyIntents[i].Refresh(enemies[i], playerStatus);
        }
        for (int i = count; i < enemyIntents.Count; i++) enemyIntents[i].Refresh(null, playerStatus);
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
            bool visible = won && (battleManager.HasPendingCardReward || runManager.HasMap
                || (runManager.HasStageSequence && !runManager.IsRunComplete));
            if (nextBattleButton.gameObject.activeSelf != visible) nextBattleButton.gameObject.SetActive(visible);
            if (nextBattleLabel != null)
            {
                string text = "다음으로";
                if (nextBattleLabel.text != text) nextBattleLabel.text = text;
            }
        }
    }
}


