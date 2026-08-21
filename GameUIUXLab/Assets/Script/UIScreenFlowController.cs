using UnityEngine;

public class UIScreenFlowController : MonoBehaviour
{
   private enum UIScreenState
    {
        Title,
        Play,
        Pause,
        Result
    }


    [SerializeField] private GameObject titleScreen;
    [SerializeField] private GameObject playScreen; 
        
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject resultScreen;

    [SerializeField] private RunTimer runTimer;
   
    private UIScreenState currentState;

    public bool IsPlaying => currentState == UIScreenState.Play;
    public bool IsPaused => currentState == UIScreenState.Pause;

    void Start()
    {
        ShowTitleScreen();
    }
    public void ShowTitleScreen()
    {
        SetActiveScreen(UIScreenState.Title);
        Time.timeScale = 1f;

        runTimer.StopTimer();
    }

    public void ShowPlayScreen()
    {
        SetActiveScreen(UIScreenState.Play);
        Time.timeScale = 1f;

        runTimer.StartTimer();
    }

    public void ShowPauseScreen()
    {
        SetActiveScreen(UIScreenState.Pause);
        Time.timeScale = 0f;

        runTimer.PauseTimer();
    }

    public void ResumeGame()
    {
        SetActiveScreen(UIScreenState.Play);
        Time.timeScale = 1f;

        runTimer.ResumeTimer();
    }

    public void ShowResultScreen()
    {
        SetActiveScreen(UIScreenState.Result);
        Time.timeScale = 1f;

        runTimer.StopTimer();
    }
    private void SetActiveScreen(UIScreenState nextState)
    {
        currentState = nextState;
        titleScreen.SetActive(currentState == UIScreenState.Title);
        playScreen.SetActive(currentState == UIScreenState.Play);
        pauseScreen.SetActive(currentState == UIScreenState.Pause);
        resultScreen.SetActive(currentState == UIScreenState.Result);
    }
}
