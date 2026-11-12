using UnityEngine;

[CreateAssetMenu(fileName = "BlockEffect", menuName = "Card Effects/Block")]
public class BlockEffect : CardEffect
{
    [SerializeField] private int block;
    public override void Execute(CardEffectContext context)
    {
        if (context == null || context.Player == null) return;
        context.Player.GainBlockFromCard(block);
    }
    public override string GetDescription() => $"방어도를 {block} 얻습니다.";
}

