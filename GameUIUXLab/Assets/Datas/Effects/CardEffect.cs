using UnityEngine;

public abstract class CardEffect : ScriptableObject
{
    public abstract void Execute(CardEffectContext context);

    public abstract string GetDescription();
}