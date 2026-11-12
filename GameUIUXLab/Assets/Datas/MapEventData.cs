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
            if (choice == null || string.IsNullOrWhiteSpace(choice.Label) || choice.GoldCost < 0
                || choice.HealthCost < 0 || choice.GoldGain < 0 || choice.HealAmount < 0
                || choice.HealPercent < 0 || choice.HealPercent > 100) return false;
            if (choice.GoldCost == 0 && choice.HealthCost == 0) freeChoice = true;
        }
        return freeChoice;
    }
}

