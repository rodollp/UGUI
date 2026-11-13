using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class EnemyPatternValidation
{
    private static int checks;
    private static void Expect(bool value, string message)
    {
        checks++;
        if (!value) throw new InvalidOperationException("Enemy pattern: " + message);
    }

    public static void Run()
    {
        try { Verify(); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); return; }
        BattleValidation.Run();
    }

    public static void Verify()
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        var go = new GameObject("PatternValidationEnemy");
        try
        {
            data.MaxHp = 50;
            data.Actions = new[] {
                new EnemyAction { Type = EnemyActionType.Debuff, Debuff = DebuffType.Weak, DebuffTurns = 1 },
                new EnemyAction { Type = EnemyActionType.Block, Amount = 9 },
                new EnemyAction { Type = EnemyActionType.Attack, Amount = 7, Hits = 2 }
            };
            Enemy enemy = go.AddComponent<Enemy>();
            enemy.Initialize(data);
            var player = new PlayerStatus(100);
            for (int cycle = 0; cycle < 3; cycle++)
            {
                player.ResetCombatState();
                enemy.PlanNextAction();
                Expect(enemy.CurrentPatternStep == 1 && enemy.Intent.Type == EnemyActionType.Debuff,
                    "cycle begins at debuff");
                enemy.ExecuteIntent(player);
                Expect(player.WeakTurns == 1, "debuff applied");
                enemy.PlanNextAction();
                Expect(enemy.CurrentPatternStep == 2 && enemy.GetIntentDescription(player) == "방어 9",
                    "second intent is block");
                enemy.ExecuteIntent(player);
                Expect(enemy.Status.CurrentBlock == 9, "block executed");
                enemy.PlanNextAction();
                Expect(enemy.CurrentPatternStep == 3 && enemy.GetIntentDescription(player) == "공격 7 × 2",
                    "third intent is multi-hit");
                player.AddBlock(5);
                int hp = player.CurrentHp;
                enemy.ExecuteIntent(player);
                Expect(player.CurrentHp == hp - 9 && enemy.Status.CurrentBlock == 0,
                    "multi-hit result matches intent and clears old block");
            }

            string name = "PatternValidation_" + Guid.NewGuid().ToString("N");
            string path = "Assets/Datas/BattleSamples/" + name + ".asset";
            var saved = UnityEngine.Object.Instantiate(data);
            saved.MaxHp = 73;
            AssetDatabase.CreateAsset(saved, path);
            try
            {
                MethodInfo create = typeof(BattleSampleSetup).GetMethod("EnemyAsset",
                    BindingFlags.Static | BindingFlags.NonPublic);
                var result = (EnemyData)create.Invoke(null, new object[] {
                    name, "Default name", 999, EnemyType.Boss,
                    new[] { new EnemyAction { Type = EnemyActionType.Attack, Amount = 99 } }
                });
                Expect(result == saved && result.MaxHp == 73, "setup preserves edited data asset");
                Expect(result.Actions.Length == 3 && result.Actions[0].Type == EnemyActionType.Debuff
                    && result.Actions[1].Amount == 9, "setup preserves edited action sequence");
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(data); }
        Debug.Log($"ENEMY_PATTERN_VALIDATION_OK: {checks} assertions passed.");
    }
}
