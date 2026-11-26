using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Relic", menuName = "Scriptable Objects/Relic")]
public class RelicData : ScriptableObject
{
    public const string StrikeCardName = "타격";

    public string DisplayName;
    [FormerlySerializedAs("VictoryHeal"), Min(0)] public int BattleEndHeal;
    [Min(0)] public int FirstTurnBlock;
    [Min(0)] public int FirstTurnEnergy;
    [Min(0)] public int FirstTurnStrength;
    [Min(0)] public int StrikeDamageBonus;

    public string GetEffectDescription()
    {
        var lines = new List<string>();
        if (FirstTurnBlock > 0) lines.Add($"전투 첫 턴에 방어도를 {FirstTurnBlock} 얻습니다.");
        if (FirstTurnEnergy > 0) lines.Add($"전투 첫 턴에 에너지를 {FirstTurnEnergy} 얻습니다.");
        if (FirstTurnStrength > 0) lines.Add($"전투 첫 턴에 힘을 {FirstTurnStrength} 얻습니다.");
        if (BattleEndHeal > 0) lines.Add($"전투 종료 시 체력을 최대 {BattleEndHeal} 회복합니다.");
        if (StrikeDamageBonus > 0)
            lines.Add($"이름에 ‘{StrikeCardName}’이 들어가는 카드의 기본 피해가 적중당 {StrikeDamageBonus} 증가합니다.");
        return lines.Count == 0 ? "등록된 효과가 없습니다." : string.Join("\n", lines);
    }
}
