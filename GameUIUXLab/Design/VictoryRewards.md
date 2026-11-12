# 전투 승리 보상

## 흐름

1. 몬스터 한 마리를 처치했을 때는 보상을 지급하지 않는다.
2. 모든 몬스터를 처치해 전투 승리가 확정되면 골드를 한 번 지급한다.
3. 결과 화면에 서로 다른 카드 3장을 표시한다.
4. 한 장을 클릭하면 새 CardInstance를 만들어 회차 MasterDeck에 추가한다.
5. 맵 모드에서는 선택 완료 후 '맵으로' 버튼으로 복귀하고 다음 노드를 클릭한다.
   다음 전투부터 획득한 카드가 드로우될 수 있다. StageData 호환 모드만 다음 층으로 직접 이동한다.

마지막 층에서도 골드와 카드 보상을 제공한다. 맵 모드에서는 복귀 후 탐험 완료를 표시한다.
패배에는 보상이 없으며, 보상 화면을 다시 열거나 버튼을 연타해도 중복 지급되지 않는다.
미선택 상태에서 타이틀로 돌아가면 카드 보상은 취소된다.
골드는 회차 안에서 유지되며 새 회차 시작 시 0으로 초기화한다. 디스크 저장은 아직 없다.

## 설정

Create → Battle → Rewards로 BattleRewardData를 만든다.

- Min Gold / Max Gold: 승리 시 지급할 골드의 최소·최대값. 양 끝값을 포함한다.
- Card Pool: 보상 후보 카드. null과 중복 에셋은 제외하며 서로 다른 카드가 3장 이상 필요하다.

EncounterData.Rewards에 지정하면 해당 전투의 설정을 사용한다.
비어 있으면 BattleManager.Default Rewards를 사용한다.
설정이 누락되거나 유효한 카드가 3장 미만이면 오류를 출력하고 전투 시작을 막는다.

후보가 4장 이상이면 매 승리마다 3장을 중복 없이 무작위로 고른다.
동일한 CardData가 회차 덱에 여러 장 들어가는 것은 허용한다.

## 연결된 샘플

Assets/Datas/Rewards/DefaultBattleRewards.asset:

- 골드 10~20
- 강타: 에너지 2, 피해 8 + 취약 2
- 위협: 에너지 1, 약화 2
- 철벽: 에너지 1, 방어도 8

샘플 풀에는 3종이므로 매번 같은 3종을 무작위 순서로 보여준다.
Card Pool에 카드를 추가하면 선택지 조합도 달라진다.
기존 시작 덱과 몬스터 패턴은 변경하지 않는다.
Tools → Battle → Configure Victory Rewards로 샘플과 결과 UI 연결을 다시 구성할 수 있다.
기존 샘플 보상 데이터와 카드 에셋의 편집 값은 보존한다.

## 코드 역할

- RunManager.Gold / AddGold: 회차 골드 관리, 정수 오버플로 방지.
- BattleRewardData: 지급 범위와 카드 풀.
- BattleRewardOffer: 이번 승리의 고정된 골드·3장 선택지와 수령 여부.
- BattleManager.TrySelectRewardCard: 승리·미수령 상태를 검사하고 한 장만 덱에 추가.
- BattleRewardUI: 기존 CardUI 프리팹을 재사용하는 클릭 선택 화면.
- CardUI.SetRewardMode: 보상 카드의 드래그를 막고 클릭 선택을 사용.

## 검증

```text
Unity.exe -batchmode -projectPath "<GameUIUXLab 경로>" -executeMethod RewardSetup.ConfigureAndRun -logFile "<로그 경로>"
```

보상 모델, 실제 UI 클릭, 중복 방지, 개별 처치·패배 시 미지급,
다음 층 이동 차단·해제, 회차 덱 유지, 마지막 층 보상을 기존 전투 검증과 함께 검사한다.
검증 완료 시 종료하므로 -quit은 지정하지 않는다.
