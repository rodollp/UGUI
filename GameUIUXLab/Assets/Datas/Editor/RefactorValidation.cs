using System;
using UnityEditor;
using UnityEngine;

public static class RefactorValidation
{
    private static int checks;
    public static void Run()
    {
        try
        {
            VerifyTargets();
            VerifyPatternHistory();
            MultiTargetCardValidation.Verify();
            CardDragValidation.Verify();
            RandomPatternValidation.Verify();
            EnemyPatternValidation.Verify();
            Debug.Log($"REFACTOR_VALIDATION_OK: {checks} assertions passed.");
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); return; }
        // Map validation also runs upgrades, rewards and the legacy battle scene suite.
        MapValidation.Run();
    }

    private static void VerifyTargets()
    {
        var objects = new[] { new GameObject("First"), new GameObject("Second"),
            new GameObject("Dead"), new GameObject("Foreign"), new GameObject("Uninitialized") };
        var data = ScriptableObject.CreateInstance<EnemyData>();
        try
        {
            var enemies = new Enemy[objects.Length];
            for (int i = 0; i < enemies.Length; i++)
            {
                enemies[i] = objects[i].AddComponent<Enemy>();
                if (i < enemies.Length - 1) enemies[i].Initialize(data);
            }
            enemies[2].Status.TakeDamage(int.MaxValue);
            var roster = new[] { enemies[0], null, enemies[1], enemies[2], enemies[4] };
            Expect(CardTargetResolver.TryResolve(CardTargetType.None, null, null, out var targets)
                && targets.Length == 0, "self card needs no enemy");
            Expect(CardTargetResolver.TryResolve(CardTargetType.Enemy, roster, enemies[1], out targets)
                && targets.Length == 1 && targets[0] == enemies[1].Status, "selected living roster member");
            Expect(!CardTargetResolver.TryResolve(CardTargetType.Enemy, roster, enemies[3], out targets), "foreign target rejected");
            Expect(!CardTargetResolver.TryResolve(CardTargetType.Enemy, roster, enemies[2], out targets), "dead target rejected");
            Expect(!CardTargetResolver.TryResolve(CardTargetType.Enemy, roster, null, out targets), "missing target rejected");
            Expect(CardTargetResolver.TryResolve(CardTargetType.AllEnemies, roster, null, out targets)
                && targets.Length == 2 && targets[0] == enemies[0].Status && targets[1] == enemies[1].Status,
                "all enemies preserves order and excludes invalid entries");
            Expect(CardTargetResolver.TryResolve(CardTargetType.RandomEnemy, roster, null, out targets, count => count - 1)
                && targets.Length == 1 && targets[0] == enemies[1].Status, "random picks from living candidates only");
            Expect(CardTargetResolver.TryResolve(CardTargetType.RandomEnemy, roster, null, out targets, count => 0)
                && targets[0] == enemies[0].Status, "first candidate reachable");
            var none = new[] { enemies[2], enemies[4], null };
            Expect(!CardTargetResolver.TryResolve(CardTargetType.RandomEnemy, none, null, out targets), "no random target");
            Expect(!CardTargetResolver.TryResolve(CardTargetType.AllEnemies, none, null, out targets), "no area targets");
            Expect(!CardTargetResolver.TryResolve((CardTargetType)999, roster, null, out targets), "invalid target type rejected");
        }
        finally
        {
            foreach (var go in objects) UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(data);
        }
    }

    private static void VerifyPatternHistory()
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        try
        {
            data.Actions = new[] { new EnemyAction(), new EnemyAction(), new EnemyAction() };
            var state = new EnemyPatternState(new System.Random(7));
            for (int i = 0; i < 9; i++) Expect(state.Next(data, 100, 100) == i % 3, "sequential cycle");
            state.Reset();
            Expect(state.Next(data, 100, 100) == 0, "reset returns to first action");
            data.PatternMode = EnemyPatternMode.WeightedRandom;
            data.OpeningAction = 2;
            data.Actions[0].Weight = 0;
            data.Actions[1].Weight = 0;
            data.Actions[2].Weight = 1;
            data.Actions[2].MaxConsecutiveUses = 1;
            state.Reset();
            Expect(state.Next(data, 100, 100) == 1, "forced opener");
            Expect(state.Next(data, 100, 100) == 2, "weighted choice after opener");
            Expect(state.Next(data, 100, 100) == -1, "repeat limit fallback");
            Expect(state.Next(data, 100, 100) == 2, "fallback breaks consecutive run");
            var other = new EnemyPatternState(new System.Random(7));
            Expect(other.Next(data, 100, 100) == 1, "other enemy history independent");
            state.Reset();
            Expect(state.Next(data, 100, 100) == 1, "new battle restores opener");
            data.Actions = Array.Empty<EnemyAction>();
            Expect(state.Next(data, 100, 100) == -1, "empty action list fallback");
        }
        finally { UnityEngine.Object.DestroyImmediate(data); }
    }

    private static void Expect(bool value, string message)
    {
        checks++;
        if (!value) throw new InvalidOperationException("Refactor: " + message);
    }
}
