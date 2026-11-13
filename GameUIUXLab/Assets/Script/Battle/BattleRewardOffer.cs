using System;
using System.Collections.Generic;

public sealed class BattleRewardOffer
{
    public const int ChoiceCount = 3;
    public int Gold { get; }
    public IReadOnlyList<CardData> Choices { get; }
    public bool IsClaimed { get; private set; }
    public CardData SelectedCard { get; private set; }

    public BattleRewardOffer(int gold, IList<CardData> choices)
    {
        if (gold < 0 || choices == null || choices.Count != ChoiceCount)
            throw new ArgumentException("카드 보상은 정확히 3개의 선택지가 필요합니다.");
        var copy = new List<CardData>();
        foreach (CardData card in choices)
        {
            if (card == null || copy.Contains(card))
                throw new ArgumentException("보상 선택지는 서로 다른 카드여야 합니다.");
            copy.Add(card);
        }
        Gold = gold;
        Choices = copy.AsReadOnly();
    }

    public bool TryClaim(int index, out CardData card)
    {
        card = null;
        if (IsClaimed || index < 0 || index >= Choices.Count) return false;
        IsClaimed = true;
        SelectedCard = Choices[index];
        card = SelectedCard;
        return true;
    }
}

