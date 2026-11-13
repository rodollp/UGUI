using UnityEngine;
using System.Linq;

public class CardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private PlayHUD playHUD;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private RunManager runManager;
    [SerializeField] private TargetSelector targetSelector;
    [Header("Deck Settings")]
    [SerializeField] private int drawCount = 5;
    [Header("Card Use Settings")]
    [SerializeField, Range(0f, 1f)] private float useHeightRatio = 0.35f;

    private BattleDeck battleDeck;
    private BattleManager battleManager;
    public bool IsResolvingCard { get; private set; }

    private void Awake()
    {
        playerHand.OnCardDropped += HandleCardDropped;
    }

    private void OnDestroy()
    {
        if (playerHand != null) playerHand.OnCardDropped -= HandleCardDropped;
    }

    public void InitializeBattle(BattleManager manager)
    {
        battleManager = manager;
        battleDeck = new BattleDeck(runManager.MasterDeck);
        battleDeck.Shuffle();
        playerHand.ClearHand();
        playHUD.ResetHUD();
    }

    public void BeginPlayerTurn()
    {
        if (battleDeck == null) return;
        for (int i = 0; i < drawCount; i++)
        {
            CardInstance card = battleDeck.Draw();
            if (card == null) break;
            playerHand.AddCard(card);
        }
    }

    public void DiscardPlayerHand()
    {
        if (battleDeck != null) battleDeck.DiscardHand();
        if (playerHand != null) playerHand.ClearHand();
    }

    public void EndBattle()
    {
        DiscardPlayerHand();
        battleDeck = null;
    }

    private void HandleCardDropped(CardUI cardUI, Vector2 screenPosition)
    {
        if (battleManager == null || !battleManager.CanPlayCards || cardUI == null
            || cardUI.CardInstance == null) return;
        CardData data = cardUI.CardInstance.CardData;
        if (data.TargetType != CardTargetType.Enemy)
        {
            if (screenPosition.y >= Screen.height * useHeightRatio) UseCard(cardUI);
        }
        else if (data.TargetType == CardTargetType.Enemy && targetSelector != null)
        {
            UseCard(cardUI, targetSelector.GetTarget(screenPosition));
        }
    }

    public void UseCard(CardUI cardUI, Enemy target = null)
    {
        if (IsResolvingCard || battleManager == null || !battleManager.CanPlayCards
            || battleDeck == null || cardUI == null || !playerHand.OwnsCard(cardUI)) return;
        CardInstance card = cardUI.CardInstance;
        if (card == null || card.CardData == null || !battleDeck.ContainsInHand(card)) return;
        if (card.CardData.TargetType == CardTargetType.Enemy && !battleManager.IsValidTarget(target))
            return;
        EnemyStatus[] targets;
        if (card.CardData.TargetType == CardTargetType.RandomEnemy || card.CardData.TargetType == CardTargetType.AllEnemies)
        {
            targets = battleManager.Enemies.Where(battleManager.IsValidTarget).Select(e => e.Status).ToArray();
            if (targets.Length == 0) return;
            if (card.CardData.TargetType == CardTargetType.RandomEnemy)
                targets = new[] { targets[Random.Range(0, targets.Length)] };
        }
        else targets = card.CardData.TargetType == CardTargetType.Enemy
            ? new[] { target.Status } : System.Array.Empty<EnemyStatus>();
        PlayerStatus status = runManager.PlayerStatus;
        if (!status.CanUseEnergy(card.CurrentCost))
        {
            if (uiManager != null) uiManager.ShowToast("에너지가 부족합니다.");
            return;
        }

        IsResolvingCard = true;
        try
        {
            if (!status.TryUseEnergy(card.CurrentCost)) return;
            // 소유권을 먼저 제거하여 같은 카드를 중복 실행하지 못하게 합니다.
            battleDeck.PlayCard(card);
            playerHand.RemoveCard(cardUI);
            CardEffectContext context = new CardEffectContext(status,
                targets.Length == 1 ? targets[0] : null, targets);
            if (card.CardData.Effects != null)
                foreach (CardEffect effect in card.CardData.Effects)
                    if (effect != null) effect.Execute(context);
        }
        finally
        {
            IsResolvingCard = false;
            battleManager.CheckBattleEnd();
        }
    }

    // 기존 턴 종료 버튼의 연결을 유지하고 BattleManager에 위임합니다.
    public void EndTurn()
    {
        if (battleManager != null) battleManager.EndPlayerTurn();
    }
}

