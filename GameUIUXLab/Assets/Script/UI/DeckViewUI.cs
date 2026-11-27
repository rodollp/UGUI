using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shows the persistent run deck, including cards currently in any battle pile.
public class DeckViewUI : MonoBehaviour
{
    [SerializeField] private RunManager run;
    [SerializeField] private UIScreenFlowController flow;
    [SerializeField] private TMP_Text textTemplate;
    [SerializeField] private CardUI cardPrefab;
    [SerializeField] private Transform canvasRoot;
    private Button openButton;
    private Button mapButton;
    private GameObject panel;
    private RectTransform content;
    private TMP_Text heading;
    private ScrollRect scroll;
    private GameObject detailPanel;
    private CardUI detailCard;
    private Toggle upgradeToggle;
    private TMP_Text detailNote;
    private CardInstance inspectedCard;
    private readonly List<GameObject> cells = new();
    public bool IsOpen => panel != null && panel.activeSelf;
    public int DisplayedCardCount => cells.Count;
    public bool IsInspectingCard => detailPanel != null && detailPanel.activeSelf;

    private void Start()
    {
        if (!flow.HasCommonHUD)
        {
            openButton = MakeShortcut("OpenDeck", "덱", false, -35, Open);
            mapButton = MakeShortcut("OpenMapPreview", "지도", true, -151, () => flow.OpenMapPreview(canvasRoot));
        }
        panel = Rect("DeckOverlay", canvasRoot).gameObject;
        Stretch((RectTransform)panel.transform, Vector2.zero, Vector2.zero);
        panel.AddComponent<Image>().color = new Color(.035f, .045f, .06f, .99f);
        heading = Label(panel.transform, "DeckTitle", "보유 덱");
        Place(heading.rectTransform, new Vector2(.5f, 1), new Vector2(0, -65), new Vector2(1000, 70));
        Button close = MakeButton(panel.transform, "CloseDeck", "닫기 (ESC)", Close);
        Place((RectTransform)close.transform, new Vector2(1, 1), new Vector2(-155, -65), new Vector2(240, 65));
        RectTransform scrollRoot = Rect("DeckScroll", panel.transform);
        Stretch(scrollRoot, new Vector2(60, 35), new Vector2(-60, -125));
        scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 55;
        RectTransform viewport = Rect("Viewport", scrollRoot);
        Stretch(viewport, Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .01f);
        viewport.gameObject.AddComponent<RectMask2D>();
        content = Rect("Cards", viewport);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(250, 400); grid.spacing = new Vector2(30, 30);
        grid.padding = new RectOffset(30, 30, 30, 40);
        grid.childAlignment = TextAnchor.UpperCenter;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport; scroll.content = content;
        BuildCardDetail();
        panel.SetActive(false);
    }

    private void Update()
    {
        if (openButton == null) return;
        bool available = run.MasterDeck != null && (flow.IsAdventureActive || flow.IsResult);
        openButton.gameObject.SetActive(available && !flow.IsOverlayOpen);
        mapButton.gameObject.SetActive(run.HasMap && flow.CanOpenMapPreview);
    }

    public void Open()
    {
        if (panel == null || flow.IsOverlayOpen || run.MasterDeck == null || !(flow.IsAdventureActive || flow.IsResult)) return;
        foreach (GameObject cell in cells) { cell.SetActive(false); Destroy(cell); }
        cells.Clear();
        foreach (CardInstance card in run.MasterDeck.Cards)
        {
            RectTransform cell = Rect("DeckCard", content);
            cells.Add(cell.gameObject);
            CardUI view = Instantiate(cardPrefab, cell);
            RectTransform rect = (RectTransform)view.transform;
            Place(rect, new Vector2(.5f, .5f), Vector2.zero, new Vector2(240, 380));
            rect.localScale = Vector3.one;
            view.SetBasePosition(Vector2.zero);
            view.SetBrowseMode();
            view.CancelDrag();
            view.SetCard(card);
            view.OnCardClicked += InspectCard;
        }
        heading.text = $"보유 덱 · {cells.Count}장";
        panel.SetActive(true); panel.transform.SetAsLastSibling();
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 1;
        flow.SetDeckOpen(true);
        if (openButton != null) openButton.gameObject.SetActive(false);
    }

    public void Close()
    {
        if (!IsOpen) return;
        CloseCardDetail();
        panel.SetActive(false);
        flow.SetDeckOpen(false);
    }

    public void HandleBack()
    {
        if (IsInspectingCard) CloseCardDetail();
        else Close();
    }

    private void BuildCardDetail()
    {
        detailPanel = Rect("CardDetail", panel.transform).gameObject;
        Stretch((RectTransform)detailPanel.transform, Vector2.zero, Vector2.zero);
        var shade = detailPanel.AddComponent<Image>();
        shade.color = new Color(.025f, .03f, .045f, .97f);
        var backdrop = detailPanel.AddComponent<Button>();
        backdrop.targetGraphic = shade;
        backdrop.transition = Selectable.Transition.None;
        backdrop.onClick.AddListener(CloseCardDetail);

        // Scale the wrapper so CardUI's hover/reset logic cannot undo the enlargement.
        RectTransform holder = Rect("EnlargedCard", detailPanel.transform);
        Place(holder, new Vector2(.5f, .5f), new Vector2(0, 65), new Vector2(240, 380));
        float availableHeight = ((RectTransform)canvasRoot).rect.height;
        holder.localScale = Vector3.one * Mathf.Clamp((availableHeight - 340) / 380, 1, 1.8f);
        detailCard = Instantiate(cardPrefab, holder);
        Place((RectTransform)detailCard.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(240, 380));
        detailCard.SetRewardMode(); detailCard.SetBasePosition(Vector2.zero); detailCard.CancelDrag();

        RectTransform toggleRoot = Rect("UpgradePreview", detailPanel.transform);
        Place(toggleRoot, new Vector2(.5f, 0), new Vector2(0, 180), new Vector2(360, 60));
        var hitArea = toggleRoot.gameObject.AddComponent<Image>(); hitArea.color = Color.clear;
        upgradeToggle = toggleRoot.gameObject.AddComponent<Toggle>();
        RectTransform box = Rect("Checkbox", toggleRoot);
        Place(box, new Vector2(0, .5f), new Vector2(25, 0), new Vector2(44, 44));
        var background = box.gameObject.AddComponent<Image>(); background.color = new Color(.32f, .36f, .42f);
        RectTransform mark = Rect("Checkmark", box);
        Stretch(mark, new Vector2(8, 8), new Vector2(-8, -8));
        var checkmark = mark.gameObject.AddComponent<Image>(); checkmark.color = new Color(.95f, .77f, .35f);
        upgradeToggle.targetGraphic = background; upgradeToggle.graphic = checkmark;
        var caption = Label(toggleRoot, "Caption", "강화 적용시");
        Place(caption.rectTransform, new Vector2(.5f, .5f), new Vector2(40, 0), new Vector2(260, 60));
        upgradeToggle.onValueChanged.AddListener(ShowUpgradePreview);
        detailNote = Label(detailPanel.transform, "PreviewNote", ""); detailNote.fontSize = 24;
        Place(detailNote.rectTransform, new Vector2(.5f, 0), new Vector2(0, 120), new Vector2(1050, 55));
        Button back = MakeButton(detailPanel.transform, "BackToDeck", "덱으로 (ESC)", CloseCardDetail);
        Place((RectTransform)back.transform, new Vector2(.5f, 0), new Vector2(0, 60), new Vector2(280, 65));
        detailPanel.SetActive(false);
    }

    private void InspectCard(CardUI view)
    {
        if (!IsOpen || view == null || view.CardInstance == null) return;
        inspectedCard = view.CardInstance;
        upgradeToggle.SetIsOnWithoutNotify(inspectedCard.IsUpgraded);
        upgradeToggle.interactable = inspectedCard.CanUpgrade;
        detailPanel.SetActive(true); detailPanel.transform.SetAsLastSibling();
        ShowUpgradePreview(upgradeToggle.isOn);
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    private void ShowUpgradePreview(bool upgraded)
    {
        if (inspectedCard == null) return;
        bool preview = upgraded && inspectedCard.CanUpgrade;
        detailCard.SetCard(inspectedCard.CopyForPreview(preview));
        detailNote.text = inspectedCard.IsUpgraded ? "이미 강화된 카드입니다."
            : !inspectedCard.CanUpgrade ? "강화 정보가 없는 카드입니다."
            : preview ? "강화 후 미리보기 · 실제 카드는 변경되지 않습니다." : "현재 카드 · 체크하면 강화 후 효과를 확인할 수 있습니다.";
    }

    private void CloseCardDetail()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
        inspectedCard = null;
    }

    private TMP_Text Label(Transform parent, string name, string caption)
    {
        var label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = textTemplate.font; label.fontSharedMaterial = textTemplate.fontSharedMaterial;
        label.fontSize = 30; label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(.95f, .9f, .78f); label.raycastTarget = false; label.text = caption;
        return label;
    }
    private Button MakeButton(Transform parent, string name, string caption, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = Rect(name, parent);
        var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.22f, .25f, .3f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        button.onClick.AddListener(action);
        Stretch(Label(rect, "Label", caption).rectTransform, Vector2.zero, Vector2.zero);
        return button;
    }
    private Button MakeShortcut(string name, string caption, bool isMap, float rightOffset, UnityEngine.Events.UnityAction action)
    {
        Button button = MakeButton(canvasRoot, name, caption, action);
        var rect = (RectTransform)button.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(rightOffset, -30);
        rect.sizeDelta = new Vector2(108, 65);
        TMP_Text label = button.GetComponentInChildren<TMP_Text>();
        label.fontSize = 22;
        Place(label.rectTransform, new Vector2(.5f, .5f), new Vector2(19, 0), new Vector2(58, 50));
        var icon = Rect("Icon", rect).gameObject.AddComponent<AdventureShortcutIcon>();
        icon.ShowMap = isMap;
        icon.color = label.color;
        icon.raycastTarget = false;
        Place(icon.rectTransform, new Vector2(.5f, .5f), new Vector2(-27, 0), new Vector2(33, 33));
        return button;
    }
    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); return rect;
    }
    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = min; rect.offsetMax = max;
    }
    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position; rect.sizeDelta = size;
    }
    private void OnDisable() => Close();
    private void OnDestroy()
    {
        if (panel != null) Destroy(panel);
        if (openButton != null) Destroy(openButton.gameObject);
        if (mapButton != null) Destroy(mapButton.gameObject);
    }
}
