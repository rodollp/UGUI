# Unity UGUI 카드 게임 UI/UX 프로젝트

Unity UGUI를 활용하여 제작한 카드 게임 형태의 UI/UX 실습 프로젝트입니다.

카드 손패 관리, 화면 전환, HUD, 일시정지, 토스트 메시지 등
게임에서 사용되는 기본적인 UI 시스템 구현을 목표로 제작했습니다.

## 주요 기능

### 화면 전환
- Title
- Play
- Pause
- Result
- Refresh
- 화면 상태에 따른 `Time.timeScale` 제어

### 카드 시스템
- Object Pooling을 이용한 카드 UI 관리
- 전투 시작 시 카드 드로우
- 턴 종료 시 손패 전체 버리기
- 버림 더미를 이용한 드로우 더미 재구성
- 카드 사용 시 해당 카드 제거
- 카드 수에 따른 손패 자동 정렬

### 카드 UI
- 마우스 Hover 시 카드 위치 상승
- Hover 시 카드 확대
- 카드 클릭 이벤트 처리
- 이벤트를 이용한 `CardUI → PlayerHand → CardManager` 연결

### HUD
- 플레이 시간 표시
- 턴 수 표시
- HP Slider
- HP가 0이 되면 Result 화면으로 전환

### Toast Message
- Object Pooling 방식으로 Toast UI 관리
- 카드 사용 시 Toast Message 출력

### 입력
- Unity Input System 사용
- ESC 키를 이용한 Pause 
- UI 버튼을 이용한 화면 전환

## 주요 스크립트

- `CardManager` : 카드 드로우, 사용, 버림 더미 관리 //구현 중
- `CardUI` : 카드 클릭 및 Hover 처리
- `PlayerHand` : 카드 풀링 및 손패 배치
- `UIScreenFlowController` : UI 화면 상태 전환
- `PlayHUD` : HP, 턴, 플레이 시간 표시 //아직 직접 대입중
- `RunTimer` : 플레이 시간 관리
- `UIManager` : Toast Message 풀링 및 출력
- `PauseInputController` : ESC 입력 처리 // 캔슬이나 버튼을 누르는 형식 미구현

## 프로젝트 목적

Unity UI/UX 구조를 학습하고,
카드 게임에서 사용할 수 있는 기본적인 화면 전환과
카드 인터랙션 시스템을 구현하기 위해 제작했습니다.