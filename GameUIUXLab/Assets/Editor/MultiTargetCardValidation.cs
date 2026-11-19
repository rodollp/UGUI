using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MultiTargetCardValidation
{
    public static void Verify()
    {
        var damage = ScriptableObject.CreateInstance<DamageEffect>();
        try
        {
            var serialized = new SerializedObject(damage);
            serialized.FindProperty("damage").intValue = 10;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var player = new PlayerStatus(80);
            player.AddAttackPower(2);
            var a = new EnemyStatus(50);
            var b = new EnemyStatus(50);
            var dead = new EnemyStatus(1);
            dead.TakeDamage(1);
            b.AddBlock(3);
            damage.Execute(new CardEffectContext(player, null, new[] { a, b, dead }));
            Expect(a.CurrentHp == 38 && b.CurrentHp == 41 && dead.IsDead, "AOE applies strength and individual block");
            damage.Execute(new CardEffectContext(player, a));
            Expect(a.CurrentHp == 26 && b.CurrentHp == 41, "legacy single-target behavior");
            damage.Execute(new CardEffectContext(player, b, new[] { b }));
            Expect(a.CurrentHp == 26 && b.CurrentHp == 29, "one selected random target only");
            damage.Execute(new CardEffectContext(player, null));
            damage.Execute(null);
            Expect(a.CurrentHp == 26 && b.CurrentHp == 29, "empty context safe");
            a.ApplyDebuff(DebuffType.Vulnerable, 2);
            damage.Execute(new CardEffectContext(player, null, new[] { a, b }));
            Expect(a.CurrentHp == 8 && b.CurrentHp == 17, "per-target vulnerable modifier");
            damage.Execute(new CardEffectContext(player, null, new[] { a, b }));
            Expect(a.IsDead && b.CurrentHp == 5, "lethal hit does not stop remaining targets");

            var rewards = AssetDatabase.LoadAssetAtPath<BattleRewardData>("Assets/Datas/Rewards/DefaultBattleRewards.asset");
            foreach (string name in new[] { "WildShot", "WhirlSlash" })
            {
                var card = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Datas/Rewards/" + name + ".asset");
                Expect(card != null && rewards.CardPool.Contains(card), "card obtainable from rewards");
                Expect(card.EnergyCost == 1 && card.UpgradedCard != null
                    && card.UpgradedCard.TargetType == card.TargetType, "upgrade preserves targeting");
                Expect(card.GetDescription().Contains(name == "WildShot" ? "무작위" : "모든 적"), "target description visible");
            }
            int count = rewards.CardPool.Count;
            MultiTargetCardSetup.Configure();
            Expect(rewards.CardPool.Count == count && rewards.CardPool.Distinct().Count() == count, "setup idempotent");
            Expect((int)CardTargetType.None == 0 && (int)CardTargetType.Enemy == 1, "serialized target compatibility");
            Debug.Log("MULTI_TARGET_CARD_VALIDATION_OK: 14 assertions passed.");
        }
        finally { UnityEngine.Object.DestroyImmediate(damage); }
    }
    private static void Expect(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Multi target: " + message);
    }
}
