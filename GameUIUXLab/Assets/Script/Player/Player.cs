using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private HealthBarUI healthBarUI;

    public PlayerStatus Status { get; private set; }

    public void Initialize(PlayerStatus status)
    {
        Status = status;
        healthBarUI.Initialize(status);
    }
}