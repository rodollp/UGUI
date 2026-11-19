using System;
using System.Collections.Generic;

// Target selection rules are independent of card UI, energy payment and effect execution.
public static class CardTargetResolver
{
    public static bool TryResolve(CardTargetType type, IReadOnlyList<Enemy> enemies,
        Enemy selected, out EnemyStatus[] targets, Func<int, int> randomIndex = null)
    {
        targets = Array.Empty<EnemyStatus>();
        if (type == CardTargetType.None) return true;
        if (enemies == null) return false;

        if (type == CardTargetType.Enemy)
        {
            if (!IsAlive(selected)) return false;
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i] == selected)
                {
                    targets = new[] { selected.Status };
                    return true;
                }
            return false;
        }

        if (type != CardTargetType.RandomEnemy && type != CardTargetType.AllEnemies) return false;
        var living = new List<EnemyStatus>();
        for (int i = 0; i < enemies.Count; i++)
            if (IsAlive(enemies[i])) living.Add(enemies[i].Status);
        if (living.Count == 0) return false;

        if (type == CardTargetType.AllEnemies) targets = living.ToArray();
        else
        {
            int index = randomIndex != null ? randomIndex(living.Count) : UnityEngine.Random.Range(0, living.Count);
            targets = new[] { living[index] };
        }
        return true;
    }

    private static bool IsAlive(Enemy enemy) => enemy != null && enemy.Status != null && !enemy.Status.IsDead;
}
