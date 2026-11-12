using UnityEngine;

[CreateAssetMenu(fileName = "RunMap", menuName = "Map/Configuration")]
public class RunMapData : ScriptableObject
{
    [Range(5, 10)] public int Rows = 7;
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
        if (Rows < 5 || Rows > 10 || !ValidEncounters(Battles) || !ValidEncounters(Elites)
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

