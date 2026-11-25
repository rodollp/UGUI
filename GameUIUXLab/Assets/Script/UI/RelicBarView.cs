using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class RelicBarView
{
    private const float Height = 40;
    private const float HorizontalPadding = 14;
    private const float MaxWidth = 600;
    private readonly RectTransform panel;
    private readonly TextMeshProUGUI label;
    private (RunManager Run, int RunVersion, int Count) displayed;

    public RelicBarView(TMP_Text template, Vector2 position)
    {
        var background = new GameObject("RelicBar", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        background.color = new Color(.08f, .14f, .17f, .96f);
        background.raycastTarget = false;
        panel = background.rectTransform;
        panel.SetParent(template.transform.parent, false);
        panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.up;
        panel.anchoredPosition = position;
        panel.sizeDelta = new Vector2(MaxWidth, Height);

        var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        accent.color = new Color(.91f, .65f, .37f);
        accent.raycastTarget = false;
        RectTransform accentRect = accent.rectTransform;
        accentRect.SetParent(panel, false);
        accentRect.anchorMin = Vector2.zero;
        accentRect.anchorMax = Vector2.up;
        accentRect.pivot = new Vector2(0, .5f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(3, 0);

        label = new GameObject("RelicNames", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        label.rectTransform.SetParent(panel, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(HorizontalPadding, 0);
        label.rectTransform.offsetMax = new Vector2(-HorizontalPadding, 0);
        label.font = template.font;
        label.fontSharedMaterial = template.fontSharedMaterial;
        label.fontSize = 23;
        label.enableAutoSizing = true;
        label.fontSizeMin = 13;
        label.fontSizeMax = 23;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.color = new Color(.98f, .92f, .81f);
        label.richText = false;
        label.raycastTarget = false;
        panel.gameObject.SetActive(false);
    }

    public void Refresh(RunManager run)
    {
        var status = (Run: run, run.RunVersion, Count: run.Relics.Count);
        if (displayed == status) return;
        displayed = status;
        panel.gameObject.SetActive(status.Count > 0);
        if (status.Count == 0) return;

        label.text = "유물  ·  " + string.Join("  ·  ", run.Relics.Select(relic => relic.DisplayName));
        float width = Mathf.Ceil(label.GetPreferredValues(label.text).x) + HorizontalPadding * 2 + 2;
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(MaxWidth, width));
    }
}
