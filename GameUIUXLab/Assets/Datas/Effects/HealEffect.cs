using UnityEngine;

[CreateAssetMenu(fileName = "HealEffect", menuName = "Card Effects/Heal")]
public class HealEffect : CardEffect
{
    [SerializeField, Min(0)] private int amount = 5;
    public override void Execute(CardEffectContext context)
    {
        if (context != null && context.Player != null) context.Player.Heal(amount);
    }
    public override string GetDescription() => $"체력을 {amount} 회복합니다.";
}

