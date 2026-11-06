using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Slider hpSlider;

    [SerializeField] private RunTimer runTimer;
    [SerializeField] private UIScreenFlowController screenFlowController;

    private int turn = 1;

    private void Start()
    {
        hpSlider.minValue = 0f;

        RefreshUI();
    }

    private void Update()
    {
        RefreshTimeUI();
    }


    public void NextTurn()
    {
        turn++;
        RefreshTurnUI();
    }

    private void GameOver()
    {
        screenFlowController.ShowResultScreen();
    }

    public void ResetHUD()
    {
        turn = 1;

        RefreshUI();
    }

    private void RefreshUI()
    {
        RefreshTurnUI();
        RefreshTimeUI();
        
    }

    private void RefreshTurnUI()
    {
        turnText.text = $"Turn : {turn}";
    }

    private void RefreshTimeUI()
    {
        timeText.text = $"Time : {runTimer.PlayTime:00:00}";
    }

    
}