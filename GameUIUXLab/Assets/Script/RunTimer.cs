using UnityEngine;

public class RunTimer : MonoBehaviour
{
    public float PlayTime { get; private set; }

    private bool isRunning;

    private void Update()
    {
        if (!isRunning)
            return;

        PlayTime += Time.unscaledDeltaTime;
    }

    public void StartTimer()
    {
        PlayTime = 0f;
        isRunning = true;
    }

    public void PauseTimer()
    {
        isRunning = false;
    }

    public void ResumeTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }
}