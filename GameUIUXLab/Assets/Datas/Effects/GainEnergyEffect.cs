using UnityEngine;

[CreateAssetMenu(fileName = "GainEnergyEffect", menuName = "Card Effects/Gain Energy")]
public class GainEnergyEffect : CardEffect
{
    [SerializeField, Min(0)] private int amount = 1;
    public override void Execute(CardEffectContext context)
    {
        if (context != null && context.Player != null) context.Player.GainEnergy(amount);
    }
    public override string GetDescription() => $"에너지를 {amount} 얻습니다.";
}

