
using UnityEngine;

public class CardInputController : MonoBehaviour
{
    private enum InputState
    {
        Idle,
        Selecting,
        Targeting
    }

    private InputState currentState = InputState.Idle;

    private CardUI selectedCard;

    public void SelectCard(CardUI card)
    {
        if (card == null || card.CardInstance == null)
            return;

        selectedCard = card;
        currentState = InputState.Selecting;

        Debug.Log($"카드 선택: {card.CardInstance.CardData.CardName}");
    }

    public void StartTargeting()
    {
        if (currentState != InputState.Selecting)
            return;

        currentState = InputState.Targeting;

        Debug.Log("대상 지정 시작");
    }

    public void CancelSelection()
    {
        selectedCard = null;
        currentState = InputState.Idle;

        Debug.Log("카드 선택 취소");
    }
}
