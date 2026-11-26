using System;
using UnityEngine;

[Serializable]
public class MapEventChoice
{
    public string Label;
    [TextArea] public string Result;
    [Min(0)] public int GoldCost;
    [Min(0)] public int HealthCost;
    [Min(0)] public int GoldGain;
    [Min(0)] public int HealAmount;
    [Range(0, 100)] public int HealPercent;
    public CardData Card;
    public RelicData Relic;
    public bool UpgradeCard;
    public ShopData Shop;
}

[CreateAssetMenu(fileName = "MapEvent", menuName = "Map/Event")]
public class MapEventData : ScriptableObject
{
    public string Title;
    [TextArea(3, 8)] public string Description;
    public MapEventChoice[] Choices;

    public bool IsValid()
    {
        if (Choices == null || Choices.Length < 1 || Choices.Length > 3) return false;
        bool freeChoice = false;
        foreach (MapEventChoice choice in Choices)
        {
            if (!IsValidChoice(choice)) return false;
            if (choice.GoldCost == 0 && choice.HealthCost == 0 && !choice.UpgradeCard
                && choice.Shop == null && choice.Relic == null) freeChoice = true;
        }
        return freeChoice;
    }

    public bool IsValidTreasure()
    {
        if (Choices == null || Choices.Length != 1) return false;
        MapEventChoice choice = Choices[0];
        return IsValidChoice(choice) && choice.GoldCost == 0 && choice.HealthCost == 0
            && choice.GoldGain > 0 && choice.Relic != null && !choice.UpgradeCard && choice.Shop == null;
    }

    private static bool IsValidChoice(MapEventChoice choice)
    {
        if (choice == null || string.IsNullOrWhiteSpace(choice.Label) || choice.GoldCost < 0
            || choice.HealthCost < 0 || choice.GoldGain < 0 || choice.HealAmount < 0
            || choice.HealPercent < 0 || choice.HealPercent > 100) return false;
        return choice.Shop == null || (choice.Shop.IsValid() && !choice.UpgradeCard
            && choice.GoldCost == 0 && choice.HealthCost == 0 && choice.GoldGain == 0
            && choice.HealAmount == 0 && choice.HealPercent == 0 && choice.Card == null && choice.Relic == null);
    }
}

