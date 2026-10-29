using System.Collections.Generic;
public class MasterDeck
{

    private List<CardInstance> cards = new();

    public IReadOnlyList<CardInstance> Cards => cards;
    public void AddCard(CardInstance card)
    {
        cards.Add(card);
    }

    public MasterDeck(StarterDeckData deckData)
    {
        foreach (StartingCardEntry entry in deckData.StartingCards)
        {
            for (int i = 0; i < entry.Count; i++)
            {
                CardInstance card = new CardInstance(entry.CardData);

                AddCard(card);
            }
        }
    }
}
