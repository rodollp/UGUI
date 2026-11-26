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
    private BattleRewardOffer displayedOffer;

    private void Awake()
    {
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
        battleManager.TrySelectRewardCard(Array.IndexOf(rewardCards, card));
    }

    public void Continue()
    {
        if (battleManager.State != BattleState.Victory) return;
        if (battleManager.HasPendingCardReward && !battleManager.TrySkipCardReward()) return;
        if (!runManager.HasMap && runManager.IsRunComplete) return;
        battleManager.StartNextBattle();
    }

    private void Refresh()
    {
        BattleRewardOffer offer = battleManager.RewardOffer;
        bool won = battleManager.State == BattleState.Victory && offer != null;
        bool pending = won && battleManager.HasPendingCardReward;
        if (continueButton != null) continueButton.gameObject.SetActive(won
            && (pending || runManager.HasMap || !runManager.IsRunComplete));
        choicesRoot.SetActive(pending);
        goldText.gameObject.SetActive(won);
        promptText.gameObject.SetActive(won);
        if (!won) { displayedOffer = null; return; }
        string relicHealing = battleManager.HpRestoredByRelics > 0
            ? $"  ·  유물 회복 +{battleManager.HpRestoredByRelics}" : "";
        RelicData relic = battleManager.RelicAwarded;
        string relicReward = relic != null ? $"  ·  유물 획득 {relic.DisplayName}" : "";
        goldText.text = $"골드 +{battleManager.GoldAwarded}  ·  보유 골드 {runManager.Gold}{relicHealing}{relicReward}";
        promptText.text = pending ? "카드 한 장을 선택하거나 다음으로 넘어가세요" :
            offer.IsSkipped ? "카드 보상을 건너뛰었습니다." :
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

