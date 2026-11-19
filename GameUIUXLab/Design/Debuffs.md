# 취약 · 약화 · 손상

## 적용 규칙

| 디버프 | 효과 |
| --- | --- |
| 취약 (Vulnerable) | 받는 **공격** 피해 × 1.5 |
| 약화 (Weak) | 주는 **공격** 피해 × 0.75 |
| 손상 (Frail) | **카드로** 얻는 방어도 × 0.75 |

중첩 시 배율은 고정이며 남은 지속시간만 더한다.
공격 피해는 기본값에 힘을 더하고, 약화와 대상의 취약 배율을 적용한 뒤 마지막에 한 번 내림한다.
예: 기본 공격 7 + 힘 2, 약화와 대상의 취약이 함께 있으면 floor(9 × 0.75 × 1.5) = 10.
이 수치에서 대상 방어도를 먼저 차감하고 남은 피해만 체력에 적용한다.
다중 타격은 매 타격마다 같은 계산을 수행한다.

방어 카드는 기본값에 민첩을 더한 뒤 손상을 적용하고 내림한다.
AddBlock으로 얻는 고정 방어도(현재 적의 방어 행동 포함)는 손상의 영향을 받지 않는다.
TakeDamage는 고정 피해 API이므로 취약·약화의 영향을 받지 않는다.
공격을 구현할 때는 공격자의 DealAttackDamage(target, baseDamage)를 사용한다.

## 지속시간

플레이어 턴과 적 전체의 행동을 하나의 라운드로 본다.
모든 적의 행동이 끝난 뒤 살아 있는 플레이어와 적의 디버프 지속시간을 한 번 줄인다.
적 턴에 새로 부여된 디버프는 해당 라운드의 감소를 건너뛰어 다음 플레이어 턴에 사용할 시간이 보장된다.
따라서 적이 약화 1을 부여하면 다음 플레이어 턴 동안 공격력이 감소한다.
플레이어가 적에게 약화 1을 부여하면 바로 이어지는 적 행동에 적용된 뒤 사라진다.
같은 적 턴 중 새로 중첩되면 해당 디버프 전체가 그 라운드의 감소를 건너뛴다.
0이 되면 효과와 표시가 제거된다. 다음 전투 시작 시 모든 디버프와 지속시간을 초기화한다.

## 카드에 연결

1. Create → Card Effects → Apply Debuff로 효과 에셋을 만든다.
2. Debuff에서 Vulnerable / Weak / Frail을 선택한다.
3. Turns에 지속시간을 설정한다.
4. Target을 Enemy 또는 Player로 설정한다.
5. 해당 CardData의 Effects 배열에 추가한다.

Enemy 대상 효과가 있는 카드는 CardData.TargetType도 Enemy로 설정해야 한다.
Player는 자신에게 디버프를 주는 대가가 있는 카드에 사용할 수 있다.
Effects의 순서대로 실행하므로 취약 효과를 공격 효과 앞에 넣으면 그 카드의 공격에도 취약이 적용된다.

Tools → Battle → Create Debuff Effect Samples 메뉴로 2라운드 샘플 효과 3개를 만든다.
Assets/Datas/Effects/Debuffs의 VulnerableEffect와 WeakEffect는 적 대상,
FrailEffect는 플레이어 자신 대상이다. 기본 덱과 기존 카드의 밸런스는 변경하지 않는다.

## 적 행동에 연결

EnemyData.Actions에 Type = Debuff인 행동을 추가하고 Debuff 및 DebuffTurns를 설정한다.
예: Debuff = Weak, DebuffTurns = 1이면 플레이어에게 약화 1을 부여한다.
기존 Attack / Block / Strength의 enum 번호를 유지하므로 기존 적 에셋은 그대로 작동한다.

## 표시 및 검증

체력 바 아래에 디버프 이름과 남은 지속시간을 표시한다.
기존 체력 바 프리팹은 수정할 필요 없이 Initialize에서 텍스트를 한 번 생성한다.
적 행동 예고의 피해 수치는 현재 약화와 플레이어의 취약을 반영하며, HUD에도 적 디버프를 표시한다.

Unity 배치 실행:

```text
Unity.exe -batchmode -projectPath "<GameUIUXLab 경로>" -executeMethod DebuffValidation.ConfigureAndRun -logFile "<로그 경로>"
```

샘플 에셋 생성 후 디버프 모델 검증과 기존 전투 씬 검증을 함께 실행한다.
헤드리스 실행에는 -nographics를 추가한다. 검증 종료 전 -quit은 사용하지 않는다.
