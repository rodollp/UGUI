using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Shop", menuName = "Map/Shop")]
public class ShopData : ScriptableObject
{
    [Min(2)] public int CardPrice = 50;
    public CardData[] CardPool;
    public CardData[] ExcludedCards;
    [Min(1)] public int RelicPrice = 60;
    public RelicData[] RelicPool;

    public List<CardData> GetEligibleCards()
    {
        var result = new List<CardData>();
        if (CardPool == null) return result;
        foreach (CardData card in CardPool)
        {
            if (card == null || result.Contains(card)) continue;
            if (ExcludedCards != null && System.Array.IndexOf(ExcludedCards, card) >= 0) continue;
            result.Add(card);
        }
        return result;
    }
    public bool IsValid() => CardPrice >= 2 && GetEligibleCards().Count >= ShopInventory.CardCount
        && (RelicPool == null || RelicPool.Length == 0 || RelicPrice > 0);
}
