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
            shop.ExcludedCards = new[] { "CardDis/ATK", "CardDis/BLO", "Upgrades/StrikePlus", "Upgrades/DefendPlus" }
                .Select(p => AssetDatabase.LoadAssetAtPath<CardData>("Assets/Datas/" + p + ".asset")).ToArray();
            AssetDatabase.CreateAsset(shop, path);
        }
        var merchant = AssetDatabase.LoadAssetAtPath<MapEventData>("Assets/Datas/Adventure/Merchant.asset");
        if (!merchant.Choices.Any(c => c.Shop != null))
        {
            merchant.Description = "상인이 카드 다섯 장을 펼쳐 보입니다. 카드 구매와 제거 서비스를 이용할 수 있습니다.";
            merchant.Choices = new[] {
                new MapEventChoice { Label = "상점에 들어간다", Shop = shop },
                new MapEventChoice { Label = "지나간다", Result = "상인을 지나쳐 길을 떠납니다." }
            };
            EditorUtility.SetDirty(merchant);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("SHOP_SETUP_OK");
    }
    [MenuItem("Tools/Shop/Validate Shop Models")]
    public static void Validate() => ShopValidation.VerifyModels();
}
