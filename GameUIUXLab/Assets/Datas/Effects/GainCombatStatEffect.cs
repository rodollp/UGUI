using UnityEngine;

[CreateAssetMenu(fileName = "GainCombatStatEffect", menuName = "Card Effects/Gain Combat Stat")]
public class GainCombatStatEffect : CardEffect
{
    private enum CombatStat { Strength, Dexterity }

    [SerializeField] private CombatStat stat = CombatStat.Strength;
    [SerializeField, Min(1)] private int amount = 3;

    public override void Execute(CardEffectContext context)
    {
        PlayerStatus player = context?.Player;
        if (player == null || player.IsDead || amount <= 0) return;

        switch (stat)
        {
            case CombatStat.Strength:
                player.AddAttackPower(amount);
                break;
            case CombatStat.Dexterity:
                player.AddDexterity(amount);
                break;
        }
    }

    public override string GetDescription() => stat switch
    {
        CombatStat.Strength => $"이번 전투 동안 힘을 {amount} 얻습니다.",
        CombatStat.Dexterity => $"이번 전투 동안 민첩을 {amount} 얻습니다.",
        _ => string.Empty
    };
}
