using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class MultiTargetCardSetup
{
    private const string Folder = "Assets/Datas/Rewards/";
    [MenuItem("Tools/Cards/Configure Multi Target Cards")]
    public static void Configure()
    {
        var rewards = AssetDatabase.LoadAssetAtPath<BattleRewardData>(Folder + "DefaultBattleRewards.asset");
        if (rewards == null) throw new System.InvalidOperationException("Default battle rewards missing");
        Add(rewards, "WildShot", "유탄", 110, CardTargetType.RandomEnemy, 10, 14);
        Add(rewards, "WhirlSlash", "회전베기", 111, CardTargetType.AllEnemies, 6, 9);
        EditorUtility.SetDirty(rewards);
        AssetDatabase.SaveAssets();
        Debug.Log("MULTI_TARGET_CARD_SETUP_OK");
    }

    private static void Add(BattleRewardData rewards, string name, string title, uint id,
        CardTargetType target, int damage, int upgradedDamage)
    {
        var card = AssetDatabase.LoadAssetAtPath<CardData>(Folder + name + ".asset");
        if (card == null)
        {
            var upgrade = Create(name + "Plus", title + "+", id + 1000, target, upgradedDamage);
            card = Create(name, title, id, target, damage);
            card.UpgradedCard = upgrade;
            EditorUtility.SetDirty(card);
        }
        if (!rewards.CardPool.Contains(card)) rewards.CardPool.Add(card);
    }

    private static CardData Create(string name, string title, uint id, CardTargetType target, int damage)
    {
        var effect = ScriptableObject.CreateInstance<DamageEffect>();
        var serialized = new SerializedObject(effect);
        serialized.FindProperty("damage").intValue = damage;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(effect, Folder + name + "Damage.asset");
        var card = ScriptableObject.CreateInstance<CardData>();
        card.CardID = id;
        card.CardName = title;
        card.Type = CardType.Attack;
        card.TargetType = target;
        card.EnergyCost = 1;
        card.Artwork = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Datas/CardDis/ATK.asset").Artwork;
        card.Effects = new List<CardEffect> { effect };
        AssetDatabase.CreateAsset(card, Folder + name + ".asset");
        return card;
    }

    public static void ConfigureAndRun()
    {
        try { Configure(); MultiTargetCardValidation.Verify(); }
        catch (System.Exception error) { Debug.LogException(error); EditorApplication.Exit(1); return; }
        CardDragValidation.Run();
    }
}
