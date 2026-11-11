using System;
using UnityEngine;

[Serializable]
public class EnemySpawnEntry
{
    public Enemy Prefab;
    public EnemyData Data;
    public Vector3 LocalPosition;
}

[CreateAssetMenu(fileName = "Encounter", menuName = "Battle/Encounter")]
public class EncounterData : ScriptableObject
{
    public string EncounterName;
    public EnemySpawnEntry[] Enemies;

    public bool IsValid()
    {
        if (Enemies == null || Enemies.Length == 0) return false;
        foreach (EnemySpawnEntry entry in Enemies)
            if (entry == null || entry.Prefab == null || entry.Data == null || entry.Data.MaxHp < 1)
                return false;
        return true;
    }
}
