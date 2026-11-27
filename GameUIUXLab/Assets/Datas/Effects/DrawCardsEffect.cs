using UnityEngine;

[CreateAssetMenu(fileName = "DrawCardsEffect", menuName = "Card Effects/Draw Cards")]
public class DrawCardsEffect : CardEffect
{
    [SerializeField, Min(1)] private int amount = 2;

    public override void Execute(CardEffectContext context)
    {
        if (amount <= 0 || context?.Player == null || context.Player.IsDead) return;
        context.DrawCards?.Invoke(amount);
    }

    public override string GetDescription() => $"카드를 {amount}장 뽑습니다.";
}
