using UnityEngine;

[CreateAssetMenu(fileName = "DamageEffect", menuName = "Card Effects/Damage")]
public class DamageEffect : CardEffect
{
    [SerializeField] private int damage;
    public override void Execute(CardEffectContext context)
    {
        if (context == null || context.Player == null || context.Target == null) return;
        context.Player.DealAttackDamage(context.Target, damage);
    }
    public override string GetDescription() => $"피해를 {damage} 줍니다.";
}

