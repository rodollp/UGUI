
using System;

public abstract class CreatureStatus
{
    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public int CurrentBlock { get; private set; }

    public int AttackPower { get; private set; }
    public int Dexterity { get; private set; }

    private readonly int[] debuffTurns = new int[3];
    private readonly bool[] appliedDuringEnemyTurn = new bool[3];
    public int VulnerableTurns => GetDebuffTurns(DebuffType.Vulnerable);
    public int WeakTurns => GetDebuffTurns(DebuffType.Weak);
    public int FrailTurns => GetDebuffTurns(DebuffType.Frail);

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

        CurrentBlock += Math.Min(amount, int.MaxValue - CurrentBlock);
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
        Array.Clear(debuffTurns, 0, debuffTurns.Length);
        Array.Clear(appliedDuringEnemyTurn, 0, appliedDuringEnemyTurn.Length);
        OnStatusChanged?.Invoke();
    }


    public int GetDebuffTurns(DebuffType type)
    {
        int index = (int)type;
        return index >= 0 && index < debuffTurns.Length ? debuffTurns[index] : 0;
    }

    /// <summary>같은 디버프는 배율 대신 남은 라운드를 더합니다.</summary>
    public void ApplyDebuff(DebuffType type, int turns, bool duringEnemyTurn = false)
    {
        int index = (int)type;
        if (IsDead || turns <= 0 || index < 0 || index >= debuffTurns.Length) return;
        debuffTurns[index] += Math.Min(turns, int.MaxValue - debuffTurns[index]);
        appliedDuringEnemyTurn[index] |= duringEnemyTurn;
        OnStatusChanged?.Invoke();
    }

    public void RemoveDebuff(DebuffType type)
    {
        int index = (int)type;
        if (index < 0 || index >= debuffTurns.Length || debuffTurns[index] == 0) return;
        debuffTurns[index] = 0;
        appliedDuringEnemyTurn[index] = false;
        OnStatusChanged?.Invoke();
    }

    /// <summary>모든 적 행동이 끝난 뒤 한 번 호출합니다.
    /// 적 턴에 새로 걸린 디버프는 그 라운드의 감소를 건너뜁니다.</summary>
    public void EndRoundDebuffs()
    {
        bool changed = false;
        for (int i = 0; i < debuffTurns.Length; i++)
        {
            if (appliedDuringEnemyTurn[i])
                appliedDuringEnemyTurn[i] = false;
            else if (debuffTurns[i] > 0)
            {
                debuffTurns[i]--;
                changed = true;
            }
        }
        if (changed) OnStatusChanged?.Invoke();
    }

    public string GetDebuffDescription()
    {
        var parts = new System.Collections.Generic.List<string>(3);
        for (int i = 0; i < debuffTurns.Length; i++)
            if (debuffTurns[i] > 0)
                parts.Add(DebuffNames.Get((DebuffType)i) + " " + debuffTurns[i]);
        return string.Join(" · ", parts);
    }

    /// <summary>힘 → 약화와 취약의 배율 → 마지막에 한 번 내림. 방어도 차감 전 피해입니다.</summary>
    public int CalculateAttackDamage(int baseDamage, CreatureStatus target = null)
    {
        if (IsDead || (target != null && target.IsDead)) return 0;
        long amount = Math.Max(0L, (long)baseDamage + AttackPower);
        long divisor = 1;
        if (WeakTurns > 0) { amount *= 3; divisor *= 4; }
        if (target != null && target.VulnerableTurns > 0) { amount *= 3; divisor *= 2; }
        return (int)Math.Min(int.MaxValue, amount / divisor);
    }

    public void DealAttackDamage(CreatureStatus target, int baseDamage)
    {
        if (target != null) target.TakeDamage(CalculateAttackDamage(baseDamage, target));
    }

    /// <summary>민첩을 더한 뒤 손상을 적용합니다. 고정 방어도 획득 AddBlock은 영향을 받지 않습니다.</summary>
    public int CalculateCardBlock(int baseBlock)
    {
        if (IsDead) return 0;
        long amount = Math.Max(0L, (long)baseBlock + Dexterity);
        if (FrailTurns > 0) amount = amount * 3 / 4;
        return (int)Math.Min(int.MaxValue, amount);
    }

    public void GainBlockFromCard(int baseBlock) => AddBlock(CalculateCardBlock(baseBlock));

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead)
            return;

        CurrentHp += Math.Min(amount, MaxHp - CurrentHp);
        OnStatusChanged?.Invoke();
    }
}


