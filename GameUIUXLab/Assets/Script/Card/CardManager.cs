using System;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private PlayHUD playHUD;
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
        RefreshPileCounts();
        playHUD.ResetHUD();
    }

    public void BeginPlayerTurn() => DrawCards(drawCount);

    private void DrawCards(int amount)
    {
        if (battleDeck == null) return;
        PlayerStatus player = runManager.PlayerStatus;
        Func<Vector2, EnemyStatus> getPreviewTarget = GetDamagePreviewTarget;
        Action onInsufficientEnergy = ShowEnergyShortfall;
        for (int i = 0; i < amount && playerHand.HasRoom; i++)
        {
            CardInstance card = battleDeck.Draw();
            if (card == null) break;
            bool previewsTarget = card.Version.TargetType is CardTargetType.Enemy or CardTargetType.AllEnemies;
            playerHand.AddCard(card, player, previewsTarget ? getPreviewTarget : null, onInsufficientEnergy);
        }
        RefreshPileCounts();
    }

    public void DiscardPlayerHand()
    {
        if (battleDeck != null) battleDeck.DiscardHand();
        if (playerHand != null) playerHand.ClearHand();
        RefreshPileCounts();
    }

    public void EndBattle()
    {
        battleDeck = null;
        if (playerHand != null) playerHand.ClearHand();
        RefreshPileCounts();
    }

    private EnemyStatus GetDamagePreviewTarget(Vector2 screenPosition)
    {
        if (targetSelector == null || !battleManager.CanPlayCards) return null;
        Enemy target = targetSelector.GetTarget(screenPosition);
        return battleManager.IsValidTarget(target) ? target.Status : null;
    }

    private void HandleCardDropped(CardUI cardUI, Vector2 screenPosition)
    {
        if (battleManager == null || !battleManager.CanPlayCards || cardUI == null
            || cardUI.CardInstance == null) return;
        if (cardUI.CardInstance.Version.TargetType != CardTargetType.Enemy)
        {
            if (screenPosition.y >= Screen.height * useHeightRatio) UseCard(cardUI);
        }
        else if (targetSelector != null)
        {
            UseCard(cardUI, targetSelector.GetTarget(screenPosition));
        }
    }

    public void UseCard(CardUI cardUI, Enemy target = null)
    {
        BattleDeck deck = battleDeck;
        if (IsResolvingCard || battleManager == null || !battleManager.CanPlayCards
            || deck == null || cardUI == null || !playerHand.OwnsCard(cardUI)) return;
        CardInstance card = cardUI.CardInstance;
        if (card == null || card.CardData == null || !deck.ContainsInHand(card)) return;
        CardVersion version = card.Version;
        if (!CardTargetResolver.TryResolve(version.TargetType, battleManager.Enemies,
            target, out EnemyStatus[] targets)) return;
        PlayerStatus status = runManager.PlayerStatus;
        if (!status.CanUseEnergy(card.CurrentCost))
        {
            ShowEnergyShortfall();
            return;
        }

        IsResolvingCard = true;
        try
        {
            if (!status.TryUseEnergy(card.CurrentCost)) return;
            deck.PlayCard(card, () =>
            {
                playerHand.RemoveCard(cardUI);
                CardEffectContext context = new CardEffectContext(status,
                    targets.Length == 1 ? targets[0] : null, targets, DrawCards, version.CardName);
                ExecuteEffects(version, context);
            });
        }
        finally
        {
            IsResolvingCard = false;
            RefreshPileCounts();
            battleManager.CheckBattleEnd();
        }
    }

    private void ShowEnergyShortfall()
    {
        if (battleManager != null && battleManager.CanPlayCards)
            playHUD?.ShowEnergyShortfall();
    }

    private static void ExecuteEffects(CardVersion data, CardEffectContext context)
    {
        if (data.Effects == null) return;
        foreach (CardEffect effect in data.Effects)
            if (effect != null) effect.Execute(context);
    }

    private void RefreshPileCounts()
    {
        if (playHUD != null)
            playHUD.SetPileCounts(battleDeck?.DrawCount ?? 0, battleDeck?.DiscardCount ?? 0);
    }

    // 기존 턴 종료 버튼의 연결을 유지하고 BattleManager에 위임합니다.
    public void EndTurn()
    {
        if (battleManager != null) battleManager.EndPlayerTurn();
    }
}

