# 덱빌딩 로그라이크 — 시스템 구조

## 1. 전체 시스템 구조

```text
Game
│
├── RunManager                 [구현]
│   └── MasterDeck             [구현]
│       └── CardInstance       [구현]
│           └── CardData       [구현]
│               └── CardEffect [구현]
│
├── CardManager                [부분 구현]
│   ├── BattleDeck             [부분 구현]
│   │   ├── DrawPile           [구현]
│   │   ├── Hand               [예정]
│   │   └── DiscardPile        [예정]
│   │
│   └── PlayerHand             [UI 구현]
│       └── CardUI             [UI 구현]
│
└── UIManager                  [구현]
    └── PlayHUD                [구현]
```

※ 위 구조는 시스템의 논리적 관계를 표현하며, 실제 C# 상속 관계나 GameObject 계층 구조는 아님.

---

## 2. 카드 데이터 구조

```text
CardData (ScriptableObject)
│
├── CardID
├── CardName
├── CardType
├── EnergyCost
│
└── Effects : List<CardEffect>
    │
    ├── DamageEffect
    └── BlockEffect
```

```text
CardInstance (C# Class)
│
├── CardData
└── CurrentCost
```

카드의 기본 정보와 실제 카드 인스턴스를 분리한다.

---

## 3. 덱 시스템 구조

```text
StarterDeckData (ScriptableObject)
│
└── StartingCards
    ├── CardData
    └── Count

          │
          ▼

RunManager
│
└── MasterDeck
    │
    └── Cards : List<CardInstance>
          │
          │ 전투 시작 시 복제
          ▼
      BattleDeck
      │
      ├── DrawPile
      ├── Hand
      └── DiscardPile
```

### MasterDeck
- 플레이어가 보유한 전체 카드 관리
- 게임 진행 중 유지
- 카드 획득 및 영구 강화 상태 보관 (강화는 예정)

### BattleDeck
- 전투 시작 시 MasterDeck을 기준으로 생성
- 드로우 더미, 손패, 버림 더미 관리
- 전투 종료 시 폐기

---

## 4. 카드 UI 구조

```text
CardManager
│
├── BattleDeck
│   └── Hand : List<CardInstance>
│
└── PlayerHand
    │
    └── CardPool : List<CardUI>
        │
        ├── 카드 표시
        ├── 카드 클릭
        ├── 카드 Hover
        └── 카드 배치
```

**BattleDeck은 실제 카드 데이터를 관리하고 PlayerHand는 화면 표시를 담당한다.**

---

## 5. 게임 실행 흐름

```text
게임 시작
   │
   ▼
RunManager.StartNewRun()
   │
   ▼
MasterDeck 생성
   │
   ▼
전투 시작
   │
   ▼
BattleDeck 생성
   │
   ▼
DrawPile 셔플           [예정]
   │
   ▼
턴 시작
   │
   ▼
카드 드로우             [예정]
   │
   ▼
PlayerHand UI 표시
   │
   ▼
카드 사용               [부분 구현]
   │
   ▼
턴 종료
   │
   ▼
손패 버리기             [부분 구현]
   │
   ▼
적 행동                 [예정]
   │
   ▼
다음 턴 또는 전투 종료
   │
   ▼
보상 획득               [예정]
   │
   ▼
MasterDeck 갱신         [예정]
   │
   ▼
다음 전투
```

---

## 6. 현재 개발 원칙

1. 카드 원본 데이터는 ScriptableObject로 정의한다.
2. 개별 카드 상태는 CardInstance로 관리한다.
3. MasterDeck은 게임 진행 중 유지한다.
4. BattleDeck은 전투마다 새로 생성한다.
5. BattleDeck이 실제 카드의 위치를 관리한다.
6. CardManager는 카드 시스템의 흐름을 조정한다.
7. PlayerHand는 UI만 담당한다.
8. 카드 효과는 CardEffect의 하위 클래스로 확장한다.

---

## 7. 향후 설계 항목

- 카드 강화 시스템
- 카드 효과 실행과 대상 선택
- 에너지 시스템
- 턴 관리 시스템
- 상태이상 시스템
- 적 행동 및 전투 시스템
- 카드 보상과 덱 편집
- 전투 종료 후 다음 전투로의 전환