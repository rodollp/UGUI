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
        var cards = new[] {
            GetOrCreateWildShot(),
            GetOrCreateAttack("WhirlSlash", "회전베기", 111, CardTargetType.AllEnemies, 6, 9)
        };
        foreach (CardData card in cards)
            if (!rewards.CardPool.Contains(card)) rewards.CardPool.Add(card);
        EditorUtility.SetDirty(rewards);
        AssetDatabase.SaveAssets();
        Debug.Log("MULTI_TARGET_CARD_SETUP_OK");
    }

    internal static CardData GetOrCreateWildShot() =>
        GetOrCreateAttack("WildShot", "유탄", 110, CardTargetType.RandomEnemy, 10, 14);

    private static CardData GetOrCreateAttack(string name, string title, uint id,
        CardTargetType target, int damage, int upgradedDamage)
    {
        var card = AssetDatabase.LoadAssetAtPath<CardData>(Folder + name + ".asset");
        if (card != null) return card;

        Sprite art = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Datas/CardDis/ATK.asset").Base.Artwork;
        card = ScriptableObject.CreateInstance<CardData>();
        card.CardID = id;
        card.Base = Attack(title, target, art);
        card.HasUpgrade = true;
        card.Upgrade = Attack(title + "+", target, art);
        AssetDatabase.CreateAsset(card, Folder + name + ".asset");
        AddDamage(card, card.Base, damage);
        AddDamage(card, card.Upgrade, upgradedDamage);
        return card;
    }

    private static CardVersion Attack(string title, CardTargetType target, Sprite art) => new CardVersion
    {
        CardName = title, Type = CardType.Attack, TargetType = target, EnergyCost = 1, Artwork = art
    };

    private static void AddDamage(CardData card, CardVersion version, int damage)
    {
        var effect = ScriptableObject.CreateInstance<DamageEffect>();
        var serialized = new SerializedObject(effect);
        serialized.FindProperty("damage").intValue = damage;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        CardDataEditor.EmbedEffect(card, version, effect);
    }

    public static void ConfigureAndRun()
    {
        try { Configure(); MultiTargetCardValidation.Verify(); }
        catch (System.Exception error) { Debug.LogException(error); EditorApplication.Exit(1); return; }
        CardDragValidation.Run();
    }
}
