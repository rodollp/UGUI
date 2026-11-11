using System;
using UnityEngine;

public enum EnemyType { Normal, Elite, Boss }
public enum EnemyActionType { Attack, Block, Strength }

[Serializable]
public class EnemyAction
{
    public EnemyActionType Type;
    [Min(0)] public int Amount = 6;
    [Min(1)] public int Hits = 1;

    public string Describe(int strength)
    {
        switch (Type)
        {
            case EnemyActionType.Block: return $"방어 {Amount}";
            case EnemyActionType.Strength: return $"힘 +{Amount}";
            default: return $"공격 {Math.Max(0, Amount + strength)} × {Math.Max(1, Hits)}";
        }
    }
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Battle/Enemy")]
public class EnemyData : ScriptableObject
{
    public string EnemyName;
    public EnemyType EnemyType;
    [Min(1)] public int MaxHp = 20;
    // 앞에서부터 순서대로 반복합니다. 비어 있으면 기본 공격을 사용합니다.
    public EnemyAction[] Actions = { new EnemyAction() };
}
