public class CardInstance
{
    public CardData CardData { get; private set; }
    public int CurrentCost { get; private set; }

    public CardInstance(CardData cardData)
    {
        CardData = cardData;
        CurrentCost = cardData.EnergyCost;
    }
}