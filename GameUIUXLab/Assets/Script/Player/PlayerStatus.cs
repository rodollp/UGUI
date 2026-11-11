using System;

public class PlayerStatus : CreatureStatus
{
    public int MaxEnergy { get; private set; }
    public int CurrentEnergy { get; private set; }

    public event Action OnEnergyChanged;

    // 기존 new PlayerStatus(maxHp) 호출과 호환됩니다.
    public PlayerStatus(int maxHp, int maxEnergy = 3) : base(maxHp)
    {
        MaxEnergy = Math.Max(0, maxEnergy);
        CurrentEnergy = MaxEnergy;
    }

    public bool CanUseEnergy(int cost)
    {
        return !IsDead && cost >= 0 && CurrentEnergy >= cost;
    }

    public bool TryUseEnergy(int cost)
    {
        if (!CanUseEnergy(cost))
            return false;

        if (cost > 0)
        {
            CurrentEnergy -= cost;
            OnEnergyChanged?.Invoke();
        }

        return true;
    }

    // 플레이어 턴이 시작될 때 턴 관리자가 호출합니다.
    public void StartTurn()
    {
        RestoreEnergy();
    }

    // 이전 턴의 초과 에너지도 최대 에너지로 초기화합니다.
    public void RestoreEnergy()
    {
        if (IsDead || CurrentEnergy == MaxEnergy)
            return;

        CurrentEnergy = MaxEnergy;
        OnEnergyChanged?.Invoke();
    }

    // 카드 효과로 얻는 에너지는 최대 에너지를 초과할 수 있습니다.
    public int GainEnergy(int amount)
    {
        if (amount <= 0 || IsDead)
            return 0;

        int gained = Math.Min(amount, int.MaxValue - CurrentEnergy);
        if (gained == 0)
            return 0;

        CurrentEnergy += gained;
        OnEnergyChanged?.Invoke();
        return gained;
    }

    // 체력 회복은 CreatureStatus에서 상속받은 Heal(amount)를 사용합니다.
}
