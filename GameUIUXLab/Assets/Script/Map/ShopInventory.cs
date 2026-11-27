using System;
using System.Collections.Generic;

public sealed class ShopInventory
{
    public const int CardCount = 5;
    public const int RelicCount = 3;
    public IReadOnlyList<CardData> Cards { get; }
    public IReadOnlyList<RelicData> Relics { get; }
    public int RelicPrice { get; }
    public int DiscountIndex { get; }
    public int BasePrice { get; }
    public bool CardRemovalUsed { get; private set; }
    private readonly bool[] purchased = new bool[CardCount];

    public ShopInventory(ShopData data, Random random, IReadOnlyList<RelicData> ownedRelics = null)
    {
        if (data == null || !data.IsValid() || random == null)
            throw new ArgumentException("상점의 카드 풀과 상품 가격을 확인하세요.");
        List<CardData> pool = data.GetEligibleCards();
        Shuffle(pool, random);
        Cards = pool.GetRange(0, CardCount).AsReadOnly();
        BasePrice = data.CardPrice;
        DiscountIndex = random.Next(CardCount);

        List<RelicData> availableRelics = RelicData.GetUnowned(data.RelicPool, ownedRelics);
        Shuffle(availableRelics, random);
        Relics = availableRelics.GetRange(0, Math.Min(RelicCount, availableRelics.Count)).AsReadOnly();
        RelicPrice = data.RelicPrice;
    }

    private static void Shuffle<T>(List<T> pool, Random random)
    {
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
    }

    public RelicData GetRelic(int index) => index >= 0 && index < Relics.Count ? Relics[index] : null;

    public bool IsSold(int index) => index < 0 || index >= CardCount || purchased[index];
    public int Price(int index) => index == DiscountIndex ? BasePrice / 2 : BasePrice;
    internal void MarkCardRemovalUsed() => CardRemovalUsed = true;
    public bool TryMarkPurchased(int index)
    {
        if (IsSold(index)) return false;
        purchased[index] = true;
        return true;
    }
}
