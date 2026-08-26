using System;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    [SerializeField] private float hoverMoveY = 100f;
    [SerializeField] private float hoverScale = 1.3f;
    [SerializeField] private float moveSpeed = 12f;

    private RectTransform rectTransform;
    private CardManager cardManager;

    private Vector2 basePosition;
    private Vector3 baseScale;

    private Vector2 targetPosition;
    private Vector3 targetScale;

    public event Action<CardUI> OnCardClicked;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        basePosition = rectTransform.anchoredPosition;
        baseScale = rectTransform.localScale;

        targetPosition = basePosition;
        targetScale = baseScale;
    }

    private void Update()
    {
        rectTransform.anchoredPosition = Vector2.Lerp(
            rectTransform.anchoredPosition,
            targetPosition,
            moveSpeed * Time.deltaTime
        );

        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            targetScale,
            moveSpeed * Time.deltaTime
        );
    }
    public void OnClickCard()
    {
     OnCardClicked?.Invoke(this);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetPosition = basePosition + Vector2.up * hoverMoveY;
        targetScale = baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetPosition = basePosition;
        targetScale = baseScale;
    }

    public void SetBasePosition(Vector2 position)
    {
        basePosition = position;

        targetPosition = basePosition;
    }

    
}