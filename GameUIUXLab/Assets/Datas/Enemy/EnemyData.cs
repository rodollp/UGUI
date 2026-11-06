using UnityEngine;



public enum EnemyType
{
    Normal,
    Elite,
    Boss
}
[CreateAssetMenu(fileName = "EnemyData", menuName = "Scriptable Objects/EnemyData")]
public class EnemyData : ScriptableObject
{
    public string EnemyName;
    public EnemyType EnemyType;
    public int MaxHp;
    
}
