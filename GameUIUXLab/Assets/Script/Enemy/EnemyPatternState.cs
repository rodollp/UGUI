using System;

// Per-enemy history; shared EnemyData assets contain configuration only.
public sealed class EnemyPatternState
{
    private int nextSequentialIndex;
    private int lastIndex = -1;
    private int consecutiveUses;
    private int plannedActions;
    private readonly Random random;

    public EnemyPatternState(Random random = null) => this.random = random ?? new Random();

    public void Reset()
    {
        nextSequentialIndex = 0;
        lastIndex = -1;
        consecutiveUses = 0;
        plannedActions = 0;
    }

    public int Next(EnemyData data, int currentHp, int maxHp)
    {
        EnemyAction[] actions = data.Actions;
        if (actions == null || actions.Length == 0)
        {
            plannedActions++;
            return -1;
        }

        int index;
        if (data.PatternMode != EnemyPatternMode.WeightedRandom)
        {
            index = nextSequentialIndex % actions.Length;
            nextSequentialIndex = (index + 1) % actions.Length;
        }
        else
        {
            int opening = data.OpeningAction - 1;
            index = plannedActions == 0 && opening >= 0 && opening < actions.Length && actions[opening] != null
                ? opening : EnemyPatternSelector.Select(actions, currentHp, maxHp, lastIndex, consecutiveUses, random);
        }
        consecutiveUses = index >= 0 && index == lastIndex ? consecutiveUses + 1 : 1;
        lastIndex = index;
        plannedActions++;
        return index;
    }
}
