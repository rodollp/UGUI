using UnityEngine;

[CreateAssetMenu(fileName = "DamageEffect", menuName = "Card Effects/Damage")]
public class DamageEffect : CardEffect
{
    [SerializeField] private int damage;

    public override void Execute(CardEffectContext context)
    {
        if (context == null || context.Target == null)
            return;

        int finalDamage = context.Player.AttackPower + damage;

        context.Target.TakeDamage(finalDamage);
    }

    public override string GetDescription()
    {
        return $"피해를 {damage} 입힙니다";
    }
}