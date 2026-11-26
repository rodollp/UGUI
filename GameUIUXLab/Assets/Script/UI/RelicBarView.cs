using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class RelicBarView
{
    public const float BarHeight = 40;
    private const float MaxBarWidth = 600;
    private const float NamesStart = 53;
    private const float TooltipWidth = 330;
    private const float TooltipPadding = 14;
    private static readonly Color PanelColor = new(.08f, .14f, .17f, .98f);
    private static readonly Color AccentColor = new(.91f, .65f, .37f);
    private static readonly Color TextColor = new(.98f, .92f, .81f);

    private readonly TMP_Text template;
    private readonly RectTransform panel;
    private readonly RectTransform tooltip;
    private readonly TextMeshProUGUI tooltipTitle;
    private readonly TextMeshProUGUI tooltipDescription;
    private readonly float tooltipGap;
    private readonly List<(RelicTooltipTarget Target, TextMeshProUGUI Label)> entries = new();
    private RelicTooltipTarget hovered;
    private (RunManager Run, int RunVersion, int Count) displayed;

    public RelicBarView(TMP_Text template, Vector2 position, float tooltipGap = 8)
    {
        this.template = template;
        this.tooltipGap = tooltipGap;
        panel = CreatePanel(template.transform.parent, "RelicBar");
        Place(panel, position, new Vector2(MaxBarWidth, BarHeight));
        panel.GetComponent<Image>().color = PanelColor;

        RectTransform accent = CreatePanel(panel, "Accent");
        Place(accent, Vector2.zero, new Vector2(3, BarHeight));
        accent.GetComponent<Image>().color = AccentColor;

        TextMeshProUGUI heading = CreateLabel(panel, "RelicHeading", 20);
        heading.text = "유물";
        heading.color = AccentColor;
        Place(heading.rectTransform, new Vector2(14, 0), new Vector2(36, BarHeight));

        tooltip = CreatePanel(panel, "RelicTooltip");
        tooltip.GetComponent<Image>().color = PanelColor;
        tooltipTitle = CreateLabel(tooltip, "Title", 21);
        tooltipTitle.color = AccentColor;
        tooltipDescription = CreateLabel(tooltip, "Description", 19);
        tooltipDescription.textWrappingMode = TextWrappingModes.Normal;
        tooltipDescription.alignment = TextAlignmentOptions.TopLeft;

        tooltip.gameObject.SetActive(false);
        panel.gameObject.SetActive(false);
    }

    public void Refresh(RunManager run)
    {
        var status = (Run: run, run.RunVersion, Count: run.Relics.Count);
        if (displayed == status) return;
        displayed = status;
        if (hovered != null) HideTooltip(hovered);
        panel.gameObject.SetActive(status.Count > 0);
        if (status.Count == 0) return;

        float x = NamesStart;
        float maxNameWidth = (MaxBarWidth - NamesStart - TooltipPadding) / status.Count;
        for (int i = 0; i < status.Count; i++)
        {
            if (i == entries.Count) entries.Add(CreateEntry());
            var (target, label) = entries[i];
            RelicData relic = run.Relics[i];
            label.text = relic.DisplayName;
            target.Bind(this, relic);
            float width = Mathf.Min(maxNameWidth, Mathf.Ceil(label.GetPreferredValues(label.text).x) + 18);
            Place(target.Rect, new Vector2(x, -3), new Vector2(width, BarHeight - 6));
            target.gameObject.SetActive(true);
            x += width;
        }
        for (int i = status.Count; i < entries.Count; i++)
            entries[i].Target.gameObject.SetActive(false);
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, x + 7);
    }

    internal void ShowTooltip(RelicTooltipTarget target, RelicData relic)
    {
        if (hovered != null && hovered != target) hovered.SetHighlighted(false);
        hovered = target;
        hovered.SetHighlighted(true);

        float contentWidth = TooltipWidth - TooltipPadding * 2;
        tooltipTitle.text = relic.DisplayName;
        tooltipDescription.text = relic.GetEffectDescription();
        float descriptionHeight = Mathf.Ceil(tooltipDescription.GetPreferredValues(
            tooltipDescription.text, contentWidth, Mathf.Infinity).y);
        const float descriptionTop = 40;
        Place(tooltipTitle.rectTransform, new Vector2(TooltipPadding, -9), new Vector2(contentWidth, 26));
        Place(tooltipDescription.rectTransform, new Vector2(TooltipPadding, -descriptionTop),
            new Vector2(contentWidth, descriptionHeight));
        Place(tooltip, new Vector2(Mathf.Min(target.Rect.anchoredPosition.x, MaxBarWidth - TooltipWidth), -BarHeight - tooltipGap),
            new Vector2(TooltipWidth, descriptionTop + descriptionHeight + TooltipPadding));
        tooltip.SetAsLastSibling();
        tooltip.gameObject.SetActive(true);
    }

    internal void HideTooltip(RelicTooltipTarget target)
    {
        if (hovered != target) return;
        target.SetHighlighted(false);
        hovered = null;
        tooltip.gameObject.SetActive(false);
    }

    private (RelicTooltipTarget, TextMeshProUGUI) CreateEntry()
    {
        RectTransform rect = CreatePanel(panel, "Relic");
        var hitArea = rect.GetComponent<Image>();
        hitArea.color = Color.clear;
        hitArea.raycastTarget = true;
        var target = rect.gameObject.AddComponent<RelicTooltipTarget>();

        TextMeshProUGUI nameLabel = CreateLabel(rect, "Name", 23);
        nameLabel.alignment = TextAlignmentOptions.Center;
        nameLabel.overflowMode = TextOverflowModes.Ellipsis;
        nameLabel.rectTransform.anchorMin = Vector2.zero;
        nameLabel.rectTransform.anchorMax = Vector2.one;
        nameLabel.rectTransform.offsetMin = new Vector2(4, 0);
        nameLabel.rectTransform.offsetMax = new Vector2(-4, 0);
        return (target, nameLabel);
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string name, float size)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.rectTransform.SetParent(parent, false);
        text.font = template.font;
        text.fontSharedMaterial = template.fontSharedMaterial;
        text.fontSize = size;
        text.color = TextColor;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.richText = false;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreatePanel(Transform parent, string name)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.raycastTarget = false;
        image.rectTransform.SetParent(parent, false);
        return image.rectTransform;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
