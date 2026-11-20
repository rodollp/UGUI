using UnityEngine;

[CreateAssetMenu(fileName = "DamageEffect", menuName = "Card Effects/Damage")]
public class DamageEffect : CardEffect
{
    [SerializeField] private int damage;
    public override void Execute(CardEffectContext context)
    {
        if (context == null || context.Player == null) return;
        foreach (EnemyStatus target in context.Targets)
            if (target != null && !target.IsDead) context.Player.DealAttackDamage(target, damage);
    }
    public override string GetDescription() => GetDescription(null, null);

    public string GetDescription(PlayerStatus player, EnemyStatus target) =>
        $"피해를 {player?.CalculateAttackDamage(damage, target) ?? damage} 줍니다.";
}

