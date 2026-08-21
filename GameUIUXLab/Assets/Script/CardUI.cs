using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Card Visual")]
    [SerializeField] private RectTransform cardVisual;

    [Header("Hover Settings")]
    [SerializeField] private float hoverMoveY = 100f;
    [SerializeField] private float hoverScale = 1.3f;
    [SerializeField] private float moveSpeed = 12f;

    private Vector2 originalPosition;
    private Vector3 originalScale;

    private Vector2 targetPosition;
    private Vector3 targetScale;

    private void Awake()
    {
        originalPosition = cardVisual.anchoredPosition;
        originalScale = cardVisual.localScale;

        targetPosition = originalPosition;
        targetScale = originalScale;
    }

    private void Update()
    {
        cardVisual.anchoredPosition = Vector2.Lerp(
            cardVisual.anchoredPosition,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        cardVisual.localScale = Vector3.Lerp(
            cardVisual.localScale,
            targetScale,
            moveSpeed * Time.deltaTime
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPosition = originalPosition + Vector2.up * hoverMoveY;
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPosition = originalPosition;
        targetScale = originalScale;
    }
}