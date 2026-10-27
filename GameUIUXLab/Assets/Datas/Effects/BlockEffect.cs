using UnityEngine;

[CreateAssetMenu(fileName = "BlockEffect", menuName = "Card Effects/Block")]
public class BlockEffect : CardEffect
{
    [SerializeField] private int block; 
    public override void Execute()
    {
        Debug.Log($"방어 출력 {block}");
    }
}
