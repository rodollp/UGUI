using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class PlayerBuffView
{
    private const float RowHeight = 32;
    private readonly RectTransform row;
    private readonly RectTransform debuffText;
    private readonly Vector2 debuffPosition;
    private readonly (GameObject Root, TMP_Text Label) strength;
    private readonly (GameObject Root, TMP_Text Label) dexterity;
    private (int Strength, int Dexterity) displayed;

    public PlayerBuffView(TMP_Text fontTemplate, RectTransform debuffText)
    {
        this.debuffText = debuffText;
        debuffPosition = debuffText.anchoredPosition;
        row = new GameObject("PlayerBuffs", typeof(RectTransform), typeof(HorizontalLayoutGroup))
            .GetComponent<RectTransform>();
        row.SetParent(fontTemplate.transform.parent, false);
        row.anchorMin = row.anchorMax = new Vector2(.5f, 0);
        row.pivot = new Vector2(.5f, 1);
        row.anchoredPosition = new Vector2(0, -5);
        row.sizeDelta = new Vector2(232, RowHeight);

        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 8;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;

        strength = CreateBadge(fontTemplate, "Strength", new Color(1f, .72f, .36f), isDexterity: false);
        dexterity = CreateBadge(fontTemplate, "Dexterity", new Color(.51f, .91f, .83f), isDexterity: true);
        row.gameObject.SetActive(false);
    }

    public void Refresh(PlayerStatus player)
    {
        var values = player != null && !player.IsDead
            ? (player.AttackPower, player.Dexterity) : (0, 0);
        if (displayed == values) return;
        displayed = values;

        SetBadge(strength, "힘", displayed.Strength);
        SetBadge(dexterity, "민첩", displayed.Dexterity);
        bool visible = displayed != (0, 0);
        row.gameObject.SetActive(visible);
        debuffText.anchoredPosition = visible
            ? debuffPosition + Vector2.down * (RowHeight + 5) : debuffPosition;
    }

    private (GameObject Root, TMP_Text Label) CreateBadge(TMP_Text fontTemplate, string name, Color accent, bool isDexterity)
    {
        var badge = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        badge.rectTransform.SetParent(row, false);
        badge.rectTransform.sizeDelta = new Vector2(103, 30);
        badge.raycastTarget = false;
        badge.color = new Color(.06f, .12f, .15f, .96f);

        var icon = AddGraphic<PlayerBuffIcon>(badge.rectTransform, "Icon", new Vector2(-34, 0), new Vector2(22, 22));
        icon.color = accent;
        icon.SetDexterity(isDexterity);

        var label = AddGraphic<TextMeshProUGUI>(badge.rectTransform, "Value", new Vector2(12, 0), new Vector2(75, 28));
        label.font = fontTemplate.font;
        label.fontSharedMaterial = fontTemplate.fontSharedMaterial;
        label.color = accent;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        label.fontSize = label.fontSizeMax = 18;
        label.fontSizeMin = 12;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        badge.gameObject.SetActive(false);
        return (badge.gameObject, label);
    }

    private static T AddGraphic<T>(RectTransform parent, string name, Vector2 position, Vector2 size) where T : Graphic
    {
        var graphic = new GameObject(name, typeof(RectTransform), typeof(T)).GetComponent<T>();
        graphic.rectTransform.SetParent(parent, false);
        graphic.rectTransform.anchorMin = graphic.rectTransform.anchorMax = new Vector2(.5f, .5f);
        graphic.rectTransform.anchoredPosition = position;
        graphic.rectTransform.sizeDelta = size;
        graphic.raycastTarget = false;
        return graphic;
    }

    private static void SetBadge((GameObject Root, TMP_Text Label) badge, string caption, int value)
    {
        bool visible = value != 0;
        if (visible) badge.Label.text = $"{caption} {value:+0;-0}";
        badge.Root.SetActive(visible);
    }
}
