using System.Collections.Generic;

public class BattleDeck
{
    private List<CardInstance> drawPile = new();

    public BattleDeck(MasterDeck masterDeck)
    {

        foreach (CardInstance card in masterDeck.Cards)
        {
            drawPile.Add(new CardInstance(card.CardData));
        }

    }

}