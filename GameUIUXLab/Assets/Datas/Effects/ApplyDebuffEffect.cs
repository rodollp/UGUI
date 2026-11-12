using UnityEngine;

public enum DebuffTarget { Enemy, Player }

[CreateAssetMenu(fileName = "ApplyDebuffEffect", menuName = "Card Effects/Apply Debuff")]
public class ApplyDebuffEffect : CardEffect
{
    [SerializeField] private DebuffType debuff;
    [SerializeField, Min(1)] private int turns = 2;
    [SerializeField] private DebuffTarget target = DebuffTarget.Enemy;

    public override void Execute(CardEffectContext context)
    {
        if (context == null || context.Player == null || context.Player.IsDead) return;
        CreatureStatus receiver = target == DebuffTarget.Player ? context.Player : context.Target;
        if (receiver != null) receiver.ApplyDebuff(debuff, turns);
    }

    public override string GetDescription() =>
        $"{(target == DebuffTarget.Player ? "자신" : "적")}에게 {DebuffNames.Get(debuff)} 효과를 {turns}턴 부여합니다.";
}

