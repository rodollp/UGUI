using UnityEngine;
using UnityEngine.InputSystem;

public class TargetSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Target Settings")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float maxDistance = 100f;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    public Enemy GetTarget(Vector2 screenPosition)
    {
        if (targetCamera == null)
            return null;

        Ray ray = targetCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, enemyLayer))
        {
            return hit.collider.GetComponentInParent<Enemy>();
        }

        return null;
    }
}