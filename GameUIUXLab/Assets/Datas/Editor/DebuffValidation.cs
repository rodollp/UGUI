using System;
using UnityEditor;
using UnityEngine;

public static class DebuffValidation
{
    private static int checks;
    [MenuItem("Tools/Battle/Create Debuff Effect Samples")]
    public static void CreateSamples()
    {
        const string folder = "Assets/Datas/Effects/Debuffs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/Datas/Effects", "Debuffs");
        foreach (DebuffType type in Enum.GetValues(typeof(DebuffType)))
        {
            string path = folder + "/" + type + "Effect.asset";
            var effect = AssetDatabase.LoadAssetAtPath<ApplyDebuffEffect>(path);
            if (effect == null)
            {
                effect = ScriptableObject.CreateInstance<ApplyDebuffEffect>();
                AssetDatabase.CreateAsset(effect, path);
            }
            var serialized = new SerializedObject(effect);
            serialized.FindProperty("debuff").enumValueIndex = (int)type;
            serialized.FindProperty("turns").intValue = 2;
            serialized.FindProperty("target").enumValueIndex = type == DebuffType.Frail
                ? (int)DebuffTarget.Player : (int)DebuffTarget.Enemy;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(effect);
        }
        AssetDatabase.SaveAssets();
    }

    public static void ConfigureAndRun()
    {
        CreateSamples();
        BattleValidation.Run();
    }

    private static void Expect(bool value, string message)
    {
        checks++;
        if (!value) throw new InvalidOperationException("Debuff: " + message);
    }

    public static void VerifyModels()
    {
        checks = 0;
        PlayerStatus attacker = new PlayerStatus(100);
        EnemyStatus target = new EnemyStatus(100);
        Expect(attacker.CalculateAttackDamage(7, target) == 7, "base attack");
        attacker.AddAttackPower(2);
        Expect(attacker.CalculateAttackDamage(7, target) == 9, "strength before modifiers");
        attacker.ApplyDebuff(DebuffType.Weak, 2);
        Expect(attacker.CalculateAttackDamage(7, target) == 6, "weak floors damage");
        target.ApplyDebuff(DebuffType.Vulnerable, 2);
        Expect(attacker.CalculateAttackDamage(7, target) == 10, "combined modifiers floor only once");
        target.AddBlock(4);
        attacker.DealAttackDamage(target, 7);
        Expect(target.CurrentHp == 94 && target.CurrentBlock == 0, "vulnerability before block");
        int hp = target.CurrentHp;
        target.TakeDamage(4);
        Expect(target.CurrentHp == hp - 4, "non-attack damage ignores vulnerability");
        attacker.RemoveDebuff(DebuffType.Weak);
        Expect(attacker.CalculateAttackDamage(7, target) == 13, "weak removed without clearing vulnerable");
        attacker.DealAttackDamage(target, 0);
        Expect(target.CurrentHp == hp - 7, "zero base attack can still gain strength");
        attacker.ResetCombatState();
        Expect(attacker.CalculateAttackDamage(0, target) == 0, "zero attack stays zero");

        attacker.AddDexterity(2);
        attacker.ApplyDebuff(DebuffType.Frail, 2);
        Expect(attacker.CalculateCardBlock(5) == 5, "dexterity before frail");
        attacker.GainBlockFromCard(5);
        Expect(attacker.CurrentBlock == 5, "card block reduced");
        attacker.AddBlock(5);
        Expect(attacker.CurrentBlock == 10, "fixed block unchanged");
        attacker.GainBlockFromCard(1);
        Expect(attacker.CurrentBlock == 12, "small card block floors");
        attacker.AddBlock(int.MaxValue);
        Expect(attacker.CurrentBlock == int.MaxValue, "block cannot overflow");
        attacker.ApplyDebuff(DebuffType.Frail, 3);
        Expect(attacker.FrailTurns == 5 && attacker.CalculateCardBlock(5) == 5, "stack duration not multiplier");
        attacker.ApplyDebuff(DebuffType.Frail, 0);
        attacker.ApplyDebuff(DebuffType.Frail, -1);
        attacker.ApplyDebuff((DebuffType)99, 2);
        Expect(attacker.FrailTurns == 5 && attacker.GetDebuffTurns((DebuffType)99) == 0, "invalid inputs ignored");
        attacker.ApplyDebuff(DebuffType.Frail, int.MaxValue);
        Expect(attacker.FrailTurns == int.MaxValue, "duration overflow guarded");

        PlayerStatus clock = new PlayerStatus(100);
        clock.ApplyDebuff(DebuffType.Weak, 2);
        clock.ApplyDebuff(DebuffType.Vulnerable, 1);
        clock.ApplyDebuff(DebuffType.Frail, 1, duringEnemyTurn: true);
        clock.EndRoundDebuffs();
        Expect(clock.WeakTurns == 1 && clock.VulnerableTurns == 0 && clock.FrailTurns == 1,
            "fresh enemy debuff skips first round end only");
        clock.EndRoundDebuffs();
        Expect(clock.WeakTurns == 0 && clock.FrailTurns == 0, "expires at following round end");
        clock.EndRoundDebuffs();
        Expect(clock.GetDebuffDescription() == "", "expired statuses hidden");
        clock.ApplyDebuff(DebuffType.Vulnerable, 2);
        clock.ApplyDebuff(DebuffType.Weak, 1);
        Expect(clock.GetDebuffDescription() == "취약 2 · 약화 1", "names and duration labels");
        clock.ApplyDebuff(DebuffType.Frail, 1, duringEnemyTurn: true);
        clock.RemoveDebuff(DebuffType.Frail);
        clock.ApplyDebuff(DebuffType.Frail, 1);
        clock.EndRoundDebuffs();
        Expect(clock.FrailTurns == 0, "cleansing clears freshness flag");
        int beforeReset = clock.CurrentHp;
        clock.ResetCombatState();
        Expect(clock.GetDebuffDescription() == "" && clock.CurrentHp == beforeReset,
            "new combat clears debuffs without healing");
        clock.ApplyDebuff(DebuffType.Weak, 1, duringEnemyTurn: true);
        clock.ResetCombatState();
        clock.ApplyDebuff(DebuffType.Weak, 1);
        clock.EndRoundDebuffs();
        Expect(clock.WeakTurns == 0, "new combat clears all freshness flags");

        var data = ScriptableObject.CreateInstance<EnemyData>();
        data.MaxHp = 50;
        data.Actions = new[] {
            new EnemyAction { Type = EnemyActionType.Debuff, Debuff = DebuffType.Weak, DebuffTurns = 1 },
            new EnemyAction { Type = EnemyActionType.Attack, Amount = 7, Hits = 2 }
        };
        GameObject go = new GameObject("DebuffValidationEnemy");
        try
        {
            Enemy enemy = go.AddComponent<Enemy>();
            enemy.Initialize(data);
            PlayerStatus player = new PlayerStatus(100);
            enemy.PlanNextAction();
            Expect(enemy.GetIntentDescription(player) == "약화 1", "debuff intent text");
            enemy.ExecuteIntent(player);
            player.EndRoundDebuffs();
            Expect(player.WeakTurns == 1, "enemy action preserves next player turn");
            enemy.PlanNextAction();
            enemy.Status.ApplyDebuff(DebuffType.Weak, 1);
            player.ApplyDebuff(DebuffType.Vulnerable, 1);
            Expect(enemy.GetIntentDescription(player) == "공격 7 × 2", "intent reflects both modifiers");
            player.AddBlock(5);
            enemy.ExecuteIntent(player);
            Expect(player.CurrentHp == 91 && player.CurrentBlock == 0, "each hit uses same modifiers");
            var effect = ScriptableObject.CreateInstance<ApplyDebuffEffect>();
            try
            {
                SerializedObject serialized = new SerializedObject(effect);
                serialized.FindProperty("debuff").enumValueIndex = (int)DebuffType.Vulnerable;
                serialized.FindProperty("turns").intValue = 3;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                effect.Execute(new CardEffectContext(player, enemy.Status));
                Expect(enemy.Status.VulnerableTurns == 3, "card applies to selected enemy");
                serialized.FindProperty("target").enumValueIndex = (int)DebuffTarget.Player;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                effect.Execute(new CardEffectContext(player, null));
                Expect(player.VulnerableTurns == 4, "self-target debuff supported");
                effect.Execute(null);
                Expect(effect.GetDescription().Contains("취약"), "card description includes debuff");

                var damageEffect = ScriptableObject.CreateInstance<DamageEffect>();
                var blockEffect = ScriptableObject.CreateInstance<BlockEffect>();
                try
                {
                    var damageSettings = new SerializedObject(damageEffect);
                    damageSettings.FindProperty("damage").intValue = 7;
                    damageSettings.ApplyModifiedPropertiesWithoutUndo();
                    int enemyHp = enemy.Status.CurrentHp;
                    damageEffect.Execute(new CardEffectContext(player, enemy.Status));
                    Expect(enemy.Status.CurrentHp == enemyHp - 7, "DamageEffect routes through common calculation");
                    player.ApplyDebuff(DebuffType.Frail, 1);
                    var blockSettings = new SerializedObject(blockEffect);
                    blockSettings.FindProperty("block").intValue = 7;
                    blockSettings.ApplyModifiedPropertiesWithoutUndo();
                    blockEffect.Execute(new CardEffectContext(player, null));
                    Expect(player.CurrentBlock == 5, "BlockEffect routes through frail calculation");
                }
                finally { UnityEngine.Object.DestroyImmediate(damageEffect); UnityEngine.Object.DestroyImmediate(blockEffect); }
            }
            finally { UnityEngine.Object.DestroyImmediate(effect); }
        }
        finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(data); }

        PlayerStatus large = new PlayerStatus(100);
        EnemyStatus largeTarget = new EnemyStatus(100);
        large.AddAttackPower(int.MaxValue);
        large.ApplyDebuff(DebuffType.Weak, 1);
        largeTarget.ApplyDebuff(DebuffType.Vulnerable, 1);
        Expect(large.CalculateAttackDamage(int.MaxValue, largeTarget) == int.MaxValue, "large damage clamps safely");
        large.AddDexterity(int.MaxValue);
        large.ApplyDebuff(DebuffType.Frail, 1);
        Expect(large.CalculateCardBlock(int.MaxValue) == int.MaxValue, "large block clamps safely");
        large.TakeDamage(int.MaxValue);
        large.ApplyDebuff(DebuffType.Weak, 4);
        Expect(large.WeakTurns == 1 && large.CalculateAttackDamage(5) == 0
            && large.CalculateCardBlock(5) == 0, "dead creatures cannot apply or use effects");
        Debug.Log($"DEBUFF_MODEL_VALIDATION_OK: {checks} assertions passed.");
    }
}
