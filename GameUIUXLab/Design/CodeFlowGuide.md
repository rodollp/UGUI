# 덱빌딩 로그라이크 코드 흐름 안내

이 문서는 현재 `CardGame.unity`와 연결된 코드의 시작 지점과 전체 실행 흐름을 설명합니다.
작성 기준: 2026-11-13. 이후 코드가 변경되면 실제 코드와 함께 확인하세요.

## 1. 전체 흐름 먼저 보기

```text
Unity에서 게임 씬 실행
  → Awake / OnEnable: 초기화 및 이벤트 연결
  → UIScreenFlowController.Start(): 타이틀 표시
  → 시작 버튼 클릭
  → AdventureController.BeginRun()
  → RunManager.StartNewRun(): 플레이어·보유 덱·골드·맵 초기화
  → 맵 화면
  → 노드 선택: AdventureController.SelectNode(id)
      ├─ 전투 노드
      │   → BattleManager.StartBattle()
      │   → 플레이어 턴: 카드 드로우 및 사용
      │   → 턴 종료: 적 행동
      │   → 다음 플레이어 턴으로 반복
      │   → 승리 또는 패배
      │   → 결과 화면
      │   → 승리 시 보상 카드 선택 → 맵 복귀
      └─ 사건·휴식·보물 노드
          → 사건 화면
          → 선택 결과 또는 카드 강화 적용
          → 맵 복귀
```

런(Run)은 새 게임을 시작해서 클리어하거나 패배할 때까지의 한 판을 뜻합니다.
맵 데이터가 없는 구성에서는 맵을 거치지 않고 바로 전투를 시작합니다.

## 2. 핵심 클래스의 역할

| 클래스 | 담당하는 일 |
|---|---|
| `AdventureController` | 새 런 시작, 맵 노드 선택, 전투·사건 연결 |
| `RunManager` | 런 동안 유지할 체력 상태, 보유 덱, 골드, 맵, 진행도 보관 |
| `BattleManager` | 전투 준비, 플레이어·적 턴 진행, 승패 및 보상 처리 |
| `CardManager` | 전투 덱 초기화, 드로우, 카드 사용 검증 및 효과 실행 |
| `BattleDeck` | 드로우 더미·손패·버림 더미의 카드 데이터 관리 |
| `PlayerHand` | 카드 UI 풀과 손패 배치 관리 |
| `UIScreenFlowController` | 화면 전환, 일시정지, 타이머 제어 |
| `PlayHUD` | 턴·시간·에너지·골드·적 행동 예고 등 표시 |

## 3. Unity 실행 직후 호출되는 메소드

Unity가 자동으로 부르는 메소드와 코드 또는 버튼이 직접 부르는 메소드를 구분해야 합니다.

| 메소드 | 호출 시점 | 현재 프로젝트의 용도 |
|---|---|---|
| `Awake()` | 컴포넌트 초기화 시 | 풀 생성, 입력 준비, 이벤트 연결 |
| `OnEnable()` | 오브젝트가 활성화될 때마다 | UI 갱신 이벤트 구독 |
| `Start()` | 활성화된 컴포넌트의 첫 업데이트 전 한 번 | 타이틀 표시 |
| `Update()` | 활성화된 동안 매 프레임 | 시간 표시 등 지속적인 갱신 |
| `OnDisable()` | 비활성화될 때 | 이벤트 구독 해제 |
| `OnDestroy()` | 오브젝트 파괴 시 | 이벤트 해제 및 정리 |

서로 다른 오브젝트의 `Awake()` 실행 순서가 고정되어 있다고 가정하지 마세요.
비활성 오브젝트는 초기화 및 활성화 시점에 따라 호출 시점도 달라집니다.

### 주요 준비 작업

- `PlayerHand.Awake()` → `CreateCardPool()`: 카드 UI를 미리 만들어 비활성 상태로 보관합니다.
- `CardManager.Awake()`: `PlayerHand.OnCardDropped`에 `HandleCardDropped()`를 연결합니다.
- `PlayHUD.OnEnable()`: 전투·골드 변경 이벤트를 구독하고 UI를 갱신합니다.
- `UIScreenFlowController.Start()` → `ShowTitleScreen()`: 타이틀을 표시합니다.

실제 타이틀 진입 코드는 다음과 같습니다.

```csharp
private void Start() => ShowTitleScreen();
```

`ShowTitleScreen()`은 기존 전투를 정리하고 타이틀을 활성화하며, 타이머를 멈춥니다.

코드: [UIScreenFlowController.cs](../Assets/Script/UI/UIScreenFlowController.cs)

## 4. 시작 버튼과 새 런 생성

`CardGame.unity`의 시작 버튼에는 `AdventureController.BeginRun()`이 연결되어 있습니다.
이 메소드는 Unity의 자동 생명주기 메소드가 아니라 버튼 클릭으로 호출됩니다.

```text
BeginRun()
  → battleManager.AbortBattle(): 기존 전투 정리
  → ResetEvent(): 기존 사건 상태 정리
  → runManager.StartNewRun(): 새 런 데이터 생성
  → RunVersion 변경 여부 확인
  → 맵이 있으면 ShowMapForNewRun()
  → 맵이 없으면 ShowPlayScreen() → StartBattle()
```

### StartNewRun()이 초기화하는 데이터

| 대상 | 처리 내용 |
|---|---|
| `MasterDeck` | 시작 덱 데이터로 이번 런의 보유 덱 생성 |
| `PlayerStatus` | 최대 체력에 맞춘 새 플레이어 상태 생성 |
| `Gold` | 0으로 초기화 |
| `StageIndex` | 0으로 초기화 |
| `Map` | 맵 데이터가 있다면 시드에 따라 생성 |
| `RunVersion` | 새 런 생성 성공 시 증가 |

플레이어 데이터나 시작 덱, 맵 설정이 유효하지 않으면 새 런 생성을 중단합니다.
`BeginRun()`은 `RunVersion`이 증가했는지 확인하여 실패한 상태에서 다음 화면으로 넘어가지 않게 합니다.

코드: [AdventureController.cs](../Assets/Script/Map/AdventureController.cs), [RunManager.cs](../Assets/Script/RunTime/RunManager.cs)

## 5. 맵 노드 선택

```text
AdventureController.SelectNode(id)
  → RunManager.TryEnterMapNode(id)
      → 이동 가능 여부 검증 및 현재 노드 확정
      → 현재 층과 전투 데이터 설정
  → 전투 노드인가?
      ├─ 예: ShowPlayScreen() → StartBattle()
      └─ 아니오: 사건 데이터 설정 → ShowEventScreen()
```

`RunManager`는 진행 데이터를 검증하고 변경합니다.
`AdventureController`는 그 결과에 따라 화면과 기능을 연결합니다.
전투 시작에 실패하면 활성 노드 진입을 취소하고 맵으로 돌아갑니다.

코드: [AdventureController.cs](../Assets/Script/Map/AdventureController.cs)

## 6. 전투 시작

`BattleManager.StartBattle()`이 전투의 실질적인 시작 지점입니다.

```text
StartBattle()
  → 진행 중인 전투·미선택 보상 등이 있는지 확인
  → 플레이어·덱·적 조합·보상 데이터 검증
  → AbortBattle(): 이전 전투 정리
  → State = Preparing
  → 플레이어의 전투용 상태 초기화 및 이벤트 연결
  → 적 프리팹 생성 → Initialize()
  → 적 사망·상태 변경 이벤트 연결
  → 적 PlanNextAction(): 첫 행동 예고
  → CardManager.InitializeBattle(this)
  → Turn = 0
  → BeginPlayerTurn()
```

### CardManager.InitializeBattle()

1. 사용할 전투 관리자를 저장합니다.
2. `MasterDeck`을 바탕으로 `BattleDeck`을 만듭니다.
3. 전투 덱을 섞습니다.
4. 손패 UI와 HUD를 정리합니다.

### BattleManager.BeginPlayerTurn()

1. 전투가 이미 끝났는지 확인합니다.
2. 턴 수를 증가시킵니다.
3. 플레이어 방어도를 초기화합니다.
4. `PlayerStatus.StartTurn()`으로 에너지를 회복합니다.
5. 상태를 `PlayerTurn`으로 변경합니다.
6. `CardManager.BeginPlayerTurn()`으로 카드를 뽑습니다.
7. 변경 이벤트를 발생시켜 UI를 갱신합니다.

드로우 기본값은 5장이며 `drawCount`를 Inspector에서 변경할 수 있습니다.

코드: [BattleManager.cs](../Assets/Script/Battle/BattleManager.cs), [CardManager.cs](../Assets/Script/Card/CardManager.cs)

## 7. 세 가지 덱·손패 개념 구분

| 구성 | 의미 | 유지 범위 |
|---|---|---|
| `MasterDeck` | 이번 런에서 보유한 카드 목록 | 런 전체 |
| `BattleDeck` | 전투 중 드로우 더미·손패·버림 더미 | 전투 하나 |
| `PlayerHand` | 손패 카드를 화면에 보여주는 UI | 표시 및 배치 |

`BattleDeck`은 `MasterDeck`의 카드를 `CopyForBattle()`로 복사하여 사용합니다.
보상 획득과 영구 강화는 `MasterDeck` 쪽에 적용되어 이후 전투에 반영됩니다.

```text
드로우 더미 → Draw() → 손패
손패 → 카드 사용 또는 턴 종료 → 버림 더미
드로우 더미가 비면 → 버림 더미를 옮기고 섞기
```

파워 카드는 사용하면 이번 전투의 순환 더미에서 빠집니다.
`BattleDeck.Draw()`는 손패가 10장이면 추가 드로우를 중단합니다.

코드: [MasterDeck.cs](../Assets/CardDeck/MasterDeck.cs), [BattleDeck.cs](../Assets/Script/Card/BattleDeck.cs), [PlayerHand.cs](../Assets/Script/PlayerHand.cs)

## 8. 카드 사용 흐름

```text
CardUI에서 드롭 이벤트 발생
  → PlayerHand.HandleCardDropped()
  → PlayerHand.OnCardDropped 이벤트
  → CardManager.HandleCardDropped()
  → 사용 위치 또는 적 대상 확인
  → CardManager.UseCard()
```

대상이 없는 카드는 화면의 지정 높이 이상에 놓았는지 확인합니다.
적 대상 카드는 `TargetSelector`로 놓은 위치의 적을 찾습니다.

### UseCard() 내부 순서

1. 다른 카드 효과를 처리 중인지, 현재 카드를 사용할 수 있는 턴인지 확인합니다.
2. 실제 보유하고 있는 손패 카드인지 확인합니다.
3. 적 대상이 필요하다면 유효한 적인지 확인합니다.
4. 에너지를 확인하고 차감합니다.
5. 전투 손패에서 카드를 제거하고 해당 UI를 숨깁니다.
6. `CardEffectContext`에 사용자와 대상을 담습니다.
7. 카드에 설정된 효과들의 `Execute(context)`를 순서대로 호출합니다.
8. 효과 처리가 끝나면 승패를 확인합니다.

`DamageEffect`, `BlockEffect`, `HealEffect`, `ApplyDebuffEffect` 등이 실제 효과를 처리합니다.
`CardManager`는 사용 조건과 절차를 담당하고, 효과 클래스는 실제 결과를 적용합니다.

`IsResolvingCard`는 카드 효과 처리 중이라는 표시입니다.
중복 사용을 방지하고, 여러 효과가 있는 카드의 효과를 모두 처리한 뒤 승리를 판정하도록 돕습니다.
`finally` 블록에서 이 표시를 해제하고 `CheckBattleEnd()`를 호출합니다.

코드: [CardManager.cs](../Assets/Script/Card/CardManager.cs), [효과 클래스 폴더](../Assets/Datas/Effects/)

## 9. 턴 종료와 적 행동

```text
턴 종료 버튼
  → CardManager.EndTurn()
  → BattleManager.EndPlayerTurn()
  → State = EnemyTurn
  → 남은 손패 모두 버리기
  → EnemyTurn() 코루틴 시작
      → 살아 있는 적마다 일정 시간 대기
      → Enemy.ExecuteIntent(playerStatus)
      → 전투가 끝났으면 중단
      → 라운드 종료 디버프 처리
      → 적 PlanNextAction(): 다음 행동 예고
  → BeginPlayerTurn()
```

코루틴은 적의 행동 사이에 대기 시간을 두면서 순서대로 보여주기 위해 사용합니다.
상태를 코루틴 시작 전에 `EnemyTurn`으로 바꾸므로 턴 종료 버튼 연타도 차단합니다.

### 적의 예고와 실행

- `PlanNextAction()`: 적 데이터의 행동 목록에서 다음 행동을 정합니다.
- `ExecuteIntent()`: 예고해 둔 공격·방어·공격력 증가·디버프 행동을 실행합니다.

적의 방어도는 해당 적의 행동 시작에 초기화되고, 플레이어 방어도는 플레이어 턴 시작에 초기화됩니다.

코드: [BattleManager.cs](../Assets/Script/Battle/BattleManager.cs), [Enemy.cs](../Assets/Script/Enemy/Enemy.cs)

## 10. 전투 종료와 보상

```text
적 전멸 확인 또는 플레이어 사망
  → FinishBattle(won)
  → State = Victory 또는 Defeat
  → CardManager.EndBattle(): 손패·전투 덱 정리
  → 승리한 경우
      → 보상 생성
      → 골드 지급
      → RunManager.CompleteStage(): 현재 노드 완료
  → 변경 및 전투 종료 이벤트 발생
  → ShowResultScreen()
```

플레이어 사망은 `OnDead` 이벤트를 통해 패배 처리로 연결됩니다.
적 사망 시에는 카드 효과 처리 중인지 확인하고 승패를 판정합니다.

### 카드 보상과 맵 복귀

```text
보상 카드 선택
  → BattleManager.TrySelectRewardCard(index)
  → 선택한 카드를 MasterDeck에 추가
  → 다음 이동 버튼 사용 가능
  → StartNextBattle()
      ├─ 맵 구성: AdventureController.ReturnToMap()
      │           → 전투 정리 → 맵 표시
      └─ 순차 스테이지 구성: 다음 전투 시작
```

미선택 카드 보상이 남아 있으면 다음 전투나 맵 복귀를 막습니다.
런 완료 여부는 `RunManager.IsRunComplete`로 확인합니다.

코드: [BattleManager.cs](../Assets/Script/Battle/BattleManager.cs), [BattleRewardUI.cs](../Assets/Script/UI/BattleRewardUI.cs)

## 11. 사건·휴식·보물·카드 강화

```text
ChooseEvent(index)
  → 선택 가능한 상태인지, 비용을 낼 수 있는지 확인
      ├─ 일반 선택 → ResolveChoice()
      └─ 카드 강화 → 강화 대상 선택 화면
                     → ConfirmUpgrade(card)
                     → ResolveChoice()
  → 골드·체력 비용 지불
  → 강화·회복·골드·카드 획득 적용
  → CompleteStage(): 노드 완료
  → 결과 표시
  → ReturnFromEvent(): 맵 복귀
```

비용으로 플레이어가 사망하지 않도록 현재 체력 이상을 요구하는 선택은 막습니다.
강화는 보유 덱에 실제로 존재하며 강화 가능한 카드인지 다시 확인합니다.
이 결과들은 `RunManager`가 보관하는 데이터에 적용되어 이후 전투에도 유지됩니다.

코드: [AdventureController.cs](../Assets/Script/Map/AdventureController.cs)

## 12. 화면 전환과 일시정지

`UIScreenFlowController.SetActiveScreen()`이 현재 상태에 맞는 화면을 켜고 나머지를 끕니다.
월드의 플레이어와 적은 전투 화면 또는 전투 중 일시정지 화면에서 표시됩니다.

| 메소드 | 동작 |
|---|---|
| `ShowTitleScreen()` | 전투 정리, 타이틀 표시, 타이머 정지 |
| `ShowMapForNewRun()` | 맵 표시, 타이머 새로 시작 |
| `ShowMapScreen()` | 맵 표시, 타이머 재개 |
| `ShowPlayScreen()` | 전투 표시, 타이머 시작 또는 재개 |
| `ShowEventScreen()` | 사건 표시, 타이머 재개 |
| `ShowPauseScreen()` | 직전 화면 기록, 일시정지 표시, `Time.timeScale = 0` |
| `ResumeGame()` | 직전 화면 복구, `Time.timeScale = 1`, 타이머 재개 |
| `ShowResultScreen()` | 결과 표시, 타이머 정지 |
| `ShowRefreshScreen()` | 전투 정리, Refresh 화면 표시, 시간과 타이머 정지 |

코드: [UIScreenFlowController.cs](../Assets/Script/UI/UIScreenFlowController.cs)

## 13. 이벤트로 UI가 갱신되는 방식

이벤트는 상태가 바뀌었다는 소식을 구독자에게 전달하는 방식입니다.

```csharp
// UI가 활성화될 때 구독
battleManager.OnBattleChanged += RefreshUI;

// 전투 데이터가 변경되면 알림
OnBattleChanged?.Invoke();

// UI가 비활성화될 때 구독 해제
battleManager.OnBattleChanged -= RefreshUI;
```

위 코드는 서로 다른 위치에 있는 구독·알림·해제의 관계를 설명한 예시입니다.
`+=`는 알림을 받을 메소드를 등록하고, `-=`는 등록을 해제합니다.
`?.Invoke()`는 구독자가 있을 때 이벤트를 호출합니다.

예를 들어 에너지가 바뀌면 전투 관리자가 변경 알림을 전달하고, 이를 구독한 HUD가 현재 값을 다시 읽어 표시합니다.
시간 표시처럼 지속 갱신이 필요한 값은 `Update()`에서 처리합니다.

코드: [PlayHUD.cs](../Assets/Script/UI/PlayHUD.cs)

## 14. 추천 코드 읽기 순서

1. `AdventureController.BeginRun()` — 새 게임의 출발점
2. `RunManager.StartNewRun()` — 한 판의 데이터 생성
3. `AdventureController.SelectNode()` — 맵에서 기능으로 분기
4. `BattleManager.StartBattle()` — 전투 준비
5. `BattleManager.BeginPlayerTurn()` — 턴 시작
6. `CardManager.UseCard()` — 카드 사용
7. `BattleManager.EndPlayerTurn()` / `EnemyTurn()` — 턴 반복
8. `BattleManager.FinishBattle()` — 승패와 보상
9. `AdventureController.ChooseEvent()` — 사건과 강화
10. `UIScreenFlowController` / `PlayHUD` — 화면 표시와 데이터 연결

각 메소드를 읽을 때는 **누가 호출하는지 → 어떤 데이터를 바꾸는지 → 다음에 무엇을 호출하는지**를 확인하세요.

## 15. 실행 설정 참고

현재 확인한 `EditorBuildSettings.asset`에는 활성화된 씬이 없습니다.
Unity 에디터에서 씬을 열어 실행하는 것과 별도로, 실행 파일을 만들 때는 게임 씬을 빌드 목록에 등록해야 합니다.

이 문서는 코드를 읽어 정리한 설명이며, 문서 작성 과정에서 게임 코드를 변경하지 않았습니다.
