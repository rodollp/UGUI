using UnityEngine;


public abstract class CardEffect : ScriptableObject
{
    public abstract void Execute();

    public abstract string GetDescription();
}
