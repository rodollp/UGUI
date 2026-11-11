using System;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockEffect", menuName = "Card Effects/Block")]
public class BlockEffect : CardEffect
{
    [SerializeField] private int block;
    public override void Execute(CardEffectContext context)
    {
        if (context == null || context.Player == null) return;
        int amount = (int)Math.Min(int.MaxValue,
            Math.Max(0L, (long)block + context.Player.Dexterity));
        context.Player.AddBlock(amount);
    }
    public override string GetDescription() => $"방어도를 {block} 얻습니다.";
}

