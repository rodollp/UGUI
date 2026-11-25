using UnityEngine;

[CreateAssetMenu(fileName = "DamageEffect", menuName = "Card Effects/Damage")]
public class DamageEffect : CardEffect
{
    [SerializeField] private int damage;
    [SerializeField, Min(1)] private int hits = 1;

    private int HitCount => Mathf.Max(1, hits);

    public override void Execute(CardEffectContext context)
    {
        if (context == null || context.Player == null) return;
        int hitCount = HitCount;
        for (int hit = 0; hit < hitCount; hit++)
        {
            foreach (EnemyStatus target in context.Targets)
                if (target != null && !target.IsDead) context.Player.DealAttackDamage(target, damage);
        }
    }
    public override string GetDescription() => GetDescription(null, null);

    public string GetDescription(PlayerStatus player, EnemyStatus target)
    {
        int damagePerHit = player?.CalculateAttackDamage(damage, target) ?? damage;
        int hitCount = HitCount;
        return hitCount > 1 ? $"피해를 {damagePerHit}만큼 {hitCount}번 줍니다." : $"피해를 {damagePerHit} 줍니다.";
    }
}

