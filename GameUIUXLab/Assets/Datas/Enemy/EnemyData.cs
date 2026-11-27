using System;
using UnityEngine;

public enum EnemyType { Normal, Elite, Boss }
public enum EnemyActionType { Attack, Block, Strength, Debuff }
public enum EnemyPatternMode { Sequential, WeightedRandom }

[Serializable]
public class EnemyAction
{
    [Header("Random selection")]
    [Min(0)] public int Weight = 1;
    [Tooltip("0: unlimited. Limit repeats of this action entry.")]
    [Min(0)] public int MaxConsecutiveUses = 2;
    [Range(0, 100)] public int MinHpPercent = 0;
    [Range(0, 100)] public int MaxHpPercent = 100;
    public EnemyActionType Type;
    [Min(0)] public int Amount = 6;
    [Min(1)] public int Hits = 1;

    public DebuffType Debuff;
    [Min(1)] public int DebuffTurns = 2;

    public string Describe(CreatureStatus source, CreatureStatus target = null)
    {
        switch (Type)
        {
            case EnemyActionType.Block: return $"방어 {Amount}";
            case EnemyActionType.Strength: return $"힘 +{Amount}";
            case EnemyActionType.Debuff: return $"{DebuffNames.Get(Debuff)} {DebuffTurns}";
            default: return $"공격 {source.CalculateAttackDamage(Amount, target)} × {Math.Max(1, Hits)}";
        }
    }
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Battle/Enemy")]
public class EnemyData : ScriptableObject
{
    public string EnemyName;
    public EnemyType EnemyType;
    [Min(1)] public int MaxHp = 20;
    public EnemyPatternMode PatternMode;
    [Tooltip("0: choose normally. Otherwise use this 1-based action on the first turn.")]
    [Min(0)] public int OpeningAction = 0;
    // Sequential: 순서대로 반복. WeightedRandom: 조건을 만족하는 항목을 가중 추첨.
    public EnemyAction[] Actions = { new EnemyAction() };
}

