using System;

public class PlayerStatus : CreatureStatus
{
    private readonly int baseMaxEnergy;
    private int pendingMaxEnergyIncrease;
    private int strikeDamageBonus;

    public int MaxEnergy { get; private set; }
    public int CurrentEnergy { get; private set; }

    public event Action OnEnergyChanged;

    // 기존 new PlayerStatus(maxHp) 호출과 호환됩니다.
    public PlayerStatus(int maxHp, int maxEnergy = 3) : base(maxHp)
    {
        baseMaxEnergy = Math.Max(0, maxEnergy);
        MaxEnergy = baseMaxEnergy;
        CurrentEnergy = baseMaxEnergy;
    }

    internal void AddStrikeDamageBonus(int amount)
    {
        if (amount <= 0) return;
        strikeDamageBonus += Math.Min(amount, int.MaxValue - strikeDamageBonus);
        NotifyStatusChanged();
    }

    internal int CalculateCardAttackDamage(int baseDamage, EnemyStatus target, string cardName)
    {
        bool isStrike = cardName?.IndexOf(RelicData.StrikeCardName, StringComparison.Ordinal) >= 0;
        return CalculateAttackDamage(baseDamage, target, isStrike ? strikeDamageBonus : 0);
    }

    public bool CanUseEnergy(int cost)
    {
        return !IsDead && cost >= 0 && CurrentEnergy >= cost;
    }

    public bool TryUseEnergy(int cost)
    {
        if (!CanUseEnergy(cost))
            return false;

        SetEnergy(CurrentEnergy - cost, MaxEnergy);

        return true;
    }

    // 플레이어 턴이 시작될 때 턴 관리자가 호출합니다.
    public void StartTurn()
    {
        if (IsDead) return;

        int nextMaxEnergy = MaxEnergy + pendingMaxEnergyIncrease;
        pendingMaxEnergyIncrease = 0;
        SetEnergy(nextMaxEnergy, nextMaxEnergy);
    }

    public void ScheduleMaxEnergyIncrease(int amount)
    {
        if (amount <= 0 || IsDead) return;

        int available = int.MaxValue - MaxEnergy - pendingMaxEnergyIncrease;
        pendingMaxEnergyIncrease += Math.Min(amount, available);
    }

    // 이전 턴의 초과 에너지도 최대 에너지로 초기화합니다.
    public void RestoreEnergy()
    {
        if (!IsDead) SetEnergy(MaxEnergy, MaxEnergy);
    }

    public override void ResetCombatState()
    {
        pendingMaxEnergyIncrease = 0;
        if (MaxEnergy != baseMaxEnergy)
            SetEnergy(Math.Min(CurrentEnergy, baseMaxEnergy), baseMaxEnergy);
        base.ResetCombatState();
    }

    public int GainEnergy(int amount)
    {
        if (amount <= 0 || IsDead)
            return 0;

        int gained = Math.Min(amount, int.MaxValue - CurrentEnergy);
        SetEnergy(CurrentEnergy + gained, MaxEnergy);
        return gained;
    }

    private void SetEnergy(int current, int maximum)
    {
        if (CurrentEnergy == current && MaxEnergy == maximum) return;
        CurrentEnergy = current;
        MaxEnergy = maximum;
        OnEnergyChanged?.Invoke();
    }

}
