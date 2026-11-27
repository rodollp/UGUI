using System;

// Stateless selection; history belongs to each enemy, never to the shared data asset.
public static class EnemyPatternSelector
{
    public static int Select(EnemyAction[] actions, int hp, int maxHp,
        int lastIndex, int consecutiveUses, Random random)
    {
        if (actions == null || actions.Length == 0) return -1;
        double total = 0;
        for (int i = 0; i < actions.Length; i++)
            if (Eligible(actions[i], i, hp, maxHp, lastIndex, consecutiveUses))
                total += actions[i].Weight;
        // Never violate HP or repeat restrictions to fill an empty pool.
        // The caller announces the same fallback attack that it will execute.
        if (total <= 0) return -1;
        double roll = random.NextDouble() * total;
        for (int i = 0; i < actions.Length; i++)
        {
            if (!Eligible(actions[i], i, hp, maxHp, lastIndex, consecutiveUses)) continue;
            roll -= actions[i].Weight;
            if (roll < 0) return i;
        }
        return -1;
    }

    private static bool Eligible(EnemyAction action, int index, int hp, int maxHp,
        int lastIndex, int consecutiveUses)
    {
        if (action == null || action.Weight <= 0 || maxHp <= 0) return false;
        double percent = 100.0 * hp / maxHp;
        return percent >= action.MinHpPercent && percent <= action.MaxHpPercent
            && (index != lastIndex || action.MaxConsecutiveUses <= 0
                || consecutiveUses < action.MaxConsecutiveUses);
    }
}
