using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class EnemyIntentView
{
    private const float BadgeWidth = 208;
    private const float BadgeHeight = 74;
    private const float DebuffRowHeight = 40;
    private static readonly Color PanelColor = new Color(.06f, .13f, .15f, .96f);
    private static readonly Color TextColor = new Color(.98f, .92f, .81f);

    private readonly RectTransform parent;
    private readonly RectTransform badge;
    private readonly Image trim;
    private readonly EnemyIntentIcon icon;
    private readonly TMP_Text enemyName;
    private readonly TMP_Text actionText;
    private readonly TMP_Text activeDebuffs;
    private Enemy enemy;
    private Renderer model;
    private (EnemyActionType Type, DebuffType Debuff, int Amount, int Hits)? displayedAction;
    private (int Vulnerable, int Weak, int Frail) displayedDebuffs;
    private bool HasIntent => enemy != null && enemy.Status != null && !enemy.Status.IsDead && enemy.Intent != null;

    public EnemyIntentView(RectTransform parent, TMP_Text fontTemplate)
    {
        this.parent = parent;
        var panel = new GameObject("EnemyIntent", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        badge = panel.rectTransform;
        badge.SetParent(parent, false);
        badge.anchorMin = badge.anchorMax = parent.pivot;
        badge.pivot = new Vector2(.5f, 0);
        badge.sizeDelta = new Vector2(BadgeWidth, BadgeHeight);
        panel.color = PanelColor;
        panel.raycastTarget = false;
        trim = Add<Image>("Trim", new Vector2(0, -2), new Vector2(200, 3));
        Add<Image>("Divider", new Vector2(0, -42), new Vector2(182, 1)).color = new Color(.3f, .4f, .4f);
        icon = Add<EnemyIntentIcon>("ActionIcon", new Vector2(-76, -21), new Vector2(30, 30));
        actionText = Label("Action", fontTemplate, 23, new Vector2(16, -21), new Vector2(152, 33));
        enemyName = Label("EnemyName", fontTemplate, 21, new Vector2(0, -58), new Vector2(192, 26));
        enemyName.color = TextColor;
        activeDebuffs = Label("ActiveDebuffs", fontTemplate, 17, new Vector2(0, -92), new Vector2(192, 36));
        activeDebuffs.color = new Color(1f, .80f, .53f);
        activeDebuffs.textWrappingMode = TextWrappingModes.Normal;
        activeDebuffs.gameObject.SetActive(false);
        Hide();
    }

    public void Refresh(Enemy target, PlayerStatus player)
    {
        Bind(target);
        if (!HasIntent)
        {
            Hide();
            return;
        }

        string name = enemy.Data.EnemyName;
        if (enemyName.text != name) enemyName.text = name;
        RefreshDebuffs(enemy.Status);
        RefreshAction(enemy.Intent, enemy.Status, player);
    }

    public void Clear()
    {
        Bind(null);
        Hide();
    }

    private void Bind(Enemy target)
    {
        if (ReferenceEquals(enemy, target)) return;
        if (enemy != null) enemy.SetHealthBarDebuffVisibility(true);
        enemy = target;
        if (enemy != null) enemy.SetHealthBarDebuffVisibility(false);
        model = enemy != null ? enemy.GetComponentInChildren<Renderer>() : null;
    }

    private void RefreshAction(EnemyAction action, EnemyStatus status, PlayerStatus player)
    {
        var next = (
            action.Type,
            Debuff: action.Type == EnemyActionType.Debuff ? action.Debuff : default,
            Amount: action.Type switch
            {
                EnemyActionType.Attack => status.CalculateAttackDamage(action.Amount, player),
                EnemyActionType.Debuff => action.DebuffTurns,
                _ => action.Amount
            },
            Hits: action.Type == EnemyActionType.Attack ? Mathf.Max(1, action.Hits) : 1);
        if (displayedAction == next) return;
        displayedAction = next;

        (string label, Color accent) = next.Type switch
        {
            EnemyActionType.Block => ($"방어 {next.Amount}", new Color(.51f, .83f, 1f)),
            EnemyActionType.Strength => ($"힘 +{next.Amount}", new Color(1f, .83f, .43f)),
            EnemyActionType.Debuff => ($"{DebuffNames.Get(next.Debuff)} {next.Amount}턴 부여", new Color(.86f, .69f, 1f)),
            _ => (next.Hits > 1 ? $"공격 {next.Amount} × {next.Hits}" : $"공격 {next.Amount}", new Color(1f, .59f, .47f))
        };
        actionText.text = label;
        trim.color = icon.color = actionText.color = accent;
        icon.SetKind(next.Type);
    }

    private void RefreshDebuffs(EnemyStatus status)
    {
        var durations = (status.VulnerableTurns, status.WeakTurns, status.FrailTurns);
        if (displayedDebuffs == durations) return;
        displayedDebuffs = durations;
        activeDebuffs.text = status.GetDebuffDescription(includeTurnUnit: true);
        bool visible = activeDebuffs.text.Length > 0;
        badge.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, BadgeHeight + (visible ? DebuffRowHeight : 0));
        activeDebuffs.gameObject.SetActive(visible);
    }

    public void FollowEnemy(Camera battleCamera, Camera uiCamera)
    {
        if (!HasIntent || !enemy.gameObject.activeInHierarchy)
        {
            Hide();
            return;
        }

        Vector3 head = enemy.transform.position + Vector3.up;
        if (model != null)
        {
            Bounds bounds = model.bounds;
            head = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }

        Vector3 screenPoint = battleCamera.WorldToScreenPoint(head);
        Rect area = parent.rect;
        if (screenPoint.z <= 0 || !RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent, screenPoint, uiCamera, out Vector2 point) || !area.Contains(point))
        {
            Hide();
            return;
        }

        float horizontalRoom = Mathf.Max(0, (area.width - BadgeWidth) * .5f - 6);
        point.x = Mathf.Clamp(point.x, area.center.x - horizontalRoom, area.center.x + horizontalRoom);
        point.y = Mathf.Min(point.y + 12, area.yMax - badge.rect.height - 6);
        if (badge.anchoredPosition != point) badge.anchoredPosition = point;
        if (!badge.gameObject.activeSelf) badge.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (badge.gameObject.activeSelf) badge.gameObject.SetActive(false);
    }

    private T Add<T>(string name, Vector2 position, Vector2 size) where T : Graphic
    {
        var graphic = new GameObject(name, typeof(RectTransform), typeof(T)).GetComponent<T>();
        var rect = graphic.rectTransform;
        rect.SetParent(badge, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        graphic.raycastTarget = false;
        return graphic;
    }

    private TMP_Text Label(string name, TMP_Text fontTemplate, float fontSize, Vector2 position, Vector2 size)
    {
        var text = Add<TextMeshProUGUI>(name, position, size);
        if (fontTemplate != null)
        {
            text.font = fontTemplate.font;
            text.fontSharedMaterial = fontTemplate.fontSharedMaterial;
        }
        text.enableAutoSizing = true;
        text.fontSize = text.fontSizeMax = fontSize;
        text.fontSizeMin = 14;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }
}
