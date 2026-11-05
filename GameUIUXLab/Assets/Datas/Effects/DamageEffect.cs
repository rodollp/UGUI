using UnityEngine;

[CreateAssetMenu(fileName = "DamageEffect", menuName = "Card Effects/Damage")]
public class DamageEffect : CardEffect
{
    [SerializeField] private int damage;

    public override void Execute()
    {
        Debug.Log($"카드 공격 효과 실행: {damage} 데미지");
    }

    public override string GetDescription()
    {
        return $"피해를 {damage} 입힙니다";
    }
}