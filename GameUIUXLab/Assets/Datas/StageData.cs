using UnityEngine;

public enum StageType { Normal, Elite, Boss }

[CreateAssetMenu(fileName = "Stage", menuName = "Battle/Stage")]
public class StageData : ScriptableObject
{
    public string StageName;
    public StageType Type;
    public EncounterData[] Encounters;

    public EncounterData SelectEncounter()
    {
        if (Encounters == null || Encounters.Length == 0) return null;
        return Encounters[Random.Range(0, Encounters.Length)];
    }
}
