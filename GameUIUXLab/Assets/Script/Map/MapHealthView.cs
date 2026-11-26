using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class MapHealthView
{
    public const float Height = 64;

    private readonly RectTransform panel;
    private readonly RectTransform fill;
    private readonly TextMeshProUGUI value;
    private PlayerStatus player;
    private (int Current, int Maximum) displayed = (-1, -1);

    public MapHealthView(TMP_Text template, Vector2 position)
    {
        panel = CreateImage(template.transform.parent, "MapHealth", new Color(.08f, .14f, .17f, .98f));
        Place(panel, position, new Vector2(236, Height));

        TextMeshProUGUI label = CreateLabel(panel, template, "Label", TextAlignmentOptions.MidlineLeft);
        label.text = "체력";
        Place(label.rectTransform, new Vector2(14, -7), new Vector2(64, 30));
        value = CreateLabel(panel, template, "Value", TextAlignmentOptions.MidlineRight);
        Place(value.rectTransform, new Vector2(78, -7), new Vector2(144, 30));

        RectTransform track = CreateImage(panel, "Track", new Color(.20f, .26f, .28f));
        Place(track, new Vector2(14, -44), new Vector2(208, 9));
        fill = CreateImage(track, "Fill", new Color(.89f, .39f, .32f));
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.up;
        fill.offsetMin = fill.offsetMax = Vector2.zero;

        panel.gameObject.SetActive(false);
    }

    public void Bind(PlayerStatus status)
    {
        if (player == status) return;
        if (player != null) player.OnStatusChanged -= Refresh;
        player = status;
        panel.gameObject.SetActive(player != null);
        if (player == null) return;
        player.OnStatusChanged += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        var health = (Current: player.CurrentHp, Maximum: player.MaxHp);
        if (displayed == health) return;
        displayed = health;
        value.text = $"{health.Current} / {health.Maximum}";
        float fraction = health.Maximum > 0 ? (float)health.Current / health.Maximum : 0f;
        fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    private static RectTransform CreateImage(Transform parent, string name, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.rectTransform.SetParent(parent, false);
        image.color = color;
        image.raycastTarget = false;
        return image.rectTransform;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, TMP_Text template, string name,
        TextAlignmentOptions alignment)
    {
        var text = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        text.rectTransform.SetParent(parent, false);
        text.font = template.font;
        text.fontSharedMaterial = template.fontSharedMaterial;
        text.color = new Color(.98f, .92f, .81f);
        text.fontSize = 23;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.up;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
