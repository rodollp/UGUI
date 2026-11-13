
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(RectTransform))]
public class CardUI : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
{
    [Header("Card Information")]
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text typeText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image artworkImage;

    [Header("Hover Settings")]
    [SerializeField] private float hoverMoveY = 100f;
    [SerializeField] private float hoverScale = 1.3f;
    [SerializeField] private float moveSpeed = 12f;

    private RectTransform rectTransform;
    private RectTransform parentRect;

    private Vector2 basePosition;
    private Vector3 baseScale;

    private Vector2 targetPosition;
    private Vector3 targetScale;

    private bool isDragging;
    private bool isRewardCard;
    private Vector2 dragOffset;

    public event Action<CardUI> OnCardClicked;
    public event Action<CardUI,Vector2> OnCardDropped;

    public CardInstance CardInstance { get; private set; }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentRect = rectTransform.parent as RectTransform;

        basePosition = rectTransform.anchoredPosition;
        baseScale = rectTransform.localScale;

        targetPosition = basePosition;
        targetScale = baseScale;
    }

    private void Update()
    {
        if (isDragging && IsCancelPressed()) CancelDrag();
        if (!isDragging)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(
                rectTransform.anchoredPosition,
                targetPosition,
                moveSpeed * Time.deltaTime
            );
        }

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
        if (artworkImage != null)
        {
            artworkImage.sprite = data.Artwork;
            artworkImage.enabled = data.Artwork != null;
        }
    }

    public void ClearCard()
    {
        CardInstance = null;

        isDragging = false;

        nameText.text = "";
        costText.text = "";
        typeText.text = "";
        descriptionText.text = "";
        if (artworkImage != null)
        {
            artworkImage.sprite = null;
            artworkImage.enabled = false;
        }

        targetPosition = basePosition;
        targetScale = baseScale;

        rectTransform.localScale = baseScale;
    }

    public void OnClickCard()
    {
        if (CardInstance == null || isDragging)
            return;

        OnCardClicked?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isDragging)
            return;

        targetPosition = basePosition + Vector2.up * hoverMoveY;
        targetScale = baseScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isDragging)
            return;

        targetPosition = basePosition;
        targetScale = baseScale;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left
            || CardInstance == null || isRewardCard)
            return;

        isDragging = true;

        targetScale = baseScale * hoverScale;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            dragOffset = rectTransform.anchoredPosition - localPoint;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left || !isDragging)
            return;

        if (IsCancelPressed()) { CancelDrag(); return; }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left || !isDragging)
            return;

        // EventSystem may deliver the release before this component's Update.
        if (IsCancelPressed()) { CancelDrag(); return; }

        isDragging = false;

        OnCardDropped?.Invoke(this, eventData.position);

        targetPosition = basePosition;
        targetScale = baseScale;
    }

    private static bool IsCancelPressed() => Mouse.current != null
        && (Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.rightButton.isPressed);

    public void CancelDrag()
    {
        isDragging = false;
        targetPosition = basePosition;
        targetScale = baseScale;
        if (rectTransform == null) return;
        rectTransform.anchoredPosition = basePosition;
        rectTransform.localScale = baseScale;
    }

    private void OnDisable() => CancelDrag();

    public void SetBasePosition(Vector2 position)
    {
        basePosition = position;

        if (!isDragging)
        {
            targetPosition = basePosition;
        }
    }

    public void SetRewardMode()
    {
        isRewardCard = true;
        hoverMoveY = 0;
        hoverScale = 1.06f;
    }
}
