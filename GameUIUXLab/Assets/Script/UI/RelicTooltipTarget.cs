using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class RelicTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RelicBarView owner;
    private RelicData relic;
    private Image background;

    internal RectTransform Rect => (RectTransform)transform;

    private void Awake() => background = GetComponent<Image>();

    internal void Bind(RelicBarView view, RelicData data)
    {
        owner = view;
        relic = data;
    }

    internal void SetHighlighted(bool highlighted) => background.color = highlighted
        ? new Color(.24f, .33f, .35f) : Color.clear;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (relic != null) owner.ShowTooltip(this, relic);
    }

    public void OnPointerExit(PointerEventData eventData) => owner?.HideTooltip(this);

    private void OnDisable() => owner?.HideTooltip(this);
}
