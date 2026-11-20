using System;
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
    [SerializeField] private float cardHigh;

    private readonly List<CardUI> cardPool = new();
    private readonly List<CardUI> activeCards = new();

    public bool HasRoom => activeCards.Count < cardPool.Count;

    public event Action<CardUI> OnCardClicked;
    public event Action<CardUI, Vector2> OnCardDropped;

    private void Awake()
    {
        CreateCardPool();
    }

    private void CreateCardPool()
    {
        for (int i = 0; i < maxHandSize; i++)
        {
            CardUI card = Instantiate(cardPrefab, transform);

            card.OnCardClicked += HandleCardClicked;
            card.OnCardDropped += HandleCardDropped;

            card.gameObject.SetActive(false);

            cardPool.Add(card);
        }
    }

    private void HandleCardClicked(CardUI card)
    {
        OnCardClicked?.Invoke(card);
    }
    private void HandleCardDropped(CardUI card, Vector2 screenPosition)
    {
        OnCardDropped?.Invoke(card, screenPosition);
    }
    public void AddCard(CardInstance cardInstance, PlayerStatus player = null,
        Func<Vector2, EnemyStatus> getDamagePreviewTarget = null)
    {
        if (cardInstance == null)
            return;

        foreach (CardUI card in cardPool)
        {
            if (!card.gameObject.activeSelf)
            {
                card.SetCard(cardInstance, player, getDamagePreviewTarget);
                card.gameObject.SetActive(true);

                RefreshLayout();
                return;
            }
        }
    }


    public bool OwnsCard(CardUI card) => card != null && cardPool.Contains(card)
        && card.gameObject.activeSelf;

    public void RemoveCard(CardUI card)
    {
        if (card == null)
            return;

        if (!cardPool.Contains(card))
            return;

        card.gameObject.SetActive(false);

        RefreshLayout();
    }

    private void RefreshLayout()
    {
        activeCards.Clear();

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
            float availableWidth = maxHandWidth;
            if (transform is RectTransform handRect && handRect.rect.width > 0)
            {
                // Leave both lower corners clear for the combat controls.
                float cardWidth = ((RectTransform)activeCards[0].transform).rect.width;
                availableWidth = Mathf.Min(availableWidth, Mathf.Max(0, handRect.rect.width - 470f - cardWidth));
            }
            spacing = Mathf.Min(spacing, availableWidth / (cardCount - 1));
        }

        float totalWidth = spacing * (cardCount - 1);
        float startX = -totalWidth * 0.5f;

        for (int i = 0; i < cardCount; i++)
        {
            float x = startX + spacing * i;

            float edgeOffset = totalWidth > 0 ? 18f * Mathf.Abs(x) / (totalWidth * .5f) : 0f;
            activeCards[i].SetBasePosition(new Vector2(x, cardHigh - edgeOffset));
        }
    }

    private void OnRectTransformDimensionsChange() => RefreshLayout();

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
