using System;
using TMPro;
using UnityEngine;

public class BattleRewardUI : MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private RunManager runManager;
    [SerializeField] private GameObject choicesRoot;
    [SerializeField] private CardUI[] rewardCards;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text promptText;
    private BattleRewardOffer displayedOffer;

    private void Awake()
    {
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
        if (rewardCards == null) return;
        foreach (CardUI card in rewardCards)
            if (card != null) card.OnCardClicked -= HandleChoice;
    }

    private void HandleChoice(CardUI card)
    {
        battleManager.TrySelectRewardCard(Array.IndexOf(rewardCards, card));
    }

    private void Refresh()
    {
        BattleRewardOffer offer = battleManager.RewardOffer;
        bool won = battleManager.State == BattleState.Victory && offer != null;
        bool pending = won && battleManager.HasPendingCardReward;
        choicesRoot.SetActive(pending);
        goldText.gameObject.SetActive(won);
        promptText.gameObject.SetActive(won);
        if (!won) { displayedOffer = null; return; }
        goldText.text = $"골드 +{battleManager.GoldAwarded}  ·  보유 골드 {runManager.Gold}";
        promptText.text = pending ? "카드 3장 중 하나를 선택하세요" :
            $"{offer.SelectedCard.CardName} 카드를 덱에 추가했습니다.";
        if (!pending || ReferenceEquals(displayedOffer, offer)) return;
        displayedOffer = offer;
        for (int i = 0; i < rewardCards.Length; i++)
        {
            rewardCards[i].gameObject.SetActive(true);
            rewardCards[i].SetCard(new CardInstance(offer.Choices[i]));
        }
    }
}

