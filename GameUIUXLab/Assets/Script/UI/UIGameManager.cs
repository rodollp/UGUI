using UnityEngine;

public class UIGameManager : MonoBehaviour
{

    [SerializeField] private UIScreenFlowController screenFlowController;
    [SerializeField] private PlayerHand hand;


    public void ReturnToTitle()
    {
        hand.ClearHand();
        screenFlowController.ShowTitleScreen();
    }

}
