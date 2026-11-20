
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CardType
{
    Attack,
    Skill,
    Power
}

public enum CardTargetType
{
    None,
    Enemy,
    RandomEnemy,
    AllEnemies
}

[CreateAssetMenu(fileName = "CardData", menuName = "Scriptable Objects/CardData")]
public class CardData : ScriptableObject
{
    public uint CardID;
    public CardVersion Base = new();
    public bool HasUpgrade;
    public CardVersion Upgrade = new();

    public string CardName => Base.CardName;
    public string GetDescription() => Base.GetDescription();
}

[Serializable]
public sealed class CardVersion
{
    public string CardName;
    public CardType Type;
    public CardTargetType TargetType;
    public int EnergyCost;
    public Sprite Artwork;
    public List<CardEffect> Effects = new();

    public string GetDescription(PlayerStatus player = null, EnemyStatus target = null)
    {
        if (target != null && target.IsDead) target = null;
        string prefix = TargetType == CardTargetType.RandomEnemy ? "무작위 적 1명에게\n"
            : TargetType == CardTargetType.AllEnemies
                ? (target != null ? "가리킨 적 기준\n" : "모든 적에게\n")
                : "";
        return prefix + (Effects == null ? "" : string.Join("\n", Effects
            .Where(effect => effect != null)
            .Select(effect => effect is DamageEffect damage ? damage.GetDescription(player, target)
                : effect.GetDescription())));
    }
}
