
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private PlayHUD playHUD;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private RunManager runManager;
    [SerializeField] private TargetSelector targetSelector;

    [Header("Deck Settings")]
    [SerializeField] private int drawCount = 5;

    [Header("Card Use Settings")]
    [SerializeField, Range(0f, 1f)]
    private float useHeightRatio = 0.35f;

    private BattleDeck battleDeck;

    private void Awake()
    {
        playerHand.OnCardDropped += HandleCardDropped;
    }

    private void OnDestroy()
    {
        if (playerHand != null)
        {
            playerHand.OnCardDropped -= HandleCardDropped;
        }
    }

    public void StartBattle()
    {
        if (runManager.MasterDeck == null)
        {
            Debug.LogError("MasterDeck이 생성되지 않았습니다.");
            return;
        }

        battleDeck = new BattleDeck(runManager.MasterDeck);
        battleDeck.Shuffle();

        playHUD.ResetHUD();
        playerHand.ClearHand();

        DrawCards();
    }

    private void DrawCards()
    {
        if (battleDeck == null)
            return;

        for (int i = 0; i < drawCount; i++)
        {
            CardInstance card = battleDeck.Draw();

            if (card == null)
                return;

            playerHand.AddCard(card);
        }
    }

    private void DiscardHand()
    {
        if (battleDeck == null)
            return;

        battleDeck.DiscardHand();
        playerHand.ClearHand();
    }

    private void HandleCardDropped(CardUI cardUI, Vector2 screenPosition)
    {
        if (battleDeck == null || cardUI == null)
            return;

        CardInstance card = cardUI.CardInstance;

        if (card == null)
            return;

        CardData data = card.CardData;

        switch (data.TargetType)
        {
            case CardTargetType.None:
                {
                    float useHeight = Screen.height * useHeightRatio;

                    if (screenPosition.y < useHeight)
                        return;

                    UseCard(cardUI);
                    break;
                }

            case CardTargetType.Enemy:
                {
                    if (targetSelector == null)
                        return;

                    Enemy target = targetSelector.GetTarget(screenPosition);

                    if (target == null)
                    {
                        Debug.Log("선택된 적이 없습니다.");
                        return;
                    }

                    Debug.Log($"선택된 적: {target.name}");

                    UseCard(cardUI);
                    break;
                }
        }
    }

    public void UseCard(CardUI cardUI)
    {
        if (battleDeck == null || cardUI == null)
            return;

        CardInstance card = cardUI.CardInstance;

        if (card == null)
            return;

        if (!battleDeck.DiscardCard(card))
            return;

        playerHand.RemoveCard(cardUI);

        uiManager.ShowToast("카드를 사용했습니다.");
    }

    public void EndTurn()
    {
        if (battleDeck == null)
            return;

        DiscardHand();

        playHUD.NextTurn();

        DrawCards();
    }
}
