using UnityEngine;

public class CardManager : MonoBehaviour
{

    [SerializeField] private PlayerHand playerHand;

    [SerializeField] private PlayHUD playHUD;

    /// 전투 시작 시 카드 5장을 드로우한다.
    ///
    /// 턴 종료 시 손패에 남아있는 카드를 모두 버림 더미로 이동한다.
    ///
    /// 다음 턴 시작 시 새로운 카드 5장을 드로우한다.
    ///
    /// 드로우할 카드가 부족하면
    /// 버림 더미를 섞어 새로운 드로우 더미를 만든다.

    private void DrawCards()
    {
        for (int i = 0; i < 5; i++)
        {
            playerHand.AddCard();
        }
    }

    private void DiscardHand()
    {
       playerHand.RemoveCard();
    }

    private void RefillDrawPile()
    {

    }

}
