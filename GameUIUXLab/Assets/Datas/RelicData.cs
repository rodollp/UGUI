using UnityEngine;

[CreateAssetMenu(fileName = "Relic", menuName = "Scriptable Objects/Relic")]
public class RelicData : ScriptableObject
{
    public string DisplayName;
    [Min(0)] public int VictoryHeal;
    [Min(0)] public int FirstTurnBlock;
}
