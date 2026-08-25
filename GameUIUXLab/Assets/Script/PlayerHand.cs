using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : MonoBehaviour
{
    [Header("Card Pool")]
    [SerializeField] private CardUI cardPrefab;
    [SerializeField] private int maxHandSize = 10;

    [Header("Hand Layout")]
    [SerializeField] private float cardSpacing = 220f;
    [SerializeField] private float maxHandWidth = 1200f;

    [SerializeField] private float cardHigh = -100f;
    private readonly List<CardUI> cardPool = new();

    private void Awake()
    {
        CreateCardPool();
    }

    private void CreateCardPool()
    {
        for (int i = 0; i < maxHandSize; i++)
        {
            CardUI card = Instantiate(cardPrefab, transform);

            card.gameObject.SetActive(false);

            cardPool.Add(card);
        }
    }

    public void AddCard()
    {
        foreach (CardUI card in cardPool)
        {
            if (!card.gameObject.activeSelf)
            {
                card.gameObject.SetActive(true);

                RefreshLayout();

                return;
            }
        }
    }

    public void RemoveCard()
    {
        for (int i = cardPool.Count - 1; i >= 0; i--)
        {
            if (cardPool[i].gameObject.activeSelf)
            {
                cardPool[i].gameObject.SetActive(false);

                RefreshLayout();

                return;
            }
        }
    }

    private void RefreshLayout()
    {
        List<CardUI> activeCards = new();

        foreach (CardUI card in cardPool)
        {
            if (card.gameObject.activeSelf)
            {
                activeCards.Add(card);
            }
        }

        int cardCount = activeCards.Count;

        if (cardCount == 0)
            return;

        float spacing = cardSpacing;

        if (cardCount > 1)
        {
            float requiredWidth = spacing * (cardCount - 1);

            if (requiredWidth > maxHandWidth)
            {
                spacing = maxHandWidth / (cardCount - 1);
            }
        }

        float totalWidth = spacing * (cardCount - 1);

        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < cardCount; i++)
        {
            float x = startX + spacing * i;

            activeCards[i].SetBasePosition(
                new Vector2(x, cardHigh)
            );
        }
    }

    public void ClearHand()
    {
        foreach (CardUI card in cardPool)
        {
            if (card.gameObject.activeSelf)
            {
                card.gameObject.SetActive(false);
            }
        }
        RefreshLayout();
    }
}