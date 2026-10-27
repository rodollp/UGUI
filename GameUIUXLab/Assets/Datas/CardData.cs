using System.Collections.Generic;
using UnityEngine;

public enum CardType
{
    Attack,
    Skill,
    Power
}

[CreateAssetMenu(fileName = "CardData", menuName = "Scriptable Objects/CardData")]
public class CardData : ScriptableObject
{
    public uint CardID;
    public string CardName;
    public CardType Type;
    public int EnergyCost;

    public List<CardEffect> Effects;
}