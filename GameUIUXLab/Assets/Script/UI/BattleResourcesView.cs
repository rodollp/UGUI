using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class BattleResourcesView
{
    private readonly TMP_Text drawCountText;
    private readonly TMP_Text discardCountText;
    private (int Draw, int Discard) displayedPiles = (-1, -1);

    private static readonly Color PanelColor = new Color(.06f, .13f, .15f, .96f);
    private static readonly Color Accent = new Color(.94f, .65f, .38f);
    private static readonly Color TextColor = new Color(.98f, .92f, .81f);

    private readonly TMP_Text energyText;
    private (int? Current, int? Maximum) displayedEnergy;

    public BattleResourcesView(TMP_Text energyText)
    {
        this.energyText = energyText;
        Transform playScreen = energyText.transform.parent;
        Vector2 bottomLeft = Vector2.zero;
        RectTransform energyPanel = CreatePanel(playScreen, "EnergyPanel", bottomLeft,
            new Vector2(28, 93), new Vector2(196, 80));
        energyPanel.SetSiblingIndex(energyText.transform.GetSiblingIndex());
        CreateLabel(energyPanel, "EnergyLabel", "에너지", 18, new Vector2(0, 20));
        energyText.rectTransform.SetParent(energyPanel, false);
        StyleLabel(energyText, 34);
        Place(energyText.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(0, -12), new Vector2(176, 45));

        drawCountText = CreatePile(playScreen, "DrawPile", "뽑기", 28);
        discardCountText = CreatePile(playScreen, "DiscardPile", "버림", 130);
        energyText.text = string.Empty;
        SetPileCounts(0, 0);
    }

    public void SetEnergy(int? current, int? maximum)
    {
        if (displayedEnergy == (current, maximum)) return;
        displayedEnergy = (current, maximum);
        energyText.text = current.HasValue ? $"{current} / {maximum}" : string.Empty;
    }

    public void SetPileCounts(int draw, int discard)
    {
        if (displayedPiles == (draw, discard)) return;
        if (displayedPiles.Draw != draw) drawCountText.text = draw.ToString();
        if (displayedPiles.Discard != discard) discardCountText.text = discard.ToString();
        displayedPiles = (draw, discard);
    }

    private TMP_Text CreatePile(Transform parent, string name, string caption, float x)
    {
        RectTransform panel = CreatePanel(parent, name, Vector2.zero, new Vector2(x, 24), new Vector2(94, 61));
        CreateLabel(panel, "Label", caption, 15, new Vector2(0, 15));
        return CreateLabel(panel, "Count", "", 25, new Vector2(0, -11));
    }

    private RectTransform CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = (RectTransform)panel.transform;
        rect.SetParent(parent, false);
        Place(rect, anchor, anchor, position, size);
        var image = panel.GetComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = false;
        AddTrim(rect);
        return rect;
    }

    private TMP_Text CreateLabel(Transform parent, string name, string caption, float fontSize, Vector2 position)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        label.transform.SetParent(parent, false);
        label.font = energyText.font;
        label.fontSharedMaterial = energyText.fontSharedMaterial;
        label.text = caption;
        StyleLabel(label, fontSize);
        Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), position, new Vector2(176, 32));
        return label;
    }

    private static void AddTrim(RectTransform parent)
    {
        var trim = new GameObject("TopTrim", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        trim.rectTransform.SetParent(parent, false);
        trim.rectTransform.anchorMin = new Vector2(0, 1);
        trim.rectTransform.anchorMax = Vector2.one;
        trim.rectTransform.pivot = new Vector2(.5f, 1);
        trim.rectTransform.anchoredPosition = Vector2.zero;
        trim.rectTransform.sizeDelta = new Vector2(-12, 3);
        trim.color = Accent;
        trim.raycastTarget = false;
    }

    private static void StyleLabel(TMP_Text label, float size)
    {
        label.enableAutoSizing = false;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.color = TextColor;
        label.raycastTarget = false;
    }

    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
