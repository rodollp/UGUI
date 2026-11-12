# 몬스터 패턴 변경 확인

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
