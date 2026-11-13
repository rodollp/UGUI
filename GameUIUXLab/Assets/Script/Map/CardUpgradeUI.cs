using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardUpgradeUI : MonoBehaviour
{
    [SerializeField] private AdventureController adventure;
    [SerializeField] private CardUI[] cards;
    [SerializeField] private TMP_Text prompt;
    [SerializeField] private Button previous;
    [SerializeField] private Button next;
    [SerializeField] private Button cancel;
    [SerializeField] private Button confirm;
    private CardInstance[] candidates = Array.Empty<CardInstance>();
    private CardInstance selected;
    private int page;

    private void Awake()
    {
        foreach (CardUI card in cards)
        {
            card.SetRewardMode();
            card.OnCardClicked += Select;
        }
        previous.onClick.AddListener(() => { page--; Refresh(); });
        next.onClick.AddListener(() => { page++; Refresh(); });
        cancel.onClick.AddListener(adventure.CancelUpgrade);
        confirm.onClick.AddListener(() => adventure.ConfirmUpgrade(selected));
    }
    private void OnEnable()
    {
        page = 0; selected = null;
        adventure.OnEventChanged += Refresh;
        Refresh();
    }
    private void OnDisable() => adventure.OnEventChanged -= Refresh;
    private void OnDestroy()
    {
        foreach (CardUI card in cards) if (card != null) card.OnCardClicked -= Select;
    }
    private void Select(CardUI view)
    {
        int index = page * cards.Length + Array.IndexOf(cards, view);
        if (index < 0 || index >= candidates.Length) return;
        selected = candidates[index];
        Refresh();
    }
    private void Refresh()
    {
        if (!adventure.IsChoosingUpgrade) return;
        candidates = adventure.UpgradeCandidates;
        int pages = Mathf.Max(1, Mathf.CeilToInt(candidates.Length / (float)cards.Length));
        page = Mathf.Clamp(page, 0, pages - 1);
        for (int i = 0; i < cards.Length; i++)
        {
            int index = page * cards.Length + i;
            cards[i].gameObject.SetActive(index < candidates.Length);
            if (index < candidates.Length)
                cards[i].SetCard(new CardInstance(candidates[index].CardData.UpgradedCard));
        }
        bool valid = selected != null && Array.IndexOf(candidates, selected) >= 0;
        confirm.interactable = valid;
        prompt.text = $"카드 강화 · {page + 1}/{pages} 페이지\n표시된 카드는 강화 후 모습입니다. 카드를 고른 뒤 확정하세요.";
        if (valid) prompt.text += $"\n선택: {selected.CardData.CardName} → {selected.CardData.UpgradedCard.CardName}"
            + $"\n{selected.CardData.GetDescription()} → {selected.CardData.UpgradedCard.GetDescription()}";
        previous.interactable = page > 0;
        next.interactable = page < pages - 1;
    }
}
