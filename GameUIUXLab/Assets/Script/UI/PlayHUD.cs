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
    private TMP_Text drawCountText;
    private TMP_Text discardCountText;
    private (int Draw, int Discard) displayedPiles = (-1, -1);

    private static readonly Color PanelColor = new Color(.06f, .13f, .15f, .96f);
    private static readonly Color Accent = new Color(.94f, .65f, .38f);
    private static readonly Color TextColor = new Color(.98f, .92f, .81f);

    private void Awake()
    {
        if (energyText == null || endTurnButton == null) return;
        Transform playScreen = energyText.transform.parent;
        Vector2 bottomLeft = Vector2.zero;
        RectTransform energyPanel = CreatePanel(playScreen, "EnergyPanel", bottomLeft,
            new Vector2(28, 93), new Vector2(196, 80));
        energyPanel.SetSiblingIndex(energyText.transform.GetSiblingIndex());
        CreateLabel(energyPanel, "EnergyLabel", "에너지", 18, new Vector2(0, 20));
        energyText.rectTransform.SetParent(energyPanel, false);
        StyleLabel(energyText, 34);
        Place(energyText.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(0, -12), new Vector2(176, 45));

        drawCountText = CreatePile(playScreen, "DrawPile", "뽑기", 28);
        discardCountText = CreatePile(playScreen, "DiscardPile", "버림", 130);
        StyleEndTurnButton();
        SetPileCounts(0, 0);
    }

    public void SetPileCounts(int draw, int discard)
    {
        if (drawCountText == null || displayedPiles == (draw, discard)) return;
        if (displayedPiles.Draw != draw) drawCountText.text = draw.ToString();
        if (displayedPiles.Discard != discard) discardCountText.text = discard.ToString();
        displayedPiles = (draw, discard);
    }

    private TMP_Text CreatePile(Transform parent, string name, string caption, float x)
    {
        RectTransform panel = CreatePanel(parent, name, Vector2.zero, new Vector2(x, 24), new Vector2(94, 61));
        CreateLabel(panel, "Label", caption, 15, new Vector2(0, 15));
        return CreateLabel(panel, "Count", "", 25, new Vector2(0, -11));
    }

    private RectTransform CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)panel.transform;
        rect.SetParent(parent, false);
        Place(rect, anchor, anchor, position, size);
        var image = panel.GetComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = false;
        AddTrim(rect);
        return rect;
    }

    private TMP_Text CreateLabel(Transform parent, string name, string caption, float fontSize, Vector2 position)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        label.transform.SetParent(parent, false);
        label.font = energyText.font;
        label.fontSharedMaterial = energyText.fontSharedMaterial;
        label.text = caption;
        StyleLabel(label, fontSize);
        Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, new Vector2(176, 32));
        return label;
    }

    private void StyleEndTurnButton()
    {
        var rect = (RectTransform)endTurnButton.transform;
        Vector2 bottomRight = new Vector2(1, 0);
        Place(rect, bottomRight, bottomRight, new Vector2(-28, 24), new Vector2(184, 64));
        if (endTurnButton.targetGraphic is Image image)
        {
            image.sprite = null;
            image.type = Image.Type.Simple;
            image.color = new Color(.51f, .27f, .17f);
        }
        AddTrim(rect);
        TMP_Text label = endTurnButton.GetComponentInChildren<TMP_Text>(true);
        if (label == null) return;
        label.text = "턴 종료";
        StyleLabel(label, 27);
        Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(164, 54));
    }

    private static void AddTrim(RectTransform parent)
    {
        var trim = new GameObject("TopTrim", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        trim.rectTransform.SetParent(parent, false);
        trim.rectTransform.anchorMin = new Vector2(0, 1);
        trim.rectTransform.anchorMax = Vector2.one;
        trim.rectTransform.pivot = new Vector2(.5f, 1);
        trim.rectTransform.anchoredPosition = Vector2.zero;
        trim.rectTransform.sizeDelta = new Vector2(-12, 3);
        trim.color = Accent;
        trim.raycastTarget = false;
    }

    private static void StyleLabel(TMP_Text label, float size)
    {
        label.enableAutoSizing = false;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = TextColor;
        label.raycastTarget = false;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

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
                $"{runManager.PlayerStatus.CurrentEnergy} / {runManager.PlayerStatus.MaxEnergy}";
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


