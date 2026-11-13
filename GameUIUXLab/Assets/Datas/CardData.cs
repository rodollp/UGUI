
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
    public string CardName;
    public CardType Type;
    public CardTargetType TargetType;
    public int EnergyCost;
    public Sprite Artwork;
    public CardData UpgradedCard;

    public List<CardEffect> Effects;

    public string GetDescription()
    {
        string prefix = TargetType == CardTargetType.RandomEnemy ? "무작위 적 1명에게\n"
            : TargetType == CardTargetType.AllEnemies ? "모든 적에게\n" : "";
        return prefix + string.Join("\n",
            Effects.Select(effect => effect.GetDescription()));
    }
}
