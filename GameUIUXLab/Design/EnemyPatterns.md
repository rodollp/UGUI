# 몬스터 패턴 변경 확인

## 확률 행동 패턴 (2026-11-13)

EnemyData Inspector에서 `Pattern Mode`를 설정한다.

- `Sequential`: 기존처럼 Actions를 순서대로 반복한다. 기존 에셋의 기본 모드다.
- `WeightedRandom`: 조건을 만족하는 Actions 중 가중치로 다음 예고를 뽑는다.
- `Opening Action`: 0은 일반 선택, 1 이상은 첫 턴에 고정할 Actions의 번호다(1부터 시작).
  첫 행동 고정은 가중치·체력 조건을 무시한다. 범위가 잘못되거나 해당 항목이 null이면 일반 선택한다.

각 Actions 항목의 설정:

| 필드 | 의미 |
|---|---|
| Weight | 선택 가중치. 0이면 일반 추첨에서 제외 |
| Max Consecutive Uses | 같은 항목의 연속 선택 상한. 0은 무제한 |
| Min / Max Hp Percent | 적 자신의 현재 체력 비율 조건. 양 끝 포함 |
| Type / Amount / Hits | 기존 행동 종류·수치·타격 횟수 |

가중치는 고정 확률이 아니다. 현재 선택 가능한 항목의 가중치 합으로 나눈다.
예를 들어 60/25/15이면 기본적으로 60%/25%/15%지만, 첫 항목이 연속 제한에 걸리면
남은 두 항목은 62.5%/37.5%가 된다. 제한은 행동 종류가 아니라 배열의 동일 항목에 적용한다.
조건을 만족하는 항목이 전혀 없으면 기본 공격 6을 예고하고 실행한다(Current Pattern Step = 0).
기본 공격에도 힘·약화·취약 등의 전투 보정은 적용된다.

확률 선택은 `PlanNextAction()`에서 한 번만 수행한다. `ExecuteIntent()`는 예고한 행동을 그대로 실행한다.
개별 적이 이력을 보유하므로 같은 EnemyData를 쓰는 적끼리 연속 사용 이력이 섞이지 않는다.

### 적용한 샘플

| 적 | 첫 행동 | 이후 가중치와 제한 |
|---|---|---|
| 고블린 | 공격 6 | 공격 6: 60(최대 2연속), 공격 3×2: 25(연속 금지), 약화 1턴: 15(연속 금지) |
| 수호병 | 방어 7 | 방어 7: 35(연속 금지), 공격 8: 65(최대 2연속) |
| 우두머리 | 힘 +2 | 힘 +2: 15(연속 금지), 공격 5×2: 40(최대 2연속), 방어 10: 25(연속 금지), 공격 12: 20(체력 50% 이하, 연속 금지) |

기존 체력과 기존 행동의 피해 수치는 유지했다. 실제 난이도 균형은 플레이하면서 조정한다.
`Tools → Battle → Validate Random Patterns`로 가중 분포·연속 제한·체력 경계·첫 행동·예고 실행을 검증한다.
배치 실행: `-executeMethod RandomPatternValidation.Run` (기존 순차 패턴·전투 검증도 이어서 실행).

아래 순차 반복 예시는 `Sequential` 모드에 해당한다.

## 실제 사용하는 데이터

현재 전투는 아래 경로로 적을 생성한다.

RunManager.Stages → StageData.Encounters → EncounterData.Enemies → Data (EnemyData)

현재 1층은 Assets/Datas/BattleSamples/Goblin.asset을 사용한다.
Assets/Datas/Enemy/No.asset은 Normal 프리팹의 기본값이며 전투 생성 시
EncounterData의 Data로 교체된다. No.asset을 수정해도 Goblin.asset의 패턴은 바뀌지 않는다.

## 확인 순서

1. 플레이 모드를 종료한다.
2. 해당 층의 EncounterData를 선택하고 Enemies의 Data를 클릭한다.
3. 그 EnemyData의 Actions 배열을 편집하고 프로젝트를 저장한다.
4. 다시 START를 눌러 전투를 시작한다.
5. Console 검색창에 [EnemyPattern]을 입력하고 Collapse를 해제한다.

Console에는 최종 EnemyData 에셋 경로, 패턴 수, 예고/실행 단계와 행동이 출력된다.
예: 단계 1 약화 1 → 단계 2 방어 9 → 단계 3 공격 7 × 2 → 다시 단계 1.
플레이 중 적 인스턴스를 선택하면 Inspector의 Current Pattern Step에서도 현재 단계를 볼 수 있다.
기존에 예고한 행동은 다음 적 턴에 실행되고, 그 뒤 다음 행동이 예고된다.
Actions가 비어 있으면 기본 공격 6을 사용하고 경고를 출력한다.
로그는 Unity 에디터에서만 출력하며 빌드에는 포함하지 않는다.

## 샘플 재구성

Tools → Battle → Configure Sample Battles는 기존 EnemyData의 체력과 Actions를 보존한다.
이전 버전은 이 메뉴를 실행할 때 EnemyData를 기본 패턴으로 덮어쓰던 문제가 있었다.
새 EnemyData 샘플이 없는 경우에만 기본값을 생성한다.
다른 씬/UI/스테이지 연결은 이 메뉴의 원래 재구성 동작을 따른다.

## 자동 검증

-executeMethod EnemyPatternValidation.Run으로 디버프 → 방어 → 다중 공격을
3회 반복하여 예고와 실행 결과를 확인하고, 샘플 재구성이 편집된 데이터와 패턴을
보존하는지 검증한다. 이후 기존 전투·디버프 검증도 실행한다.
