using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerData", menuName = "Scriptable Objects/Player Data")]
public class PlayerData : ScriptableObject
{
    public string CharacterName;
    public int MaxHp;
    public StarterDeckData StarterDeck;
    
}