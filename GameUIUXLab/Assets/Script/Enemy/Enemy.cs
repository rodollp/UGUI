using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private HealthBarUI healthBarUI;

    private EnemyStatus enemyStatus;
    private int actionIndex;
    private int lastActionIndex = -1;
    private int consecutiveUses;
    private int plannedActions;
    private readonly System.Random patternRandom = new System.Random();
    [SerializeField] private int currentPatternStep;
    public EnemyStatus Status => enemyStatus;
    public EnemyData Data => enemyData;
    public EnemyAction Intent { get; private set; }
    public int CurrentPatternStep => currentPatternStep;
    public event Action OnIntentChanged;
    public string IntentDescription => Intent == null || Status == null || Status.IsDead
        ? "" : Intent.Describe(Status);

    public string GetIntentDescription(PlayerStatus player)
    {
        return Intent == null || Status == null || Status.IsDead ? "" : Intent.Describe(Status, player);
    }

    private void Awake()
    {
        if (enemyData != null) Initialize(enemyData);
    }

    public void Initialize(EnemyData data)
    {
        enemyData = data;
        enemyStatus = new EnemyStatus(data.MaxHp);
        actionIndex = 0;
        lastActionIndex = -1;
        consecutiveUses = 0;
        plannedActions = 0;
        currentPatternStep = 0;
        Intent = null;
        if (healthBarUI != null) healthBarUI.Initialize(enemyStatus);
    }

    public void PlanNextAction()
    {
        if (Status == null || Status.IsDead) return;
        EnemyAction[] actions = enemyData.Actions;
        if (actions == null || actions.Length == 0)
        {
            currentPatternStep = 1;
            Intent = new EnemyAction();
        }
        else
        {
            int index;
            if (enemyData.PatternMode == EnemyPatternMode.WeightedRandom)
            {
                index = plannedActions == 0 && enemyData.OpeningAction > 0
                    && enemyData.OpeningAction <= actions.Length && actions[enemyData.OpeningAction - 1] != null
                    ? enemyData.OpeningAction - 1
                    : EnemyPatternSelector.Select(actions, Status.CurrentHp, Status.MaxHp,
                        lastActionIndex, consecutiveUses, patternRandom);
            }
            else
            {
                index = actionIndex % actions.Length;
                actionIndex = (index + 1) % actions.Length;
            }
            currentPatternStep = index + 1;
            Intent = index < 0 ? new EnemyAction() : actions[index] ?? new EnemyAction();
            consecutiveUses = index >= 0 && index == lastActionIndex ? consecutiveUses + 1 : 1;
            lastActionIndex = index;
        }
        plannedActions++;
        LogPattern("예고");
        OnIntentChanged?.Invoke();
    }

    public void ExecuteIntent(PlayerStatus player)
    {
        if (Status == null || Status.IsDead || player == null || player.IsDead || Intent == null)
            return;

        LogPattern("실행", player);

        // 방어도는 소유자의 다음 턴 시작에 사라집니다.
        Status.ResetBlock();
        switch (Intent.Type)
        {
            case EnemyActionType.Block:
                Status.AddBlock(Math.Max(0, Intent.Amount));
                break;
            case EnemyActionType.Strength:
                Status.AddAttackPower(Math.Max(0, Intent.Amount));
                break;
            case EnemyActionType.Debuff:
                player.ApplyDebuff(Intent.Debuff, Intent.DebuffTurns, duringEnemyTurn: true);
                break;
            default:
                for (int hit = 0; hit < Math.Max(1, Intent.Hits) && !player.IsDead; hit++)
                    Status.DealAttackDamage(player, Intent.Amount);
                break;
        }
    }

    // 최종 EncounterData가 적용된 뒤 호출하여 프리팹 기본 데이터와 혼동하지 않게 합니다.
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void LogPatternSource()
    {
#if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GetAssetPath(enemyData);
        int count = enemyData.Actions == null ? 0 : enemyData.Actions.Length;
        Debug.Log($"[EnemyPattern] {name} / 데이터: {path} / 패턴 수: {count}", this);
        if (count == 0)
            Debug.LogWarning("[EnemyPattern] Actions가 비어 있어 기본 공격 6을 사용합니다.", this);
#endif
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void LogPattern(string phase, PlayerStatus player = null)
    {
#if UNITY_EDITOR
        Debug.Log($"[EnemyPattern] {name} / {phase} / 단계 {currentPatternStep} / " +
            GetIntentDescription(player), this);
#endif
    }
}

