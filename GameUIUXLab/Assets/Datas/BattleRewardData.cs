using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleRewards", menuName = "Battle/Rewards")]
public class BattleRewardData : ScriptableObject
{
    [Min(0)] public int MinGold = 10;
    [Min(0)] public int MaxGold = 20;
    public List<CardData> CardPool = new List<CardData>();

    private List<CardData> GetUniqueCards()
    {
        var cards = new List<CardData>();
        if (CardPool != null)
            foreach (CardData card in CardPool)
                if (card != null && !cards.Contains(card)) cards.Add(card);
        return cards;
    }

    public bool IsValid() => MinGold >= 0 && MaxGold >= MinGold
        && GetUniqueCards().Count >= BattleRewardOffer.ChoiceCount;

    public BattleRewardOffer CreateOffer(System.Random random)
    {
        if (random == null) throw new ArgumentNullException(nameof(random));
        if (!IsValid()) throw new InvalidOperationException("보상 풀에 서로 다른 카드가 3장 이상 필요합니다.");
        List<CardData> cards = GetUniqueCards();
        for (int i = 0; i < BattleRewardOffer.ChoiceCount; i++)
        {
            int index = random.Next(i, cards.Count);
            CardData temp = cards[i]; cards[i] = cards[index]; cards[index] = temp;
        }
        int gold = (int)(MinGold + (long)(random.NextDouble() * ((long)MaxGold - MinGold + 1)));
        return new BattleRewardOffer(gold, cards.GetRange(0, BattleRewardOffer.ChoiceCount));
    }
}

