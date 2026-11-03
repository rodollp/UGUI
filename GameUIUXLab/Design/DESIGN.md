### 내가 원하는 덱 빌딩 로그라이크 게임

- 1. 슬더스같은 카드 구성. 공격,스킬,파워 종류의 카드 (대신 파워 카드는 보류)
- 2. 게임 사이클은 슬더스와 동일.
- 3. 드로우 시스템과 에너지 시스템도 동일.
- 4. 전반적인 모든것은 슬더스와 동일하게 제작하고자 하는중.

### 시스템 구조

BattleDeck (실제 카드 데이터 관리)
│
├── DrawPile     : List<CardInstance>
├── Hand         : List<CardInstance>
└── DiscardPile  : List<CardInstance>


CardManager (시스템 연결 및 제어)
│
└── BattleDeck ↔ PlayerHand


PlayerHand (UI 관리)
│
└── CardPool     : List<CardUI>