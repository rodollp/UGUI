public class CardInstance
{
    public CardData CardData { get; }
    public CardVersion Version => IsUpgraded ? CardData.Upgrade : CardData.Base;
    public int CurrentCost => Version.EnergyCost;
    public bool IsUpgraded { get; private set; }
    public bool CanUpgrade => !IsUpgraded && CardData.HasUpgrade && CardData.Upgrade != null;
    public CardInstance CopyForBattle() => new CardInstance(CardData) { IsUpgraded = IsUpgraded };

    public CardInstance CopyForPreview(bool showUpgrade)
    {
        CardInstance copy = CopyForBattle();
        if (showUpgrade) copy.TryUpgrade();
        return copy;
    }

    public bool TryUpgrade()
    {
        if (!CanUpgrade) return false;
        IsUpgraded = true;
        return true;
    }

    public CardInstance(CardData cardData)
    {
        CardData = cardData;
    }
}
