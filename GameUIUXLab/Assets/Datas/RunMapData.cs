using UnityEngine;

[CreateAssetMenu(fileName = "RunMap", menuName = "Map/Configuration")]
public class RunMapData : ScriptableObject
{
    [SerializeField, Min(3)] private int rows = 12;
    [SerializeField, Min(2)] private int eliteStartFloor = 5;
    [SerializeField, Min(2)] private int restStartFloor = 4;
    [SerializeField, Min(2)] private int treasureFloor = 6;
    public int Rows => rows;
    public int EliteStartFloor => eliteStartFloor;
    public int RestStartFloor => restStartFloor;
    public int TreasureFloor => treasureFloor;
    public int TreasureRow => treasureFloor - 1;
    public bool UseFixedSeed;
    public int Seed = 42;
    public EncounterData[] Battles;
    public EncounterData[] Elites;
    public EncounterData[] Bosses;
    public MapEventData[] Events;
    public MapEventData Rest;
    public MapEventData Treasure;

    public bool IsValid()
    {
        if (rows < 3 || eliteStartFloor < 2 || eliteStartFloor >= rows - 1
            || restStartFloor < 2 || restStartFloor >= rows - 1
            || treasureFloor < 2 || treasureFloor >= rows - 1) return false;
        if (!ValidEncounters(Battles) || !ValidEncounters(Elites)
            || !ValidEncounters(Bosses) || Events == null || Events.Length == 0
            || Rest == null || !Rest.IsValid() || Treasure == null || !Treasure.IsValid()) return false;
        foreach (MapEventData data in Events)
            if (data == null || !data.IsValid()) return false;
        return true;
    }

    private static bool ValidEncounters(EncounterData[] pool)
    {
        if (pool == null || pool.Length == 0) return false;
        foreach (EncounterData data in pool)
            if (data == null || !data.IsValid()) return false;
        return true;
    }
}

