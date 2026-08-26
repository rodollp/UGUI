using UnityEngine;

public class CardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private PlayHUD playHUD;

    [Header("Deck Settings")]
    [SerializeField] private int startDeckCount = 20;
    [SerializeField] private int drawCount = 5;

    private int drawPileCount;
    private int discardPileCount;
    private int handCount;

    /// <summary>
    /// 전투 시작
    /// </summary>
    public void StartBattle()
    {
        drawPileCount = startDeckCount;
        discardPileCount = 0;
        handCount = 0;

        DrawCards();
    }

    /// <summary>
    /// 카드 drawCount 만큼 드로우
    /// </summary>
    private void DrawCards()
    {
        for (int i = 0; i < drawCount; i++)
        {
            // 드로우 더미가 비었다면 버림 더미 재사용
            if (drawPileCount <= 0)
            {
                RefillDrawPile();
            }

            // 버림 더미까지 없으면 더 이상 뽑을 카드가 없음
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
    /// 현재 손패 전체를 버림 더미로 이동
    /// </summary>
    private void DiscardHand()
    {
        int currentHandCount = handCount;

        for (int i = 0; i < currentHandCount; i++)
        {
            playerHand.RemoveCard();

            discardPileCount++;
            handCount--;
        }
    }

    /// <summary>
    /// 버림 더미를 다시 드로우 더미로 이동
    /// 현재는 실제 CardData가 없기 때문에 숫자만 이동한다.
    /// 나중에는 List<CardData>를 Shuffle하면 된다.
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
    /// 턴 종료 버튼에서 호출
    /// </summary>
    public void EndTurn()
    {
        // 1. 현재 손패 버리기
        DiscardHand();

        // 여기 나중에
        // EnemyTurn();
        // 상태이상 처리
        // 방어도 초기화
        // 에너지 회복
        // 등이 들어간다.

        // 2. 턴 증가
        playHUD.NextTurn();

        // 3. 새로운 카드 드로우
        DrawCards();
    }
}