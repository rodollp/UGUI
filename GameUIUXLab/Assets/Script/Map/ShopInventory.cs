using System;
using System.Collections.Generic;

public sealed class ShopInventory
{
    public const int CardCount = 5;
    public IReadOnlyList<CardData> Cards { get; }
    public int DiscountIndex { get; }
    public int BasePrice { get; }
    private readonly bool[] purchased = new bool[CardCount];

    public ShopInventory(ShopData data, Random random)
    {
        if (data == null || !data.IsValid() || random == null)
            throw new ArgumentException("상점에는 서로 다른 판매 가능 카드가 5종 이상 필요합니다.");
        List<CardData> pool = data.GetEligibleCards();
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        Cards = pool.GetRange(0, CardCount).AsReadOnly();
        BasePrice = data.CardPrice;
        DiscountIndex = random.Next(CardCount);
    }
    public bool IsSold(int index) => index < 0 || index >= CardCount || purchased[index];
    public int Price(int index) => index == DiscountIndex ? BasePrice / 2 : BasePrice;
    public bool TryMarkPurchased(int index)
    {
        if (IsSold(index)) return false;
        purchased[index] = true;
        return true;
    }
}
