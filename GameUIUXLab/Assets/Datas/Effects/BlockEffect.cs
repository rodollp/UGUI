using UnityEngine;

public class BlockEffect : CardEffect
{
    [SerializeField] private int block; 
    public override void Execute()
    {
        Debug.Log($"방어 출력 {block}");
    }
}
