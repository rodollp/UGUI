using System;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageEffect", menuName = "Card Effects/Damage")]
public class DamageEffect : CardEffect
{
    [SerializeField] private int damage;
    public override void Execute(CardEffectContext context)
    {
        if (context == null || context.Player == null || context.Target == null) return;
        int amount = (int)Math.Min(int.MaxValue,
            Math.Max(0L, (long)damage + context.Player.AttackPower));
        context.Target.TakeDamage(amount);
    }
    public override string GetDescription() => $"피해를 {damage} 줍니다.";
}

