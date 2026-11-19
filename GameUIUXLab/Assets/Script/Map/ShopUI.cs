using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    private AdventureController adventure;
    private CardUI template;
    private TMP_Text fontTemplate;
    private TMP_Text wallet, message, removalHeading, detailNote;
    private Button removeButton, confirmButton;
    private readonly List<CardUI> offers = new();
    private readonly List<TMP_Text> prices = new();
    private readonly List<CardUI> removalViews = new();
    private GameObject removalPanel, detailPanel;
    private RectTransform removalContent;
    private ScrollRect removalScroll;
    private CardUI detailCard;
    private Toggle previewToggle;
    private CardInstance selectedRemoval;
    private CardInstance previewCard;
    private int selectedOffer = -1;
    private bool balancesDirty;
    public IReadOnlyList<CardUI> OfferViews => offers;
    public IReadOnlyList<CardUI> RemovalViews => removalViews;
    public Button ConfirmButton => confirmButton;

    public void Initialize(AdventureController controller, CardUI cardTemplate, TMP_Text textTemplate)
    {
        adventure = controller; template = cardTemplate; fontTemplate = textTemplate;
        gameObject.AddComponent<Image>().color = new Color(.045f, .055f, .07f, 1);
        Stretch((RectTransform)transform, Vector2.zero, Vector2.zero);
        Text(transform, "Title", "떠돌이 상점", new Vector2(0, -55), new Vector2(1000, 65), new Vector2(.5f, 1), 38);
        wallet = Text(transform, "Wallet", "", new Vector2(0, -115), new Vector2(1000, 50), new Vector2(.5f, 1));
        for (int i = 0; i < ShopInventory.CardCount; i++)
        {
            RectTransform slot = Rect("Offer" + i, transform);
            Place(slot, new Vector2(.5f, .5f), new Vector2((i - 2) * 285, 80), new Vector2(260, 460));
            CardUI view = Card(slot, new Vector2(0, 35));
            int index = i; view.OnCardClicked += _ => SelectOffer(index);
            offers.Add(view);
            prices.Add(Text(slot, "Price", "", new Vector2(0, -200), new Vector2(280, 90)));
        }
        message = Text(transform, "Message", "", new Vector2(0, 245), new Vector2(1500, 60), new Vector2(.5f, 0), 26);
        removeButton = Button(transform, "RemoveCard", "", new Vector2(0, 155), OpenRemoval, new Vector2(.5f, 0), 460);
        Button(transform, "LeaveShop", "상점 나가기", new Vector2(0, 65), adventure.LeaveShop, new Vector2(.5f, 0));
        BuildRemoval(); BuildDetail();
    }
    private void OnEnable()
    {
        if (adventure == null) return;
        adventure.OnEventChanged += Refresh;
        adventure.Run.OnGoldChanged += MarkBalances;
        Refresh();
    }
    private void OnDisable()
    {
        if (adventure == null) return;
        adventure.OnEventChanged -= Refresh;
        adventure.Run.OnGoldChanged -= MarkBalances;
        CloseDetail();
        removalPanel.SetActive(false);
    }
    private void MarkBalances() => balancesDirty = true;
    private void LateUpdate()
    {
        if (balancesDirty) RefreshBalances();
    }
    private void Refresh()
    {
        if (!adventure.IsShopping) return;
        CloseDetail();
        ShopInventory shop = adventure.CurrentShop;
        for (int i = 0; i < offers.Count; i++) offers[i].SetCard(new CardInstance(shop.Cards[i]));
        if (shop.CardRemovalUsed) CloseRemoval();
        else if (removalPanel.activeSelf) RefreshRemoval();
        RefreshBalances();
    }
    private void RefreshBalances()
    {
        balancesDirty = false;
        if (!adventure.IsShopping) return;
        RunManager run = adventure.Run;
        ShopInventory shop = adventure.CurrentShop;
        wallet.text = $"보유 골드 {run.Gold} · 덱 {run.MasterDeck.Cards.Count}장";
        message.text = adventure.ShopMessage;
        for (int i = 0; i < offers.Count; i++)
        {
            bool sold = shop.IsSold(i);
            prices[i].text = sold ? "품절" : i == shop.DiscountIndex
                ? $"50% 할인\n<s>{shop.BasePrice}골드</s> → {shop.Price(i)}골드" : $"{shop.Price(i)}골드";
            prices[i].color = sold ? Color.gray : i == shop.DiscountIndex
                ? new Color(1, .8f, .35f) : new Color(.95f, .9f, .78f);
            offers[i].GetComponent<Button>().interactable = !sold;
        }
        removeButton.GetComponentInChildren<TMP_Text>().text = shop.CardRemovalUsed
            ? "카드 제거 · 이용 완료" : $"카드 제거 · {run.CardRemovalPrice}골드";
        removeButton.interactable = adventure.CanRemoveShopCard;
        removalHeading.text = $"카드 제거 · {run.CardRemovalPrice}골드 · 보유 {run.Gold}골드\n제거할 카드를 선택하세요";
        if (detailPanel.activeSelf) RefreshDetailAction();
    }
    private void BuildRemoval()
    {
        removalPanel = Rect("RemovePicker", transform).gameObject;
        Stretch((RectTransform)removalPanel.transform, Vector2.zero, Vector2.zero);
        removalPanel.AddComponent<Image>().color = new Color(.035f, .045f, .06f, 1);
        removalHeading = Text(removalPanel.transform, "Title", "", new Vector2(0, -70), new Vector2(1300, 100), new Vector2(.5f, 1));
        RectTransform root = Rect("Scroll", removalPanel.transform);
        Stretch(root, new Vector2(60, 115), new Vector2(-60, -140));
        removalScroll = root.gameObject.AddComponent<ScrollRect>();
        removalScroll.horizontal = false; removalScroll.movementType = ScrollRect.MovementType.Clamped; removalScroll.scrollSensitivity = 55;
        RectTransform viewport = Rect("Viewport", root);
        Stretch(viewport, Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .01f);
        viewport.gameObject.AddComponent<RectMask2D>();
        removalContent = Rect("Cards", viewport);
        removalContent.anchorMin = new Vector2(0, 1); removalContent.anchorMax = Vector2.one;
        removalContent.pivot = new Vector2(.5f, 1); removalContent.sizeDelta = Vector2.zero;
        var grid = removalContent.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(250, 400); grid.spacing = new Vector2(30, 30);
        grid.padding = new RectOffset(30, 30, 30, 40); grid.childAlignment = TextAnchor.UpperCenter;
        removalContent.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        removalScroll.viewport = viewport; removalScroll.content = removalContent;
        Button(removalPanel.transform, "Back", "상점으로 (ESC)", new Vector2(0, 60), CloseRemoval, new Vector2(.5f, 0));
        removalPanel.SetActive(false);
    }
    private void OpenRemoval()
    {
        if (!adventure.CanRemoveShopCard) return;
        removalPanel.SetActive(true); removalPanel.transform.SetAsLastSibling();
        RefreshRemoval(); RefreshBalances();
    }
    private void RefreshRemoval()
    {
        foreach (CardUI view in removalViews)
        {
            view.transform.parent.gameObject.SetActive(false);
            Destroy(view.transform.parent.gameObject);
        }
        removalViews.Clear();
        foreach (CardInstance card in adventure.Run.MasterDeck.Cards)
        {
            CardUI view = Card(Rect("RemovalCard", removalContent), Vector2.zero);
            view.SetCard(card); view.SetBrowseMode();
            view.OnCardClicked += v => SelectRemoval(v.CardInstance);
            removalViews.Add(view);
        }
        Canvas.ForceUpdateCanvases(); removalScroll.verticalNormalizedPosition = 1;
    }
    private void CloseRemoval() => removalPanel.SetActive(false);
    private void BuildDetail()
    {
        detailPanel = Rect("ShopCardDetail", transform).gameObject;
        Stretch((RectTransform)detailPanel.transform, Vector2.zero, Vector2.zero);
        var shade = detailPanel.AddComponent<Image>(); shade.color = new Color(.025f, .03f, .045f, .98f);
        var backdrop = detailPanel.AddComponent<Button>(); backdrop.targetGraphic = shade;
        backdrop.transition = Selectable.Transition.None; backdrop.onClick.AddListener(CloseDetail);
        RectTransform holder = Rect("EnlargedCard", detailPanel.transform);
        Place(holder, new Vector2(.5f, .5f), new Vector2(0, 65), new Vector2(240, 380));
        holder.localScale = Vector3.one * 1.8f;
        detailCard = Card(holder, Vector2.zero);
        RectTransform toggleRoot = Rect("UpgradePreview", detailPanel.transform);
        Place(toggleRoot, new Vector2(.5f, .5f), new Vector2(0, -325), new Vector2(360, 60));
        toggleRoot.gameObject.AddComponent<Image>().color = Color.clear;
        previewToggle = toggleRoot.gameObject.AddComponent<Toggle>();
        RectTransform box = Rect("Checkbox", toggleRoot);
        Place(box, new Vector2(0, .5f), new Vector2(25, 0), new Vector2(44, 44));
        var background = box.gameObject.AddComponent<Image>(); background.color = new Color(.32f, .36f, .42f);
        RectTransform mark = Rect("Checkmark", box); Stretch(mark, new Vector2(8, 8), new Vector2(-8, -8));
        var check = mark.gameObject.AddComponent<Image>(); check.color = new Color(.95f, .77f, .35f);
        previewToggle.targetGraphic = background; previewToggle.graphic = check;
        Text(toggleRoot, "Caption", "강화 적용시", new Vector2(40, 0), new Vector2(260, 60));
        previewToggle.onValueChanged.AddListener(Preview);
        detailNote = Text(detailPanel.transform, "Note", "", new Vector2(0, -390), new Vector2(1300, 55), fontSize: 25);
        confirmButton = Button(detailPanel.transform, "Confirm", "", new Vector2(-200, 60), Confirm, new Vector2(.5f, 0), 360);
        Button(detailPanel.transform, "Back", "돌아가기 (ESC)", new Vector2(200, 60), CloseDetail, new Vector2(.5f, 0), 320);
        detailPanel.SetActive(false);
    }
    private void SelectOffer(int index)
    {
        if (!adventure.IsShopping || adventure.CurrentShop.IsSold(index)) return;
        selectedOffer = index; selectedRemoval = null;
        ShowDetail(new CardInstance(adventure.CurrentShop.Cards[index]));
    }
    private void SelectRemoval(CardInstance card)
    {
        selectedRemoval = card; selectedOffer = -1;
        ShowDetail(card);
    }
    private void ShowDetail(CardInstance card)
    {
        previewCard = card;
        previewToggle.SetIsOnWithoutNotify(card.IsUpgraded); previewToggle.interactable = card.CanUpgrade;
        detailPanel.SetActive(true); detailPanel.transform.SetAsLastSibling();
        Preview(previewToggle.isOn); RefreshDetailAction();
        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }
    private void Preview(bool upgraded)
    {
        if (previewCard == null) return;
        detailCard.SetCard(new CardInstance(upgraded && previewCard.CanUpgrade ? previewCard.CardData.UpgradedCard : previewCard.CardData));
    }
    private void RefreshDetailAction()
    {
        int price = selectedRemoval != null ? adventure.Run.CardRemovalPrice : adventure.CurrentShop.Price(selectedOffer);
        bool canPay = adventure.Run.Gold >= price;
        confirmButton.GetComponentInChildren<TMP_Text>().text = $"{(selectedRemoval != null ? "제거 확정" : "구매 확정")} · {price}골드";
        confirmButton.interactable = canPay;
        detailNote.text = !canPay ? $"골드가 부족합니다. · 보유 {adventure.Run.Gold}골드"
            : selectedRemoval != null ? "확정하면 선택한 카드 한 장을 덱에서 제거합니다."
            : "확정하면 현재 카드를 구매합니다. 강화 체크는 미리보기입니다.";
    }
    private void Confirm()
    {
        if (selectedRemoval != null) adventure.RemoveShopCard(selectedRemoval);
        else if (selectedOffer >= 0) adventure.BuyShopCard(selectedOffer);
    }
    private void CloseDetail()
    {
        if (detailPanel != null) detailPanel.SetActive(false);
        selectedRemoval = null; previewCard = null; selectedOffer = -1;
    }
    public bool HandleBack()
    {
        if (!adventure.IsShopping) return false;
        if (detailPanel.activeSelf) CloseDetail();
        else if (removalPanel.activeSelf) CloseRemoval();
        else adventure.LeaveShop();
        return true;
    }
    private CardUI Card(Transform parent, Vector2 position)
    {
        // Inactive wrapper lets CardUI capture the final size/scale in Awake.
        bool active = parent.gameObject.activeSelf; parent.gameObject.SetActive(false);
        CardUI view = Instantiate(template, parent);
        Place((RectTransform)view.transform, new Vector2(.5f, .5f), position, new Vector2(240, 380));
        view.transform.localScale = Vector3.one; view.gameObject.SetActive(true);
        parent.gameObject.SetActive(active);
        view.SetBasePosition(position); view.SetRewardMode(); view.CancelDrag();
        return view;
    }
    private TMP_Text Text(Transform parent, string name, string caption, Vector2 pos, Vector2 size, Vector2? anchor = null, float fontSize = 28)
    {
        var label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = fontTemplate.font; label.fontSharedMaterial = fontTemplate.fontSharedMaterial;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        label.color = new Color(.95f, .9f, .78f); label.text = caption;
        Place(label.rectTransform, anchor ?? new Vector2(.5f, .5f), pos, size); return label;
    }
    private Button Button(Transform parent, string name, string caption, Vector2 pos, UnityEngine.Events.UnityAction action, Vector2 anchor, float width = 300)
    {
        var rect = Rect(name, parent); Place(rect, anchor, pos, new Vector2(width, 70));
        var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.22f, .25f, .3f);
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(action);
        Stretch(Text(rect, "Label", caption, Vector2.zero, Vector2.zero).rectTransform, Vector2.zero, Vector2.zero);
        return button;
    }
    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
    }
    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = min; rect.offsetMax = max; }
    private static void Place(RectTransform rect, Vector2 anchor, Vector2 pos, Vector2 size)
    { rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = pos; rect.sizeDelta = size; }
}
