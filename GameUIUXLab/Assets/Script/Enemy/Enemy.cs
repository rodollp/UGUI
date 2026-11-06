using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private HealthBarUI healthBarUI;

    private EnemyStatus enemyStatus;

    public EnemyStatus Status => enemyStatus;

    private void Awake()
    {
        enemyStatus = new EnemyStatus(enemyData.MaxHp);

        healthBarUI.Initialize(enemyStatus);
    }
}