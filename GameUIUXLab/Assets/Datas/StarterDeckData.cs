using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StartingCardEntry
{
    public CardData CardData;
    public int Count;
}

[CreateAssetMenu(fileName = "StarterDeckData", menuName = "Card/StarterDeck")]
public class StarterDeckData : ScriptableObject
{
    public List<StartingCardEntry> StartingCards = new();
}