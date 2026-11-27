using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardUpgradeUI : MonoBehaviour
{
    [SerializeField] private AdventureController adventure;
    // Keep existing scene references as templates; pagination is no longer used.
    [SerializeField] private CardUI[] cards;
    [SerializeField] private TMP_Text prompt;
    [SerializeField] private Button previous;
    [SerializeField] private Button next;
    [SerializeField] private Button cancel;
    [SerializeField] private Button confirm;
    private readonly List<CardUI> candidateViews = new();
    private RectTransform content;
    private ScrollRect scroll;
    private GameObject detailPanel;
    private CardUI detailCard;
    private Toggle upgradeToggle;
    private TMP_Text detailNote;
    private CardInstance selected;
    public IReadOnlyList<CardUI> CandidateViews => candidateViews;
    public bool IsInspectingCard => detailPanel != null && detailPanel.activeSelf;

    private void Awake()
    {
        BuildPicker();
        cancel.onClick.AddListener(adventure.CancelUpgrade);
        confirm.onClick.AddListener(Confirm);
    }
    private void OnEnable()
    {
        selected = null;
        adventure.OnEventChanged += Refresh;
        Refresh();
    }
    private void OnDisable()
    {
        adventure.OnEventChanged -= Refresh;
        CloseDetail();
    }
    private void OnDestroy()
    {
        if (cancel != null) cancel.onClick.RemoveListener(adventure.CancelUpgrade);
        if (confirm != null) confirm.onClick.RemoveListener(Confirm);
    }
    private void BuildPicker()
    {
        previous.gameObject.SetActive(false); next.gameObject.SetActive(false);
        foreach (CardUI card in cards) card.gameObject.SetActive(false);
        Place(prompt.rectTransform, new Vector2(.5f, 1), new Vector2(0, -70), new Vector2(1300, 100));
        prompt.fontSize = 28;
        Place((RectTransform)cancel.transform, new Vector2(.5f, 0), new Vector2(0, 60), new Vector2(270, 70));
        RectTransform scrollRoot = Rect("UpgradeScroll", transform);
        Stretch(scrollRoot, new Vector2(60, 115), new Vector2(-60, -140));
        scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 55;
        RectTransform viewport = Rect("Viewport", scrollRoot);
        Stretch(viewport, Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .01f);
        viewport.gameObject.AddComponent<RectMask2D>();
        content = Rect("Cards", viewport);
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1); content.sizeDelta = Vector2.zero;
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(250, 400); grid.spacing = new Vector2(30, 30);
        grid.padding = new RectOffset(30, 30, 30, 40); grid.childAlignment = TextAnchor.UpperCenter;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport; scroll.content = content;
        detailPanel = Rect("UpgradeDetail", transform).gameObject;
        Stretch((RectTransform)detailPanel.transform, Vector2.zero, Vector2.zero);
        var shade = detailPanel.AddComponent<Image>(); shade.color = new Color(.025f, .03f, .045f, .98f);
        var backdrop = detailPanel.AddComponent<Button>(); backdrop.targetGraphic = shade;
        backdrop.transition = Selectable.Transition.None; backdrop.onClick.AddListener(CloseDetail);
        RectTransform holder = Rect("EnlargedCard", detailPanel.transform);
        Place(holder, new Vector2(.5f, .5f), new Vector2(0, 65), new Vector2(240, 380));
        holder.localScale = Vector3.one * Mathf.Clamp((((RectTransform)transform).rect.height - 360) / 380, 1, 1.8f);
        detailCard = MakeCard(holder);
        RectTransform toggleRoot = Rect("UpgradePreview", detailPanel.transform);
        Place(toggleRoot, new Vector2(.5f, 0), new Vector2(0, 200), new Vector2(360, 60));
        toggleRoot.gameObject.AddComponent<Image>().color = Color.clear;
        upgradeToggle = toggleRoot.gameObject.AddComponent<Toggle>();
        RectTransform box = Rect("Checkbox", toggleRoot);
        Place(box, new Vector2(0, .5f), new Vector2(25, 0), new Vector2(44, 44));
        var background = box.gameObject.AddComponent<Image>(); background.color = new Color(.32f, .36f, .42f);
        RectTransform mark = Rect("Checkmark", box); Stretch(mark, new Vector2(8, 8), new Vector2(-8, -8));
        var checkmark = mark.gameObject.AddComponent<Image>(); checkmark.color = new Color(.95f, .77f, .35f);
        upgradeToggle.targetGraphic = background; upgradeToggle.graphic = checkmark;
        var caption = Label(toggleRoot, "Caption", "강화 적용시", 30);
        Place(caption.rectTransform, new Vector2(.5f, .5f), new Vector2(40, 0), new Vector2(260, 60));
        upgradeToggle.onValueChanged.AddListener(Preview);
        detailNote = Label(detailPanel.transform, "PreviewNote", "", 24);
        Place(detailNote.rectTransform, new Vector2(.5f, 0), new Vector2(0, 130), new Vector2(1200, 55));
        confirm.transform.SetParent(detailPanel.transform, false);
        Place((RectTransform)confirm.transform, new Vector2(.5f, 0), new Vector2(-160, 60), new Vector2(270, 70));
        var back = Rect("BackToList", detailPanel.transform);
        var image = back.gameObject.AddComponent<Image>(); image.color = new Color(.22f, .25f, .3f);
        var button = back.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        button.onClick.AddListener(CloseDetail);
        Place(back, new Vector2(.5f, 0), new Vector2(160, 60), new Vector2(270, 70));
        Stretch(Label(back, "Label", "목록으로 (ESC)", 28).rectTransform, Vector2.zero, Vector2.zero);
        detailPanel.SetActive(false);
    }
    private CardUI MakeCard(Transform parent)
    {
        CardUI view = Instantiate(cards[0], parent);
        Place((RectTransform)view.transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(240, 380));
        view.transform.localScale = Vector3.one;
        view.gameObject.SetActive(true);
        view.SetBasePosition(Vector2.zero); view.SetRewardMode(); view.CancelDrag();
        return view;
    }
    private void Refresh()
    {
        if (!adventure.IsChoosingUpgrade) return;
        CloseDetail();
        foreach (CardUI view in candidateViews)
        {
            view.OnCardClicked -= Select;
            view.transform.parent.gameObject.SetActive(false);
            Destroy(view.transform.parent.gameObject);
        }
        candidateViews.Clear();
        foreach (CardInstance card in adventure.UpgradeCandidates)
        {
            var cell = Rect("UpgradeCard", content);
            CardUI view = MakeCard(cell);
            view.SetCard(card); view.SetBrowseMode(); view.OnCardClicked += Select;
            candidateViews.Add(view);
        }
        prompt.text = $"카드 강화 · 강화 가능한 카드 {candidateViews.Count}장\n카드를 선택하면 강화 후 모습을 확인할 수 있습니다.";
        Canvas.ForceUpdateCanvases(); scroll.verticalNormalizedPosition = 1;
    }
    private void Select(CardUI view)
    {
        if (!isActiveAndEnabled || !adventure.IsChoosingUpgrade
            || !candidateViews.Contains(view) || !view.CardInstance.CanUpgrade) return;
        selected = view.CardInstance;
        upgradeToggle.SetIsOnWithoutNotify(true);
        detailPanel.SetActive(true); detailPanel.transform.SetAsLastSibling();
        Preview(true);
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }
    private void Preview(bool upgraded)
    {
        if (selected == null || !selected.CanUpgrade) return;
        detailCard.SetCard(selected.CopyForPreview(upgraded));
        detailNote.text = $"{selected.Version.CardName} → {selected.CardData.Upgrade.CardName} · "
            + (upgraded ? "강화 후 미리보기" : "현재 카드") + " · 강화 확정 시에만 적용됩니다.";
        confirm.interactable = true;
    }
    private void Confirm()
    {
        if (IsInspectingCard && selected != null) adventure.ConfirmUpgrade(selected);
    }
    private void CloseDetail()
    {
        selected = null;
        if (detailPanel != null) detailPanel.SetActive(false);
        if (confirm != null) confirm.interactable = false;
    }
    public bool HandleBack()
    {
        if (!adventure.IsChoosingUpgrade) return false;
        if (IsInspectingCard) CloseDetail(); else adventure.CancelUpgrade();
        return true;
    }
    private TMP_Text Label(Transform parent, string name, string caption, float size)
    {
        var label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = prompt.font; label.fontSharedMaterial = prompt.fontSharedMaterial;
        label.fontSize = size; label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(.95f, .9f, .78f); label.raycastTarget = false; label.text = caption;
        return label;
    }
    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); return rect;
    }
    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = min; rect.offsetMax = max;
    }
    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = pos; rect.sizeDelta = size;
    }
}
