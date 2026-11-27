using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleRewardUI : MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private RunManager runManager;
    [SerializeField] private GameObject choicesRoot;
    [SerializeField] private CardUI[] rewardCards;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private Button continueButton;
    private BattleRewardOffer activeOffer, displayedOffer;
    private RectTransform summary;
    private GameObject backdrop;
    private TMP_Text relicText, cardResultText;
    private Button goldRewardButton, relicRewardButton, cardRewardButton, backButton;
    private Transform titleButton;
    private bool cardChoicesOpen;

    private void Awake()
    {
        titleButton = transform.Find("TitleButton");
        BuildRewardSummary();
        if (continueButton != null) continueButton.onClick.AddListener(Continue);
        foreach (CardUI card in rewardCards)
        {
            card.SetRewardMode();
            card.OnCardClicked += HandleChoice;
        }
    }

    private void OnEnable()
    {
        battleManager.OnBattleChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => battleManager.OnBattleChanged -= Refresh;

    private void OnDestroy()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
        if (rewardCards == null) return;
        foreach (CardUI card in rewardCards)
            if (card != null) card.OnCardClicked -= HandleChoice;
    }

    private void HandleChoice(CardUI card)
    {
        if (cardChoicesOpen && choicesRoot.activeInHierarchy)
            battleManager.TrySelectRewardCard(Array.IndexOf(rewardCards, card));
    }

    public void Continue()
    {
        if (!TryBackToSummary()) battleManager.ContinueAfterRewards();
    }

    public bool TryBackToSummary()
    {
        if (!cardChoicesOpen) return false;
        SetCardChoicesOpen(false);
        return true;
    }

    private void SetCardChoicesOpen(bool open)
    {
        cardChoicesOpen = open && battleManager.State == BattleState.Victory
            && battleManager.HasPendingCardReward;
        Refresh();
    }

    private void Refresh()
    {
        BattleRewardOffer offer = battleManager.RewardOffer;
        bool won = battleManager.State == BattleState.Victory && offer != null;
        bool pending = won && battleManager.HasPendingCardReward;
        if (!ReferenceEquals(activeOffer, offer))
        {
            activeOffer = offer;
            displayedOffer = null;
            cardChoicesOpen = false;
        }
        if (!pending) cardChoicesOpen = false;

        if (titleButton != null)
            titleButton.gameObject.SetActive(battleManager.State == BattleState.Defeat);
        backdrop.SetActive(won);
        summary.gameObject.SetActive(won && !cardChoicesOpen);
        choicesRoot.SetActive(cardChoicesOpen);
        promptText.gameObject.SetActive(cardChoicesOpen);
        backButton.gameObject.SetActive(cardChoicesOpen);
        if (continueButton != null) continueButton.interactable = !cardChoicesOpen;
        if (!won) return;

        goldRewardButton.gameObject.SetActive(battleManager.HasPendingGoldReward);
        goldText.text = $"골드 {offer.Gold}";
        relicRewardButton.gameObject.SetActive(battleManager.HasPendingRelicReward);
        if (offer.Relic != null) relicText.text = $"{offer.Relic.DisplayName} 받기";
        cardRewardButton.gameObject.SetActive(pending);
        cardResultText.transform.parent.gameObject.SetActive(!pending && offer.SelectedCard != null);
        if (offer.SelectedCard != null)
            cardResultText.text = $"{offer.SelectedCard.CardName} 카드를 덱에 추가했습니다.";

        if (!cardChoicesOpen || ReferenceEquals(displayedOffer, offer)) return;
        displayedOffer = offer;
        for (int i = 0; i < rewardCards.Length; i++)
        {
            rewardCards[i].gameObject.SetActive(true);
            rewardCards[i].SetCard(new CardInstance(offer.Choices[i]));
        }
    }

    private void BuildRewardSummary()
    {
        var shade = new GameObject("RewardBackdrop", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        shade.transform.SetParent(transform, false);
        shade.transform.SetAsFirstSibling();
        Stretch(shade.rectTransform, 0);
        shade.color = new Color(.018f, .024f, .043f, .83f);
        shade.raycastTarget = false;
        backdrop = shade.gameObject;

        summary = new GameObject("RewardSummary", typeof(RectTransform), typeof(Image),
            typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        summary.SetParent(transform, false);
        summary.anchorMin = summary.anchorMax = new Vector2(.5f, .5f);
        summary.anchoredPosition = new Vector2(0, 44);
        summary.sizeDelta = new Vector2(500, 0);
        var background = summary.GetComponent<Image>();
        background.color = new Color(.075f, .11f, .16f, .98f);
        background.raycastTarget = false;
        var border = summary.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(.60f, .43f, .23f, .9f);
        border.effectDistance = new Vector2(2, -2);
        var layout = summary.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(22, 22, 18, 22);
        layout.spacing = 10;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        summary.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var title = AddLabel(summary, "Heading", "보상", 36);
        title.color = new Color(.96f, .78f, .47f);
        title.alignment = TextAlignmentOptions.Center;
        SetRowHeight(title, 56);
        var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        divider.transform.SetParent(summary, false);
        divider.color = new Color(.60f, .43f, .23f);
        divider.raycastTarget = false;
        divider.gameObject.AddComponent<LayoutElement>().preferredHeight = 2;

        relicText = AddLabel(summary, "RewardRelicText", "", 25);
        cardResultText = AddLabel(summary, "CardRewardResultText", "", 24);
        goldRewardButton = AddRewardRow("GoldRow", goldText, new Color(.95f, .72f, .32f),
            () => battleManager.TryClaimGoldReward());
        relicRewardButton = AddRewardRow("RewardRelic", relicText, new Color(.69f, .57f, .85f),
            () => battleManager.TryClaimRelicReward());
        cardRewardButton = AddButton(summary, "OpenCardReward", "카드 보상   ›", () => SetCardChoicesOpen(true));
        cardRewardButton.image.color = new Color(.26f, .37f, .47f);
        cardRewardButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 54;
        AddRewardRow("CardRewardResult", cardResultText, new Color(.51f, .69f, .83f));

        if (continueButton != null)
        {
            var next = (RectTransform)continueButton.transform;
            next.anchorMin = next.anchorMax = new Vector2(1, .5f);
            next.anchoredPosition = new Vector2(-195, -170);
            next.sizeDelta = new Vector2(230, 66);
            if (continueButton.image != null) continueButton.image.color = new Color(.77f, .52f, .28f);
            var nextLabel = continueButton.GetComponentInChildren<TMP_Text>(true);
            if (nextLabel != null) nextLabel.fontSize = 36;
        }

        promptText.text = "카드 한 장을 선택하세요";
        backButton = AddButton(transform, "BackToRewards", "보상 목록으로 (ESC)", () => TryBackToSummary());
        var backRect = (RectTransform)backButton.transform;
        backRect.anchorMin = backRect.anchorMax = new Vector2(.5f, 0);
        backRect.anchoredPosition = new Vector2(0, 76);
        backRect.sizeDelta = new Vector2(280, 52);
    }

    private Button AddRewardRow(string name, TMP_Text label, Color accent, UnityEngine.Events.UnityAction click = null)
    {
        var row = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement)).GetComponent<Image>();
        row.transform.SetParent(summary, false);
        row.color = new Color(.14f, .21f, .29f);
        row.raycastTarget = false;
        row.GetComponent<LayoutElement>().preferredHeight = 53;
        label.rectTransform.SetParent(row.transform, false);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(label.rectTransform, 40);

        var marker = new GameObject("Marker", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        marker.transform.SetParent(row.transform, false);
        marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(0, .5f);
        marker.rectTransform.anchoredPosition = new Vector2(21, 0);
        marker.rectTransform.sizeDelta = new Vector2(10, 10);
        marker.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
        marker.color = accent;
        marker.raycastTarget = false;

        if (click == null) return null;
        row.raycastTarget = true;
        row.color = new Color(.23f, .34f, .41f);
        label.raycastTarget = false;
        Button button = row.gameObject.AddComponent<Button>();
        button.targetGraphic = row;
        button.onClick.AddListener(click);
        return button;
    }

    private static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, 0);
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRowHeight(TMP_Text label, float height) =>
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = height;

    private TMP_Text AddLabel(Transform parent, string name, string caption, float size)
    {
        var label = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        label.rectTransform.SetParent(parent, false);
        label.font = goldText.font;
        label.fontSharedMaterial = goldText.fontSharedMaterial;
        label.color = new Color(.96f, .91f, .80f);
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        label.richText = false;
        label.text = caption;
        return label;
    }

    private Button AddButton(Transform parent, string name, string caption, UnityEngine.Events.UnityAction click)
    {
        var button = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
        button.transform.SetParent(parent, false);
        var image = button.GetComponent<Image>();
        image.color = new Color(.27f, .33f, .42f);
        button.targetGraphic = image;
        button.onClick.AddListener(click);
        var label = AddLabel(button.transform, "Label", caption, 26);
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        return button;
    }
}

