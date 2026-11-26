using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ShopSetup
{
    [MenuItem("Tools/Shop/Configure Card Shop")]
    public static void Configure()
    {
        const string path = "Assets/Datas/Adventure/DefaultShop.asset";
        var shop = AssetDatabase.LoadAssetAtPath<ShopData>(path);
        if (shop == null)
        {
            shop = ScriptableObject.CreateInstance<ShopData>();
            shop.CardPrice = 50;
            shop.CardPool = AssetDatabase.LoadAssetAtPath<BattleRewardData>(
                "Assets/Datas/Rewards/DefaultBattleRewards.asset").CardPool.ToArray();
            shop.ExcludedCards = new[] { "CardDis/ATK", "CardDis/BLO", "Rewards/Bash" }
                .Select(p => AssetDatabase.LoadAssetAtPath<CardData>("Assets/Datas/" + p + ".asset")).ToArray();
            shop.RelicPool = new[] { "EliteGuardBadge", "MarchingHorn", "BattleStandard", "WornTalisman", "SmithHammer" }
                .Select(name => AssetDatabase.LoadAssetAtPath<RelicData>($"Assets/Datas/Relics/{name}.asset")).ToArray();
            AssetDatabase.CreateAsset(shop, path);
        }
        var map = AssetDatabase.LoadAssetAtPath<RunMapData>("Assets/Datas/Adventure/DefaultMap.asset");
        if (map != null)
        {
            if (map.Shop == null) map.Shop = shop;
            var merchant = AssetDatabase.LoadAssetAtPath<MapEventData>("Assets/Datas/Adventure/Merchant.asset");
            if (merchant != null && map.Events != null && map.Events.Contains(merchant))
            {
                var remainingEvents = map.Events.Where(data => data != merchant).ToArray();
                if (remainingEvents.Length > 0) map.Events = remainingEvents;
            }
            EditorUtility.SetDirty(map);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("SHOP_SETUP_OK");
    }
    [MenuItem("Tools/Shop/Validate Shop Models")]
    public static void Validate() => ShopValidation.VerifyModels();
}
