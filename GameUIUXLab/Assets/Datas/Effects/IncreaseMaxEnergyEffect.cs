using UnityEngine;

[CreateAssetMenu(fileName = "IncreaseMaxEnergyEffect", menuName = "Card Effects/Increase Max Energy")]
public class IncreaseMaxEnergyEffect : CardEffect
{
    [SerializeField, Min(1)] private int amount = 1;

    public override void Execute(CardEffectContext context)
    {
        context?.Player?.ScheduleMaxEnergyIncrease(amount);
    }

    public override string GetDescription() => $"다음 내 턴부터 전투 종료까지 최대 에너지 +{amount}.";
}
