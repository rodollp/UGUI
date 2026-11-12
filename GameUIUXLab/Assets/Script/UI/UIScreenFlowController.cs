using UnityEngine;

public class UIScreenFlowController : MonoBehaviour
{
    private enum UIScreenState { Title, Play, Pause, Result, Refresh, Map, Event }
    [SerializeField] private GameObject titleScreen;
    [SerializeField] private GameObject playScreen;
    [SerializeField] private GameObject pauseScreen;
    [SerializeField] private GameObject resultScreen;
    [SerializeField] private GameObject refreshScreen;
    [SerializeField] private GameObject mapScreen;
    [SerializeField] private GameObject eventScreen;
    [SerializeField] private GameObject worldPlayer;
    [SerializeField] private GameObject worldEnemies;
    [SerializeField] private RunTimer runTimer;
    [SerializeField] private BattleManager battleManager;
    private UIScreenState currentState;
    private UIScreenState pausedFrom = UIScreenState.Play;

    public bool IsPlaying => currentState == UIScreenState.Play;
    public bool IsPaused => currentState == UIScreenState.Pause;
    public bool IsMap => currentState == UIScreenState.Map;
    public bool IsEvent => currentState == UIScreenState.Event;
    public bool IsAdventureActive => IsPlaying || IsMap || IsEvent;

    private void Start() => ShowTitleScreen();

    public void ShowTitleScreen()
    {
        if (battleManager != null) battleManager.AbortBattle();
        SetActiveScreen(UIScreenState.Title);
        Time.timeScale = 1;
        runTimer.StopTimer();
    }

    public void ShowPlayScreen()
    {
        bool newRun = currentState == UIScreenState.Title;
        SetActiveScreen(UIScreenState.Play);
        Time.timeScale = 1;
        if (newRun) runTimer.StartTimer(); else runTimer.ResumeTimer();
    }

    public void ShowMapForNewRun()
    {
        SetActiveScreen(UIScreenState.Map);
        Time.timeScale = 1;
        runTimer.StartTimer();
    }

    public void ShowMapScreen()
    {
        SetActiveScreen(UIScreenState.Map);
        Time.timeScale = 1;
        runTimer.ResumeTimer();
    }

    public void ShowEventScreen()
    {
        SetActiveScreen(UIScreenState.Event);
        Time.timeScale = 1;
        runTimer.ResumeTimer();
    }

    public void ShowPauseScreen()
    {
        if (!IsAdventureActive) return;
        pausedFrom = currentState;
        SetActiveScreen(UIScreenState.Pause);
        Time.timeScale = 0;
        runTimer.PauseTimer();
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;
        SetActiveScreen(pausedFrom);
        Time.timeScale = 1;
        runTimer.ResumeTimer();
    }

    public void ShowResultScreen()
    {
        SetActiveScreen(UIScreenState.Result);
        Time.timeScale = 1;
        runTimer.StopTimer();
    }

    public void ShowRefreshScreen()
    {
        if (battleManager != null) battleManager.AbortBattle();
        SetActiveScreen(UIScreenState.Refresh);
        Time.timeScale = 0;
        runTimer.StopTimer();
    }

    private void SetActiveScreen(UIScreenState next)
    {
        currentState = next;
        titleScreen.SetActive(next == UIScreenState.Title);
        playScreen.SetActive(next == UIScreenState.Play);
        pauseScreen.SetActive(next == UIScreenState.Pause);
        resultScreen.SetActive(next == UIScreenState.Result);
        refreshScreen.SetActive(next == UIScreenState.Refresh);
        if (mapScreen != null) mapScreen.SetActive(next == UIScreenState.Map);
        if (eventScreen != null) eventScreen.SetActive(next == UIScreenState.Event);
        bool inBattle = next == UIScreenState.Play || (next == UIScreenState.Pause && pausedFrom == UIScreenState.Play);
        if (worldPlayer != null) worldPlayer.SetActive(inBattle);
        if (worldEnemies != null) worldEnemies.SetActive(inBattle);
    }
}

