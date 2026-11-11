
using System;

public abstract class CreatureStatus
{
    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public int CurrentBlock { get; private set; }

    public int AttackPower { get; private set; }
    public int Dexterity { get; private set; }

    public bool IsDead => CurrentHp <= 0;

    public event Action OnStatusChanged;
    public event Action OnDead;

    protected CreatureStatus(int maxHp)
    {
        MaxHp = Math.Max(1, maxHp);
        CurrentHp = MaxHp;

        CurrentBlock = 0;
        AttackPower = 0;
        Dexterity = 0;
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || IsDead)
            return;

        int blockedDamage = Math.Min(CurrentBlock, damage);

        CurrentBlock -= blockedDamage;

        int remainingDamage = damage - blockedDamage;

        CurrentHp = Math.Max(0, CurrentHp - remainingDamage);

        OnStatusChanged?.Invoke();

        if (IsDead)
        {
            OnDead?.Invoke();
        }
    }

    public void AddBlock(int amount)
    {
        if (amount <= 0 || IsDead)
            return;

        CurrentBlock += amount;
        OnStatusChanged?.Invoke();
    }

    public void ResetBlock()
    {
        CurrentBlock = 0;
        OnStatusChanged?.Invoke();
    }

    public void AddAttackPower(int amount)
    {
        AttackPower += amount;
        OnStatusChanged?.Invoke();
    }

    public void AddDexterity(int amount)
    {
        Dexterity += amount;
        OnStatusChanged?.Invoke();
    }

    // 체력은 유지하고 전투 중에만 유효한 능력치를 제거합니다.
    public void ResetCombatState()
    {
        CurrentBlock = 0;
        AttackPower = 0;
        Dexterity = 0;
        OnStatusChanged?.Invoke();
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead)
            return;

        CurrentHp += Math.Min(amount, MaxHp - CurrentHp);
        OnStatusChanged?.Invoke();
    }
}

