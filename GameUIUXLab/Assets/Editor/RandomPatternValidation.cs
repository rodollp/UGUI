using System;
using UnityEditor;
using UnityEngine;

public static class RandomPatternValidation
{
    [MenuItem("Tools/Battle/Validate Random Patterns")]
    public static void Verify()
    {
        var random = new System.Random(12345);
        var actions = new[] {
            new EnemyAction { Weight = 70, MaxConsecutiveUses = 0 },
            new EnemyAction { Weight = 30, MaxConsecutiveUses = 0 }
        };
        int first = 0;
        for (int i = 0; i < 20000; i++)
            if (EnemyPatternSelector.Select(actions, 100, 100, -1, 0, random) == 0) first++;
        Expect(first > 13600 && first < 14400, "70:30 weighted distribution");
        actions[0].MaxConsecutiveUses = 2;
        for (int i = 0; i < 100; i++)
            Expect(EnemyPatternSelector.Select(actions, 100, 100, 0, 2, random) == 1, "repeat cap");
        actions[1].MaxHpPercent = 50;
        Expect(EnemyPatternSelector.Select(actions, 51, 100, 0, 2, random) == -1, "empty eligible pool");
        Expect(EnemyPatternSelector.Select(actions, 50, 100, 0, 2, random) == 1, "HP boundary inclusive");
        actions[1].Weight = 0;
        Expect(EnemyPatternSelector.Select(actions, 50, 100, 0, 2, random) == -1, "zero weight disabled");
        Expect(EnemyPatternSelector.Select(null, 100, 100, -1, 0, random) == -1, "null actions");

        var data = ScriptableObject.CreateInstance<EnemyData>();
        var go = new GameObject("RandomPatternValidation");
        try
        {
            data.PatternMode = EnemyPatternMode.WeightedRandom;
            data.OpeningAction = 2;
            data.Actions = new[] {
                new EnemyAction { Type = EnemyActionType.Attack, Amount = 7, Weight = 100 },
                new EnemyAction { Type = EnemyActionType.Block, Amount = 9, Weight = 0 }
            };
            var enemy = go.AddComponent<Enemy>();
            enemy.Initialize(data);
            enemy.PlanNextAction();
            Expect(enemy.CurrentPatternStep == 2, "forced opener overrides weights");
            var player = new PlayerStatus(100);
            enemy.ExecuteIntent(player);
            Expect(enemy.Status.CurrentBlock == 9, "announced opener executes");
            enemy.PlanNextAction();
            Expect(enemy.CurrentPatternStep == 1, "weighted action after opener");
            enemy.ExecuteIntent(player);
            Expect(player.CurrentHp == 93, "announced attack executes");
            enemy.Initialize(data);
            enemy.PlanNextAction();
            Expect(enemy.CurrentPatternStep == 2, "history resets on initialize");
            data.OpeningAction = 0;
            data.Actions[0].Weight = 0;
            enemy.Initialize(data);
            enemy.PlanNextAction();
            Expect(enemy.CurrentPatternStep == 0 && enemy.Intent.Amount == 6, "fallback intent");
        }
        finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(data); }
        Debug.Log("RANDOM_PATTERN_VALIDATION_OK");
    }

    public static void Run()
    {
        try { Verify(); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); return; }
        EnemyPatternValidation.Run();
    }

    private static void Expect(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Random pattern: " + message);
    }
}
