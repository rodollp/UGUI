
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Card Information")]
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Hover Settings")]
    [SerializeField] private float hoverMoveY = 100f;
    [SerializeField] private float hoverScale = 1.3f;
    [SerializeField] private float moveSpeed = 12f;

    private RectTransform rectTransform;

    private Vector2 basePosition;
    private Vector3 baseScale;

    private Vector2 targetPosition;
    private Vector3 targetScale;

    public event Action<CardUI> OnCardClicked;

    public CardInstance CardInstance { get; private set; }

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

    public void SetCard(CardInstance cardInstance)
    {
        CardInstance = cardInstance;

        if (cardInstance == null)
        {
            ClearCard();
            return;
        }

        CardData data = cardInstance.CardData;

        nameText.text = data.CardName;
        costText.text = cardInstance.CurrentCost.ToString();
        typeText.text = data.Type.ToString();

        descriptionText.text = data.GetDescription();
    }

    public void ClearCard()
    {
        CardInstance = null;

        nameText.text = "";
        costText.text = "";
        typeText.text = "";
        descriptionText.text = "";

        targetPosition = basePosition;
        targetScale = baseScale;

        rectTransform.localScale = baseScale;
    }

    public void OnClickCard()
    {
        if (CardInstance == null)
            return;

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
