using System.Collections.Generic;
public class MasterDeck
{

    private List<CardInstance> cards = new();

    public IReadOnlyList<CardInstance> Cards => cards;
    public void AddCard(CardInstance card)
    {
        cards.Add(card);
    }

}
