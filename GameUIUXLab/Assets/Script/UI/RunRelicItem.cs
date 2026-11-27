using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class RunRelicItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;
    private Color normalColor;
    private RunHUD owner;
    private RelicData relic;

    private void Awake() => normalColor = background.color;

    public void Bind(RunHUD hud, RelicData data)
    {
        owner = hud;
        relic = data;
        label.text = data.DisplayName;
        icon.sprite = data.Icon;
        icon.gameObject.SetActive(data.Icon != null);
    }

    public void Highlight(bool active) => background.color = active ? new Color(.28f, .36f, .36f) : normalColor;
    public void OnPointerEnter(PointerEventData eventData) { if (relic != null) owner.ShowTooltip(this, relic); }
    public void OnPointerExit(PointerEventData eventData) => owner?.HideTooltip(this);
    private void OnDisable() => owner?.HideTooltip(this);
}
