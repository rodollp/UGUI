using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private HealthBarUI healthBarUI;

    private EnemyStatus enemyStatus;
    private int actionIndex;
    public EnemyStatus Status => enemyStatus;
    public EnemyData Data => enemyData;
    public EnemyAction Intent { get; private set; }
    public event Action OnIntentChanged;
    public string IntentDescription => Intent == null || Status == null || Status.IsDead
        ? "" : Intent.Describe(Status.AttackPower);

    private void Awake()
    {
        if (enemyData != null) Initialize(enemyData);
    }

    public void Initialize(EnemyData data)
    {
        enemyData = data;
        enemyStatus = new EnemyStatus(data.MaxHp);
        actionIndex = 0;
        Intent = null;
        if (healthBarUI != null) healthBarUI.Initialize(enemyStatus);
    }

    public void PlanNextAction()
    {
        if (Status == null || Status.IsDead) return;
        EnemyAction[] actions = enemyData.Actions;
        Intent = actions == null || actions.Length == 0
            ? new EnemyAction() : actions[actionIndex++ % actions.Length] ?? new EnemyAction();
        OnIntentChanged?.Invoke();
    }

    public void ExecuteIntent(PlayerStatus player)
    {
        if (Status == null || Status.IsDead || player == null || player.IsDead || Intent == null)
            return;

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
            default:
                int damage = (int)Math.Min(int.MaxValue,
                    Math.Max(0L, (long)Intent.Amount + Status.AttackPower));
                for (int hit = 0; hit < Math.Max(1, Intent.Hits) && !player.IsDead; hit++)
                    player.TakeDamage(damage);
                break;
        }
    }
}
