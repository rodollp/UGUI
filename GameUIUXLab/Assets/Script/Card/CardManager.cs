using UnityEngine;

public class CardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private PlayHUD playHUD;

    [SerializeField] private UIManager uiManager;
    [SerializeField] private RunManager runManager;


    [Header("Deck Settings")]
    [SerializeField] private StarterDeckData starterDeckData;
    [SerializeField] private int drawCount = 5;

    private MasterDeck masterDeck;
    private int drawPileCount;
    private int discardPileCount;
    private int handCount;

    private void Awake()
    {
        playerHand.OnCardClicked += UseCard;
    }

    private void OnDestroy()
    {
        playerHand.OnCardClicked -= UseCard;
    }
    /// <summary>
    /// 전투 시작
    /// </summary>
    public void StartBattle()
    {
        if (runManager.MasterDeck == null)
        {
            Debug.LogError("MasterDeck이 생성되지 않았습니다.");
            return;
        }

        playHUD.ResetHUD();
        playerHand.ClearHand();

        // 현재는 실제 DrawPile이 없으므로 카드 수만 사용
        drawPileCount = runManager.MasterDeck.Cards.Count;

        discardPileCount = 0;
        handCount = 0;

        Debug.Log($"전투 시작! 덱 카드 수: {drawPileCount}");

        DrawCards();
    }

    /// <summary>
    /// 카드 drawCount 만큼 드로우
    /// </summary>
    private void DrawCards()
    {
        for (int i = 0; i < drawCount; i++)
        {
            if (drawPileCount <= 0)
            {
                RefillDrawPile();
            }

            if (drawPileCount <= 0)
            {
                return;
            }

            playerHand.AddCard();

            drawPileCount--;
            handCount++;
        }
    }

    /// <summary>
    /// 현재 손패 전체 버리기
    /// </summary>
    private void DiscardHand()
    {
        discardPileCount += handCount;

        handCount = 0;

        playerHand.ClearHand();
    }

    /// <summary>
    /// 카드 한 장 사용
    /// </summary>
    public void UseCard(CardUI card)
    {
        if (card == null)
            return;

        playerHand.RemoveCard(card);

        discardPileCount++;

        if (handCount > 0)
        {
            handCount--;
        }

        // 지금 테스트용
        playHUD.TakeDamage();

        uiManager.ShowToast("카드를 사용했습니다."); 
    }

    /// <summary>
    /// 버림 더미를 다시 드로우 더미로 이동
    /// </summary>
    private void RefillDrawPile()
    {
        if (discardPileCount <= 0)
        {
            return;
        }

        drawPileCount = discardPileCount;
        discardPileCount = 0;

        Debug.Log("버림 더미를 섞어 새로운 드로우 더미를 만들었습니다.");
    }

    /// <summary>
    /// 턴 종료
    /// </summary>
    public void EndTurn()
    {
        // 1. 현재 손패 전부 버리기
        DiscardHand();

        // 2. 턴 증가
        playHUD.NextTurn();

        // 3. 새로운 카드 5장 드로우
        DrawCards();
    }
}