# 전투·카드 코드 리팩토링

기준 저장소: `rodollp/UGUI` / 브랜치: `codex/slay-style-battle-20261111`

## 목적과 범위

기존 조작, 카드 수치, 보상, 몬스터 행동 규칙을 유지하면서 최근 추가된 카드 대상 선택과
확률 패턴의 책임을 분리했다. 이번 범위는 카드 사용·적 행동 선택·전투 준비/정리다.
맵과 UI 전체를 새 구조로 교체하는 작업은 포함하지 않는다.

## 변경한 역할

| 코드 | 역할 |
|---|---|
| CardTargetResolver | 지정·무작위·전체 대상 선택과 생존/소속 검증 |
| CardManager | 입력 연결, 손패 소유권 확인, 비용 지불, 효과 실행 순서 |
| EnemyPatternState | 적 한 개체의 순차 인덱스, 첫 행동, 연속 사용 이력 |
| EnemyPatternSelector | 체력과 연속 제한을 만족하는 후보의 가중 추첨 |
| Enemy | 상태 초기화, 행동 예고 표시 및 실제 행동 실행 |
| BattleManager | 전투와 턴의 전체 진행, 준비·보상·정리 메소드 호출 |

## 카드 사용 흐름

```text
CardManager.UseCard()
  → 손패·턴 조건 검사
  → CardTargetResolver.TryResolve()
  → 에너지 확인 및 지불
  → 손패 데이터와 UI에서 카드 제거
  → ExecuteEffects()
  → 모든 효과 처리 후 승패 판정
```

무작위 선택에는 테스트에서 난수 선택 함수를 전달할 수 있다.
실제 플레이에서는 기존처럼 Unity의 균등 난수 선택을 사용한다.
알 수 없는 대상 종류는 유효하지 않은 설정으로 거절한다.

## 적 행동 흐름

```text
Enemy.Initialize() → EnemyPatternState.Reset()
Enemy.PlanNextAction()
  → EnemyPatternState.Next()
      → 순차 선택 또는 EnemyPatternSelector의 가중 선택
  → Intent 저장 및 예고 알림
Enemy.ExecuteIntent() → 저장된 Intent 실행
```

각 적이 별도 상태 객체를 가지며 공유 EnemyData에는 진행 이력을 저장하지 않는다.
기존 CurrentPatternStep 표시와 기본 공격 폴백 규칙도 유지한다.

## 전투 준비와 정리

- StartBattle: 기존 검증 → PreparePlayer → SpawnEnemies → 덱 준비 → 첫 턴
- FinishBattle: 상태 확정 → 덱 정리 → 승리 시 GrantVictoryRewards → 결과 화면
- AbortBattle: 코루틴 정지 → 전투 상태 정리 → ReleasePlayer → ReleaseEnemies → UI 알림

이벤트 구독과 해제는 기존 순서와 대상을 유지한다.
씬/프리팹에서 참조하는 클래스·직렬화 필드·버튼 메소드와 기존 GUID는 유지한다.

## 검증

`-executeMethod RefactorValidation.Run`

대상 선택 경계 조건과 적별 행동 이력을 검증한 뒤 기존의 확률 패턴·순차 패턴·
무작위/전체 피해·드래그 취소·맵·강화·보상·전투 검증을 실행한다.
Unity가 열려 있는 실제 프로젝트 대신 실제 저장소 소스와 일치하는 검증 사본에서 실행한다.
