using System;
using System.Collections.Generic;

public class BattleDeck
{
    private List<CardInstance> drawPile = new(); // 드로우 더미
    private List<CardInstance> hand = new(); // 손 패
    private List<CardInstance> discardPile = new(); // 버린 카드 더미


    private Random random = new Random();


    public BattleDeck(MasterDeck masterDeck)
    {

        foreach (CardInstance card in masterDeck.Cards)
        {
            drawPile.Add(card.CopyForBattle());
        }

    }


    public void Shuffle()
    {
        for (int i = drawPile.Count - 1; i > 0; i--)
        {
            int randomIndex = random.Next(0, i + 1);

            CardInstance temp = drawPile[i];
            drawPile[i] = drawPile[randomIndex];
            drawPile[randomIndex] = temp;
        }
    }
    public CardInstance Draw()
    {
        if (hand.Count >= 10)
        {
            return null;
        }

        if (drawPile.Count == 0)
        {
            RefillDrawPile();
        }

        if (drawPile.Count == 0)
        {
            return null;
        }

        int lastIndex = drawPile.Count - 1;
        CardInstance card = drawPile[lastIndex];

        drawPile.RemoveAt(lastIndex);

        hand.Add(card);

        return card;
    }

    private void RefillDrawPile()
    {
        if (discardPile.Count == 0)
        {
            return;
        }

        drawPile.AddRange(discardPile);

        discardPile.Clear();

        Shuffle();
    }


    public bool ContainsInHand(CardInstance card) => hand.Contains(card);

    public bool PlayCard(CardInstance card)
    {
        if (!hand.Remove(card)) return false;
        // 파워 카드는 이번 전투의 순환 더미에서 빠집니다.
        if (card.CardData.Type != CardType.Power) discardPile.Add(card);
        return true;
    }

    public bool DiscardCard(CardInstance card)
    {
        if (!hand.Remove(card))
            return false;

        discardPile.Add(card);
        return true;
    }

    public void DiscardHand()
    {
        discardPile.AddRange(hand);
        hand.Clear();
    }

}
