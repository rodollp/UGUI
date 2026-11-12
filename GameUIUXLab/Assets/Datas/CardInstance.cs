public class CardInstance
{
    public CardData CardData { get; private set; }
    public int CurrentCost { get; private set; }
    public bool IsUpgraded { get; private set; }
    public bool CanUpgrade => !IsUpgraded && CardData.UpgradedCard != null && CardData.UpgradedCard != CardData;
    public CardInstance CopyForBattle() => new CardInstance(CardData) { IsUpgraded = IsUpgraded };

    public bool TryUpgrade()
    {
        if (!CanUpgrade) return false;
        CardData = CardData.UpgradedCard;
        IsUpgraded = true;
        CurrentCost = CardData.EnergyCost;
        return true;
    }

    public CardInstance(CardData cardData)
    {
        CardData = cardData;
        CurrentCost = cardData.EnergyCost;
    }
}
