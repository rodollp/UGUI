using UnityEngine;
using UnityEngine.InputSystem;

public class PauseInputController : MonoBehaviour
{
    [SerializeField] private UIScreenFlowController screenFlow;

    private InputAction pauseAction;
    

    private void Awake()
    {
        pauseAction = new InputAction("Pause", binding: "<Keyboard>/escape");
    }

    void OnEnable()
    {
        pauseAction.Enable();
        pauseAction.performed += OnPausePerformed;
    }

    void OnDisable()
    {
        pauseAction.performed -= OnPausePerformed;
        pauseAction.Disable();
    }

    private void OnDestroy()
    {
        pauseAction.Dispose();
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {


        if (screenFlow.IsPlaying)
        {
            screenFlow.ShowPauseScreen();
        }
        else if (screenFlow.IsPaused)
        {
            screenFlow.ResumeGame();
        }
    }
}
    