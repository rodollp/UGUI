using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Scene/prefab owns the presentation; only live values and relic instances change at runtime.
public sealed class RunHUD : MonoBehaviour
{
    [SerializeField] private RunManager run;
    [SerializeField] private RunTimer timer;
    [SerializeField] private UIScreenFlowController flow;
    [SerializeField] private DeckViewUI deck;
    [SerializeField] private Image portrait;
    [SerializeField] private GameObject portraitPlaceholder;
    [SerializeField] private TMP_Text characterText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text nodeText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text deckText;
    [SerializeField] private Button deckButton;
    [SerializeField] private Button mapButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private RectTransform overlayRoot;
    [SerializeField] private RunRelicItem relicTemplate;
    [SerializeField] private TMP_Text emptyRelicsText;
    [SerializeField] private RectTransform tooltip;
    [SerializeField] private TMP_Text tooltipTitle;
    [SerializeField] private TMP_Text tooltipDescription;
    private readonly List<RunRelicItem> relicItems = new();
    private RunRelicItem hovered;
    private (int Version, int Count) relicState = (-1, -1);
    private (PlayerStatus Player, int Hp, int MaxHp, int Gold, int Cards, int Second,
        int Version, MapNode Node, int Floor, bool Paused, bool Deck, bool Map, bool CanPreviewMap) displayed;
    private bool initialized;

    private void Awake()
    {
        relicTemplate.gameObject.SetActive(false);
        tooltip.gameObject.SetActive(false);
        deckButton.onClick.AddListener(ToggleDeck);
        mapButton.onClick.AddListener(ToggleMap);
        settingsButton.onClick.AddListener(ToggleSettings);
    }

    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        if (run == null || run.PlayerStatus == null) return;
        PlayerStatus status = run.PlayerStatus;
        int seconds = timer != null ? (int)timer.PlayTime : 0;
        MapNode node = run.Map?.ActiveNode ?? run.Map?.LastCompletedNode;
        var state = (run.RunVersion, run.Relics.Count);
        var values = (status, status.CurrentHp, status.MaxHp, run.Gold, run.MasterDeck?.Cards.Count ?? 0,
            seconds, run.RunVersion, node, run.Floor, flow.IsPaused, flow.IsDeckOpen, flow.IsMapPreviewOpen, flow.CanOpenMapPreview);
        if (initialized && displayed.Equals(values) && relicState == state) return;
        initialized = true;
        displayed = values;
        SetText(healthText, $"{status.CurrentHp} / {status.MaxHp}");
        healthFill.fillAmount = status.MaxHp > 0 ? (float)status.CurrentHp / status.MaxHp : 0;
        SetText(goldText, run.Gold.ToString());
        SetText(characterText, run.PlayerData != null ? run.PlayerData.CharacterName : "모험가");
        // An Inspector-assigned portrait remains usable when the player data has no sprite yet.
        if (run.PlayerData != null && run.PlayerData.Portrait != null) portrait.sprite = run.PlayerData.Portrait;
        portraitPlaceholder.SetActive(portrait.sprite == null);
        SetText(timeText, TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss"));
        string location = node == null ? (run.HasMap ? "경로 선택" : "전투") : NodeName(node.Type);
        SetText(nodeText, $"{(node != null ? node.Row + 1 : run.Floor)}층 · {location}");
        SetText(deckText, $"덱 {run.MasterDeck?.Cards.Count ?? 0}");
        deckButton.interactable = !flow.IsPaused && !flow.IsMapPreviewOpen && run.MasterDeck != null;
        mapButton.interactable = flow.IsMapPreviewOpen || (run.HasMap && flow.CanOpenMapPreview);
        settingsButton.interactable = !flow.IsOverlayOpen;
        if (relicState == state) return;
        relicState = state;
        HideTooltip(hovered);
        emptyRelicsText.gameObject.SetActive(run.Relics.Count == 0);
        for (int i = 0; i < run.Relics.Count; i++)
        {
            if (i == relicItems.Count) relicItems.Add(Instantiate(relicTemplate, relicTemplate.transform.parent));
            relicItems[i].Bind(this, run.Relics[i]);
            relicItems[i].gameObject.SetActive(true);
        }
        for (int i = run.Relics.Count; i < relicItems.Count; i++) relicItems[i].gameObject.SetActive(false);
    }

    private static string NodeName(MapNodeType type) => type switch
    {
        MapNodeType.Elite => "정예", MapNodeType.Event => "이벤트", MapNodeType.Rest => "휴식",
        MapNodeType.Treasure => "보물", MapNodeType.Boss => "보스", MapNodeType.Shop => "상점", _ => "전투"
    };

    private static void SetText(TMP_Text label, string value)
    {
        if (label.text != value) label.text = value;
    }

    private void ToggleDeck() { if (deck.IsOpen) deck.Close(); else deck.Open(); }
    private void ToggleMap() { if (!flow.TryCloseMapPreview()) flow.OpenMapPreview(overlayRoot); }
    private void ToggleSettings() { if (flow.IsPaused) flow.ResumeGame(); else flow.ShowPauseScreen(); }

    public void ShowTooltip(RunRelicItem item, RelicData relic)
    {
        if (hovered != null) hovered.Highlight(false);
        hovered = item;
        item.Highlight(true);
        tooltipTitle.text = relic.DisplayName;
        tooltipDescription.text = relic.GetEffectDescription();
        float descriptionHeight = tooltipDescription.GetPreferredValues(tooltipDescription.text,
            tooltip.rect.width - 36, Mathf.Infinity).y;
        tooltip.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(110, descriptionHeight + 76));
        Canvas.ForceUpdateCanvases();
        RectTransform root = (RectTransform)transform;
        Vector3 local = root.InverseTransformPoint(item.transform.position);
        float left = Mathf.Clamp(local.x - root.rect.xMin, 12, Mathf.Max(12, root.rect.width - tooltip.rect.width - 12));
        tooltip.anchoredPosition = new Vector2(left, -root.rect.height - 6);
        tooltip.SetAsLastSibling();
        tooltip.gameObject.SetActive(true);
    }

    public void HideTooltip(RunRelicItem item)
    {
        if (hovered != item) return;
        if (hovered != null) hovered.Highlight(false);
        hovered = null;
        if (tooltip != null) tooltip.gameObject.SetActive(false);
    }

    private void OnEnable() => initialized = false;
    private void OnDisable() => HideTooltip(hovered);
}
